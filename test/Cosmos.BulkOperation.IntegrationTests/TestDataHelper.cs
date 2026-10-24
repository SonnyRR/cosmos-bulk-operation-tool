using System.Threading;
using System.Threading.Tasks;

using Cosmos.BulkOperation.TestUtilities;

using Spectre.Console.Testing;

namespace Cosmos.BulkOperation.IntegrationTests;

/// <summary>
/// Provides helper methods for test data operations.
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// Seeds the Cosmos container with test records via the CLI (same approach as BulkInsertionTests).
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedRecordsAsync(CancellationToken ct = default)
    {
        var console = TestHarness.CreateTestConsole(confirmMutation: true);

        var app = TestHarness.CreateTester("Testing", console);
        await app.RunAsync(["--strategy", "SampleRecordsInsertionStrategy"], ct);
    }
}
