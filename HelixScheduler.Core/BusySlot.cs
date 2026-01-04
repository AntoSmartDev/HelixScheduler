namespace HelixScheduler.Core;

/// <summary>
/// Busy interval for a single resource (UTC).
/// </summary>
public sealed class BusySlot
{
    /// <summary>
    /// Resource id for the busy interval.
    /// </summary>
    public int ResourceId { get; }
    /// <summary>
    /// Inclusive start in UTC.
    /// </summary>
    public DateTime StartUtc { get; }
    /// <summary>
    /// Exclusive end in UTC.
    /// </summary>
    public DateTime EndUtc { get; }

    /// <summary>
    /// Builds a new busy slot.
    /// </summary>
    public BusySlot(int resourceId, DateTime startUtc, DateTime endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("EndUtc must be greater than StartUtc.", nameof(endUtc));
        }

        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("BusySlot requires UTC DateTime values.");
        }

        ResourceId = resourceId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }
}
