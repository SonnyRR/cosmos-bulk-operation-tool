using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Cosmos.BulkOperation.CLI.Settings;

using Serilog;

namespace Cosmos.BulkOperation.CLI.Strategies;

/// <summary>
/// Provides strategy implementations for bulk operations.
/// </summary>
public class StrategyProvider : IStrategyProvider
{
    private readonly Dictionary<string, (Type type, string configKey)> strategyMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyProvider"/> class.
    /// </summary>
    public StrategyProvider()
    {
        var map = new Dictionary<string, (Type type, string configKey)>(StringComparer.OrdinalIgnoreCase);

        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a => a.GetExportedTypes());

        var implementations = allTypes
            .Where(t => !t.IsAbstract
                        && !t.IsGenericTypeDefinition
                        && typeof(IBulkOperationStrategy).IsAssignableFrom(t));

        foreach (var type in implementations)
        {
            if (type.GetCustomAttribute<SettingsKeyAttribute>() is not SettingsKeyAttribute attr)
            {
                throw new InvalidOperationException(
                    $"Strategy '{type.Name}' implements {nameof(IBulkOperationStrategy)} " +
                    $"but is missing [{nameof(SettingsKeyAttribute)}] attribute. " +
                    $"Add [{nameof(SettingsKeyAttribute)}(\"YourConfigSection\")] to the class.");
            }

            map[type.Name] = (type, attr.Name);
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException(
                $"No strategies implementing {nameof(IBulkOperationStrategy)} were found. " +
                "Ensure strategies have the [SettingsKey] attribute.");
        }

        this.strategyMap = map;
    }

    /// <summary>
    /// Gets all available strategy names.
    /// </summary>
    /// <returns>An enumerable of strategy names.</returns>
    public IEnumerable<string> GetAvailableStrategies()
        => this.strategyMap.Keys.Order();

    /// <summary>
    /// Tries to get the configuration key for a strategy.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="configKey">The output configuration key if found.</param>
    /// <returns>True if the configuration key was found; otherwise, false.</returns>
    public bool TryGetConfigKey(string strategyName, out string configKey)
    {
        ArgumentNullException.ThrowIfNull(strategyName);

        if (this.strategyMap.TryGetValue(strategyName, out var entry))
        {
            configKey = entry.configKey;
            return true;
        }

        configKey = null;
        return false;
    }

    /// <summary>
    /// Gets a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    /// <returns>The bulk operation strategy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the strategy is not found.</exception>
    public IBulkOperationStrategy GetStrategy(
        string strategyName,
        CosmosSettings cosmosSettings,
        ContainerSettings containerSettings)
    {
        return !this.TryGetStrategy(strategyName, cosmosSettings, containerSettings, out var strategy)
            ? throw new InvalidOperationException($"Unknown strategy: {strategyName}")
            : strategy;
    }

    /// <summary>
    /// Tries to get a strategy by name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    /// <param name="strategy">The output strategy if found.</param>
    /// <returns>True if the strategy was found; otherwise, false.</returns>
    public bool TryGetStrategy(
        string strategyName,
        CosmosSettings cosmosSettings,
        ContainerSettings containerSettings,
        out IBulkOperationStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(cosmosSettings);
        ArgumentNullException.ThrowIfNull(containerSettings);

        strategy = null;

        if (!this.strategyMap.TryGetValue(strategyName, out var entry))
        {
            Log.Error("Strategy {@Strategy} not found", strategyName);
            return false;
        }

        try
        {
            strategy = (IBulkOperationStrategy)Activator.CreateInstance(
                entry.type,
                cosmosSettings,
                containerSettings);
            Log.Information("Creating instance of {@Strategy}", strategyName);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create strategy {@Strategy}", strategyName);
            return false;
        }
    }
}
