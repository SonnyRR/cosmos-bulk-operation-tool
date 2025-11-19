using System.Threading;
using System.Threading.Tasks;

using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;

using Microsoft.Azure.Cosmos;

namespace Cosmos.BulkOperation.CLI.Strategies;

/// <summary>
/// A sample strategy for bulk importing dummy test data.
/// </summary>

[SettingsKey("RunSettings")]
public class SampleRecordsInsertionStrategy : BulkInsertOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleRecordsInsertionStrategy"/> class.
    /// </summary>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    public SampleRecordsInsertionStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
        : base(cosmosSettings, containerSettings, useSystemTextJson: true) { }

    /// <summary>
    /// Executes the bulk insertion of sample records.
    /// </summary>
    /// <param name="dryRun">If true, does not apply changes to Cosmos DB.</param>
    /// <param name="ct">A cancellation token.</param>
    public override async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
    {
        await this.CreateDatabaseAndContainerIfNotExisting(ct);
        var fakeRecords = FakeDataHelper.GenerateDummyRuns();

        this.QueueInsertionOperationTasks(fakeRecords, r => new(r.UserId), ct);
        await base.EvaluateAsync(dryRun, ct);
    }

    /// <summary>
    /// Creates the Cosmos sample database &amp; container with shared throughput.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    private async Task CreateDatabaseAndContainerIfNotExisting(CancellationToken ct = default)
    {
        await this.CosmosClient.CreateDatabaseIfNotExistsAsync("sandbox", 1000, cancellationToken: ct);
        await this.Database.CreateContainerIfNotExistsAsync(new("Runs", "/userId"), cancellationToken: ct);
    }
}
