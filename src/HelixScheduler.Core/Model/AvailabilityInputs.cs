namespace HelixScheduler.Core;

/// <summary>
/// Normalized inputs for availability computation (rules, busy slots, capacities).
/// </summary>
public sealed class AvailabilityInputs
{
    public IReadOnlyList<AvailabilityRule> Rules { get; }
    public IReadOnlyList<BusySlot> BusySlots { get; }
    public IReadOnlyDictionary<int, int> ResourceCapacities { get; }

    public AvailabilityInputs(
        IReadOnlyList<AvailabilityRule> rules,
        IReadOnlyList<BusySlot> busySlots,
        IReadOnlyDictionary<int, int>? resourceCapacities = null)
    {
        if (rules != null)
        {
            for (var i = 0; i < rules.Count; i++)
            {
                if (rules[i] is null)
                {
                    throw new ArgumentException("Rules cannot contain null entries.", nameof(rules));
                }
            }
        }

        if (busySlots != null)
        {
            for (var i = 0; i < busySlots.Count; i++)
            {
                if (busySlots[i] is null)
                {
                    throw new ArgumentException("BusySlots cannot contain null entries.", nameof(busySlots));
                }
            }
        }

        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        BusySlots = busySlots ?? throw new ArgumentNullException(nameof(busySlots));
        ResourceCapacities = resourceCapacities ?? new Dictionary<int, int>();
    }
}
