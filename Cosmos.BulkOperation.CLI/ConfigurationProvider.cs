using System;
using System.Linq;

using Cosmos.BulkOperation.CLI.Settings;

using Microsoft.Extensions.Configuration;

using Serilog;

namespace Cosmos.BulkOperation.CLI;

/// <summary>
/// Provides configuration building and retrieval for the application.
/// </summary>
public static class ConfigurationProvider
{
    /// <summary>
    /// Builds the application configuration by loading JSON settings files.
    /// </summary>
    /// <param name="environment">The environment name (e.g., Development, Production). Defaults to DOTNET_ENVIRONMENT or Development.</param>
    /// <param name="basePath">The base path for configuration files. Defaults to the current directory.</param>
    /// <returns>The built configuration root.</returns>
    public static IConfigurationRoot BuildConfiguration(string environment = null, string basePath = null)
    {
        var builder = new ConfigurationBuilder();

        if (!string.IsNullOrEmpty(basePath))
        {
            builder.SetBasePath(basePath);
        }

        return builder
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                    $"appsettings.{environment ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json",
                    optional: true,
                    reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// Configures Serilog logging from the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration root to read logging settings from.</param>
    public static void ConfigureSerilog(IConfigurationRoot configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }

    /// <summary>
    /// Retrieves application settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration root to read settings from.</param>
    /// <returns>The application settings containing Cosmos and container configurations.</returns>
    public static ApplicationSettings GetApplicationSettings(IConfigurationRoot configuration)
    {
        var cosmosSettings = configuration
            .GetRequiredSection(nameof(CosmosSettings))
            .Get<CosmosSettings>();

        var rootContainerSettingsSection = configuration.GetRequiredSection("ContainerSettings");
        var containerSettings = rootContainerSettingsSection
            .GetChildren()
            .Where(s => !s.Key.Contains(':'))
            .ToDictionary(k => k.Key, w => w.Get<ContainerSettings>());

        return new ApplicationSettings
        {
            CosmosSettings = cosmosSettings,
            ContainerConfigSectionsAndSettings = containerSettings
        };
    }
}
