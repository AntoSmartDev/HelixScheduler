using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.Availability.Validation;

namespace HelixScheduler.Application.Availability.Filtering;

internal sealed class AvailabilityAncestorHandler
{
    private readonly IAvailabilityAncestorQueryService _ancestorQueryService;
    private readonly PropertySchema.IPropertySchemaService _propertySchemaService;
    private readonly AvailabilityPropertyFilterEvaluator _propertyFilterEvaluator;

    public AvailabilityAncestorHandler(
        IAvailabilityAncestorQueryService ancestorQueryService,
        PropertySchema.IPropertySchemaService propertySchemaService,
        AvailabilityPropertyFilterEvaluator propertyFilterEvaluator)
    {
        _ancestorQueryService = ancestorQueryService;
        _propertySchemaService = propertySchemaService;
        _propertyFilterEvaluator = propertyFilterEvaluator;
    }

    public async Task<AncestorExpansion> BuildAncestorExpansionAsync(IReadOnlyCollection<int> resourceIds, IReadOnlyList<string>? relationTypes, CancellationToken ct)
    {
        if (resourceIds.Count == 0) return AncestorExpansion.Empty;

        var relations = await _ancestorQueryService
            .GetResourceRelationsByTypesAsync(AvailabilityRequestNormalization.NormalizeRelationTypes(relationTypes), ct)
            .ConfigureAwait(false);

        var reachable = BuildReachableAncestorParents(resourceIds, relations);
        if (reachable.Count == 0) return AncestorExpansion.Empty;

        var ancestorMap = new Dictionary<int, HashSet<int>>();
        var allAncestors = new HashSet<int>();
        foreach (var resourceId in resourceIds)
        {
            var ancestors = ResolveAncestors(resourceId, reachable, ancestorMap);
            if (ancestors.Count > 0) allAncestors.UnionWith(ancestors);
        }

        return new AncestorExpansion(ancestorMap, allAncestors, reachable);
    }

    public HashSet<int> CollectAncestors(IReadOnlyCollection<int> resourceIds, AncestorExpansion expansion)
    {
        if (resourceIds.Count == 0 || expansion.AncestorMap.Count == 0) return new HashSet<int>();

        var result = new HashSet<int>();
        foreach (var resourceId in resourceIds)
        {
            if (expansion.AncestorMap.TryGetValue(resourceId, out var ancestors)) result.UnionWith(ancestors);
        }

        return result;
    }

    public List<int> ExpandRequiredAncestors(IReadOnlyList<int> requiredIds, AncestorExpansion expansion)
    {
        if (requiredIds.Count == 0 || expansion.AncestorMap.Count == 0) return requiredIds.ToList();

        var result = new HashSet<int>(requiredIds);
        for (var i = 0; i < requiredIds.Count; i++)
        {
            if (expansion.AncestorMap.TryGetValue(requiredIds[i], out var ancestors)) result.UnionWith(ancestors);
        }

        var list = result.ToList();
        list.Sort();
        return list;
    }

    public async Task<AncestorFilterResult> ApplyAncestorFiltersAsync(
        IReadOnlyList<int> requiredIds,
        IReadOnlyList<List<int>> orGroups,
        AncestorExpansion expansion,
        IReadOnlyList<AncestorPropertyFilter> filters,
        PropertyFilterExecutionContext context,
        CancellationToken ct)
    {
        var normalizedFilters = NormalizeAncestorFilters(filters);
        if (normalizedFilters.Count == 0) return new AncestorFilterResult(requiredIds.ToList(), orGroups.ToList(), true);

        var ancestorIds = CollectAncestors(requiredIds.Concat(orGroups.SelectMany(g => g)).ToHashSet(), expansion);
        if (ancestorIds.Count == 0) return new AncestorFilterResult(requiredIds.ToList(), orGroups.ToList(), false);

        var assignments = await _propertySchemaService.GetResourceTypeAssignmentsAsync(ancestorIds.ToList(), ct).ConfigureAwait(false);
        var typeByResourceId = assignments.GroupBy(x => x.ResourceId).ToDictionary(g => g.Key, g => g.First().ResourceTypeId);

        var filterMatches = new List<AncestorFilterMatch>();
        for (var i = 0; i < normalizedFilters.Count; i++)
        {
            var filter = normalizedFilters[i];
            var candidates = ancestorIds.Where(id => typeByResourceId.TryGetValue(id, out var typeId) && typeId == filter.ResourceTypeId).ToHashSet();
            var matches = candidates.Count == 0
                ? new HashSet<int>()
                : await ResolveMatchingAncestorsAsync(filter, candidates, context, ct).ConfigureAwait(false);
            filterMatches.Add(new AncestorFilterMatch(filter, matches));
        }

        var filteredRequired = new List<int>();
        for (var i = 0; i < requiredIds.Count; i++)
        {
            if (!ResourcePassesFilters(requiredIds[i], filterMatches, expansion, typeByResourceId))
            {
                return new AncestorFilterResult(new List<int>(), new List<List<int>>(), false);
            }

            filteredRequired.Add(requiredIds[i]);
        }

        var filteredGroups = new List<List<int>>();
        for (var gi = 0; gi < orGroups.Count; gi++)
        {
            var filteredGroup = new List<int>();
            for (var i = 0; i < orGroups[gi].Count; i++)
            {
                if (ResourcePassesFilters(orGroups[gi][i], filterMatches, expansion, typeByResourceId)) filteredGroup.Add(orGroups[gi][i]);
            }

            if (filteredGroup.Count == 0) return new AncestorFilterResult(new List<int>(), new List<List<int>>(), false);
            filteredGroup.Sort();
            filteredGroups.Add(filteredGroup);
        }

        return new AncestorFilterResult(filteredRequired, filteredGroups, true);
    }

    private async Task<HashSet<int>> ResolveMatchingAncestorsAsync(
        AncestorPropertyFilter filter,
        HashSet<int> candidates,
        PropertyFilterExecutionContext context,
        CancellationToken ct)
    {
        var propertyGroups = new List<List<int>>();
        if (filter.PropertyIds != null)
        {
            for (var i = 0; i < filter.PropertyIds.Count; i++)
            {
                var propertyId = filter.PropertyIds[i];
                var expanded = filter.IncludePropertyDescendants
                    ? await _propertyFilterEvaluator.ExpandPropertyIdsCachedAsync(propertyId, context, ct).ConfigureAwait(false)
                    : new List<int> { propertyId };
                if (expanded.Count > 0) propertyGroups.Add(expanded.Distinct().ToList());
            }
        }

        if (propertyGroups.Count == 0) return new HashSet<int>();

        HashSet<int>? intersection = null;
        var union = new HashSet<int>();
        var isAnd = AvailabilityRequestNormalization.NormalizeMatchMode(filter.MatchMode) == "and";
        var matchSets = await _propertyFilterEvaluator.GetPropertyMatchSetsAsync(propertyGroups, context, ct).ConfigureAwait(false);
        for (var i = 0; i < matchSets.Count; i++)
        {
            if (isAnd)
            {
                intersection ??= new HashSet<int>(matchSets[i]);
                intersection.IntersectWith(matchSets[i]);
            }
            else
            {
                union.UnionWith(matchSets[i]);
            }
        }

        var matches = isAnd ? intersection ?? new HashSet<int>() : union;
        matches.IntersectWith(candidates);
        return matches;
    }

    private static bool ResourcePassesFilters(
        int resourceId,
        IReadOnlyList<AncestorFilterMatch> filters,
        AncestorExpansion expansion,
        IReadOnlyDictionary<int, int> typeByResourceId)
    {
        for (var i = 0; i < filters.Count; i++)
        {
            var eligibleAncestors = GetEligibleAncestors(resourceId, filters[i].Filter, expansion, typeByResourceId);
            if (eligibleAncestors.Count == 0) return false;

            if (filters[i].Filter.MatchAllAncestors)
            {
                foreach (var ancestorId in eligibleAncestors) if (!filters[i].MatchingAncestors.Contains(ancestorId)) return false;
            }
            else
            {
                var hasMatch = false;
                foreach (var ancestorId in eligibleAncestors)
                {
                    if (filters[i].MatchingAncestors.Contains(ancestorId))
                    {
                        hasMatch = true;
                        break;
                    }
                }

                if (!hasMatch) return false;
            }
        }

        return true;
    }

    private static HashSet<int> GetEligibleAncestors(
        int resourceId,
        AncestorPropertyFilter filter,
        AncestorExpansion expansion,
        IReadOnlyDictionary<int, int> typeByResourceId)
    {
        var scope = AvailabilityRequestNormalization.NormalizeAncestorScope(filter.Scope) ?? "anyAncestor";
        var result = new HashSet<int>();

        switch (scope)
        {
            case "directParent":
                if (expansion.ParentsByChild.TryGetValue(resourceId, out var parents))
                {
                    foreach (var parent in parents)
                    {
                        if (typeByResourceId.TryGetValue(parent, out var typeId) && typeId == filter.ResourceTypeId) result.Add(parent);
                    }
                }
                return result;
            case "nearestOfType":
                return FindNearestAncestorsOfType(resourceId, filter.ResourceTypeId, expansion.ParentsByChild, typeByResourceId);
        }

        if (expansion.AncestorMap.TryGetValue(resourceId, out var ancestors))
        {
            foreach (var ancestor in ancestors)
            {
                if (typeByResourceId.TryGetValue(ancestor, out var typeId) && typeId == filter.ResourceTypeId) result.Add(ancestor);
            }
        }

        return result;
    }

    private static HashSet<int> FindNearestAncestorsOfType(
        int resourceId,
        int resourceTypeId,
        IReadOnlyDictionary<int, HashSet<int>> parentsByChild,
        IReadOnlyDictionary<int, int> typeByResourceId)
    {
        if (!parentsByChild.TryGetValue(resourceId, out var parents)) return new HashSet<int>();

        var visited = new HashSet<int>();
        var queue = new Queue<(int ResourceId, int Depth)>();
        foreach (var parent in parents) queue.Enqueue((parent, 1));

        var result = new HashSet<int>();
        int? matchDepth = null;
        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (matchDepth.HasValue && depth > matchDepth.Value) break;
            if (!visited.Add(current)) continue;

            if (typeByResourceId.TryGetValue(current, out var typeId) && typeId == resourceTypeId)
            {
                result.Add(current);
                matchDepth ??= depth;
                continue;
            }

            if (matchDepth.HasValue) continue;
            if (parentsByChild.TryGetValue(current, out var nextParents)) foreach (var parent in nextParents) queue.Enqueue((parent, depth + 1));
        }

        return result;
    }

    private static List<AncestorPropertyFilter> NormalizeAncestorFilters(IReadOnlyList<AncestorPropertyFilter> filters)
    {
        var result = new List<AncestorPropertyFilter>();
        for (var i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];
            result.Add(filter with
            {
                PropertyIds = filter.PropertyIds?.Distinct().ToList() ?? new List<int>(),
                MatchMode = AvailabilityRequestNormalization.NormalizeMatchMode(filter.MatchMode) ?? "or",
                Scope = AvailabilityRequestNormalization.NormalizeAncestorScope(filter.Scope) ?? "anyAncestor"
            });
        }

        return result;
    }

    private static Dictionary<int, HashSet<int>> BuildReachableAncestorParents(IReadOnlyCollection<int> resourceIds, IReadOnlyList<ResourceRelationLink> relations)
    {
        var fullParentsByChild = new Dictionary<int, List<int>>();
        for (var i = 0; i < relations.Count; i++)
        {
            var relation = relations[i];
            if (!fullParentsByChild.TryGetValue(relation.ChildResourceId, out var parents))
            {
                parents = new List<int>();
                fullParentsByChild[relation.ChildResourceId] = parents;
            }

            if (!parents.Contains(relation.ParentResourceId)) parents.Add(relation.ParentResourceId);
        }

        var reachable = new Dictionary<int, HashSet<int>>();
        var pending = new Queue<int>();
        var visited = new HashSet<int>();
        foreach (var resourceId in resourceIds) if (visited.Add(resourceId)) pending.Enqueue(resourceId);

        while (pending.Count > 0)
        {
            var childId = pending.Dequeue();
            if (!fullParentsByChild.TryGetValue(childId, out var parents)) continue;
            if (!reachable.TryGetValue(childId, out var reachableParents))
            {
                reachableParents = new HashSet<int>();
                reachable[childId] = reachableParents;
            }

            for (var i = 0; i < parents.Count; i++)
            {
                reachableParents.Add(parents[i]);
                if (visited.Add(parents[i])) pending.Enqueue(parents[i]);
            }
        }

        return reachable;
    }

    private static HashSet<int> ResolveAncestors(int resourceId, IReadOnlyDictionary<int, HashSet<int>> parentsByChild, IDictionary<int, HashSet<int>> cache)
    {
        if (cache.TryGetValue(resourceId, out var cached)) return cached;
        var result = new HashSet<int>();
        if (parentsByChild.TryGetValue(resourceId, out var parents))
        {
            foreach (var parent in parents)
            {
                result.Add(parent);
                result.UnionWith(ResolveAncestors(parent, parentsByChild, cache));
            }
        }

        cache[resourceId] = result;
        return result;
    }
}

internal sealed record AncestorFilterResult(List<int> RequiredIds, List<List<int>> OrGroups, bool IsSatisfied);
internal sealed record AncestorFilterMatch(AncestorPropertyFilter Filter, HashSet<int> MatchingAncestors);
internal sealed record AncestorExpansion(Dictionary<int, HashSet<int>> AncestorMap, HashSet<int> AllAncestors, Dictionary<int, HashSet<int>> ParentsByChild)
{
    public static readonly AncestorExpansion Empty = new(new Dictionary<int, HashSet<int>>(), new HashSet<int>(), new Dictionary<int, HashSet<int>>());
}
