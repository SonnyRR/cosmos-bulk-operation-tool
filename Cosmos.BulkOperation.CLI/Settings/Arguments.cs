﻿using CommandLine;

namespace Cosmos.BulkOperation.CLI.Settings
{
    /// <summary>
    /// Defines custom CLI arguments for this tool.
    /// </summary>
    public class Arguments
    {
        [Option("dry-run", Default = false, HelpText = "Dry-run mode, allowing for changes to be scheduled, but not evaluated on the destination Cosmos database. Used for debugging.")]
        public bool DryRun { get; set; }
    }
}
