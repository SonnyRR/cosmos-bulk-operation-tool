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

namespace Cosmos.BulkOperation.CLI.Strategies;

/// <summary>
/// Base bulk record insertion strategy.
/// </summary>
/// <typeparam name="TRecord">The type of records to insert.</typeparam>
/// <typeparam name="TPartitionKeyType">The partition key type</typeparam>

public abstract class BulkInsertOperationStrategy<TRecord, TPartitionKeyType> : BaseBulkOperationStrategy<TRecord, TPartitionKeyType>
    where TRecord : class
    where TPartitionKeyType : PartitionKeyType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkInsertOperationStrategy{TRecord, TPartitionKeyType}"/> class.
    /// </summary>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    /// <param name="useSystemTextJson">Flag for using System.Text.Json.</param>
    protected BulkInsertOperationStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings, bool useSystemTextJson = false)
        : base(cosmosSettings, containerSettings, useSystemTextJson)
    {
    }

    /// <summary>
    /// Inserts a record into Cosmos DB.
    /// </summary>
    /// <param name="item">The record to insert.</param>
    /// <param name="partitionKeyValue">The partition key value.</param>
    /// <param name="itemRequestOptions">The request options.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task Insert(TRecord item, PartitionKey partitionKeyValue, ItemRequestOptions itemRequestOptions = null, CancellationToken ct = default)
        => this.Container
            .CreateItemAsync(item, partitionKeyValue, itemRequestOptions, ct)
            .ContinueWith(task =>
            {
                HttpStatusCode statusCode = HttpStatusCode.MisdirectedRequest;
                if (task.IsCompletedSuccessfully)
                {
                    Interlocked.Increment(ref this.completedTasksCount);
                    var response = task.Result;

                    Log.Information("({@Count}) Processed batch - HTTP {@Status} | RU: {@RU}", this.completedTasksCount, (int)response.StatusCode, response.RequestCharge);
                    statusCode = task.Result.StatusCode;
                }
                else if (task.Exception?.InnerException is CosmosException ex)
                {
                    Log.Error("Failed insert batch request: {@Message}", ex.Message);
                    statusCode = ex.StatusCode;
                }

                this.HttpResponses.AddOrUpdate(statusCode, 1, (_, old) => old + 1);
                task.Dispose();
            },
            ct);

    /// <summary>
    /// Queues insertion tasks for bulk insert operations.
    /// </summary>
    /// <param name="recordsToInsert">The records to insert.</param>
    /// <param name="partionKeyAccessor">A delegate to access the partition key value.</param>
    /// <param name="ct">A cancellation token.</param>
    protected void QueueInsertionOperationTasks(IEnumerable<TRecord> recordsToInsert, Func<TRecord, TPartitionKeyType> partionKeyAccessor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(recordsToInsert);
        if (!recordsToInsert.Any())
        {
            return;
        }

#pragma warning disable IDE0018 // Inline variable declaration
        List<Func<Task>> insertTasksForPartitionKey;
#pragma warning restore IDE0018 // Inline variable declaration

        foreach (var partition in recordsToInsert.GroupBy(r => partionKeyAccessor(r)))
        {
            if (!this.PartitionedBulkTasks.TryGetValue(partition.Key, out insertTasksForPartitionKey))
            {
                insertTasksForPartitionKey = [];
                this.PartitionedBulkTasks.Add(partition.Key, insertTasksForPartitionKey);
            }

            foreach (var record in partition)
            {
                insertTasksForPartitionKey.Add(() => this.Insert(record, partition.Key.UnwrapPartitionKey(), null, ct));
                this.totalOperationsCount++;
            }
        }
    }
}
