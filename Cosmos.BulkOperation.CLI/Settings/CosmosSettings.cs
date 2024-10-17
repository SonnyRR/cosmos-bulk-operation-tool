using Destructurama.Attributed;
using Microsoft.Azure.Cosmos;
using System;

namespace Cosmos.BulkOperation.CLI.Settings
{
    /// <summary>
    /// Cosmos DB connection settings.
    /// </summary>
    public class CosmosSettings
    {
        /// <summary>
        /// The Cosmos resource authorization key.
        /// </summary>
        [LogMasked]
        public string AuthorizationKey { get; set; }

        /// <summary>
        /// The database on which you want to perform operations.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// The Cosmos resource identifier.
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Maximum number of retries in the case where the request fails
        /// because the Azure Cosmos DB service has applied rate limiting on the client.
        /// </summary>
        public int MaxRetryAttemptsOnRateLimitedRequests { get; set; }

        /// <summary>
        /// Maximum retry time in seconds for the Azure Cosmos DB service.
        /// </summary>
        public TimeSpan MaxRetryWaitTimeOnRateLimitedRequests { get; set; }

        /// <summary>
        /// Request timeout in seconds when connecting to the Azure Cosmos DB service.
        /// </summary>
        /// <remarks>
        /// The number specifies the time to wait for response to come back from network peer.
        /// </remarks>
        public TimeSpan RequestTimeOut { get; set; }

        /// <summary>
        /// Default set of query request options
        /// </summary>
        public QueryRequestOptions QueryRequestOptions { get; set; }
    }
}
