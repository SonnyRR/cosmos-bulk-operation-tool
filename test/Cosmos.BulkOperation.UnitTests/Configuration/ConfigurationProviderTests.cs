using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.CLI.Settings;

using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Cosmos.BulkOperation.UnitTests.Configuration;

/// <summary>
/// Tests for the <see cref="Cosmos.BulkOperation.CLI.ConfigurationProvider"/>.
/// </summary>
public class ConfigurationProviderTests
{
    /// <summary>
    /// Verifies that application settings are bound from a configuration, including multiple container sections.
    /// </summary>
    [Fact(DisplayName = "Binds Cosmos and container settings from configuration")]
    public void GetApplicationSettings_WithFullConfiguration_BindsAllSettings()
    {
        var configuration = CreateConfiguration(CreateFullConfigurationValues());
        var settings = CLI.ConfigurationProvider.GetApplicationSettings(configuration);

        settings.CosmosSettings.ShouldNotBeNull();
        settings.CosmosSettings.EndpointUrl.ShouldBe("https://localhost:8081/");
        settings.CosmosSettings.AuthorizationKey.ShouldBe("test-key");
        settings.CosmosSettings.DatabaseName.ShouldBe("sandbox");
        settings.CosmosSettings.RequestTimeOut.ShouldBe(TimeSpan.FromMinutes(1));
        settings.CosmosSettings.MaxRetryAttemptsOnRateLimitedRequests.ShouldBe(25);
        settings.CosmosSettings.MaxRetryWaitTimeOnRateLimitedRequests.ShouldBe(TimeSpan.FromMinutes(1));
        settings.CosmosSettings.QueryRequestOptions.ShouldNotBeNull();
        settings.CosmosSettings.QueryRequestOptions.MaxItemCount.ShouldBe(-1);

        // Unspecified settings fall back to their C# defaults
        settings.CosmosSettings.ConnectionMode.ShouldBe(CosmosConnectionMode.Direct);

        settings.ContainerConfigSectionsAndSettings.Count.ShouldBe(2);
        settings.ContainerConfigSectionsAndSettings["RunSettings"].ContainerName.ShouldBe("Runs");
        settings.ContainerConfigSectionsAndSettings["RunSettings"].Query.Value.ShouldBe("SELECT * FROM c");
        settings.ContainerConfigSectionsAndSettings["LogsSettings"].ContainerName.ShouldBe("Logs");
        settings.ContainerConfigSectionsAndSettings["LogsSettings"].Query.Value.ShouldBe("SELECT * FROM logs");
    }

    /// <summary>
    /// Verifies that a missing Cosmos settings section yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws when Cosmos settings section is missing")]
    public void GetApplicationSettings_WithoutCosmosSettingsSection_Throws()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["ContainerSettings:RunSettings:ContainerName"] = "Runs"
        });

        ((Action)(() => CLI.ConfigurationProvider.GetApplicationSettings(configuration)))
            .ShouldThrow<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that a missing container settings section yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws when container settings section is missing")]
    public void GetApplicationSettings_WithoutContainerSettingsSection_Throws()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string>
        {
            ["CosmosSettings:EndpointUrl"] = "https://localhost:8081/"
        });

        ((Action)(() => CLI.ConfigurationProvider.GetApplicationSettings(configuration)))
            .ShouldThrow<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that the environment-specific settings file overrides the base settings file.
    /// </summary>
    [Fact(DisplayName = "Environment-specific settings file overrides the base settings file")]
    public void BuildConfiguration_WithEnvironmentSpecificFile_AppliesOverride()
    {
        var basePath = CreateTemporaryConfigurationDirectory(
            ("appsettings.json", """{"TestSetting": "base"}"""),
            ("appsettings.Custom.json", """{"TestSetting": "custom"}"""));

        var configuration = CLI.ConfigurationProvider.BuildConfiguration("Custom", basePath);

        configuration["TestSetting"].ShouldBe("custom");
    }

    /// <summary>
    /// Verifies that the base settings are retained when they are not overridden.
    /// </summary>
    [Fact(DisplayName = "Environment-specific settings file preserves un-overridden base settings")]
    public void BuildConfiguration_WithEnvironmentSpecificFile_PreservesBaseSettings()
    {
        var basePath = CreateTemporaryConfigurationDirectory(
            ("appsettings.json", """{"TestSetting": "base", "BaseSetting": "base"}"""),
            ("appsettings.Custom.json", """{"TestSetting": "custom"}"""));

        var configuration = CLI.ConfigurationProvider.BuildConfiguration("Custom", basePath);

        configuration["BaseSetting"].ShouldBe("base");
        configuration["TestSetting"].ShouldBe("custom");
    }

    /// <summary>
    /// Verifies that a missing environment name falls back to 'Development'.
    /// </summary>
    [Fact(DisplayName = "Falls back to the Development environment settings file")]
    public void BuildConfiguration_WithoutEnvironment_FallsBackToDevelopment()
    {
        var originalEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var basePath = CreateTemporaryConfigurationDirectory(
            ("appsettings.json", """{"TestSetting": "base"}"""),
            ("appsettings.Development.json", """{"TestSetting": "development"}"""));

        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            var configuration = CLI.ConfigurationProvider.BuildConfiguration(basePath: basePath);

            configuration["TestSetting"].ShouldBe("development");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalEnvironment);
        }
    }

    /// <summary>
    /// Verifies that the DOTNET_ENVIRONMENT variable is used when no environment is provided.
    /// </summary>
    [Fact(DisplayName = "Uses the DOTNET_ENVIRONMENT variable when no environment is provided")]
    public void BuildConfiguration_WithDotnetEnvironmentVariable_UsesVariable()
    {
        var basePath = CreateTemporaryConfigurationDirectory(
            ("appsettings.json", """{"TestSetting": "base"}"""),
            ("appsettings.Staging.json", """{"TestSetting": "staging"}"""));

        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Staging");

            var configuration = CLI.ConfigurationProvider.BuildConfiguration(basePath: basePath);

            configuration["TestSetting"].ShouldBe("staging");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
        }
    }

    private static IConfigurationRoot CreateConfiguration(Dictionary<string, string> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static Dictionary<string, string> CreateFullConfigurationValues()
        => new()
        {
            ["CosmosSettings:EndpointUrl"] = "https://localhost:8081/",
            ["CosmosSettings:AuthorizationKey"] = "test-key",
            ["CosmosSettings:DatabaseName"] = "sandbox",
            ["CosmosSettings:RequestTimeOut"] = "00:01:00",
            ["CosmosSettings:MaxRetryAttemptsOnRateLimitedRequests"] = "25",
            ["CosmosSettings:MaxRetryWaitTimeOnRateLimitedRequests"] = "00:01:00",
            ["CosmosSettings:QueryRequestOptions:MaxItemCount"] = "-1",
            ["ContainerSettings:RunSettings:ContainerName"] = "Runs",
            ["ContainerSettings:RunSettings:Query:Value"] = "SELECT * FROM c",
            ["ContainerSettings:LogsSettings:ContainerName"] = "Logs",
            ["ContainerSettings:LogsSettings:Query:Value"] = "SELECT * FROM logs"
        };

    private static string CreateTemporaryConfigurationDirectory(params (string FileName, string Content)[] files)
    {
        var basePath = Path.Combine("/tmp/opencode", "configuration-tests", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(basePath);

        foreach (var (fileName, content) in files)
        {
            File.WriteAllText(Path.Combine(basePath, fileName), content);
        }

        return basePath;
    }
}
