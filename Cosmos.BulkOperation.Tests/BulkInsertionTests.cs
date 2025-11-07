using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.Tests.Fixtures;
using Microsoft.Azure.Cosmos;
using Shouldly;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Cosmos.BulkOperation.Tests;

/// <summary>
/// Integration tests for bulk insertion operations.
/// </summary>
public class BulkInsertionTests : IAsyncLifetime
{
    /// <summary>
    /// The Cosmos Database fixture.
    /// </summary>
    private readonly CosmosDatabaseFixture fixture;

    /// <summary>
    /// Constructs a new instance of <see cref="BulkInsertionTests"/>.
    /// </summary>
    /// <param name="fixture">The Cosmos Database fixture.</param>
    public BulkInsertionTests(CosmosDatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await this.fixture.GetClient()
            .CreateDatabaseIfNotExistsAsync("sandbox", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Test1()
    {
        // Arrange
        await Program.Main(["--strategy", "SampleRecordsInsertionStrategy"]);

        // Assert
        var c = this.fixture.GetClient().GetDatabase("sandbox").GetContainer("Runs");

        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");

        var iterator = c.GetItemQueryIterator<int>(query);

        int count = 0;
        while (iterator.HasMoreResults)
        {
            foreach (var result in await iterator.ReadNextAsync(TestContext.Current.CancellationToken))
            {
                count += result;
            }
        }

        count.ShouldBe(500);
    }
}