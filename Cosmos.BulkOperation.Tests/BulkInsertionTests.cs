using Shouldly;

namespace Cosmos.BulkOperation.Tests;

public class BulkInsertionTests : IClassFixture<CosmosDatabaseFixture>
{
    private readonly CosmosDatabaseFixture fixture;

    public BulkInsertionTests(CosmosDatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Test1()
    {
        // Arrange
        using var a = Utils.UseEnvironment("Testing");

        await this.fixture.GetClient().CreateDatabaseIfNotExistsAsync("sandbox");
        // Act

        // Assert
        true.ShouldBe(true);
    }
}