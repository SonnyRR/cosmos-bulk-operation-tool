using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cosmos.BulkOperation.CLI.Handlers;
using Cosmos.BulkOperation.CLI.Settings;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Serilog;

namespace Cosmos.BulkOperation.CLI.Strategies
{
    /// <summary>
    /// Represents a base bulk operation strategy with some common methods &amp; properties.
    /// </summary>
    /// <typeparam name="TRecord">The container's item model.</typeparam>
    /// <inheritdoc cref="IBulkOperationStrategy"/>
    public abstract class BaseBulkOperationStrategy<TRecord, TPartitionKey> : IBulkOperationStrategy
        where TRecord : class
        where TPartitionKey : PartitionKeyType
    {
        protected int CompletedTasksCount;
        protected int TotalOperationsCount;

        /// <summary>
        /// Sets up the strategy's database &amp; container settings.
        /// </summary>
        /// <param name="cosmosSettings">The cosmos settings.</param>
        /// <param name="containerSettings">The container's settings.</param>
        /// <param name="useSystemTextJson">Flag for using the System.Text.Json instead of NewtonsoftJson.</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected BaseBulkOperationStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings, bool useSystemTextJson = false)
        {
            CosmosSettings = cosmosSettings ?? throw new ArgumentNullException(nameof(cosmosSettings));
            ContainerSettings = containerSettings ?? throw new ArgumentNullException(nameof(containerSettings));

            CosmosClient = GetCosmosClient(cosmosSettings, useSystemTextJson);
        }

        /// <summary>
        /// Retrieves the Cosmos DB container instance.
        /// </summary>
        protected Container Container => Database.GetContainer(ContainerSettings.ContainerName);

        /// <summary>
        /// The Cosmos DB container's settings.
        /// </summary>
        protected ContainerSettings ContainerSettings { get; set; }

        /// <summary>
        /// The Cosmos DB client.
        /// </summary>
        protected CosmosClient CosmosClient { get; set; }

        /// <summary>
        /// The Cosmos DB cofniguration.
        /// </summary>
        protected CosmosSettings CosmosSettings { get; set; }

        /// <summary>
        /// The Cosmos DB database.
        /// </summary>
        protected Database Database => CosmosClient.GetDatabase(CosmosSettings.DatabaseName);

        /// <summary>
        /// Total HTTP responses received, grouped by HTTP status codes.
        /// </summary>
        protected ConcurrentDictionary<HttpStatusCode, int> HttpResponses { get; set; } = [];

        /// <summary>
        /// Bulk tasks, grouped by partition key.
        /// </summary>
        protected Dictionary<TPartitionKey, List<Func<Task>>> PartitionedBulkTasks { get; set; } = [];

        public virtual async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
        {
            Log.Information("Total operations queued: {@PatchesCount}", TotalOperationsCount);

            if (PartitionedBulkTasks.Count > 0)
            {
                await EvaluateBatchTasksAsync(dryRun);
                PartitionedBulkTasks.Clear();
            }

            Log.Information("Total completed tasks: {@CompletedTasks}", CompletedTasksCount);
            foreach (var response in HttpResponses)
            {
                Log.Information("{@StatusCode} - {@Count}", response.Key, response.Value);
            }
        }

        /// <summary>
        /// Retrieves an instance of a Cosmos DB client.
        /// </summary>
        /// <param name="settings">The Cosmos DB settings.</param>
        /// <returns>An instance of <see cref="CosmosClient"/>.</returns>
        protected static CosmosClient GetCosmosClient(CosmosSettings settings, bool useSystemTextJson = false)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var clientBuilder = new CosmosClientBuilder(settings.EndpointUrl, settings.AuthorizationKey)
                .WithBulkExecution(true)
                .WithRequestTimeout(settings.RequestTimeOut)
                // Let the SDK retry it first, then fallback to a custom policy
                .WithThrottlingRetryOptions(settings.MaxRetryWaitTimeOnRateLimitedRequests, settings.MaxRetryAttemptsOnRateLimitedRequests)
                .AddCustomHandlers(new LoggingRequestHandler(), new ThrottlingRequestHandler())
                .WithConnectionModeDirect()
                .WithLimitToEndpoint(true);

            clientBuilder = useSystemTextJson
                ? clientBuilder.WithSystemTextJsonSerializerOptions(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                        new JsonStringEnumConverter(new JsonPascalCaseNamingPolicy())
                    }
                })
                : clientBuilder.WithSerializerOptions(new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                });

            var client = clientBuilder.Build();
            Log.Information("Created a Cosmos DB client with the following settings: {@Settings:lj}", settings);
            Log.Information("Configured the Cosmos DB client with the following options: {@Options:j}", client.ClientOptions);

            return client;
        }

        /// <summary>
        /// Evaluates the queued bulk operations.
        /// </summary>
        /// <param name="dryRun">A flag indicating if the operations should actually reach the Cosmos resource</param>
        protected async Task EvaluateBatchTasksAsync(bool dryRun)
        {
            if (dryRun)
            {
                return;
            }

            var semaphore = new SemaphoreSlim(5);

            // Create one worker task per partition key, each worker will coordinate tasks for all patch tasks for the current user.
            // If bulk support is enabled (which should be in this app) the SDK will create batches, all the concurrent operations
            // will be grouped by physical partition affinity and distributed across these batches. When a batch fills up, it gets
            // dispatched, and a new batch is created to be filled with more concurrent operations. Each batch will contain many
            // operations, so this greatly reduces the amount of back end requests. There could be many batches being dispatched in
            // parallel targeting different partitions, so the more evenly distributed the operations, the better results.
            // The batches that the SDK creates to optimize throughput have a current maximum of 2Mb or 100 operations per batch,
            // the smaller the documents, the greater the optimization that can be achieved (the bigger the documents, the more batches need to be used).
            //
            // See https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/
            //
            await Task.WhenAll(PartitionedBulkTasks.Select(async kvp =>
            {
                await semaphore.WaitAsync();

                try
                {
                    await Task.WhenAll(kvp.Value.Select(v => v()));
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        /// <summary>
        /// Retrieves the feed iterator of the container that this strategy has been set up to work with.
        /// </summary>
        /// <param name="queryRequestOptions">
        /// Custom query request options, if not provided it will fallback to the global default query request options.
        /// </param>
        /// <returns>An instance of <see cref="FeedIterator{TRecord}"/></returns>
        protected FeedIterator<TRecord> GetFeedIterator(QueryRequestOptions queryRequestOptions = null) => Container
            .GetItemQueryIterator<TRecord>(ContainerSettings.Query.Value, requestOptions: queryRequestOptions ?? CosmosSettings.QueryRequestOptions);
    }
}
