using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.Availability.Validation;

namespace HelixScheduler.Application.Availability.Filtering;

internal sealed class AvailabilityPropertyFilterEvaluator
{
    private readonly IAvailabilityFilterQueryService _filterQueryService;

    public AvailabilityPropertyFilterEvaluator(IAvailabilityFilterQueryService filterQueryService) => _filterQueryService = filterQueryService;

    public static List<int> CollectPropertyFilterIds(AvailabilityComputeRequest request)
    {
        var ids = new List<int>();
        var seen = new HashSet<int>();
        if (request.PropertyFilterGroups == null || request.PropertyFilterGroups.Count == 0) return ids;
        for (var i = 0; i < request.PropertyFilterGroups.Count; i++)
        {
            var group = request.PropertyFilterGroups[i];
            if (group?.PropertyIds == null) continue;
            for (var j = 0; j < group.PropertyIds.Count; j++) if (seen.Add(group.PropertyIds[j])) ids.Add(group.PropertyIds[j]);
        }

        return ids;
    }

    public List<PropertyFilterGroup> NormalizePropertyFilterGroups(AvailabilityComputeRequest request)
    {
        var groups = new List<PropertyFilterGroup>();
        if (request.PropertyFilterGroups == null || request.PropertyFilterGroups.Count == 0) return groups;
        for (var i = 0; i < request.PropertyFilterGroups.Count; i++)
        {
            var group = request.PropertyFilterGroups[i];
            if (group?.PropertyIds == null || group.PropertyIds.Count == 0) continue;
            var ids = AvailabilityRequestNormalization.DistinctPropertyIds(group.PropertyIds);
            if (ids.Count == 0) continue;
            groups.Add(new PropertyFilterGroup(ids, AvailabilityRequestNormalization.NormalizePropertyMatchMode(group.MatchMode) ?? "and", group.IncludePropertyDescendants));
        }

        return groups;
    }

    public async Task<HashSet<int>> EvaluatePropertyFilterGroupAsync(PropertyFilterGroup group, PropertyFilterExecutionContext context, CancellationToken ct)
    {
        if (group.PropertyIds == null || group.PropertyIds.Count == 0) return new HashSet<int>();
        return (AvailabilityRequestNormalization.NormalizePropertyMatchMode(group.MatchMode) ?? "and") == "or"
            ? await EvaluateOrPropertyGroupAsync(group, context, ct).ConfigureAwait(false)
            : await EvaluateAndPropertyGroupAsync(group, context, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<int>> ExpandPropertyIdsCachedAsync(int propertyId, PropertyFilterExecutionContext context, CancellationToken ct)
    {
        if (context.ExpandedPropertyIds.TryGetValue(propertyId, out var cached)) return cached;
        var subtree = await _filterQueryService.ExpandPropertySubtreeAsync(propertyId, ct).ConfigureAwait(false);
        var ids = new List<int>(subtree.Count);
        for (var i = 0; i < subtree.Count; i++) ids.Add(subtree[i].Id);
        context.ExpandedPropertyIds[propertyId] = ids;
        return ids;
    }

    public async Task<List<HashSet<int>>> GetPropertyMatchSetsAsync(IReadOnlyList<IReadOnlyList<int>> propertySets, PropertyFilterExecutionContext context, CancellationToken ct)
    {
        var result = new List<HashSet<int>>(propertySets.Count);
        var pendingSets = new List<IReadOnlyList<int>>();
        var pendingIndexes = new List<int>();
        for (var i = 0; i < propertySets.Count; i++)
        {
            var normalized = AvailabilityRequestNormalization.DistinctPropertyIds(propertySets[i]);
            var cacheKey = AvailabilityRequestNormalization.BuildPropertySetCacheKey(normalized);
            if (context.PropertySetMatches.TryGetValue(cacheKey, out var cached))
            {
                result.Add(new HashSet<int>(cached));
            }
            else
            {
                result.Add(new HashSet<int>());
                pendingSets.Add(normalized);
                pendingIndexes.Add(i);
            }
        }

        if (pendingSets.Count == 0) return result;

        var loadedMatches = await _filterQueryService.GetResourceIdsByPropertySetsAsync(pendingSets, ct).ConfigureAwait(false);
        for (var i = 0; i < pendingSets.Count; i++)
        {
            var normalized = AvailabilityRequestNormalization.DistinctPropertyIds(pendingSets[i]);
            var cacheKey = AvailabilityRequestNormalization.BuildPropertySetCacheKey(normalized);
            var set = new HashSet<int>(loadedMatches[i]);
            context.PropertySetMatches[cacheKey] = set;
            result[pendingIndexes[i]] = new HashSet<int>(set);
        }

        return result;
    }

    private async Task<HashSet<int>> EvaluateOrPropertyGroupAsync(PropertyFilterGroup group, PropertyFilterExecutionContext context, CancellationToken ct)
    {
        if (!group.IncludePropertyDescendants)
        {
            return new HashSet<int>(await _filterQueryService.GetResourceIdsByPropertiesAsync(group.PropertyIds, ct).ConfigureAwait(false));
        }

        var expandedIds = new HashSet<int>();
        for (var i = 0; i < group.PropertyIds.Count; i++)
        {
            var expanded = await ExpandPropertyIdsCachedAsync(group.PropertyIds[i], context, ct).ConfigureAwait(false);
            for (var j = 0; j < expanded.Count; j++) expandedIds.Add(expanded[j]);
        }

        if (expandedIds.Count == 0) return new HashSet<int>();
        return new HashSet<int>(await _filterQueryService.GetResourceIdsByPropertiesAsync(expandedIds.ToList(), ct).ConfigureAwait(false));
    }

    private async Task<HashSet<int>> EvaluateAndPropertyGroupAsync(PropertyFilterGroup group, PropertyFilterExecutionContext context, CancellationToken ct)
    {
        if (!group.IncludePropertyDescendants && group.PropertyIds.Count > 1)
        {
            return new HashSet<int>(await _filterQueryService.GetResourceIdsByAllPropertiesAsync(group.PropertyIds, ct).ConfigureAwait(false));
        }

        var effectivePropertySets = new List<IReadOnlyList<int>>(group.PropertyIds.Count);
        for (var i = 0; i < group.PropertyIds.Count; i++)
        {
            effectivePropertySets.Add(group.IncludePropertyDescendants
                ? await ExpandPropertyIdsCachedAsync(group.PropertyIds[i], context, ct).ConfigureAwait(false)
                : new List<int> { group.PropertyIds[i] });
        }

        var matchSets = await GetPropertyMatchSetsAsync(effectivePropertySets, context, ct).ConfigureAwait(false);
        HashSet<int>? groupMatch = null;
        for (var i = 0; i < matchSets.Count; i++)
        {
            if (groupMatch == null) groupMatch = new HashSet<int>(matchSets[i]);
            else
            {
                groupMatch.IntersectWith(matchSets[i]);
                if (groupMatch.Count == 0) return new HashSet<int>();
            }
        }

        return groupMatch ?? new HashSet<int>();
    }
}

internal sealed class PropertyFilterExecutionContext
{
    public Dictionary<int, IReadOnlyList<int>> ExpandedPropertyIds { get; } = new();
    public Dictionary<string, HashSet<int>> PropertySetMatches { get; } = new(StringComparer.Ordinal);
}
