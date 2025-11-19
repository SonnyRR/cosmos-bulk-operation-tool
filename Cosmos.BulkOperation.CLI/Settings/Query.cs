using System.Collections.Generic;

namespace Cosmos.BulkOperation.CLI.Settings;

/// <summary>
/// Represents a Cosmos DB SQL query
/// </summary>
public class Query
{
    /// <summary>
    /// The query.
    /// </summary>
    public string Value { get; set; }
}
