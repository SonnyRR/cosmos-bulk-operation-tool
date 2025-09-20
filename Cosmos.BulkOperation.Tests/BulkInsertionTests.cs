using Cosmos.BulkOperation.CLI;

namespace Cosmos.BulkOperation.Tests;

public class BulkInsertionTests : IClassFixture<CosmosDatabaseFixture>
{
    [Fact]
    public async Task Test1()
    {
        // Arrange
        using var a = Utils.UseEnvironment("Testing");

        await Program.Main(null);
    }
}