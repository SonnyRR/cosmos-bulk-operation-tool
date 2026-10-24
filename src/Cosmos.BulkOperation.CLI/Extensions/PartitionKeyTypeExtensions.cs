using Microsoft.Azure.Cosmos;

namespace Cosmos.BulkOperation.CLI.Extensions;

/// <summary>
/// Extension methods for working with <see cref="PartitionKeyType"/>.
/// </summary>
public static class PartitionKeyTypeExtensions
{
    /// <summary>
    /// Converts a <see cref="PartitionKeyType"/> to a Cosmos <see cref="PartitionKey"/>.
    /// </summary>
    /// <param name="partitionKey">The partition key abstraction to unwrap.</param>
    /// <returns>The corresponding Cosmos <see cref="PartitionKey"/>.</returns>
    public static PartitionKey UnwrapPartitionKey(this PartitionKeyType partitionKey)
        => partitionKey switch
        {
            PartitionKeyType.StringPartitionKey s => new PartitionKey(s.Key),
            PartitionKeyType.DoublePartitionKey d => new PartitionKey(d.Key),
            PartitionKeyType.BooleanPartitionKey b => new PartitionKey(b.Key),
            PartitionKeyType.HierarchicalPartitionKey h => BuildHierarchicalPartitionKey(h),
            _ => PartitionKey.Null
        };

    private static PartitionKey BuildHierarchicalPartitionKey(PartitionKeyType.HierarchicalPartitionKey key)
    {
        var builder = new PartitionKeyBuilder();

        builder.Add(key.FirstKey);
        builder.Add(key.SecondKey);

        if (key.ThirdKey is not null)
        {
            builder.Add(key.ThirdKey);
        }

        return builder.Build();
    }
}
