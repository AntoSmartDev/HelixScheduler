namespace HelixScheduler.Core;

/// <summary>
/// Normalized inputs for availability computation (rules, busy slots, capacities).
/// </summary>
public sealed class AvailabilityInputs
{
    /// <summary>
    /// Scheduler rules, already expressed in UTC dates/times.
    /// </summary>
    public IReadOnlyList<RuleModel> Rules { get; }
    /// <summary>
    /// Busy slots for resources, expressed in UTC.
    /// </summary>
    public IReadOnlyList<BusySlotModel> BusySlots { get; }
    /// <summary>
    /// Optional per-resource capacity; values &lt; 1 are treated as 1.
    /// </summary>
    public IReadOnlyDictionary<int, int> ResourceCapacities { get; }

    /// <summary>
    /// Builds a new input bundle for the availability engine.
    /// </summary>
    public AvailabilityInputs(
        IReadOnlyList<RuleModel> rules,
        IReadOnlyList<BusySlotModel> busySlots,
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
