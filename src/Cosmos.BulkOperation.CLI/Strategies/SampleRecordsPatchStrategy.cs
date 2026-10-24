using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;

using Microsoft.Azure.Cosmos;

namespace Cosmos.BulkOperation.CLI.Strategies;

/// <summary>
/// A sample strategy for bulk patching sample data;
/// </summary>
/// <remarks>
/// Changes all checkpoint pin colors to 'Black' for records, that match a query.
/// </remarks>

[SettingsKey("RunSettings")]
public class SampleRecordsPatchStrategy : BulkPatchOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleRecordsPatchStrategy"/> class.
    /// </summary>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    public SampleRecordsPatchStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
        : base(cosmosSettings, containerSettings, useSystemTextJson: true)
    {
    }

    /// <summary>
    /// Executes the bulk patching of sample records.
    /// </summary>
    /// <param name="dryRun">If true, does not apply changes to Cosmos DB.</param>
    /// <param name="ct">A cancellation token.</param>
    public override async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
    {
        var feed = this.GetFeedIterator();
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            foreach (var run in page)
            {
                List<PatchOperation> patchOperations = [];
                for (var i = 0; i < run.Checkpoints.Count(); i++)
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
