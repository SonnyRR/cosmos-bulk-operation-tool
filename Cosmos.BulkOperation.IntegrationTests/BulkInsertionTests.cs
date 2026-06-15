using System.Net;
using System.Threading.Tasks;

using Cosmos.BulkOperation.IntegrationTests.Fixtures;

using Shouldly;

namespace Cosmos.BulkOperation.IntegrationTests;

/// <summary>
/// Tests for bulk insertion command functionality.
/// </summary>
public class BulkInsertionTests : BaseCosmosTest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkInsertionTests"/> class.
    /// </summary>
    /// <param name="fixture">The Cosmos database fixture.</param>
    public BulkInsertionTests(CosmosDatabaseFixture fixture) : base(fixture) { }

    /// <summary>
    /// Verifies that executing with a strategy flag inserts records into Cosmos DB.
    /// </summary>
    [Fact(DisplayName = "Inserts records into Cosmos DB when executed with a valid strategy flag")]
    public async Task Execute_WithStrategyFlag_InsertsRecordsIntoCosmos()
    {
        var initialCount = await GetDocumentCountAsync(this.CosmosContainer);
        initialCount.ShouldBe(0);

        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsInsertionStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        var finalCount = await GetDocumentCountAsync(this.CosmosContainer);
        finalCount.ShouldBe(8750);
    }

    /// <summary>
    /// Verifies that executing with dry-run and strategy flags does not insert records.
    /// </summary>
    [Fact(DisplayName = "Does not insert records when executed in dry-run mode")]
    public async Task Execute_WithDryRunAndStrategyFlag_DoesNotInsertRecords()
    {
        var initialCount = await GetDocumentCountAsync(this.CosmosContainer);
        initialCount.ShouldBe(0);

        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsInsertionStrategy", "--dry-run"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        var finalCount = await GetDocumentCountAsync(this.CosmosContainer);
        finalCount.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that executing with an invalid strategy flag returns a non-zero exit code.
    /// </summary>
    [Fact(DisplayName = "Returns non-zero exit code when executed with an invalid strategy flag")]
    public async Task Execute_WithInvalidStrategyFlag_ReturnsNonZeroExitCode()
    {
        var app = CreateAppTester(confirmMutation: false);
        var result = await app.RunAsync(["--strategy", "NonExistentStrategy"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(1);
    }

    /// <summary>
    /// Verifies that executing with help flag returns zero exit code.
    /// </summary>
    [Fact(DisplayName = "Returns zero exit code when executed with help flag")]
    public async Task Execute_WithHelpFlag_ReturnsZero()
    {
        var app = CreateAppTester(confirmMutation: false);
        var result = await app.RunAsync(["--help"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that executing with dry-run flag shows dry-run mode is enabled.
    /// </summary>
    [Fact(DisplayName = "Shows dry-run mode is enabled when executed with dry-run flag")]
    public async Task Execute_WithDryRunFlag_ShowsDryRunModeEnabled()
    {
        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsInsertionStrategy", "--dry-run"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that executing with a valid strategy creates database and container.
    /// </summary>
    [Fact(DisplayName = "Creates database and container when executed with a valid strategy")]
    public async Task Execute_WithValidStrategy_CreatesDatabaseAndContainer()
    {
        var ct = TestContext.Current.CancellationToken;

        var app = CreateAppTester(confirmMutation: true);
        var result = await app.RunAsync(["--strategy", "SampleRecordsInsertionStrategy", "--dry-run"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        var response = await this.CosmosContainer.ReadContainerAsync(cancellationToken: ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
