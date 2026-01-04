namespace HelixScheduler.Core;

/// <summary>
/// Busy interval for a single resource (UTC), v1 input model.
/// </summary>
public sealed class BusySlotModel
{
    /// <summary>
    /// Inclusive start in UTC.
    /// </summary>
    public DateTime StartUtc { get; }
    /// <summary>
    /// Exclusive end in UTC.
    /// </summary>
    public DateTime EndUtc { get; }
    /// <summary>
    /// Resource id for the busy interval.
    /// </summary>
    public int ResourceId { get; }

    /// <summary>
    /// Builds a new busy slot model.
    /// </summary>
    public BusySlotModel(DateTime startUtc, DateTime endUtc, int resourceId)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("EndUtc must be greater than StartUtc.", nameof(endUtc));
        }

        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("BusySlotModel requires UTC DateTime values.");
        }

        StartUtc = startUtc;
        EndUtc = endUtc;
        ResourceId = resourceId;
    }
}
