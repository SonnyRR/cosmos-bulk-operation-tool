using Cosmos.BulkOperation.Tests;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Testcontainers.CosmosDb;
using Testcontainers.Xunit;
using Xunit.Sdk;

[assembly: AssemblyFixture(typeof(CosmosDatabaseFixture))]

namespace Cosmos.BulkOperation.Tests
{
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
        protected override CosmosDbBuilder Configure(CosmosDbBuilder builder)
        {
            return builder
                .WithPortBinding(37122, 8081)
                .WithPortBinding(10250, 10250)
                .WithPortBinding(10251, 10251)
                .WithPortBinding(10252, 10252)
                .WithPortBinding(10253, 10253)
                .WithPortBinding(10254, 10254)
                .WithPortBinding(10255, 10255)
                .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "true");
        }

        public CosmosClient GetClient()
        {
            this.client ??= new CosmosClientBuilder(this.Container.GetConnectionString())
                .WithBulkExecution(true)
                .WithRequestTimeout(TimeSpan.FromSeconds(15))
                .WithConnectionModeDirect()
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
}