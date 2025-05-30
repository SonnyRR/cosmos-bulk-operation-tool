using System.Threading;
using System.Threading.Tasks;

namespace Cosmos.BulkOperation.CLI.Strategies
{
    /// <summary>
    /// A strategy, responsible for bulk operating on records in specific Cosmos DB containers.
    /// </summary>
    public interface IBulkOperationStrategy
    {
        /// <summary>
        /// Evaluates the bulk operation strategy.
        /// </summary>
        /// <param name="dryRun">
        /// A flag indicating that the changes should not be applied to the Cosmos DB resource.
        /// </param>
        /// <returns>An instance of <see cref="Task"/>.</returns>
        Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default);
    }
}