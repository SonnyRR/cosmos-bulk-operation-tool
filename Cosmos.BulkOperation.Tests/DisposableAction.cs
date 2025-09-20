namespace Cosmos.BulkOperation.Tests
{
    /// <summary>
    /// Represents a disposable action delegate.
    /// </summary>
    public class DisposableAction : IDisposable
    {
        private readonly Action action;

        /// <summary>
        /// Creates a new instance of a disposable action.
        /// </summary>
        public DisposableAction(Action action) => this.action = action;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal disposing method.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            this.action();
        }
    }
}