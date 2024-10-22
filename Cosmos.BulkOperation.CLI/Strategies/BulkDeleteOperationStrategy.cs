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
    /// Base bulk record deletion strategy.
    /// </summary>
    /// <typeparam name="TRecord">The type of records to insert.</typeparam>
    /// <typeparam name="TPartitionKeyType">The partition key type</typeparam>
    /// <inheritdoc cref="BaseBulkOperationStrategy{TRecord, TPartitionKeyType}"/>
    public abstract class BulkDeleteOperationStrategy<TRecord, TPartitionKeyType> : BaseBulkOperationStrategy<TRecord, TPartitionKeyType>
        where TRecord : class
        where TPartitionKeyType : PartitionKeyType
    {
        protected BulkDeleteOperationStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings, bool useSystemTextJson = false)
            : base(cosmosSettings, containerSettings, useSystemTextJson)
        {
        }

        /// <summary>
        /// Deletes a Cosmos DB record.
        /// </summary>
        /// <param name="recordId">The record's unique identifier.</param>
        /// <param name="partitionKeyValue">The partition key value.</param>
        /// <param name="itemRequestOptions">The query request options.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>An instance of <see cref="Task"/>.</returns>
        protected Task Delete(string recordId, PartitionKey partitionKeyValue, ItemRequestOptions itemRequestOptions = null, CancellationToken ct = default)
            => Container
                .DeleteItemAsync<TRecord>(recordId, partitionKeyValue, itemRequestOptions, ct)
                .ContinueWith(task =>
                {
                    HttpStatusCode statusCode = HttpStatusCode.MisdirectedRequest;
                    if (task.IsCompletedSuccessfully)
                    {
                        Interlocked.Increment(ref CompletedTasksCount);
                        var response = task.Result;

                        Log.Information("({@Count}) Processed batch - HTTP {@Status} | RU: {@RU}", CompletedTasksCount, (int)response.StatusCode, response.RequestCharge);
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
        /// Queues Cosmos DB record delete tasks into an associative array, whose key is the partition key value.
        /// </summary>
        /// <param name="recordsToDelete">The records that will be deleted.</param>
        /// <param name="idKeyAccessor">A delegate for accessing the identifier key value of a record.</param>
        /// <param name="partionKeyAccessor">A delegate for accessing the partition key value.</param>
        /// <param name="ct">A cancellation token</param>
        protected void QueueDeletionOperationTasks(
            IEnumerable<TRecord> recordsToDelete,
            Func<TRecord, string> idKeyAccessor,
            Func<TRecord, TPartitionKeyType> partionKeyAccessor,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(recordsToDelete);
            if (!recordsToDelete.Any())
            {
                return;
            }

#pragma warning disable IDE0018 // Inline variable declaration
            List<Func<Task>> deleteTasksForPartitionKey;
#pragma warning restore IDE0018 // Inline variable declaration

            foreach (var partition in recordsToDelete.GroupBy(r => partionKeyAccessor(r)))
            {
                if (!PartitionedBulkTasks.TryGetValue(partition.Key, out deleteTasksForPartitionKey))
                {
                    deleteTasksForPartitionKey = [];
                    PartitionedBulkTasks.Add(partition.Key, deleteTasksForPartitionKey);
                }

                foreach (var record in partition)
                {
                    deleteTasksForPartitionKey.Add(() => Delete(idKeyAccessor(record), partition.Key.UnwrapPartitionKey(), null, ct));
                    TotalOperationsCount++;
                }
            }
        }
    }
}