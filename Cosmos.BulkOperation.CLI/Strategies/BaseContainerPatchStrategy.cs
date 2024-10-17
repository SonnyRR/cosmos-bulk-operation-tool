using Cosmos.BulkOperation.CLI.Handlers;
using Cosmos.BulkOperation.CLI.Settings;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Cosmos.BulkOperation.CLI.Strategies
{
    /// <summary>
    /// Represents a base container patching strategy with some common methods & properties.
    /// </summary>
    /// <typeparam name="TRecord">The container's item model.</typeparam>
    /// <inheritdoc cref="IContainerPatchStrategy"/>
    public abstract class BaseContainerPatchStrategy<TRecord> : IContainerPatchStrategy where TRecord : class
    {
        protected const byte MAX_OPERATIONS_PER_PATCH = 10;

        /// <summary>
        /// The default request options for PATCH requests. We don't need the returned resource from the request, so
        /// we can optimize the network traffic by saying that we're not interested in the resource, returned in the response.
        /// </summary>
        /// <remarks>
        /// For more information see: https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/best-practice-dotnet#best-practices-for-write-heavy-workloads
        /// </remarks>
        protected readonly PatchItemRequestOptions patchItemRequestOptions = new() { EnableContentResponseOnWrite = false };

        protected int CompletedPatchTasksCount;
        protected int TotalPatchOperationsCount;

        /// <summary>
        /// Sets up the strategy's database & container settings.
        /// </summary>
        /// <param name="cosmosSettings">The cosmos settings.</param>
        /// <param name="containerSettings">The container's settings.</param>
        /// <param name="useSystemTextJson">Flag for using the System.Text.Json instead of NewtonsoftJson.</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected BaseContainerPatchStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings, bool useSystemTextJson = false)
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
        /// Patch tasks, grouped by partition key.
        /// </summary>
        protected Dictionary<string, List<Func<Task>>> UserPatchTasks { get; set; } = [];

        public virtual async Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default)
        {
            Log.Information("Total patch operations queued: {@PatchesCount}", TotalPatchOperationsCount);

            if (UserPatchTasks.Count > 0)
            {
                await EvaluatePatchesAsync(dryRun);
                UserPatchTasks.Clear();
            }

            Log.Information("Total completed tasks: {@CompletedTasks}", CompletedPatchTasksCount);
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
                .AddCustomHandlers(new LoggingRequestHandler(), new ThrottlingRequestHandler());

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
        /// Evaluates the queued patch operations.
        /// </summary>
        /// <param name="dryRun">A flag indicating if the operations should actually reach the Cosmos resource</param>
        protected async Task EvaluatePatchesAsync(bool dryRun)
        {
            if (dryRun)
            {
                return;
            }

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
            await Task.WhenAll(UserPatchTasks.Select(async kvp => await Task.WhenAll(kvp.Value.Select(v => v()))));
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

        /// <summary>
        /// Patches a Cosmos DB record with a maximum of 10 operations.
        /// </summary>
        /// <param name="recordId">The record's unique identifier.</param>
        /// <param name="partitionKeyValue">The partition key value.</param>
        /// <param name="operations">A list of patch operations that need to be applied to the record.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>An instance of <see cref="Task"/>.</returns>
        protected virtual Task Patch(string recordId, string partitionKeyValue, IReadOnlyList<PatchOperation> operations, CancellationToken ct = default)
            => Container
                .PatchItemAsync<TRecord>(
                    recordId,
                    new PartitionKey(partitionKeyValue),
                    operations,
                    patchItemRequestOptions,
                    ct)
                .ContinueWith(task =>
                {
                    HttpStatusCode statusCode = HttpStatusCode.MisdirectedRequest;
                    if (task.IsCompletedSuccessfully)
                    {
                        Interlocked.Increment(ref CompletedPatchTasksCount);
                        var response = task.Result;

                        // The batches referenced here are not the batches, which the Cosmos DB SDK creates
                        // behind the scenes when Bulk operations are enabled. Those batches can contain up to 100
                        // operations or 2MB of data. This means that an actual cosmos batch can contain 10 of these tasks.
                        // (10 TPL tasks w/ 10 max operations per task)
                        Log.Information("({@Count}) Processed batch for: '{@RecordId}' - HTTP {@Status} | RU: {@RU}", CompletedPatchTasksCount, recordId, (int)response.StatusCode, response.RequestCharge);
                        statusCode = task.Result.StatusCode;
                    }
                    else if (task.Exception?.InnerException is CosmosException ex)
                    {
                        Log.Error("Failed patch batch request: {@Message} | JSON: {@Json:j}", ex.Message, ex.Diagnostics);
                        statusCode = ex.StatusCode;
                    }

                    HttpResponses.AddOrUpdate(statusCode, 1, (_, old) => old + 1);
                    task.Dispose();
                },
                ct);

        /// <summary>
        /// Queues Cosmos DB record patch tasks into an associative array, whose key is the partition key value.
        /// </summary>
        /// <param name="patchOperations">The patch operations.</param>
        /// <param name="recordId">The record's unique identifier.</param>
        /// <param name="partitionKey">The partition key value.</param>
        /// <param name="entityType">The entity type.</param>
        /// <param name="recordName">The record's name or title.</param>
        /// <param name="ct">The cancellation token.</param>
        protected void QueuePatchOperationTasks(
            List<PatchOperation> patchOperations,
            string recordId,
            string partitionKey,
            string entityType,
            string recordName = null,
            CancellationToken ct = default)
        {
            if (patchOperations.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(recordName))
                {
                    Log.Debug("Queuing patches for '{@EntityType}': '{@Id}'| Name: '{@RecordName}' | Part. Key: '{@PartitionKey}' | Patch operations: {@PatchOpCount}",
                        entityType,
                        recordId,
                        recordName,
                        partitionKey,
                        patchOperations.Count);
                }
                else
                {
                    Log.Debug("Queuing patches for '{@EntityType}': '{@Id}' | Part. Key: '{@PartitionKey}' | Patch operations: {@PatchOpCount}",
                        entityType,
                        recordId,
                        partitionKey,
                        patchOperations.Count);
                }

#pragma warning disable IDE0018 // Inline variable declaration
                List<Func<Task>> patchTasksForPartitionKey;
#pragma warning restore IDE0018 // Inline variable declaration

                if (!UserPatchTasks.TryGetValue(partitionKey, out patchTasksForPartitionKey))
                {
                    patchTasksForPartitionKey = [];
                    UserPatchTasks.Add(partitionKey, patchTasksForPartitionKey);
                }

                // https://learn.microsoft.com/en-us/azure/cosmos-db/partial-document-update-faq#is-there-a-limit-to-the-number-of-partial-document-update-operations-
                foreach (var patchOperationsBatch in patchOperations.Chunk(MAX_OPERATIONS_PER_PATCH))
                {
                    patchTasksForPartitionKey.Add(() => Patch(recordId, partitionKey, patchOperationsBatch, ct));
                }

                TotalPatchOperationsCount += patchOperations.Count;
            }
        }
    }
}
