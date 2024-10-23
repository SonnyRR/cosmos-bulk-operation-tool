using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cosmos.BulkOperation.CLI.Extensions;
using Cosmos.BulkOperation.CLI.Settings;
using Microsoft.Azure.Cosmos;
using Serilog;

namespace Cosmos.BulkOperation.CLI.Strategies
{
    /// <summary>
    /// Base bulk record patching strategy.
    /// </summary>
    /// <typeparam name="TRecord">The type of records to insert.</typeparam>
    /// <typeparam name="TPartitionKeyType">The partition key type</typeparam>
    /// <inheritdoc cref="BaseBulkOperationStrategy{TRecord, TPartitionKeyType}"/>
    public abstract class BulkPatchOperationStrategy<TRecord, TPartitionKeyType> : BaseBulkOperationStrategy<TRecord, TPartitionKeyType>
        where TRecord : class
        where TPartitionKeyType : PartitionKeyType
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

        protected BulkPatchOperationStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings, bool useSystemTextJson = false)
        : base(cosmosSettings, containerSettings, useSystemTextJson)
        {
        }

        /// <summary>
        /// Patches a Cosmos DB record with a maximum of 10 operations.
        /// </summary>
        /// <param name="recordId">The record's unique identifier.</param>
        /// <param name="partitionKeyValue">The partition key value.</param>
        /// <param name="operations">A list of patch operations that need to be applied to the record.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>An instance of <see cref="Task"/>.</returns>
        protected virtual Task Patch(string recordId, PartitionKey partitionKeyValue, IReadOnlyList<PatchOperation> operations, CancellationToken ct = default)
            => Container
                .PatchItemAsync<TRecord>(
                    recordId,
                    partitionKeyValue,
                    operations,
                    patchItemRequestOptions,
                    ct)
                .ContinueWith(task =>
                {
                    HttpStatusCode statusCode = HttpStatusCode.MisdirectedRequest;
                    if (task.IsCompletedSuccessfully)
                    {
                        Interlocked.Increment(ref CompletedTasksCount);
                        var response = task.Result;

                        // The batches referenced here are not the batches, which the Cosmos DB SDK creates
                        // behind the scenes when Bulk operations are enabled. Those batches can contain up to 100
                        // operations or 2MB of data. This means that an actual cosmos batch can contain 10 of these tasks.
                        // (10 TPL tasks w/ 10 max operations per task)
                        Log.Information("({@Count}) Processed batch for: '{@RecordId}' - HTTP {@Status} | RU: {@RU}", CompletedTasksCount, recordId, (int)response.StatusCode, response.RequestCharge);
                        statusCode = task.Result.StatusCode;
                    }
                    else if (task.Exception?.InnerException is CosmosException ex)
                    {
                        Log.Error("Failed patch batch request: {@Message}", ex.Message);
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
            TPartitionKeyType partitionKey,
            CancellationToken ct = default)
        {
            if (patchOperations.Count > 0)
            {
#pragma warning disable IDE0018 // Inline variable declaration
                List<Func<Task>> patchTasksForPartitionKey;
#pragma warning restore IDE0018 // Inline variable declaration

                if (!PartitionedBulkTasks.TryGetValue(partitionKey, out patchTasksForPartitionKey))
                {
                    patchTasksForPartitionKey = [];
                    PartitionedBulkTasks.Add(partitionKey, patchTasksForPartitionKey);
                }

                // https://learn.microsoft.com/en-us/azure/cosmos-db/partial-document-update-faq#is-there-a-limit-to-the-number-of-partial-document-update-operations-
                foreach (var patchOperationsBatch in patchOperations.Chunk(MAX_OPERATIONS_PER_PATCH))
                {
                    patchTasksForPartitionKey.Add(() => Patch(recordId, partitionKey.UnwrapPartitionKey(), patchOperationsBatch, ct));
                }

                TotalOperationsCount += patchOperations.Count;
            }
        }
    }
}