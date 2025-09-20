using Testcontainers.CosmosDb;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace Cosmos.BulkOperation.Tests
{
    /// <summary>
    /// A xUnit fixture with a pre-defined Cosmos Database container.
    /// </summary>
    /// <inheritdoc />
    public sealed class CosmosDatabaseFixture : ContainerFixture<CosmosDbBuilder, CosmosDbContainer>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CosmosDatabaseFixture" /> class.
        /// </summary>
        public CosmosDatabaseFixture(IMessageSink messageSink) : base(messageSink)
        {
        }

        /// <inheritdoc />
        protected override CosmosDbBuilder Configure(CosmosDbBuilder builder)
        {
            return builder.WithPortBinding(8081, 4387);
        }
    }
}