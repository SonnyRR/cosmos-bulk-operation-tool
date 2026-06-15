using System.Threading.Tasks;

using Cosmos.BulkOperation.IntegrationTests.Fixtures;

using Shouldly;

namespace Cosmos.BulkOperation.IntegrationTests;

/// <summary>
/// Tests for bulk deletion command functionality.
/// </summary>
public class BulkDeleteTests : BaseCosmosTest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkDeleteTests"/> class.
    /// </summary>
    /// <param name="fixture">The Cosmos database fixture.</param>
    public BulkDeleteTests(CosmosDatabaseFixture fixture) : base(fixture) { }

    /// <summary>
    /// Verifies that executing with invalid strategy returns non-zero exit code.
    /// </summary>
    [Fact(DisplayName = "Returns non-zero exit code when executed with an invalid delete strategy")]
    public async Task Execute_WithInvalidStrategy_ReturnsNonZeroExitCode()
    {
        var app = CreateAppTester(confirmMutation: false);
        var result = await app.RunAsync(["--strategy", "NonExistentDeleteStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that executing with help flag returns zero exit code.
    /// </summary>
    [Fact(DisplayName = "Returns zero exit code when executed with help flag (delete)")]
    public async Task Execute_WithHelpFlag_ReturnsZero()
    {
        var app = CreateAppTester(confirmMutation: false);
        var result = await app.RunAsync(["--help"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that executing with delete strategy on empty container returns zero.
    /// </summary>
    [Fact(DisplayName = "Returns zero exit code when deleting from an empty container")]
    public async Task Execute_WithDeleteStrategy_OnEmptyContainer_ReturnsZero()
    {
        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsDeletionStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that executing with delete strategy deletes records from Cosmos DB.
    /// </summary>
    [Fact(DisplayName = "Successfully deletes records from Cosmos DB using delete strategy")]
    public async Task Execute_WithDeleteStrategy_DeletesRecordsFromCosmos()
    {
        await TestDataHelper.SeedRecordsAsync(TestContext.Current.CancellationToken);

        var initialCount = await GetDocumentCountAsync(this.CosmosContainer);
        initialCount.ShouldBe(8750);

        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsDeletionStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        var finalCount = await GetDocumentCountAsync(this.CosmosContainer);
        finalCount.ShouldBe(8500);
    }
}
