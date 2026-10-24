using System;
using System.Collections.Generic;

namespace Cosmos.BulkOperation.Samples;

/// <summary>
/// Represents a paced run.
/// </summary>
public class Run
{
    /// <summary>
    /// The unique identifier of the run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user identifier who ran this run.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// The duration of the run.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// The time at which the runner started this run.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// The checkpoints throughout the run.
    /// </summary>
    public IEnumerable<Checkpoint> Checkpoints { get; set; }
}
