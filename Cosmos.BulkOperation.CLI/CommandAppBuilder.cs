using System;

using Cosmos.BulkOperation.CLI.Strategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Cosmos.BulkOperation.CLI;

/// <summary>
/// Builds and configures the CLI command application.
/// </summary>
public class CommandAppBuilder
{
    private CommandApp<BulkImportCommand> commandApp;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAppBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="console">The Ansi console.</param>
    internal CommandAppBuilder(
        IServiceCollection services,
        IConfigurationRoot configuration,
        IAnsiConsole console)
    {
        this.Services = services ?? throw new ArgumentNullException(nameof(services));
        this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.Console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the configuration root.
    /// </summary>
    public IConfigurationRoot Configuration { get; }

    /// <summary>
    /// Gets the Ansi console.
    /// </summary>
    public IAnsiConsole Console { get; }

    private void ConfigureServices()
    {
        this.Services.AddSingleton<IStrategyProvider, StrategyProvider>();
        this.Services.AddSingleton(this.Configuration);
        this.Services.AddSingleton(this.Console);
    }

    /// <summary>
    /// Builds the command application.
    /// </summary>
    /// <returns>The configured command app.</returns>
    public CommandApp<BulkImportCommand> Build()
    {
        if (this.commandApp != null)
        {
            return this.commandApp;
        }

        this.ConfigureServices();

        var registrar = new SpectreCliTypeRegistrar(this.Services);

        this.commandApp = new CommandApp<BulkImportCommand>(registrar);
        this.commandApp.Configure(config =>
        {
            config.SetApplicationName("cosmos-bulk-operation-tool");
            config.AddExample("--dry-run");
            config.AddExample();
        });

        return this.commandApp;
    }

    /// <summary>
    /// Creates a new command app builder with default settings.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="console">
    /// The ANSI console instance. If not provided, defaults to <see cref="AnsiConsole.Console"/>
    /// </param>
    /// <returns>A new <see cref="CommandAppBuilder"/> instance.</returns>
    public static CommandAppBuilder Create(string environment = null, IAnsiConsole console = null)
    {
        console ??= AnsiConsole.Console;
        var configuration = ConfigurationProvider.BuildConfiguration(environment);
        ConfigurationProvider.ConfigureSerilog(configuration);
        return new CommandAppBuilder(new ServiceCollection(), configuration, console);
    }
}
