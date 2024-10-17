using System;

namespace Cosmos.BulkOperation.CLI.Settings
{
    /// <summary>
    /// Used for retrieving Azure Cosmos DB container settings section.
    /// </summary>
    /// <param name="name">The key name.</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SettingsKeyAttribute(string name) : Attribute
    {
        /// <summary>
        /// The container settings section name.
        /// </summary>
        public string Name { get; } = name;
    }
}
