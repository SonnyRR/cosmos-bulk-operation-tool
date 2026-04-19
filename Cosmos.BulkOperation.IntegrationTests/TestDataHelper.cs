using System.Threading;
using System.Threading.Tasks;

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
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Profile.Capabilities.Ansi = true;
        console.Input.PushTextWithEnter("y");

        var app = TestHarness.CreateTester("Testing", console);
        await app.RunAsync(["--strategy", "SampleRecordsInsertionStrategy"], ct);
    }
}
