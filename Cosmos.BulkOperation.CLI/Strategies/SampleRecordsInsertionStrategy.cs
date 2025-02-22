using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;
using Microsoft.Azure.Cosmos;
using System.Threading;
using System.Threading.Tasks;

namespace Cosmos.BulkOperation.CLI.Strategies
{

	/// <summary>
	/// A sample strategy for bulk importing dummy test data.
	/// </summary>
	/// <inheritdoc cref="BulkInsertOperationStrategy{Run, PartitionKeyType.StringPartitionKey}"/>
	[SettingsKey("RunSettings")]
	public class SampleRecordsInsertionStrategy : BulkInsertOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
	{
		public SampleRecordsInsertionStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
			: base(cosmosSettings, containerSettings, useSystemTextJson: true) { }

		public override async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
		{
			await CreateDatabaseAndContainerIfNotExisting(ct);
			var fakeRecords = FakeDataHelper.GenerateDummyRuns();

			QueueInsertionOperationTasks(fakeRecords, r => new(r.UserId), ct);
			await base.EvaluateAsync(dryRun, ct);
		}

		/// <summary>
		/// Creates the Cosmos sample database &amp; container with shared throughput.
		/// </summary>
		/// <param name="ct">A cancellation token.</param>
		private async Task CreateDatabaseAndContainerIfNotExisting(CancellationToken ct = default)
		{
			await CosmosClient.CreateDatabaseIfNotExistsAsync("sandbox", 1000, cancellationToken: ct);
			await Database.CreateContainerIfNotExistsAsync(new("Runs", "/userId"), cancellationToken: ct);
		}
	}
}