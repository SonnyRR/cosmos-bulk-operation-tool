using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;

namespace Cosmos.BulkOperation.CLI.Strategies;

/// <summary>
/// A sample strategy for bulk deleting dummy test data.
/// </summary>
/// <remarks>
/// Deletes all records matching the query.
/// </remarks>

[SettingsKey("RunSettings")]
public class SampleRecordsDeletionStrategy : BulkDeleteOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleRecordsDeletionStrategy"/> class.
    /// </summary>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    public SampleRecordsDeletionStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
        : base(cosmosSettings, containerSettings, useSystemTextJson: true)
    {
    }

    /// <summary>
    /// Executes the bulk deletion of sample records.
    /// </summary>
    /// <param name="dryRun">If true, does not apply changes to Cosmos DB.</param>
    /// <param name="ct">A cancellation token.</param>
    public override async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
    {
        var recordsToDelete = new List<Run>();

        var feed = this.GetFeedIterator();
        while (feed.HasMoreResults)
        {
            var row = await feed.ReadNextAsync(ct);
            recordsToDelete.AddRange(row.Resource);
        }

        this.QueueDeletionOperationTasks(recordsToDelete, r => r.Id.ToString(), r => new(r.UserId), ct);
        await base.EvaluateAsync(dryRun, ct);
    }
}
