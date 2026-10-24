using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Cosmos.BulkOperation.CLI;

/// <summary>
/// Substitution for Cosmos partition key union types.
/// </summary>
public record class PartitionKeyType
{
    /// <summary>
    /// Represents a boolean partition key value.
    /// </summary>
    /// <param name="Key">The boolean value for the partition key.</param>
    public record BooleanPartitionKey(bool Key) : PartitionKeyType(), IEqualityComparer<BooleanPartitionKey>
    {
        /// <summary>
        /// Determines whether two <see cref="BooleanPartitionKey"/> instances are equal.
        /// </summary>
        /// <param name="x">The first instance to compare.</param>
        /// <param name="y">The second instance to compare.</param>
        /// <returns>True if the keys are equal; otherwise, false.</returns>
        public bool Equals(BooleanPartitionKey x, BooleanPartitionKey y) => x.Key == y.Key;

        /// <summary>
        /// Returns a hash code for the specified <see cref="BooleanPartitionKey"/>.
        /// </summary>
        /// <param name="obj">The object for which to get the hash code.</param>
        /// <returns>A hash code for the specified object.</returns>
        public int GetHashCode([DisallowNull] BooleanPartitionKey obj) => obj.GetHashCode();
    }

    /// <summary>
    /// Represents a double partition key value.
    /// </summary>
    /// <param name="Key">The double value for the partition key.</param>
    public record DoublePartitionKey(double Key) : PartitionKeyType(), IEqualityComparer<DoublePartitionKey>
    {
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
        /// <summary>
        /// Determines whether two <see cref="DoublePartitionKey"/> instances are equal.
        /// </summary>
        /// <param name="x">The first instance to compare.</param>
        /// <param name="y">The second instance to compare.</param>
        /// <returns>True if the keys are equal; otherwise, false.</returns>
        public bool Equals(DoublePartitionKey x, DoublePartitionKey y) => x.Key == y.Key;
#pragma warning restore S1244 // Floating point numbers should not be tested for equality

        /// <summary>
        /// Returns a hash code for the specified <see cref="DoublePartitionKey"/>.
        /// </summary>
        /// <param name="obj">The object for which to get the hash code.</param>
        /// <returns>A hash code for the specified object.</returns>
        public int GetHashCode([DisallowNull] DoublePartitionKey obj) => obj.GetHashCode();
    }

    /// <summary>
    /// Represents a string partition key value.
    /// </summary>
    /// <param name="Key">The string value for the partition key.</param>
    public record StringPartitionKey(string Key) : PartitionKeyType(), IEqualityComparer<StringPartitionKey>
    {
        /// <summary>
        /// Determines whether two <see cref="StringPartitionKey"/> instances are equal.
        /// </summary>
        /// <param name="x">The first instance to compare.</param>
        /// <param name="y">The second instance to compare.</param>
        /// <returns>True if the keys are equal; otherwise, false.</returns>
        public bool Equals(StringPartitionKey x, StringPartitionKey y) => x.Key == y.Key;

        /// <summary>
        /// Returns a hash code for the specified <see cref="StringPartitionKey"/>.
        /// </summary>
        /// <param name="obj">The object for which to get the hash code.</param>
        /// <returns>A hash code for the specified object.</returns>
        public int GetHashCode([DisallowNull] StringPartitionKey obj) => obj.GetHashCode();
    }

    /// <summary>
    /// Represents a hierarchical partition key value with up to three string components.
    /// </summary>
    /// <param name="FirstKey">The first key component.</param>
    /// <param name="SecondKey">The second key component.</param>
    /// <param name="ThirdKey">The optional third key component.</param>
    public record HierarchicalPartitionKey(string FirstKey, string SecondKey, string ThirdKey) : PartitionKeyType(), IEqualityComparer<HierarchicalPartitionKey>
    {
        /// <summary>
        /// Determines whether two <see cref="HierarchicalPartitionKey"/> instances are equal.
        /// </summary>
        /// <param name="x">The first instance to compare.</param>
        /// <param name="y">The second instance to compare.</param>
        /// <returns>True if all key components are equal; otherwise, false.</returns>
        public bool Equals(HierarchicalPartitionKey x, HierarchicalPartitionKey y)
            => x.FirstKey == y.FirstKey && x.SecondKey == y.SecondKey && x.ThirdKey == y.ThirdKey;

        /// <summary>
        /// Returns a hash code for the specified <see cref="HierarchicalPartitionKey"/>.
        /// </summary>
        /// <param name="obj">The object for which to get the hash code.</param>
        /// <returns>A hash code for the specified object.</returns>
        public int GetHashCode([DisallowNull] HierarchicalPartitionKey obj) => obj.GetHashCode();
    }

    private PartitionKeyType() { }
}
