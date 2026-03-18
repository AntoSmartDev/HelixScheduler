namespace HelixScheduler.Core;

/// <summary>
/// Busy interval for a single resource (UTC).
/// </summary>
public sealed class BusySlot
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }
    public int ResourceId { get; }

    public BusySlot(DateTime startUtc, DateTime endUtc, int resourceId)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("EndUtc must be greater than StartUtc.", nameof(endUtc));
        }

        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("BusySlot requires UTC DateTime values.");
        }

        StartUtc = startUtc;
        EndUtc = endUtc;
        ResourceId = resourceId;
    }
}
