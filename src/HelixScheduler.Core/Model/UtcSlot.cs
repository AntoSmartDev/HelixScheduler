namespace HelixScheduler.Core;

/// <summary>
/// Time slot expressed in UTC with associated resource ids.
/// </summary>
public sealed class UtcSlot
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
    /// Resource ids associated with the slot.
    /// </summary>
    public IReadOnlyCollection<int> ResourceIds { get; }

    /// <summary>
    /// Builds a new UTC slot.
    /// </summary>
    public UtcSlot(DateTime startUtc, DateTime endUtc, IReadOnlyCollection<int> resourceIds)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("EndUtc must be greater than StartUtc.", nameof(endUtc));
        }

        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("UtcSlot requires UTC DateTime values.");
        }

        ResourceIds = resourceIds ?? throw new ArgumentNullException(nameof(resourceIds));
        StartUtc = startUtc;
        EndUtc = endUtc;
    }
}
