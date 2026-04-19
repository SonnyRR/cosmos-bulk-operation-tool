using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Cosmos.BulkOperation.IntegrationTests.Fixtures;
using Cosmos.BulkOperation.Samples;

using Microsoft.Azure.Cosmos;

using Shouldly;

namespace Cosmos.BulkOperation.IntegrationTests;

/// <summary>
/// Tests for bulk patching command functionality.
/// </summary>
public class BulkPatchTests : BaseCosmosTest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkPatchTests"/> class.
    /// </summary>
    /// <param name="fixture">The Cosmos database fixture.</param>
    public BulkPatchTests(CosmosDatabaseFixture fixture) : base(fixture) { }

    /// <summary>
    /// Verifies that executing with invalid strategy returns non-zero exit code.
    /// </summary>
    [Fact]
    public async Task Execute_WithInvalidStrategy_ReturnsNonZeroExitCode()
    {
        var app = CreateAppTester(confirmMutation: false);
        var result = await app.RunAsync(["--strategy", "NonExistentPatchStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that executing with help flag returns zero exit code.
    /// </summary>
    [Fact]
    public async Task Execute_WithHelpFlag_ReturnsZero()
    {
        var app = CreateAppTester(confirmMutation: false);
        var result = await app.RunAsync(["--help"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that executing with patch strategy on empty container returns zero.
    /// </summary>
    [Fact]
    public async Task Execute_WithPatchStrategy_OnEmptyContainer_ReturnsZero()
    {
        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsPatchStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that executing with patch strategy patches records in Cosmos DB.
    /// </summary>
    [Fact]
    public async Task Execute_WithPatchStrategy_PatchesRecordsInCosmos()
    {
        await TestDataHelper.SeedRecordsAsync(TestContext.Current.CancellationToken);

        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsPatchStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);

        var records = await this.GetAllRecordsAsync();
        var targetRecords = records.Where(r => r.UserId == "carmella@sopranos.com").ToList();
        targetRecords.ShouldAllBe(r => r.Checkpoints.All(c => c.PinColor == Color.Black));
    }

    private async Task<List<Run>> GetAllRecordsAsync()
    {
        var results = new List<Run>();
        var iterator = this.CosmosContainer.GetItemQueryIterator<Run>();

        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(TestContext.Current.CancellationToken));
        }

        return results;
    }
}
