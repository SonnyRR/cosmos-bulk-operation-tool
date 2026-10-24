using System;
using System.Linq;

using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.CLI.Settings;
using Cosmos.BulkOperation.Samples;

using Microsoft.Azure.Cosmos;

using Shouldly;

namespace Cosmos.BulkOperation.UnitTests.Strategies;

/// <summary>
/// Tests for the bulk task queueing logic of the operation strategies.
/// </summary>
public class QueueingTests
{
    /// <summary>
    /// Verifies that insertion records are grouped by their partition key.
    /// </summary>
    [Fact(DisplayName = "Groups insertion records by partition key and counts the operations")]
    public void QueueInsertions_GroupsRecordsByPartitionKeyAndCountsOperations()
    {
        var strategy = CreateInsertionStrategy();

        strategy.QueueInsertions(
        [
            CreateRun("user1"),
            CreateRun("user2"),
            CreateRun("user1"),
            CreateRun("user3"),
            CreateRun("user2")
        ]);

        strategy.QueuedTasks.Count.ShouldBe(3);
        strategy.QueuedTasks[new PartitionKeyType.StringPartitionKey("user1")].Count.ShouldBe(2);
        strategy.QueuedTasks[new PartitionKeyType.StringPartitionKey("user2")].Count.ShouldBe(2);
        strategy.QueuedTasks[new PartitionKeyType.StringPartitionKey("user3")].Count.ShouldBe(1);
        strategy.TotalOperationsCount.ShouldBe(5);
    }

    /// <summary>
    /// Verifies that multiple insert batches for the same partition accumulate into a single task list.
    /// </summary>
    [Fact(DisplayName = "Accumulates insertion tasks for the same partition key across batches")]
    public void QueueInsertions_ForSamePartitionKey_AccumulatesTasks()
    {
        var strategy = CreateInsertionStrategy();

        strategy.QueueInsertions([CreateRun("user1"), CreateRun("user1")]);
        strategy.QueueInsertions([CreateRun("user1")]);

        strategy.QueuedTasks.Count.ShouldBe(1);
        strategy.QueuedTasks[new PartitionKeyType.StringPartitionKey("user1")].Count.ShouldBe(3);
        strategy.TotalOperationsCount.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that an empty insertion batch queues nothing.
    /// </summary>
    [Fact(DisplayName = "Queues nothing when the insertion batch is empty")]
    public void QueueInsertions_WithEmptyBatch_QueuesNothing()
    {
        var strategy = CreateInsertionStrategy();

        strategy.QueueInsertions([]);

        strategy.QueuedTasks.Count.ShouldBe(0);
        strategy.TotalOperationsCount.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that a null insertion batch yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws ArgumentNullException when batching null insertion records")]
    public void QueueInsertions_WithNullBatch_ThrowsArgumentNullException()
    {
        var strategy = CreateInsertionStrategy();

        ((Action)(() => strategy.QueueInsertions(null))).ShouldThrow<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that patch operations are chunked into batches of a maximum of ten operations per record.
    /// </summary>
    [Fact(DisplayName = "Chunks patch operations into batches of a maximum of ten operations")]
    public void QueuePatchOperationTasks_WithMoreThanTenOperations_ChunksIntoBatches()
    {
        var strategy = CreatePatchStrategy();
        var record = CreateRun("user1");
        var patchOperations = Enumerable
            .Range(0, 25)
            .Select(i => PatchOperation.Replace($"/checkpoints/{i}/pinColor", Color.Black))
            .ToList();

        strategy.QueuePatchOperationTasks(patchOperations, record);

        strategy.QueuedTasks.Count.ShouldBe(1);
        strategy.QueuedTasks[new PartitionKeyType.StringPartitionKey("user1")].Count.ShouldBe(3);
        strategy.TotalOperationsCount.ShouldBe(25);
    }

    /// <summary>
    /// Verifies that empty patch operations queue nothing.
    /// </summary>
    [Fact(DisplayName = "Queues nothing when the patch operations are empty")]
    public void QueuePatchOperationTasks_WithEmptyOperations_QueuesNothing()
    {
        var strategy = CreatePatchStrategy();
        var record = CreateRun("user1");

        strategy.QueuePatchOperationTasks([], record);

        strategy.QueuedTasks.Count.ShouldBe(0);
        strategy.TotalOperationsCount.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that deletion records are grouped by their partition key.
    /// </summary>
    [Fact(DisplayName = "Groups deletion records by partition key")]
    public void QueueDeletions_GroupsRecordsByPartitionKey()
    {
        var strategy = CreateDeletionStrategy();
        var firstUserRecord = CreateRun("user1");
        var secondUserRecord = CreateRun("user2");

        strategy.QueueDeletions([firstUserRecord, secondUserRecord, firstUserRecord]);

        strategy.QueuedTasks.Count.ShouldBe(2);
        strategy.QueuedTasks[new PartitionKeyType.StringPartitionKey("user1")].Count.ShouldBe(2);
        strategy.QueuedTasks[new PartitionKeyType.StringPartitionKey("user2")].Count.ShouldBe(1);
        strategy.TotalOperationsCount.ShouldBe(3);
    }

    /// <summary>
    /// Verifies that an empty deletion batch queues nothing.
    /// </summary>
    [Fact(DisplayName = "Queues nothing when the deletion batch is empty")]
    public void QueueDeletions_WithEmptyBatch_QueuesNothing()
    {
        var strategy = CreateDeletionStrategy();

        strategy.QueueDeletions([]);

        strategy.QueuedTasks.Count.ShouldBe(0);
        strategy.TotalOperationsCount.ShouldBe(0);
    }

    /// <summary>
    /// Verifies that a null deletion batch yields an exception.
    /// </summary>
    [Fact(DisplayName = "Throws ArgumentNullException when batching null deletion records")]
    public void QueueDeletions_WithNullBatch_ThrowsArgumentNullException()
    {
        var strategy = CreateDeletionStrategy();

        ((Action)(() => strategy.QueueDeletions(null))).ShouldThrow<ArgumentNullException>();
    }

    private static TestInsertionStrategy CreateInsertionStrategy()
        => new(CreateCosmosSettings(), CreateContainerSettings());

    private static TestPatchStrategy CreatePatchStrategy()
        => new(CreateCosmosSettings(), CreateContainerSettings());

    private static TestDeletionStrategy CreateDeletionStrategy()
        => new(CreateCosmosSettings(), CreateContainerSettings());

    private static Run CreateRun(string userId)
        => new() { Id = Guid.NewGuid(), UserId = userId };

    private static CosmosSettings CreateCosmosSettings()
        => new()
        {
            EndpointUrl = "https://localhost:8081",
            AuthorizationKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            DatabaseName = "sandbox",
            RequestTimeOut = TimeSpan.FromSeconds(15),
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
            MaxRetryAttemptsOnRateLimitedRequests = 9
        };

    private static ContainerSettings CreateContainerSettings()
        => new()
        {
            ContainerName = "Runs",
            Query = new Query { Value = "SELECT * FROM c" }
        };
}
