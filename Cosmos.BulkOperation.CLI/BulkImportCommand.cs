using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.CLI.Strategies;

using Microsoft.Extensions.Configuration;

using Serilog;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Cosmos.BulkOperation.CLI;

/// <summary>
/// Command for executing bulk import operations against Cosmos DB containers.
/// </summary>
public class BulkImportCommand : Command<BulkImportCommand.Settings>
{
    /// <summary>
    /// Command settings for the bulk import operation.
    /// </summary>
    public class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets whether to run in dry-run mode.
        /// </summary>
        [CommandOption("--dry-run")]
        [Description("Dry-run mode, allowing for changes to be scheduled, but not evaluated on the destination Cosmos database. Used for debugging.")]
        public bool DryRun { get; set; }

        /// <summary>
        /// Gets or sets the strategy name to execute.
        /// </summary>
        [CommandOption("--strategy")]
        [Description("The name of the strategy to execute directly, bypassing the interactive prompt.")]
        public string Strategy { get; set; }
    }

    private readonly IConfigurationRoot configurationRoot;
    private readonly IAnsiConsole console;
    private readonly IStrategyProvider strategyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkImportCommand"/> class.
    /// </summary>
    /// <param name="configurationRoot">The configuration root.</param>
    /// <param name="console">The Ansi console for output.</param>
    /// <param name="strategyProvider">The strategy provider for bulk operations.</param>
    public BulkImportCommand(
        IConfigurationRoot configurationRoot,
        IAnsiConsole console,
        IStrategyProvider strategyProvider)
    {
        this.configurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
        this.console = console;
        this.strategyProvider = strategyProvider ?? throw new ArgumentNullException(nameof(strategyProvider));
    }

    /// <summary>
    /// Executes the bulk import command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The exit code.</returns>
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
        => this.ExecuteAsync(settings, cancellationToken).GetAwaiter().GetResult();

    private async Task<int> ExecuteAsync(Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Console.CancelKeyPress += (s, e) =>
            {
                linkedCts.Cancel();
                e.Cancel = true;
            };

            var headerPanel = new Panel(
                new Markup("Cosmos DB Bulk Operation CLI tool\n[grey]Author: Vasil Kotsev | 14/04/2026[/]"))
                .Header("[bold cyan]Cosmos Bulk Operation[/]")
                .BorderColor(Color.Cyan);
            this.console.Write(headerPanel);

            if (settings.DryRun)
            {
                this.console.Write(new Rule("[yellow]Dry-run mode enabled[/]").RuleStyle("yellow").Centered());
            }

            Thread.Sleep(500);

            var applicationSettings = ConfigurationProvider.GetApplicationSettings(this.configurationRoot);
            var chosenStrategy = !string.IsNullOrEmpty(settings.Strategy)
                ? this.ResolveStrategy(settings.Strategy, applicationSettings)
                : this.SelectStrategy(applicationSettings);

            if (chosenStrategy != null)
            {
                Thread.Sleep(500);

                this.console.WriteLine();
                var confirmed = await this.console.ConfirmAsync("Are you sure you want to mutate the records?", cancellationToken: linkedCts.Token);
                if (confirmed)
                {
                    try
                    {
                        await chosenStrategy.EvaluateAsync(settings.DryRun, linkedCts.Token);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Terminal error");
                        this.console.WriteException(ex);
                        return 1;
                    }
                }
                else
                {
                    Log.Information("User did not approve the mutation of the records. Exiting...");
                    this.console.MarkupLine("[grey]User did not approve the mutation of the records. Exiting...[/]");
                }
            }
            else
            {
                Log.Error("No valid strategy was provided. Exiting...");
                this.console.MarkupLine("[red]No valid strategy was provided. Exiting...[/]");
                return 1;
            }

            await Log.CloseAndFlushAsync();
            return 0;
        }
        catch (Exception ex)
        {
            this.console.MarkupLine($"[red]Unhandled exception: {ex.Message}[/]");
            this.console.MarkupLine($"[red]Stack trace: {ex.StackTrace}[/]");
            return -1;
        }
    }

    private IBulkOperationStrategy ResolveStrategy(string strategyName, ApplicationSettings applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.CosmosSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.ContainerConfigSectionsAndSettings);

        if (!this.strategyProvider.TryGetConfigKey(strategyName, out var configKey))
        {
            Log.Error("Strategy {@Strategy} not found", strategyName);
            this.console.MarkupLine("[red]Strategy '{0}' not found.[/]", strategyName);
            return null;
        }

        if (!applicationSettings.ContainerConfigSectionsAndSettings.TryGetValue(configKey, out var containerSetting))
        {
            Log.Error("Cannot find container settings for configuration key: {@ConfigKey}", configKey);
            this.console.MarkupLine("[red]Cannot find container settings for configuration key.[/]");
            return null;
        }

        if (!this.strategyProvider.TryGetStrategy(strategyName, applicationSettings.CosmosSettings, containerSetting, out var strategy))
        {
            Log.Error("Strategy {@Strategy} not found", strategyName);
            this.console.MarkupLine("[red]Strategy '{0}' not found.[/]", strategyName);
            return null;
        }

        this.console.MarkupLine("[green]Selected strategy: {0}[/]", strategyName);
        return strategy;
    }

    private IBulkOperationStrategy SelectStrategy(ApplicationSettings applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.CosmosSettings);
        ArgumentNullException.ThrowIfNull(applicationSettings.ContainerConfigSectionsAndSettings);

        var availableStrategies = this.strategyProvider.GetAvailableStrategies().ToList();
        if (availableStrategies.Count == 0)
        {
            Log.Error("No strategies with {Attribute} attribute found", nameof(SettingsKeyAttribute));
            this.console.MarkupLine("[red]No valid strategies found. Check configuration.[/]");
            return null;
        }

        var strategyNames = availableStrategies.Append("Exit").ToList();

        var selectedStrategyName = this.console.Prompt(
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

        if (!this.strategyProvider.TryGetConfigKey(selectedStrategyName, out var configKey))
        {
            Log.Error("Strategy {@Strategy} not found", selectedStrategyName);
            this.console.MarkupLine("[red]Strategy '{0}' not found.[/]", selectedStrategyName);
            return null;
        }

        if (!applicationSettings.ContainerConfigSectionsAndSettings.TryGetValue(configKey, out var containerSetting))
        {
            Log.Error("Cannot find container settings for configuration key: {@ConfigKey}", configKey);
            this.console.MarkupLine("[red]Cannot find container settings for configuration key.[/]");
            return null;
        }

        if (!this.strategyProvider.TryGetStrategy(selectedStrategyName, applicationSettings.CosmosSettings, containerSetting, out var strategy))
        {
            Log.Error("Strategy {@Strategy} not found", selectedStrategyName);
            this.console.MarkupLine("[red]Strategy '{0}' not found.[/]", selectedStrategyName);
            return null;
        }

        this.console.MarkupLine("[green]Selected strategy: {0}[/]", selectedStrategyName);
        return strategy;
    }
}
