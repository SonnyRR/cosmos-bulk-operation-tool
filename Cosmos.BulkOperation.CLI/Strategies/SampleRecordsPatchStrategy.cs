using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cosmos.BulkOperation.CLI.Strategies
{
    /// <summary>
    /// A sample strategy for bulk patching sample data;
    /// </summary>
    /// <remarks>
    /// Changes all checkpoint pin colors to 'Black' for records, that match a query.
    /// </remarks>
    /// <inheritdoc cref="BulkPatchOperationStrategy{Run, PartitionKeyType.StringPartitionKey}"/>
    [SettingsKey("RunSettings")]
    public class SampleRecordsPatchStrategy : BulkPatchOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
    {
        public SampleRecordsPatchStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
            : base(cosmosSettings, containerSettings, useSystemTextJson: true)
        {
        }

        public override async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
        {
            var feed = this.GetFeedIterator();
            while (feed.HasMoreResults)
            {
                var page = await feed.ReadNextAsync(ct);
                foreach (var run in page)
                {
                    List<PatchOperation> patchOperations = [];
                    for (int i = 0; i < run.Checkpoints.Count(); i++)
                    {
                        var colorSettingPropertyPath = $"/checkpoints/{i}/pinColor";
                        var patchOperation = PatchOperation.Replace(colorSettingPropertyPath, Color.Black);
                        patchOperations.Add(patchOperation);
                    }

                    this.QueuePatchOperationTasks(patchOperations, run.Id.ToString(), new(run.UserId), ct);
                }
            }

            await base.EvaluateAsync(dryRun, ct);
        }
    }
}