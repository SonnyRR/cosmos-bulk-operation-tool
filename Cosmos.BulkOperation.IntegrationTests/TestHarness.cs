using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.CLI.Strategies;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Cosmos.BulkOperation.IntegrationTests;

/// <summary>
/// Provides test utilities for setting up CLI test harness.
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
        var config = ConfigurationProvider.BuildConfiguration(environment);

        var services = new ServiceCollection();
        services.AddSingleton<IStrategyProvider, StrategyProvider>();
        services.AddSingleton(config);
        services.AddSingleton(console);

        var registrar = new SpectreCliTypeRegistrar(services);

        var tester = new CommandAppTester(registrar, console: console);
        tester.SetDefaultCommand<BulkImportCommand>();
        tester.Configure(config =>
        {
            config.SetApplicationName("cosmos-bulk-operation-tool");
            config.AddExample("--dry-run");
        });

        return tester;
    }
}
