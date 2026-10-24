using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.CLI.Strategies;
using Cosmos.BulkOperation.Samples;

using Microsoft.Azure.Cosmos;

namespace Cosmos.BulkOperation.UnitTests.Strategies;

/// <summary>
/// A test double for <see cref="BulkInsertOperationStrategy{TRecord, TPartitionKeyType}"/>, exposing the
/// queueing state and methods. Internal, so the <see cref="StrategyProvider"/> does not attempt to discover it.
/// </summary>
internal sealed class TestInsertionStrategy : BulkInsertOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestInsertionStrategy"/> class.
    /// </summary>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    public TestInsertionStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
        : base(cosmosSettings, containerSettings)
    {
    }

    /// <summary>
    /// The queued bulk tasks, grouped by partition key.
    /// </summary>
    public IReadOnlyDictionary<PartitionKeyType.StringPartitionKey, List<Func<Task>>> QueuedTasks
        => this.PartitionedBulkTasks;

    /// <summary>
    /// The total number of operations queued for execution.
    /// </summary>
    public int TotalOperationsCount => this.totalOperationsCount;

    /// <summary>
    /// Queues insertion tasks for the provided records.
    /// </summary>
    /// <param name="records">The records to insert.</param>
    public void QueueInsertions(IEnumerable<Run> records)
        => this.QueueInsertionOperationTasks(records, run => new(run.UserId));
}

/// <summary>
/// A test double for <see cref="BulkPatchOperationStrategy{TRecord, TPartitionKeyType}"/>, exposing the
/// queueing state and methods. Internal, so the <see cref="StrategyProvider"/> does not attempt to discover it.
/// </summary>
internal sealed class TestPatchStrategy : BulkPatchOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestPatchStrategy"/> class.
    /// </summary>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    public TestPatchStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
        : base(cosmosSettings, containerSettings)
    {
    }

    /// <summary>
    /// The queued bulk tasks, grouped by partition key.
    /// </summary>
    public IReadOnlyDictionary<PartitionKeyType.StringPartitionKey, List<Func<Task>>> QueuedTasks
        => this.PartitionedBulkTasks;

    /// <summary>
    /// The total number of operations queued for execution.
    /// </summary>
    public int TotalOperationsCount => this.totalOperationsCount;

    /// <summary>
    /// Queues patch tasks for the provided record.
    /// </summary>
    /// <param name="patchOperations">The patch operations to apply to the record.</param>
    /// <param name="record">The record to patch.</param>
    public void QueuePatchOperationTasks(IEnumerable<PatchOperation> patchOperations, Run record)
        => this.QueuePatchOperationTasks([.. patchOperations], record.Id.ToString(), new(record.UserId));
}

/// <summary>
/// A test double for <see cref="BulkDeleteOperationStrategy{TRecord, TPartitionKeyType}"/>, exposing the
/// queueing state and methods. Internal, so the <see cref="StrategyProvider"/> does not attempt to discover it.
/// </summary>
internal sealed class TestDeletionStrategy : BulkDeleteOperationStrategy<Run, PartitionKeyType.StringPartitionKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestDeletionStrategy"/> class.
    /// </summary>
    /// <param name="cosmosSettings">The Cosmos DB settings.</param>
    /// <param name="containerSettings">The container settings.</param>
    public TestDeletionStrategy(CosmosSettings cosmosSettings, ContainerSettings containerSettings)
        : base(cosmosSettings, containerSettings)
    {
    }

    /// <summary>
    /// The queued bulk tasks, grouped by partition key.
    /// </summary>
    public IReadOnlyDictionary<PartitionKeyType.StringPartitionKey, List<Func<Task>>> QueuedTasks
        => this.PartitionedBulkTasks;

    /// <summary>
    /// The total number of operations queued for execution.
    /// </summary>
    public int TotalOperationsCount => this.totalOperationsCount;

    /// <summary>
    /// Queues deletion tasks for the provided records.
    /// </summary>
    /// <param name="records">The records to delete.</param>
    public void QueueDeletions(IEnumerable<Run> records)
        => this.QueueDeletionOperationTasks(records, record => record.Id.ToString(), record => new(record.UserId));
}
