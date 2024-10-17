using System.Collections.Generic;

namespace Cosmos.BulkOperation.CLI.Settings
{
    /// <summary>
    /// The general settings for this CLI application.
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// The settings for the constructing the Cosmos DB clients.
        /// </summary>
        public CosmosSettings CosmosSettings { get; set; }

        /// <summary>
        /// Container settings, grouped by the configuration section name.
        /// </summary>
        /// <remarks>
        /// The key is utilized in the various strategy implementation and is used
        /// for retrieving the corresponding container settings, that the strategy
        /// needs to utilize.
        /// </remarks>
        public Dictionary<string, ContainerSettings> ContainerConfigSectionsAndSettings { get; set; }
    }
}
