using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.CLI.Strategies;
using Cosmos.BulkOperation.TestUtilities;

using Microsoft.Extensions.Configuration;

using Moq;

using Shouldly;
using Spectre.Console.Cli.Testing;

namespace Cosmos.BulkOperation.UnitTests.Command;

/// <summary>
/// Tests for the resolved-strategy paths of the <see cref="Cosmos.BulkOperation.CLI.BulkCosmosCommand"/>.
/// </summary>
public class BulkCosmosCommandTests
{
    /// <summary>
    /// Configuration values with both the Cosmos settings and the 'RunSettings' container section present.
    /// </summary>
    private static readonly Dictionary<string, string> DefaultConfigurationValues = new()
    {
        ["CosmosSettings:EndpointUrl"] = "https://localhost:8081/",
        ["CosmosSettings:AuthorizationKey"] = "test-key",
        ["ContainerSettings:RunSettings:ContainerName"] = "Runs",
        ["ContainerSettings:RunSettings:Query:Value"] = "SELECT * FROM c"
    };

    /// <summary>
    /// Verifies that an unknown strategy flag results in a non-zero exit code.
    /// </summary>
    [Fact(DisplayName = "Returns a non-zero exit code for an unknown strategy flag")]
    public async Task Execute_WithUnknownStrategyFlag_ReturnsNonZeroExitCode()
    {
        var tester = CreateTester(
            DefaultConfigurationValues,
            new FakeStrategyProvider(configKeyToReturn: null),
            confirmInput: null);

        var result = await tester.RunAsync(["--strategy", "NonExistentStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that a strategy whose configuration key has no matching container settings section
    /// results in a non-zero exit code.
    /// </summary>
    [Fact(DisplayName = "Returns a non-zero exit code when the strategy's container settings section is missing")]
    public async Task Execute_WithMissingContainerSettingsSection_ReturnsNonZeroExitCode()
    {
        var tester = CreateTester(
            new Dictionary<string, string>
            {
                ["CosmosSettings:EndpointUrl"] = "https://localhost:8081/",
                ["ContainerSettings:OtherSettings:ContainerName"] = "Other"
            },
            new FakeStrategyProvider(configKeyToReturn: "RunSettings", strategyCreationSucceeds: false),
            confirmInput: null);

        var result = await tester.RunAsync(["--strategy", "FakeStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that a strategy which cannot be instantiated results in a non-zero exit code.
    /// </summary>
    [Fact(DisplayName = "Returns a non-zero exit code when the strategy cannot be created")]
    public async Task Execute_WhenStrategyCreationFails_ReturnsNonZeroExitCode()
    {
        var tester = CreateTester(
            DefaultConfigurationValues,
            new FakeStrategyProvider(configKeyToReturn: "RunSettings", strategyCreationSucceeds: false),
            confirmInput: null);

        var result = await tester.RunAsync(["--strategy", "FakeStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that declining the mutation confirmation does not evaluate the strategy.
    /// </summary>
    [Fact(DisplayName = "Does not mutate records when the mutation confirmation is declined")]
    public async Task Execute_WhenMutationIsDeclined_DoesNotEvaluateStrategy()
    {
        var strategy = CreateStrategyMock();
        var tester = CreateTester(
            DefaultConfigurationValues,
            new FakeStrategyProvider(configKeyToReturn: "RunSettings", strategyInstance: strategy.Object),
            confirmInput: "n");

        var result = await tester.RunAsync(["--strategy", "FakeStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        strategy.Verify(s => s.EvaluateAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that confirming the mutation evaluates the strategy.
    /// </summary>
    [Fact(DisplayName = "Evaluates the strategy when the mutation is confirmed")]
    public async Task Execute_WhenMutationIsConfirmed_EvaluatesStrategy()
    {
        var strategy = CreateStrategyMock();
        var tester = CreateTester(
            DefaultConfigurationValues,
            new FakeStrategyProvider(configKeyToReturn: "RunSettings", strategyInstance: strategy.Object),
            confirmInput: "y");

        var result = await tester.RunAsync(["--strategy", "FakeStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        strategy.Verify(s => s.EvaluateAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that the dry-run flag is forwarded to the evaluated strategy.
    /// </summary>
    [Fact(DisplayName = "Forwards the dry-run flag to the evaluated strategy")]
    public async Task Execute_WithDryRunFlag_ForwardsDryRunToStrategy()
    {
        var strategy = CreateStrategyMock();
        var tester = CreateTester(
            DefaultConfigurationValues,
            new FakeStrategyProvider(configKeyToReturn: "RunSettings", strategyInstance: strategy.Object),
            confirmInput: "y");

        var result = await tester.RunAsync(["--strategy", "FakeStrategy", "--dry-run"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        strategy.Verify(s => s.EvaluateAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that an unhandled exception during settings retrieval results in an exit code of -1.
    /// </summary>
    [Fact(DisplayName = "Returns -1 when application settings cannot be retrieved")]
    public async Task Execute_WithMisconfiguredApplicationSettings_ReturnsMinusOne()
    {
        var tester = CreateTester(
            new Dictionary<string, string>
            {
                ["ContainerSettings:RunSettings:ContainerName"] = "Runs"
            },
            new FakeStrategyProvider(configKeyToReturn: "RunSettings"),
            confirmInput: null);

        var result = await tester.RunAsync(["--strategy", "FakeStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(-1);
    }

    private static Mock<IBulkOperationStrategy> CreateStrategyMock()
    {
        var strategy = new Mock<IBulkOperationStrategy>();
        strategy
            .Setup(s => s.EvaluateAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return strategy;
    }

    private static CommandAppTester CreateTester(Dictionary<string, string> configurationValues, IStrategyProvider strategyProvider, string confirmInput)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var console = TestHarness.CreateTestConsole(confirmMutation: false);
        if (confirmInput is not null)
        {
            console.Input.PushTextWithEnter(confirmInput);
        }

        return TestHarness.CreateTester(configuration, strategyProvider, console);
    }

    /// <summary>
    /// A prepared <see cref="IStrategyProvider"/> returning fixed values, for exercising the command paths.
    /// </summary>
    /// <param name="configKeyToReturn">
    /// The configuration key returned by <see cref="IStrategyProvider.TryGetConfigKey"/>, or null to simulate an unknown strategy.
    /// </param>
    /// <param name="strategyInstance">The strategy returned by <see cref="IStrategyProvider.TryGetStrategy"/>.</param>
    /// <param name="strategyCreationSucceeds">
    /// Whether <see cref="IStrategyProvider.TryGetStrategy"/> reports success.
    /// </param>
    private sealed class FakeStrategyProvider(string configKeyToReturn, IBulkOperationStrategy strategyInstance = null, bool strategyCreationSucceeds = true) : IStrategyProvider
    {
        /// <inheritdoc/>
        public IEnumerable<string> GetAvailableStrategies() => [];

        /// <inheritdoc/>
        public IBulkOperationStrategy GetStrategy(string strategyName, Cosmos.BulkOperation.CLI.Settings.CosmosSettings cosmosSettings, Cosmos.BulkOperation.CLI.Settings.ContainerSettings containerSettings)
            => throw new InvalidOperationException($"Unknown strategy: {strategyName}");

        /// <inheritdoc/>
        public bool TryGetConfigKey(string strategyName, out string configKey)
        {
            if (configKeyToReturn is null)
            {
                configKey = null;
                return false;
            }

            configKey = configKeyToReturn;
            return true;
        }

        /// <inheritdoc/>
        public bool TryGetStrategy(string strategyName, Cosmos.BulkOperation.CLI.Settings.CosmosSettings cosmosSettings, Cosmos.BulkOperation.CLI.Settings.ContainerSettings containerSettings, out IBulkOperationStrategy strategy)
        {
            strategy = strategyInstance;
            return strategyCreationSucceeds;
        }
    }
}
