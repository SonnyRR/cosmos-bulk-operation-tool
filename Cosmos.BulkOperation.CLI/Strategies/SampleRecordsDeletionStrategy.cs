using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;

namespace Cosmos.BulkOperation.CLI.Strategies
{
    /// <summary>
    /// A sample strategy for bulk deleting dummy test data.
    /// </summary>
    /// <remarks>
    /// Deletes all records matching the query.
    /// </remarks>
    /// <inheritdoc cref="BulkDeleteOperationStrategy{Run, PartitionKeyType.StringPartitionKey}"/>
    [SettingsKey("RunSettings")]
    public class SampleRecordsDeletionStrategy : BulkDeleteOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
    {
        public SampleRecordsDeletionStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
            : base(cosmosSettings, containerSettings, useSystemTextJson: true)
        {
        }

        public override async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
        {
            var recordsToDelete = new List<Run>();

            var feed = GetFeedIterator();
            while (feed.HasMoreResults)
            {
                var row = await feed.ReadNextAsync(ct);
                recordsToDelete.AddRange(row.Resource);
            }

            QueueDeletionOperationTasks(recordsToDelete, r => r.Id.ToString(), r => new(r.UserId), ct);
            await base.EvaluateAsync(dryRun, ct);
        }
    }
}
