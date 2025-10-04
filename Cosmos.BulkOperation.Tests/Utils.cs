using System;

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
        public static void UseEnvironment(string value)
        {
            const string ENV_KEY = "DOTNET_ENVIRONMENT";
            Environment.SetEnvironmentVariable(ENV_KEY, value, EnvironmentVariableTarget.Process);
        }
    }
}