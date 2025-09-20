namespace Cosmos.BulkOperation.Tests
{
    /// <summary>
    /// Various test utilities.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Changes the DOTNET_ENVIRONMENT value for the duration of the disposable context.
        /// </summary>
        public static IDisposable UseEnvironment(string value)
        {
            const string ENV_KEY = "DOTNET_ENVIRONMENT";
            var original = Environment.GetEnvironmentVariable(ENV_KEY);
            Environment.SetEnvironmentVariable(ENV_KEY, value);

            return new DisposableAction(() => Environment.SetEnvironmentVariable(ENV_KEY, original));
        }
    }
}