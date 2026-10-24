namespace Cosmos.BulkOperation.Samples;

/// <summary>
/// Represents a checkpoint during a run.
/// </summary>
public class Checkpoint
{
    /// <summary>
    /// The lattitude of the checkpoint.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// The longitude of the checkpoint.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// The checkpoint pin color.
    /// </summary>
    public Color PinColor { get; set; }
}
