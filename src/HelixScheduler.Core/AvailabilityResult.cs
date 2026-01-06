namespace HelixScheduler.Core;

/// <summary>
/// Availability computation result.
/// </summary>
public sealed class AvailabilityResult
{
    /// <summary>
    /// Final availability slots (UTC).
    /// </summary>
    public IReadOnlyList<UtcSlot> Slots { get; }

    /// <summary>
    /// Builds a new result container.
    /// </summary>
    public AvailabilityResult(IReadOnlyList<UtcSlot> slots)
    {
        Slots = slots ?? throw new ArgumentNullException(nameof(slots));
    }
}
