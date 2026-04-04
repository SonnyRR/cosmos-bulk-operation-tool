using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

using Testcontainers.CosmosDb;
using Testcontainers.Xunit;

using Xunit.Sdk;

namespace Cosmos.BulkOperation.IntegrationTests.Fixtures;

/// <summary>
/// A xUnit fixture with a pre-defined Cosmos Database container.
/// </summary>
public class CosmosDatabaseFixture : ContainerFixture<CosmosDbBuilder, CosmosDbContainer>
{
    private CosmosClient client;

    /// <summary>
    /// Creates a new instance of the <see cref="CosmosDatabaseFixture" /> class.
    /// </summary>
    public CosmosDatabaseFixture(IMessageSink messageSink) : base(messageSink)
    {
    }

    /// <inheritdoc />
    protected override CosmosDbBuilder Configure()
        => new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
            .WithPortBinding(37125, 8081)
            .WithPortBinding(37126, 1234)
            .WithReuse(false)
            .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "2")
            .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "false")
            .WithEnvironment("ENABLE_EXPLORER", "true");

    /// <inheritdoc/>
    protected override async ValueTask InitializeAsync() => await base.InitializeAsync();

    /// <summary>
    /// Retrieves a Cosmos client for the test container.
    /// </summary>
    /// <returns>An instance of <see cref="CosmosClient"/>.</returns>
    public CosmosClient GetClient()
    {
        this.client ??= new CosmosClientBuilder(this.Container.GetConnectionString())
            .WithBulkExecution(true)
            .WithRequestTimeout(TimeSpan.FromSeconds(15))
            .WithConnectionModeGateway()
            .WithLimitToEndpoint(true)
            .WithHttpClientFactory(() => new HttpClient(new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }))
            .Build();

        return this.client;
    }
}
