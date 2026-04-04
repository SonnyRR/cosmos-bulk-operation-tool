using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using Spectre.Console;
using Spectre.Console.Cli;

using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.CLI.Strategies;

namespace Cosmos.BulkOperation.CLI;

/// <summary>
/// Entry point for the Cosmos Bulk Operation CLI tool.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();

        var rootConfiguration = BuildConfiguration();
        services.AddSingleton<IConfigurationRoot>(rootConfiguration);
        services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);

        var registrar = new TypeRegistrar(services);

        var app = new CommandApp<BulkOperationCommand>(registrar);
        app.Configure(config =>
        {
            config.SetApplicationName("cosmos-bulk-operation-tool");
            config.AddExample("--dry-run");
            config.AddExample();
        });

        return await app.RunAsync(args);
    }

    /// <summary>
    /// Builds the root application configuration.
    /// </summary>
    private static IConfigurationRoot BuildConfiguration() => new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
        .Build();
}

/// <summary>
/// Command class for the bulk operation tool.
/// </summary>
public class BulkOperationCommand : Command<BulkOperationCommand.Settings>
{
    /// <summary>
    /// Command settings.
    /// </summary>
    public class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to run in dry-run mode (no changes applied to Cosmos DB).
        /// </summary>
        [CommandOption("--dry-run")]
        [Description("Dry-run mode, allowing for changes to be scheduled, but not evaluated on the destination Cosmos database. Used for debugging.")]
        public bool DryRun { get; set; }

        /// <summary>
        /// Gets or sets the strategy name to execute directly, bypassing the interactive prompt.
        /// </summary>
        [CommandOption("--strategy")]
        [Description("The name of the strategy to execute directly, bypassing the interactive prompt.")]
        public string? Strategy { get; set; }
    }

    private readonly IConfigurationRoot rootConfiguration;
    private readonly IAnsiConsole console;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationCommand"/> class.
    /// </summary>
    /// <param name="rootConfiguration">The root configuration.</param>
    /// <param name="console">The console interface.</param>
    public BulkOperationCommand(
        IConfigurationRoot rootConfiguration,
        IAnsiConsole console)
    {
        this.rootConfiguration = rootConfiguration ?? throw new ArgumentNullException(nameof(rootConfiguration));
        this.console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        return ExecuteAsync(settings, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes the bulk operation command asynchronously.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task<int> ExecuteAsync(Settings settings, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (s, e) =>
        {
            linkedCts.Cancel();
            e.Cancel = true;
        };

        ConfigureSerilog(rootConfiguration);

        var headerPanel = new Panel(
            new Markup("Cosmos DB Bulk Operation CLI tool\n[grey]Author: Vasil Kotsev | 26/09/2024[/]"))
            .Header("[bold cyan]Cosmos Bulk Operation[/]")
            .BorderColor(Color.Cyan);
        console.Write(headerPanel);

        if (settings.DryRun)
        {
            console.Write(new Rule("[yellow]Dry-run mode enabled[/]").RuleStyle("yellow").Centered());
        }

        Thread.Sleep(500);

        var applicationSettings = GetApplicationSettings(rootConfiguration);
        var chosenStrategy = !string.IsNullOrEmpty(settings.Strategy)
            ? ResolveStrategy(settings.Strategy, applicationSettings)
            : SelectStrategy(applicationSettings);

        if (chosenStrategy != null)
        {
            Thread.Sleep(500);

            console.WriteLine();
            var confirmed = console.Confirm("Are you sure you want to mutate the records?");
            if (confirmed)
            {
                try
                {
                    await chosenStrategy.EvaluateAsync(settings.DryRun, linkedCts.Token);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Terminal error");
                    console.WriteException(ex);
                    return 1;
                }
            }
            else
            {
                Log.Information("User did not approve the mutation of the records. Exiting...");
                console.MarkupLine("[grey]User did not approve the mutation of the records. Exiting...[/]");
            }
        }
        else
        {
            Log.Information("No strategy was provided. Exiting...");
            console.MarkupLine("[grey]No strategy was provided. Exiting...[/]");
        }

        await Log.CloseAndFlushAsync();
        return 0;
    }

    /// <summary>
    /// Configures logging.
    /// </summary>
    /// <param name="settings">The root configuration settings.</param>
    private static void ConfigureSerilog(IConfigurationRoot settings)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(settings)
            .CreateLogger();
    }

    /// <summary>
    /// Resolves a strategy by name from the command-line argument.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to resolve.</param>
    /// <param name="applicationSettings">The application's main settings.</param>
    /// <returns>The strategy instance, or null if not found.</returns>
    private IBulkOperationStrategy ResolveStrategy(string strategyName, ApplicationSettings applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.CosmosSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.ContainerConfigSectionsAndSettings);

        var strategyTypes = Assembly
            .GetEntryAssembly()
            .GetTypes()
            .Where(t => typeof(IBulkOperationStrategy).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && Attribute.IsDefined(t, typeof(SettingsKeyAttribute)))
            .ToList();

        var chosenStrategyType = strategyTypes.FirstOrDefault(t => t.Name == strategyName);
        if (chosenStrategyType == null)
        {
            Log.Error("Strategy {@Strategy} not found", strategyName);
            console.MarkupLine("[red]Strategy '{Strategy}' not found.[/]", strategyName);
            return null;
        }

        var configKey = chosenStrategyType.GetCustomAttribute<SettingsKeyAttribute>();
        if (!applicationSettings.ContainerConfigSectionsAndSettings.TryGetValue(configKey.Name, out var containerSetting))
        {
            Log.Error("Cannot find container settings for configuration key: {@ConfigKey}", configKey.Name);
            console.MarkupLine("[red]Cannot find container settings for configuration key.[/]");
            return null;
        }

        Log.Information("Creating instance of {@Strategy}", chosenStrategyType.Name);
        console.MarkupLine("[green]Selected strategy: {Strategy}[/]", chosenStrategyType.Name);

        return Activator.CreateInstance(chosenStrategyType, applicationSettings.CosmosSettings, containerSetting) as IBulkOperationStrategy;
    }

    /// <summary>
    /// Prompts the user to select which strategy to execute.
    /// </summary>
    /// <param name="applicationSettings">The application's main settings.</param>
    /// <returns>The selected strategy instance, or null if the user exits.</returns>
    private IBulkOperationStrategy SelectStrategy(ApplicationSettings applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.CosmosSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.ContainerConfigSectionsAndSettings);

        var strategyTypes = Assembly
            .GetEntryAssembly()
            .GetTypes()
            .Where(t => typeof(IBulkOperationStrategy).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && Attribute.IsDefined(t, typeof(SettingsKeyAttribute)))
            .OrderBy(t => t.Name)
            .ToList();

        var strategyNames = strategyTypes.ConvertAll(t => t.Name);
        strategyNames.Add("Exit");

        var selectedStrategyName = console.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]strategy[/] to execute:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to see more strategies)[/]")
                .HighlightStyle(new Style(Color.Cyan1, decoration: Decoration.Bold))
                .AddChoices(strategyNames));

        if (selectedStrategyName.Equals("Exit", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var chosenStrategyType = strategyTypes.First(t => t.Name == selectedStrategyName);
        var configKey = chosenStrategyType.GetCustomAttribute<SettingsKeyAttribute>();

        if (!applicationSettings.ContainerConfigSectionsAndSettings.TryGetValue(configKey.Name, out var containerSetting))
        {
            Log.Error("Cannot find container settings for configuration key: {@ConfigKey}", configKey.Name);
            console.MarkupLine("[red]Cannot find container settings for configuration key.[/]");
            return null;
        }

        Log.Information("Creating instance of {@Strategy}", chosenStrategyType.Name);
        console.MarkupLine("[green]Selected strategy: {Strategy}[/]", chosenStrategyType.Name);

        return Activator.CreateInstance(chosenStrategyType, applicationSettings.CosmosSettings, containerSetting) as IBulkOperationStrategy;
    }

    /// <summary>
    /// Retrieves the application's settings.
    /// </summary>
    /// <param name="settings">The configuration root settings.</param>
    /// <returns>An instance of <see cref="ApplicationSettings"/>.</returns>
    private static ApplicationSettings GetApplicationSettings(IConfigurationRoot settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var cosmosSettings = settings.GetRequiredSection(nameof(CosmosSettings)).Get<CosmosSettings>();
        var rootContainerSettingsSection = settings.GetRequiredSection("ContainerSettings");
        var containerSettings = rootContainerSettingsSection
            .GetChildren()
            .Where(s => !s.Key.Contains(':'))
            .ToDictionary(k => k.Key, w => w.Get<ContainerSettings>());

        return new()
        {
            CosmosSettings = cosmosSettings,
            ContainerConfigSectionsAndSettings = containerSettings
        };
    }
}

/// <summary>
/// Type registrar for Spectre.Console.Cli using Microsoft.Extensions.DependencyInjection.
/// </summary>
public class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="builder">The service collection to register types with.</param>
    public TypeRegistrar(IServiceCollection builder) => this.builder = builder;

    /// <inheritdoc />
    public ITypeResolver Build() => new TypeResolver(builder.BuildServiceProvider());

    /// <inheritdoc />
    public void Register(Type service, Type implementation) => builder.AddSingleton(service, implementation);

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation) => builder.AddSingleton(service, implementation);

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        builder.AddSingleton(service, _ => factory());
    }
}

/// <summary>
/// Type resolver for Spectre.Console.Cli using Microsoft.Extensions.DependencyInjection.
/// </summary>
public class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="provider">The service provider to resolve types from.</param>
    public TypeResolver(IServiceProvider provider)
        => this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <inheritdoc />
    public object Resolve(Type type) => provider.GetService(type);
}
