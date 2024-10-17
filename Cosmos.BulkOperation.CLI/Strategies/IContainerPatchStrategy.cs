using System.Threading;
using System.Threading.Tasks;

namespace Cosmos.BulkOperation.CLI.Strategies
{
    /// <summary>
    /// A strategy, responsible for patching records in specific Cosmos DB containers.
    /// </summary>
    public interface IContainerPatchStrategy
    {
        /// <summary>
        /// Evaluates the patching strategy.
        /// </summary>
        /// <param name="dryRun">
        /// A flag indicating that the changes should not be applied to the Cosmos DB resource.
        /// </param>
        /// <returns>An instance of <see cref="Task"/>.</returns>
        Task EvaluateAsync(bool dryRun = false, CancellationToken ct = default);
    }
}
