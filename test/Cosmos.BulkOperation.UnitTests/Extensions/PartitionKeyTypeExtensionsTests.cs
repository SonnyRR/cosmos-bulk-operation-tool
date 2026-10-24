using System.Linq;

using Cosmos.BulkOperation.CLI;
using Cosmos.BulkOperation.CLI.Extensions;
using Cosmos.BulkOperation.CLI.Settings;

using Microsoft.Azure.Cosmos;

using Shouldly;

namespace Cosmos.BulkOperation.UnitTests.Extensions;

/// <summary>
/// Tests for the <see cref="PartitionKeyTypeExtensions"/>.
/// </summary>
public class PartitionKeyTypeExtensionsTests
{
    /// <summary>
    /// Verifies that a string partition key is unwrapped to a Cosmos string partition key.
    /// </summary>
    [Fact(DisplayName = "Unwraps a string partition key")]
    public void UnwrapPartitionKey_WithStringPartitionKey_MapsToStringPartitionKey()
        => new PartitionKeyType.StringPartitionKey("abc").UnwrapPartitionKey()
            .ToString()
            .ShouldBe("[\"abc\"]");

    /// <summary>
    /// Verifies that a double partition key is unwrapped to a Cosmos numeric partition key.
    /// </summary>
    [Fact(DisplayName = "Unwraps a double partition key")]
    public void UnwrapPartitionKey_WithDoublePartitionKey_MapsToDoublePartitionKey()
        => new PartitionKeyType.DoublePartitionKey(1.5D).UnwrapPartitionKey()
            .ToString()
            .ShouldBe("[1.5]");

    /// <summary>
    /// Verifies that a boolean partition key is unwrapped to a Cosmos boolean partition key.
    /// </summary>
    [Fact(DisplayName = "Unwraps a boolean partition key")]
    public void UnwrapPartitionKey_WithBooleanPartitionKey_MapsToBooleanPartitionKey()
        => new PartitionKeyType.BooleanPartitionKey(true).UnwrapPartitionKey()
            .ToString()
            .ShouldBe("[true]");

    /// <summary>
    /// Verifies that a two-component hierarchical partition key is unwrapped correctly.
    /// </summary>
    [Fact(DisplayName = "Unwraps a two-component hierarchical partition key")]
    public void UnwrapPartitionKey_WithTwoKeyHierarchicalPartitionKey_BuildsTwoKeyPartitionKey()
        => new PartitionKeyType.HierarchicalPartitionKey("a", "b", null).UnwrapPartitionKey()
            .ToString()
            .ShouldBe("[\"a\",\"b\"]");

    /// <summary>
    /// Verifies that a three-component hierarchical partition key is unwrapped correctly.
    /// </summary>
    [Fact(DisplayName = "Unwraps a three-component hierarchical partition key")]
    public void UnwrapPartitionKey_WithThreeKeyHierarchicalPartitionKey_BuildsThreeKeyPartitionKey()
        => new PartitionKeyType.HierarchicalPartitionKey("a", "b", "c").UnwrapPartitionKey()
            .ToString()
            .ShouldBe("[\"a\",\"b\",\"c\"]");

    /// <summary>
    /// Verifies that hierarchical keys differ when their components differ.
    /// </summary>
    [Fact(DisplayName = "Hierarchical partition keys with different components unwrap differently")]
    public void UnwrapPartitionKey_WithDifferentHierarchicalKeys_ProducesDifferentPartitionKeys()
        => new PartitionKeyType.HierarchicalPartitionKey("a", "b", "c").UnwrapPartitionKey()
            .ToString()
            .ShouldNotBe(new PartitionKeyType.HierarchicalPartitionKey("a", "b", null).UnwrapPartitionKey().ToString());
}
