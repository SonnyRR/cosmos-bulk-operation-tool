using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Cosmos.BulkOperation.CLI
{
    /// <summary>
    /// Substitution for Cosmos partition key union types.
    /// </summary>
    public record class PartitionKeyType
    {
        public record BooleanPartitionKey(bool Key) : PartitionKeyType(), IEqualityComparer<BooleanPartitionKey>
        {
            public bool Equals(BooleanPartitionKey x, BooleanPartitionKey y) => x.Key == y.Key;

            public int GetHashCode([DisallowNull] BooleanPartitionKey obj) => obj.GetHashCode();
        }

        public record DoublePartitionKey(double Key) : PartitionKeyType(), IEqualityComparer<DoublePartitionKey>
        {
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
            public bool Equals(DoublePartitionKey x, DoublePartitionKey y) => x.Key == y.Key;
#pragma warning restore S1244 // Floating point numbers should not be tested for equality

            public int GetHashCode([DisallowNull] DoublePartitionKey obj) => obj.GetHashCode();
        }

        public record StringPartitionKey(string Key) : PartitionKeyType(), IEqualityComparer<StringPartitionKey>
        {
            public bool Equals(StringPartitionKey x, StringPartitionKey y) => x.Key == y.Key;

            public int GetHashCode([DisallowNull] StringPartitionKey obj) => obj.GetHashCode();
        }

        private PartitionKeyType() { }
    }
}