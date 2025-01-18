using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.CLI.Strategies;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Cosmos.BulkOperation.CLI
{
    public static class Program
    {
        private const string APP_EXIT_USER_INPUT = "e";

        private static readonly List<Type> AvailableRecordMutationStrategyTypes = [.. Assembly
            .GetEntryAssembly()
            .GetTypes()
            .Where(t => typeof(IBulkOperationStrategy).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && Attribute.IsDefined(t, typeof(SettingsKeyAttribute)))
            .OrderBy(t => t.Name)];

        public static async Task Main(string[] args)
        {
            // Set up a cancellation token source when the user forcibly
            // tries to stop the application with CTRL+C.
            using var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };

            await Parser.Default.ParseArguments<Arguments>(args)
                .WithParsedAsync(async cliArguments =>
                {
                    var rootApplicationConfiguration = GetRootConfiguration();

                    ConfigureSerilog(rootApplicationConfiguration);

                    Log.Information("=========== Cosmos DB Bulk Operation CLI tool ===========");
                    Log.Information("=========== Author: Vasil Kotsev | 26/09/2024 ===========");

                    if (cliArguments.DryRun)
                    {
                        Log.Warning("Dry-run mode enabled.");
                    }

                    // Give some time for the logs to be flushed before the confirmation prompt.
                    Thread.Sleep(500);

                    var chosenPatchStrategy = GetPatchStrategyFromPrompt(GetApplicationSettings(rootApplicationConfiguration));

                    if (chosenPatchStrategy != null)
                    {
                        // Give some time for the logs to be flushed.
                        Thread.Sleep(500);

                        Console.WriteLine();
                        Console.Write("Are you sure you want to mutate the records ? (y/n) ");
                        var confirmationResponse = Console.ReadKey();
                        Console.WriteLine();

                        if (confirmationResponse.Key == ConsoleKey.Y)
                        {
                            try
                            {
                                await chosenPatchStrategy.EvaluateAsync(cliArguments.DryRun, cancellationTokenSource.Token);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Terminal error");
                            }
                        }
                        else
                        {
                            Log.Information("User did not approve the mutation of the records. Exiting...");
                        }
                    }
                    else
                    {
                        Log.Information("No strategy was provided. Exiting...");
                    }

                    await Log.CloseAndFlushAsync();
                });
        }

        /// <summary>
        /// Configures logging.
        /// </summary>
        /// <param name="settings">The root configuration settings.</param>
        private static void ConfigureSerilog(IConfigurationRoot settings)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(settings)
                .CreateLogger();
        }

        /// <summary>
        /// Retrieve the root application configuration.
        /// </summary>
        /// <returns>An instance of <see cref="IConfigurationRoot"/>.</returns>
        private static IConfigurationRoot GetRootConfiguration() => new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
                .Build();

        /// <summary>
        /// Prompts the user to select which strategy he wants to evaluate & creates an instance of it.
        /// </summary>
        /// <param name="applicationSettings">The application's main settings.</param>
        /// <returns>The strategy instance.</returns>
        private static IBulkOperationStrategy GetPatchStrategyFromPrompt(ApplicationSettings applicationSettings)
        {
            ArgumentNullException.ThrowIfNull(applicationSettings);
            ArgumentNullException.ThrowIfNull(applicationSettings.CosmosSettings);
            ArgumentNullException.ThrowIfNull(applicationSettings.ContainerConfigSectionsAndSettings);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("Available strategies:");
            Console.ForegroundColor = ConsoleColor.Cyan;

            for (int i = 0; i < AvailableRecordMutationStrategyTypes.Count; i++)
            {
                Console.WriteLine($"    {i + 1}. {AvailableRecordMutationStrategyTypes[i].Name}");
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Choose strategy index ('e' - Exit): ");

            int selectedIndex;
            var userInput = Console.ReadLine();
            while (userInput == APP_EXIT_USER_INPUT || !int.TryParse(userInput, out selectedIndex) || selectedIndex < 0 || selectedIndex > AvailableRecordMutationStrategyTypes.Count)
            {
                if (userInput != APP_EXIT_USER_INPUT)
                {
                    Console.WriteLine("Invalid index. Try again");
                    userInput = Console.ReadLine();
                    continue;
                }

                return null;
            }

            Console.ForegroundColor = ConsoleColor.Gray;

            var chosenStrategyType = AvailableRecordMutationStrategyTypes[selectedIndex - 1];
            var configKey = chosenStrategyType.GetCustomAttribute<SettingsKeyAttribute>();

            if (!applicationSettings.ContainerConfigSectionsAndSettings.TryGetValue(configKey.Name, out var containerSetting))
            {
                Log.Error("Cannot find container settings for configuration key: {@ConfigKey}", configKey.Name);

                return null;
            }

            Log.Information("Creating instance of {@Strategy}", chosenStrategyType.Name);

            return Activator.CreateInstance(chosenStrategyType, applicationSettings.CosmosSettings, containerSetting) as IBulkOperationStrategy;
        }

        /// <summary>
        /// Retrieves the application's settings.
        /// </summary>
        /// <param name="settings">The configuration root settings.</param>
        /// <returns>An instance of <see cref="ApplicationSettings"/>.</returns>
        private static ApplicationSettings GetApplicationSettings(IConfigurationRoot settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var cosmosSettings = settings.GetRequiredSection(nameof(CosmosSettings)).Get<CosmosSettings>();
            var rootContainerSettingsSection = settings.GetRequiredSection("ContainerSettings");
            var containerSettings = rootContainerSettingsSection
                .GetChildren()
                .Where(s => !s.Key.Contains(':'))
                .ToDictionary(k => k.Key, w => w.Get<ContainerSettings>());

            return new()
            {
                CosmosSettings = cosmosSettings,
                ContainerConfigSectionsAndSettings = containerSettings
            };
        }
    }
}