using Microsoft.Azure.Cosmos;

namespace Cosmos.BulkOperation.CLI.Extensions
{
    public static class PartitionKeyTypeExtensions
    {
        public static PartitionKey UnwrapPartitionKey(this PartitionKeyType partitionKey)
            => partitionKey switch
            {
                PartitionKeyType.StringPartitionKey s => new PartitionKey(s.Key),
                PartitionKeyType.DoublePartitionKey d => new PartitionKey(d.Key),
                PartitionKeyType.BooleanPartitionKey b => new PartitionKey(b.Key),
                _ => PartitionKey.Null
            };
    }
}