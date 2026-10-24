using System;
using System.Linq;

using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.CLI.Strategies;

using Shouldly;

namespace Cosmos.BulkOperation.UnitTests.Strategies;

/// <summary>
/// Tests for the <see cref="StrategyProvider"/>.
/// </summary>
public class StrategyProviderTests
{
    /// <summary>
    /// The provider under test.
    /// </summary>
    private readonly StrategyProvider strategyProvider = new();

    /// <summary>
    /// Verifies that all sample strategies are discovered.
    /// </summary>
    [Fact(DisplayName = "Discovers all sample strategies")]
    public void GetAvailableStrategies_ContainsAllSampleStrategies()
    {
        var strategies = this.strategyProvider.GetAvailableStrategies().ToList();

        strategies.ShouldContain("SampleRecordsPatchStrategy");
        strategies.ShouldContain("SampleRecordsInsertionStrategy");
        strategies.ShouldContain("SampleRecordsDeletionStrategy");
    }

    /// <summary>
    /// Verifies that the available strategies are returned in a sorted order.
    /// </summary>
    [Fact(DisplayName = "Returns available strategies in sorted order")]
    public void GetAvailableStrategies_ReturnsStrategiesSorted()
        => this.strategyProvider.GetAvailableStrategies()
            .ShouldBe(this.strategyProvider.GetAvailableStrategies().Order());

    /// <summary>
    /// Verifies that the configuration key lookup is case-insensitive.
    /// </summary>
    [Theory(DisplayName = "Maps sample strategies to their configuration keys, case-insensitively")]
    [InlineData("SampleRecordsPatchStrategy")]
    [InlineData("samplerecordspatchstrategy")]
    public void TryGetConfigKey_WithExistingStrategyName_ReturnsConfigurationKey(string strategyName)
    {
        var result = this.strategyProvider.TryGetConfigKey(strategyName, out var configKey);

        result.ShouldBeTrue();
        configKey.ShouldBe("RunSettings");
    }

    /// <summary>
    /// Verifies that an unknown strategy name does not yield a configuration key.
    /// </summary>
    [Fact(DisplayName = "Does not yield a configuration key for unknown strategies")]
    public void TryGetConfigKey_WithUnknownStrategyName_ReturnsFalse()
    {
        var result = this.strategyProvider.TryGetConfigKey("NonExistentStrategy", out var configKey);

        result.ShouldBeFalse();
        configKey.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that a null strategy name yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws ArgumentNullException for a null strategy name")]
    public void TryGetConfigKey_WithNullStrategyName_ThrowsArgumentNullException()
        => ((Action)(() => this.strategyProvider.TryGetConfigKey(null, out _)))
            .ShouldThrow<ArgumentNullException>();

    /// <summary>
    /// Verifies that a strategy instance is created for an existing strategy name.
    /// </summary>
    [Fact(DisplayName = "Creates a strategy instance for an existing strategy name")]
    public void TryGetStrategy_WithExistingStrategyName_CreatesStrategyInstance()
    {
        var result = this.strategyProvider.TryGetStrategy(
            "SampleRecordsPatchStrategy",
            CreateCosmosSettings(),
            CreateContainerSettings(),
            out var strategy);

        result.ShouldBeTrue();
        strategy.ShouldNotBeNull();
        strategy.ShouldBeAssignableTo<SampleRecordsPatchStrategy>();
    }

    /// <summary>
    /// Verifies that an unknown strategy name does not yield a strategy instance.
    /// </summary>
    [Fact(DisplayName = "Does not yield a strategy instance for unknown strategies")]
    public void TryGetStrategy_WithUnknownStrategyName_ReturnsFalse()
    {
        var result = this.strategyProvider.TryGetStrategy(
            "NonExistentStrategy",
            CreateCosmosSettings(),
            CreateContainerSettings(),
            out var strategy);

        result.ShouldBeFalse();
        strategy.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that a null Cosmos settings argument yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws ArgumentNullException for null Cosmos settings")]
    public void TryGetStrategy_WithNullCosmosSettings_ThrowsArgumentNullException()
        => ((Action)(() => this.strategyProvider.TryGetStrategy(
            "SampleRecordsPatchStrategy",
            null,
            CreateContainerSettings(),
            out _)))
            .ShouldThrow<ArgumentNullException>();

    /// <summary>
    /// Verifies that a null container settings argument yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws ArgumentNullException for null container settings")]
    public void TryGetStrategy_WithNullContainerSettings_ThrowsArgumentNullException()
        => ((Action)(() => this.strategyProvider.TryGetStrategy(
            "SampleRecordsPatchStrategy",
            CreateCosmosSettings(),
            null,
            out _)))
            .ShouldThrow<ArgumentNullException>();

    /// <summary>
    /// Verifies that a strategy instance is retrieved for an existing strategy name.
    /// </summary>
    [Fact(DisplayName = "Retrieves a strategy instance for an existing strategy name")]
    public void GetStrategy_WithExistingStrategyName_RetrievesStrategyInstance()
        => this.strategyProvider.GetStrategy(
                "SampleRecordsPatchStrategy",
                CreateCosmosSettings(),
                CreateContainerSettings())
            .ShouldBeAssignableTo<SampleRecordsPatchStrategy>();

    /// <summary>
    /// Verifies that an unknown strategy name yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws InvalidOperationException for unknown strategies")]
    public void GetStrategy_WithUnknownStrategyName_ThrowsInvalidOperationException()
        => ((Action)(() => this.strategyProvider.GetStrategy(
                "NonExistentStrategy",
                CreateCosmosSettings(),
                CreateContainerSettings())))
            .ShouldThrow<InvalidOperationException>();

    private static CosmosSettings CreateCosmosSettings()
        => new()
        {
            EndpointUrl = "https://localhost:8081",
            AuthorizationKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            DatabaseName = "sandbox",
            RequestTimeOut = TimeSpan.FromSeconds(15),
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
            MaxRetryAttemptsOnRateLimitedRequests = 9
        };

    private static ContainerSettings CreateContainerSettings()
        => new()
        {
            ContainerName = "Runs",
            Query = new Query { Value = "SELECT * FROM c" }
        };
}
