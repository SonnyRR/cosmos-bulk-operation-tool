namespace Cosmos.BulkOperation.CLI;

/// <summary>
/// Entry point for the Cosmos Bulk Operation CLI tool.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The exit code.</returns>
    public static int Main(string[] args)
        => CommandAppBuilder
            .Create()
            .Build()
            .Run(args);
}
