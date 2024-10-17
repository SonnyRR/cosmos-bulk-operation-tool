namespace Cosmos.BulkOperation.CLI.Settings
{
    /// <summary>
    /// Cosmos DB container settings.
    /// </summary>
    public class ContainerSettings
    {
        /// <summary>
        /// The container's name.
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// The query used to filter the container by.
        /// </summary>
        public Query Query { get; set; }
    }
}
