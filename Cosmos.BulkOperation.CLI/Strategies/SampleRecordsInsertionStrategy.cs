using System.Threading;
using System.Threading.Tasks;
using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;
namespace Cosmos.BulkOperation.CLI.Strategies;

/// <summary>
/// A sample strategy for bulk importing dummy test data.
/// </summary>
/// <inheritdoc cref="BulkInsertOperationStrategy{Run, PartitionKeyType.StringPartitionKey}"/>
[SettingsKey("RunSettings")]
public class SampleRecordsInsertionStrategy : BulkInsertOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
{
    public SampleRecordsInsertionStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
        : base(cosmosSettings, containerSettings, useSystemTextJson: true) {}

    public override async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
    {
        var fakeRecords = FakeDataHelper.GenerateDummyRuns();

        QueueInsertionOperationTasks(fakeRecords, r => new(r.UserId), ct);
        await base.EvaluateAsync(dryRun, ct);
    }
}
