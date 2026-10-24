using System;
using System.Threading.Tasks;

using Cosmos.BulkOperation.IntegrationTests.Fixtures;

using Microsoft.Azure.Cosmos;

using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace Cosmos.BulkOperation.IntegrationTests;

/// <summary>
/// Base class for CLI tests providing common test utilities.
/// </summary>
public abstract class BaseCosmosTest : IAsyncLifetime
{
    /// <summary>
    /// The name of the test database.
    /// </summary>
    protected const string TEST_DATABASE = "sandbox";

    /// <summary>
    /// The name of the test container.
    /// </summary>
    protected const string TEST_CONTAINER = "Runs";

    /// <summary>
    /// The Cosmos database fixture for test setup.
    /// </summary>
    protected readonly CosmosDatabaseFixture fixture;

    /// <summary>
    /// The Cosmos client instance.
    /// </summary>
    protected CosmosClient CosmosClient => this.fixture.GetClient();

    /// <summary>
    /// The Cosmos database instance.
    /// </summary>
    protected Database CosmosDatabase => this.CosmosClient.GetDatabase(TEST_DATABASE);

    /// <summary>
    /// The Cosmos container instance.
    /// </summary>
    protected Container CosmosContainer => this.CosmosDatabase.GetContainer(TEST_CONTAINER);

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCosmosTest"/> class.
    /// </summary>
    /// <param name="fixture">The Cosmos database fixture.</param>
    protected BaseCosmosTest(CosmosDatabaseFixture fixture) => this.fixture = fixture;

    /// <summary>
    /// Creates a CommandAppTester for testing CLI commands.
    /// </summary>
    /// <param name="confirmMutation">Whether to confirm mutation prompts (for destructive operations).</param>
    /// <returns>A configured CommandAppTester instance.</returns>
    protected static CommandAppTester CreateAppTester(bool confirmMutation = true)
    {
        var console = new TestConsole();

        console.Profile.Capabilities.Interactive = true;
        console.Profile.Capabilities.Ansi = true;

        if (confirmMutation)
        {
            console.Input.PushTextWithEnter("y");
        }

        return TestHarness.CreateTester("Testing", console);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await CleanupContainerAsync(this.CosmosContainer);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        await this.CosmosClient.CreateDatabaseIfNotExistsAsync(TEST_DATABASE, cancellationToken: ct);
        await this.CosmosDatabase.CreateContainerIfNotExistsAsync(new ContainerProperties(TEST_CONTAINER, "/userId"), cancellationToken: ct);
    }

    /// <summary>
    /// Deletes the specified container.
    /// </summary>
    /// <param name="container">The container to delete.</param>
    protected static Task CleanupContainerAsync(Container container)
        => container.DeleteContainerAsync(cancellationToken: TestContext.Current.CancellationToken);

    /// <summary>
    /// Gets the document count in the specified container.
    /// </summary>
    /// <param name="container">The container to count documents in.</param>
    /// <returns>The document count.</returns>
    protected static async Task<int> GetDocumentCountAsync(Container container)
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
        var iterator = container.GetItemQueryIterator<int>(query);

        var count = 0;
        while (iterator.HasMoreResults)
        {
            foreach (var result in await iterator.ReadNextAsync(TestContext.Current.CancellationToken))
            {
                count += result;
            }
        }

        return count;
    }
}
