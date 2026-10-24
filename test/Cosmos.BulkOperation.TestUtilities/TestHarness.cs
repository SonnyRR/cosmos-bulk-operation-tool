using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.CLI.Strategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Cosmos.BulkOperation.TestUtilities;

/// <summary>
/// Provides test utilities for setting up CLI test harnesses.
/// </summary>
public static class TestHarness
{
    /// <summary>
    /// Creates a CommandAppTester configured for testing.
    /// </summary>
    /// <param name="environment">The environment name for configuration.</param>
    /// <param name="console">The test console instance.</param>
    /// <returns>A configured CommandAppTester instance.</returns>
    public static CommandAppTester CreateTester(string environment, TestConsole console)
    {
        var configuration = CLI.ConfigurationProvider.BuildConfiguration(environment);

        var services = new ServiceCollection();
        services.AddSingleton<IStrategyProvider, StrategyProvider>();
        services.AddSingleton(configuration);
        services.AddSingleton<IAnsiConsole>(console);

        var registrar = new SpectreCliTypeRegistrar(services);

        var tester = new CommandAppTester(registrar, console: console);
        tester.SetDefaultCommand<BulkCosmosCommand>();
        tester.Configure(config =>
        {
            config.SetApplicationName("cosmos-bulk-operation-tool");
            config.AddExample("--dry-run");
        });

        return tester;
    }

    /// <summary>
    /// Creates a CommandAppTester configured for testing with a pre-built configuration and an injected strategy provider.
    /// </summary>
    /// <param name="configuration">The configuration root to be used by the command.</param>
    /// <param name="strategyProvider">The strategy provider to be used by the command.</param>
    /// <param name="console">The test console instance.</param>
    /// <returns>A configured CommandAppTester instance.</returns>
    public static CommandAppTester CreateTester(IConfigurationRoot configuration, IStrategyProvider strategyProvider, TestConsole console)
    {
        var services = new ServiceCollection();
        services.AddSingleton(strategyProvider);
        services.AddSingleton(configuration);
        services.AddSingleton<IAnsiConsole>(console);

        var registrar = new SpectreCliTypeRegistrar(services);

        var tester = new CommandAppTester(registrar, console: console);
        tester.SetDefaultCommand<BulkCosmosCommand>();
        tester.Configure(config =>
        {
            config.SetApplicationName("cosmos-bulk-operation-tool");
            config.AddExample("--dry-run");
        });

        return tester;
    }

    /// <summary>
    /// Creates an interactive test console, optionally pre-configured with the mutation confirmation key press.
    /// </summary>
    /// <param name="confirmMutation">Whether to answer the mutation confirmation prompt with 'y'.</param>
    /// <returns>A configured TestConsole instance.</returns>
    public static TestConsole CreateTestConsole(bool confirmMutation = true)
    {
        var console = new TestConsole();

        console.Profile.Capabilities.Interactive = true;
        console.Profile.Capabilities.Ansi = true;

        if (confirmMutation)
        {
            console.Input.PushTextWithEnter("y");
        }

        return console;
    }

    /// <summary>
    /// Creates a CommandAppTester for testing CLI commands against the 'Testing' environment.
    /// </summary>
    /// <param name="confirmMutation">Whether to confirm mutation prompts (for destructive operations).</param>
    /// <returns>A configured CommandAppTester instance.</returns>
    public static CommandAppTester CreateAppTester(bool confirmMutation = true)
        => CreateTester("Testing", CreateTestConsole(confirmMutation));
}
