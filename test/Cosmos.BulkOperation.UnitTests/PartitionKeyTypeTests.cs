using Cosmos.BulkOperation.CLI;

using Shouldly;

namespace Cosmos.BulkOperation.UnitTests;

/// <summary>
/// Tests for the <see cref="PartitionKeyType"/> record equality and hashing semantics.
/// </summary>
public class PartitionKeyTypeTests
{
    /// <summary>
    /// Verifies the <see cref="PartitionKeyType.StringPartitionKey"/> equality semantics.
    /// </summary>
    [Fact(DisplayName = "String partition keys compare by key value")]
    public void StringPartitionKey_Equality_ComparesKeyValue()
    {
        var left = new PartitionKeyType.StringPartitionKey("user");
        var right = new PartitionKeyType.StringPartitionKey("user");
        var other = new PartitionKeyType.StringPartitionKey("different");

        left.Equals(right).ShouldBeTrue();
        left.Equals(other).ShouldBeFalse();
        left.GetHashCode(right).ShouldBe(right.GetHashCode(right));
    }

    /// <summary>
    /// Verifies the <see cref="PartitionKeyType.DoublePartitionKey"/> equality semantics.
    /// </summary>
    [Fact(DisplayName = "Double partition keys compare by key value")]
    public void DoublePartitionKey_Equality_ComparesKeyValue()
    {
        var left = new PartitionKeyType.DoublePartitionKey(1.5D);
        var right = new PartitionKeyType.DoublePartitionKey(1.5D);
        var other = new PartitionKeyType.DoublePartitionKey(2.5D);

        left.Equals(right).ShouldBeTrue();
        left.Equals(other).ShouldBeFalse();
        left.GetHashCode(right).ShouldBe(right.GetHashCode(right));
    }

    /// <summary>
    /// Verifies the <see cref="PartitionKeyType.BooleanPartitionKey"/> equality semantics.
    /// </summary>
    [Fact(DisplayName = "Boolean partition keys compare by key value")]
    public void BooleanPartitionKey_Equality_ComparesKeyValue()
    {
        var left = new PartitionKeyType.BooleanPartitionKey(true);
        var right = new PartitionKeyType.BooleanPartitionKey(true);
        var other = new PartitionKeyType.BooleanPartitionKey(false);

        left.Equals(right).ShouldBeTrue();
        left.Equals(other).ShouldBeFalse();
        left.GetHashCode(right).ShouldBe(right.GetHashCode(right));
    }

    /// <summary>
    /// Verifies the <see cref="PartitionKeyType.HierarchicalPartitionKey"/> equality semantics.
    /// </summary>
    [Fact(DisplayName = "Hierarchical partition keys compare all key components")]
    public void HierarchicalPartitionKey_Equality_ComparesAllComponents()
    {
        var left = new PartitionKeyType.HierarchicalPartitionKey("first", "second", null);
        var right = new PartitionKeyType.HierarchicalPartitionKey("first", "second", null);
        var differentThird = new PartitionKeyType.HierarchicalPartitionKey("first", "second", "third");
        var differentSecond = new PartitionKeyType.HierarchicalPartitionKey("first", "other", null);

        left.Equals(right).ShouldBeTrue();
        left.Equals(differentSecond).ShouldBeFalse();
        left.Equals(differentThird).ShouldBeFalse();
        left.GetHashCode(right).ShouldBe(right.GetHashCode(right));
    }

    /// <summary>
    /// Verifies that different partition key kinds are not equal.
    /// </summary>
    [Fact(DisplayName = "Different partition key kinds are not equal")]
    public void PartitionKeyTypes_OfDifferentKinds_AreNotEqual()
    {
        var stringKey = new PartitionKeyType.StringPartitionKey("123");
        var doubleKey = new PartitionKeyType.DoublePartitionKey(123D);

        stringKey.Equals(doubleKey).ShouldBeFalse();
    }
}
