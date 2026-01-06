namespace HelixScheduler.Core;

/// <summary>
/// Defines the availability request window and resource selection semantics.
/// </summary>
public sealed class AvailabilityQuery
{
    /// <summary>
    /// Inclusive period (UTC dates) to evaluate.
    /// </summary>
    public DatePeriod Period { get; }
    /// <summary>
    /// Resource ids that must all be available (logical AND).
    /// </summary>
    public IReadOnlyCollection<int> RequiredResourceIds { get; }
    /// <summary>
    /// Optional property filters applied by callers before building rules/busy slots.
    /// </summary>
    public IReadOnlyCollection<PropertyFilter> PropertyFilters { get; }
    /// <summary>
    /// OR-groups of resource ids; each group is unioned, then intersected with the main result.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<int>> ResourceOrGroups { get; }
    /// <summary>
    /// All resource ids involved in the query (required + OR groups).
    /// </summary>
    public IReadOnlyList<int> AllResourceIds { get; }
    /// <summary>
    /// Availability mode (reserved for future modes).
    /// </summary>
    public AvailabilityMode Mode { get; }

    /// <summary>
    /// Builds a new availability query.
    /// </summary>
    public AvailabilityQuery(
        DatePeriod period,
        IReadOnlyCollection<int> requiredResourceIds,
        IReadOnlyCollection<PropertyFilter>? propertyFilters = null,
        IReadOnlyList<IReadOnlyList<int>>? resourceOrGroups = null,
        AvailabilityMode mode = AvailabilityMode.Intersection)
    {
        if (propertyFilters != null)
        {
            foreach (var filter in propertyFilters)
            {
                if (filter is null)
                {
                    throw new ArgumentException("PropertyFilters cannot contain null entries.", nameof(propertyFilters));
                }
            }
        }

        if (resourceOrGroups != null)
        {
            for (var i = 0; i < resourceOrGroups.Count; i++)
            {
                if (resourceOrGroups[i] is null)
                {
                    throw new ArgumentException("ResourceOrGroups cannot contain null entries.", nameof(resourceOrGroups));
                }
            }
        }

        RequiredResourceIds = requiredResourceIds ?? throw new ArgumentNullException(nameof(requiredResourceIds));
        PropertyFilters = propertyFilters ?? Array.Empty<PropertyFilter>();
        Period = period;
        Mode = mode;
        ResourceOrGroups = resourceOrGroups ?? Array.Empty<IReadOnlyList<int>>();
        AllResourceIds = BuildAllResourceIds(requiredResourceIds, ResourceOrGroups);
    }

    private static IReadOnlyList<int> BuildAllResourceIds(
        IReadOnlyCollection<int> requiredResourceIds,
        IReadOnlyList<IReadOnlyList<int>> resourceOrGroups)
    {
        var set = new SortedSet<int>(requiredResourceIds);
        for (var i = 0; i < resourceOrGroups.Count; i++)
        {
            foreach (var resourceId in resourceOrGroups[i])
            {
                set.Add(resourceId);
            }
        }

        return set.ToList();
    }
}
