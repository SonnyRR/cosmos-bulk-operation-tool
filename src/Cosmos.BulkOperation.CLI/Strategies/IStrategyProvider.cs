using System.Collections.Generic;

using Cosmos.BulkOperation.CLI.Settings;

namespace Cosmos.BulkOperation.CLI.Strategies;

/// <summary>
/// Provides strategies for bulk operations on Cosmos DB containers.
/// </summary>
public interface IStrategyProvider
{
    /// <summary>
    /// Gets the available strategy names.
    /// </summary>
    /// <returns>An enumerable of strategy names.</returns>
    IEnumerable<string> GetAvailableStrategies();

    /// <summary>
    /// Gets a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    /// <returns>The bulk operation strategy.</returns>
    IBulkOperationStrategy GetStrategy(
        string strategyName,
        CosmosSettings cosmosSettings,
        ContainerSettings containerSettings);

    /// <summary>
    /// Tries to get a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    /// <param name="strategy">The output strategy if found.</param>
    /// <returns>True if the strategy was found; otherwise, false.</returns>
    bool TryGetStrategy(
        string strategyName,
        CosmosSettings cosmosSettings,
        ContainerSettings containerSettings,
        out IBulkOperationStrategy strategy);

    /// <summary>
    /// Tries to get the configuration key for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="configKey">The output configuration key if found.</param>
    /// <returns>True if the configuration key was found; otherwise, false.</returns>
    bool TryGetConfigKey(string strategyName, out string configKey);
}
