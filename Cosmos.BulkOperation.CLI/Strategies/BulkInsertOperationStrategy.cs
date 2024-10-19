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
    /// Base bulk record insertion strategy.
    /// </summary>
    /// <typeparam name="TRecord">The type of records to insert.</typeparam>
    /// <typeparam name="TPartitionKeyType">The partition key type</typeparam>
    /// <inheritdoc cref="BaseBulkOperationStrategy{TRecord, TPartitionKeyType}"/>
    public abstract class BulkInsertOperationStrategy<TRecord, TPartitionKeyType> : BaseBulkOperationStrategy<TRecord, TPartitionKeyType>
        where TRecord : class
        where TPartitionKeyType : PartitionKeyType
    {
        protected BulkInsertOperationStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings, bool useSystemTextJson = false)
            : base(cosmosSettings, containerSettings, useSystemTextJson)
        {
        }

        protected Task Insert(TRecord item, PartitionKey partitionKeyValue, ItemRequestOptions itemRequestOptions = null, CancellationToken ct = default)
            => Container
                .CreateItemAsync(item, partitionKeyValue, itemRequestOptions, ct)
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

        protected void QueueInsertionOperationTasks(IEnumerable<TRecord> recordsToInsert, Func<TRecord, TPartitionKeyType> partionKeyAccessor, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(recordsToInsert);
            if (!recordsToInsert.Any())
            {
                return;
            }

#pragma warning disable IDE0018 // Inline variable declaration
            List<Func<Task>> patchTasksForPartitionKey;
#pragma warning restore IDE0018 // Inline variable declaration

            foreach (var partition in recordsToInsert.GroupBy(r => partionKeyAccessor(r)))
            {
                if (!PartitionedBulkTasks.TryGetValue(partition.Key, out patchTasksForPartitionKey))
                {
                    patchTasksForPartitionKey = [];
                    PartitionedBulkTasks.Add(partition.Key, patchTasksForPartitionKey);
                }

                foreach (var record in partition)
                {
                    patchTasksForPartitionKey.Add(() => Insert(record, partition.Key.UnwrapPartitionKey(), null, ct));
                    TotalOperationsCount++;
                }
            }
        }
    }
}
