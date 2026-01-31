using HelixScheduler.Core;

namespace HelixScheduler.Application.Availability;

public sealed class AvailabilityService : IAvailabilityService
{
    private const int MaxRangeDays = 31;
    private const int MaxRequiredResources = 10;
    private const int MaxOrGroups = 5;
    private const int MaxOrGroupItems = 10;
    private const int MaxTotalResources = 20;
    private const int MaxAncestorFilters = 5;

    private readonly IAvailabilityDataSource _dataSource;
    private readonly PropertySchema.IPropertySchemaService _propertySchemaService;
    private readonly AvailabilityEngineV1 _engine;

    public AvailabilityService(
        IAvailabilityDataSource dataSource,
        PropertySchema.IPropertySchemaService propertySchemaService,
        AvailabilityEngineV1 engine)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _propertySchemaService = propertySchemaService ?? throw new ArgumentNullException(nameof(propertySchemaService));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public async Task<AvailabilityComputeResponse> ComputeAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateRequest(request);
        await ValidatePropertySchemaAsync(request, ct).ConfigureAwait(false);
        await ValidateAncestorFiltersAsync(request, ct).ConfigureAwait(false);

        var computation = await ComputeAvailabilityAsync(request, ct).ConfigureAwait(false);
        var effectiveSlots = ApplySlotDuration(computation.Result.Slots, request);
        if (!request.Explain)
        {
            return new AvailabilityComputeResponse(
                effectiveSlots,
                Array.Empty<AvailabilityExplanation>());
        }

        var explanations = new List<AvailabilityExplanation>();
        if (effectiveSlots.Count == 0 && computation.Result.Slots.Count == 0)
        {
            explanations.Add(BuildEmptyExplanation(computation, request.FromDate, request.ToDate));
        }

        return new AvailabilityComputeResponse(effectiveSlots, explanations);
    }

    private async Task ValidatePropertySchemaAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        if (request.PropertyIds == null || request.PropertyIds.Count == 0)
        {
            return;
        }

        var resourceIds = new HashSet<int>(request.RequiredResourceIds);
        var orGroups = request.ResourceOrGroups ?? Array.Empty<IReadOnlyList<int>>();
        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            for (var i = 0; i < group.Count; i++)
            {
                resourceIds.Add(group[i]);
            }
        }

        await _propertySchemaService.ValidatePropertyFiltersAsync(
            resourceIds.ToList(),
            request.PropertyIds,
            ct).ConfigureAwait(false);
    }

    private async Task ValidateAncestorFiltersAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        if (request.AncestorFilters == null || request.AncestorFilters.Count == 0)
        {
            return;
        }

        for (var i = 0; i < request.AncestorFilters.Count; i++)
        {
            var filter = request.AncestorFilters[i];
            if (filter.PropertyIds == null || filter.PropertyIds.Count == 0)
            {
                continue;
            }

            var propertyIds = filter.PropertyIds.Distinct().ToList();
            await _propertySchemaService.ValidatePropertyFiltersForTypeAsync(
                filter.ResourceTypeId,
                propertyIds,
                ct).ConfigureAwait(false);
        }
    }

    private static void ValidateRequest(AvailabilityComputeRequest request)
    {
        if (request.RequiredResourceIds == null)
        {
            throw new AvailabilityRequestException("resourceIds is required and must contain at least one item.");
        }

        if (request.FromDate > request.ToDate)
        {
            throw new AvailabilityRequestException("fromDate must be less than or equal to toDate.");
        }

        var inclusiveDays = (request.ToDate.DayNumber - request.FromDate.DayNumber) + 1;
        if (inclusiveDays > MaxRangeDays)
        {
            throw new AvailabilityRequestException("Date range must be 31 days or less.");
        }

        if (HasNonPositive(request.RequiredResourceIds))
        {
            throw new AvailabilityRequestException("resourceIds must contain only positive integers.");
        }

        var distinctRequired = request.RequiredResourceIds.Distinct().ToList();
        if (distinctRequired.Count > MaxRequiredResources)
        {
            throw new AvailabilityRequestException("resourceIds must contain at most 10 items.");
        }

        if (request.PropertyIds != null && request.PropertyIds.Count > 0 && HasNonPositive(request.PropertyIds))
        {
            throw new AvailabilityRequestException("propertyIds must contain only positive integers.");
        }

        var orGroups = request.ResourceOrGroups ?? Array.Empty<IReadOnlyList<int>>();
        if (orGroups.Count > MaxOrGroups)
        {
            throw new AvailabilityRequestException("orGroups must contain at most 5 groups.");
        }

        var usedIds = new HashSet<int>(distinctRequired);
        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            if (group == null || group.Count == 0)
            {
                throw new AvailabilityRequestException("orGroups contains an empty group.");
            }

            if (group.Count > MaxOrGroupItems)
            {
                throw new AvailabilityRequestException("orGroups groups must contain at most 10 items.");
            }

            var groupSet = new HashSet<int>();
            for (var i = 0; i < group.Count; i++)
            {
                var value = group[i];
                if (value <= 0)
                {
                    throw new AvailabilityRequestException("orGroups must contain only positive integers.");
                }

                if (!groupSet.Add(value))
                {
                    continue;
                }

                usedIds.Add(value);
            }

            if (groupSet.Count == 0)
            {
                throw new AvailabilityRequestException("orGroups group must contain at least one unique resourceId.");
            }
        }

        if (usedIds.Count == 0)
        {
            throw new AvailabilityRequestException("resourceIds is required and must contain at least one item.");
        }

        if (usedIds.Count > MaxTotalResources)
        {
            throw new AvailabilityRequestException("Total resources must be 20 or less.");
        }

        var includeAncestors = request.IncludeResourceAncestors
            || (request.AncestorFilters?.Count > 0);

        if (includeAncestors)
        {
            var mode = NormalizeAncestorMode(request.AncestorMode);
            if (mode == null)
            {
                throw new AvailabilityRequestException("ancestorMode must be 'perGroup' or 'global'.");
            }
        }

        if (request.AncestorFilters != null && request.AncestorFilters.Count > 0)
        {
            if (request.AncestorFilters.Count > MaxAncestorFilters)
            {
                throw new AvailabilityRequestException("ancestorFilters must contain at most 5 entries.");
            }

            for (var i = 0; i < request.AncestorFilters.Count; i++)
            {
                var filter = request.AncestorFilters[i];
                if (filter.ResourceTypeId <= 0)
                {
                    throw new AvailabilityRequestException("ancestorFilters requires positive resourceTypeId.");
                }

                if (filter.PropertyIds == null || filter.PropertyIds.Count == 0)
                {
                    throw new AvailabilityRequestException("ancestorFilters requires propertyIds.");
                }

                if (HasNonPositive(filter.PropertyIds))
                {
                    throw new AvailabilityRequestException("ancestorFilters propertyIds must be positive integers.");
                }

                if (NormalizeMatchMode(filter.MatchMode) == null)
                {
                    throw new AvailabilityRequestException("ancestorFilters matchMode must be 'or' or 'and'.");
                }

                if (NormalizeAncestorScope(filter.Scope) == null)
                {
                    throw new AvailabilityRequestException(
                        "ancestorFilters scope must be 'anyAncestor', 'directParent', or 'nearestOfType'.");
                }
            }
        }

        if (request.SlotDurationMinutes.HasValue)
        {
            if (request.SlotDurationMinutes.Value <= 0 || request.SlotDurationMinutes.Value > 1440)
            {
                throw new AvailabilityRequestException("slotDurationMinutes must be between 1 and 1440.");
            }
        }
    }

    private async Task<AvailabilityComputation> ComputeAvailabilityAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        var requiredIds = request.RequiredResourceIds.Distinct().ToList();
        requiredIds.Sort();

        var orGroups = NormalizeOrGroups(request.ResourceOrGroups);
        var filteredResourceIds = new HashSet<int>(requiredIds);
        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            for (var i = 0; i < orGroups[groupIndex].Count; i++)
            {
                filteredResourceIds.Add(orGroups[groupIndex][i]);
            }
        }

        HashSet<int>? propertyFiltered = null;
        var propertyIds = request.PropertyIds?.Distinct().ToList() ?? new List<int>();
        if (propertyIds.Count > 0)
        {
            if (!request.IncludePropertyDescendants && propertyIds.Count > 1)
            {
                var resourceMatches = await _dataSource
                    .GetResourceIdsByAllPropertiesAsync(propertyIds, ct)
                    .ConfigureAwait(false);
                propertyFiltered = new HashSet<int>(resourceMatches);
            }
            else
            {
                foreach (var propertyId in propertyIds)
                {
                    var effectivePropertyIds = request.IncludePropertyDescendants
                        ? await ExpandPropertyIdsAsync(propertyId, ct).ConfigureAwait(false)
                        : new List<int> { propertyId };

                    var resourceMatches = await _dataSource
                        .GetResourceIdsByPropertiesAsync(effectivePropertyIds, ct)
                        .ConfigureAwait(false);

                    var matchSet = new HashSet<int>(resourceMatches);
                    if (propertyFiltered == null)
                    {
                        propertyFiltered = matchSet;
                    }
                    else
                    {
                        propertyFiltered.IntersectWith(matchSet);
                        if (propertyFiltered.Count == 0)
                        {
                            return AvailabilityComputation.Empty();
                        }
                    }
                }
            }

            if (propertyFiltered == null)
            {
                return AvailabilityComputation.Empty();
            }

            filteredResourceIds.IntersectWith(propertyFiltered);
        }

        if (filteredResourceIds.Count == 0)
        {
            return AvailabilityComputation.Empty();
        }

        var includeAncestors = request.IncludeResourceAncestors
            || (request.AncestorFilters?.Count > 0);
        var ancestorExpansion = includeAncestors
            ? await BuildAncestorExpansionAsync(filteredResourceIds, request.AncestorRelationTypes, ct)
                .ConfigureAwait(false)
            : AncestorExpansion.Empty;

        var filteredRequiredIds = propertyFiltered == null
            ? requiredIds.ToList()
            : requiredIds.Where(filteredResourceIds.Contains).ToList();
        filteredRequiredIds = filteredRequiredIds.Distinct().ToList();
        filteredRequiredIds.Sort();

        var filteredOrGroups = new List<List<int>>(orGroups.Count);
        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            var filteredGroup = propertyFiltered == null
                ? group
                : group.Where(filteredResourceIds.Contains).ToList();
            filteredGroup.Sort();

            if (filteredGroup.Count == 0)
            {
                return AvailabilityComputation.Empty();
            }

            filteredOrGroups.Add(filteredGroup);
        }

        if (filteredRequiredIds.Count == 0 && filteredOrGroups.Count == 0)
        {
            return AvailabilityComputation.Empty();
        }

        if (request.AncestorFilters != null && request.AncestorFilters.Count > 0)
        {
            var filterResult = await ApplyAncestorFiltersAsync(
                filteredRequiredIds,
                filteredOrGroups,
                ancestorExpansion,
                request.AncestorFilters,
                ct).ConfigureAwait(false);

            if (!filterResult.IsSatisfied)
            {
                return AvailabilityComputation.Empty();
            }

            filteredRequiredIds = filterResult.RequiredIds;
            filteredOrGroups = filterResult.OrGroups;
        }

        filteredResourceIds = new HashSet<int>(filteredRequiredIds);
        for (var groupIndex = 0; groupIndex < filteredOrGroups.Count; groupIndex++)
        {
            for (var i = 0; i < filteredOrGroups[groupIndex].Count; i++)
            {
                filteredResourceIds.Add(filteredOrGroups[groupIndex][i]);
            }
        }

        if (includeAncestors)
        {
            var ancestorIds = CollectAncestors(filteredResourceIds, ancestorExpansion);
            filteredResourceIds.UnionWith(ancestorIds);
        }

        var resourceIdList = filteredResourceIds.ToList();
        resourceIdList.Sort();

        var rules = await _dataSource.GetRulesAsync(
            request.FromDate,
            request.ToDate,
            resourceIdList,
            ct).ConfigureAwait(false);

        var resourceCapacities = await _dataSource
            .GetResourceCapacitiesAsync(resourceIdList, ct)
            .ConfigureAwait(false);

        var ruleModels = new List<RuleModel>();
        var period = new DatePeriod(request.FromDate, request.ToDate);
        var hasPositive = false;
        var hasNegative = false;

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule.ResourceIds.Count == 0)
            {
                continue;
            }

            for (var r = 0; r < rule.ResourceIds.Count; r++)
            {
                var resourceId = rule.ResourceIds[r];
                if (!filteredResourceIds.Contains(resourceId))
                {
                    continue;
                }

                var model = new RuleModel(
                    rule.Id,
                    (RuleKind)rule.Kind,
                    rule.IsExclude,
                    rule.FromDateUtc,
                    rule.ToDateUtc,
                    rule.SingleDateUtc,
                    rule.StartTime.ToTimeSpan(),
                    rule.EndTime.ToTimeSpan(),
                    rule.DaysOfWeekMask,
                    rule.DayOfMonth,
                    rule.IntervalDays,
                    resourceId);

                if (!model.IsExclude && RuleAppliesToPeriod(model, period))
                {
                    hasPositive = true;
                }

                if (model.IsExclude && RuleAppliesToPeriod(model, period))
                {
                    hasNegative = true;
                }

                ruleModels.Add(model);
            }
        }

        var fromUtc = DateTime.SpecifyKind(request.FromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtcExclusive = DateTime.SpecifyKind(request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var busyEvents = await _dataSource
            .GetBusyEventsAsync(fromUtc, toUtcExclusive, resourceIdList, ct)
            .ConfigureAwait(false);

        var busySlots = new List<BusySlotModel>();
        for (var i = 0; i < busyEvents.Count; i++)
        {
            var busyEvent = busyEvents[i];
            for (var r = 0; r < busyEvent.ResourceIds.Count; r++)
            {
                var resourceId = busyEvent.ResourceIds[r];
                if (!filteredResourceIds.Contains(resourceId))
                {
                    continue;
                }

                var startUtc = DateTime.SpecifyKind(busyEvent.StartUtc, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(busyEvent.EndUtc, DateTimeKind.Utc);
                busySlots.Add(new BusySlotModel(startUtc, endUtc, resourceId));
            }
        }

        var inputs = new AvailabilityInputs(ruleModels, busySlots, resourceCapacities);
        var ancestorMode = NormalizeAncestorMode(request.AncestorMode) ?? "perGroup";

        if (includeAncestors && ancestorMode == "perGroup")
        {
            var requiredWithAncestors = ExpandRequiredAncestors(filteredRequiredIds, ancestorExpansion);
            if (filteredOrGroups.Count == 0)
            {
                var query = new AvailabilityQuery(period, requiredWithAncestors);
                var result = _engine.Compute(query, inputs);
                return new AvailabilityComputation(result, hasPositive, hasNegative, busySlots.Count > 0);
            }

            var resultWithAncestors = ComputePerGroupAvailability(
                period,
                requiredWithAncestors,
                filteredOrGroups,
                ancestorExpansion,
                resourceIdList,
                inputs);
            return new AvailabilityComputation(resultWithAncestors, hasPositive, hasNegative, busySlots.Count > 0);
        }

        var globalRequired = includeAncestors
            ? ExpandRequiredAncestors(filteredRequiredIds, ancestorExpansion)
            : filteredRequiredIds;

        if (globalRequired.Count == 0 && filteredOrGroups.Count > 0)
        {
            var result = ComputeOrOnlyAvailability(period, filteredOrGroups, resourceIdList, inputs);
            return new AvailabilityComputation(result, hasPositive, hasNegative, busySlots.Count > 0);
        }

        var queryWithRequired = new AvailabilityQuery(period, globalRequired, resourceOrGroups: filteredOrGroups);
        var resultWithRequired = _engine.Compute(queryWithRequired, inputs);

        return new AvailabilityComputation(resultWithRequired, hasPositive, hasNegative, busySlots.Count > 0);
    }

    private static HashSet<int> CollectAncestors(
        IReadOnlyCollection<int> resourceIds,
        AncestorExpansion expansion)
    {
        if (resourceIds.Count == 0 || expansion.AncestorMap.Count == 0)
        {
            return new HashSet<int>();
        }

        var result = new HashSet<int>();
        foreach (var resourceId in resourceIds)
        {
            if (expansion.AncestorMap.TryGetValue(resourceId, out var ancestors))
            {
                result.UnionWith(ancestors);
            }
        }

        return result;
    }

    private async Task<AncestorFilterResult> ApplyAncestorFiltersAsync(
        IReadOnlyList<int> requiredIds,
        IReadOnlyList<List<int>> orGroups,
        AncestorExpansion expansion,
        IReadOnlyList<AncestorPropertyFilter> filters,
        CancellationToken ct)
    {
        var normalizedFilters = NormalizeAncestorFilters(filters);
        if (normalizedFilters.Count == 0)
        {
            return new AncestorFilterResult(requiredIds.ToList(), orGroups.ToList(), true);
        }

        var ancestorIds = CollectAncestors(requiredIds.Concat(orGroups.SelectMany(group => group)).ToHashSet(), expansion);
        if (ancestorIds.Count == 0)
        {
            return new AncestorFilterResult(requiredIds.ToList(), orGroups.ToList(), false);
        }

        var assignments = await _propertySchemaService
            .GetResourceTypeAssignmentsAsync(ancestorIds.ToList(), ct)
            .ConfigureAwait(false);
        var typeByResourceId = assignments
            .GroupBy(item => item.ResourceId)
            .ToDictionary(group => group.Key, group => group.First().ResourceTypeId);

        var filterMatches = new List<AncestorFilterMatch>();
        for (var i = 0; i < normalizedFilters.Count; i++)
        {
            var filter = normalizedFilters[i];
            var candidates = ancestorIds
                .Where(id => typeByResourceId.TryGetValue(id, out var typeId) && typeId == filter.ResourceTypeId)
                .ToHashSet();

            if (candidates.Count == 0)
            {
                filterMatches.Add(new AncestorFilterMatch(filter, new HashSet<int>()));
                continue;
            }

            var matches = await ResolveMatchingAncestorsAsync(filter, candidates, ct).ConfigureAwait(false);
            filterMatches.Add(new AncestorFilterMatch(filter, matches));
        }

        var filteredRequired = new List<int>();
        for (var i = 0; i < requiredIds.Count; i++)
        {
            var resourceId = requiredIds[i];
            if (ResourcePassesFilters(resourceId, filterMatches, expansion, typeByResourceId))
            {
                filteredRequired.Add(resourceId);
            }
            else
            {
                return new AncestorFilterResult(new List<int>(), new List<List<int>>(), false);
            }
        }

        var filteredGroups = new List<List<int>>();
        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            var filteredGroup = new List<int>();
            for (var i = 0; i < group.Count; i++)
            {
                var resourceId = group[i];
                if (ResourcePassesFilters(resourceId, filterMatches, expansion, typeByResourceId))
                {
                    filteredGroup.Add(resourceId);
                }
            }

            if (filteredGroup.Count == 0)
            {
                return new AncestorFilterResult(new List<int>(), new List<List<int>>(), false);
            }

            filteredGroup.Sort();
            filteredGroups.Add(filteredGroup);
        }

        return new AncestorFilterResult(filteredRequired, filteredGroups, true);
    }

    private async Task<HashSet<int>> ResolveMatchingAncestorsAsync(
        AncestorPropertyFilter filter,
        HashSet<int> candidates,
        CancellationToken ct)
    {
        var propertyGroups = await ExpandAncestorPropertyGroupsAsync(filter, ct).ConfigureAwait(false);
        if (propertyGroups.Count == 0)
        {
            return new HashSet<int>();
        }

        HashSet<int>? intersection = null;
        var union = new HashSet<int>();
        var isAnd = NormalizeMatchMode(filter.MatchMode) == "and";

        for (var i = 0; i < propertyGroups.Count; i++)
        {
            var propertyIds = propertyGroups[i];
            var resourceMatches = await _dataSource
                .GetResourceIdsByPropertiesAsync(propertyIds, ct)
                .ConfigureAwait(false);
            var matchSet = new HashSet<int>(resourceMatches);

            if (isAnd)
            {
                intersection ??= matchSet;
                intersection.IntersectWith(matchSet);
            }
            else
            {
                union.UnionWith(matchSet);
            }
        }

        var matches = isAnd ? intersection ?? new HashSet<int>() : union;
        matches.IntersectWith(candidates);
        return matches;
    }

    private async Task<List<List<int>>> ExpandAncestorPropertyGroupsAsync(
        AncestorPropertyFilter filter,
        CancellationToken ct)
    {
        var groups = new List<List<int>>();
        if (filter.PropertyIds == null || filter.PropertyIds.Count == 0)
        {
            return groups;
        }

        for (var i = 0; i < filter.PropertyIds.Count; i++)
        {
            var propertyId = filter.PropertyIds[i];
            var expanded = filter.IncludePropertyDescendants
                ? await ExpandPropertyIdsAsync(propertyId, ct).ConfigureAwait(false)
                : new List<int> { propertyId };
            if (expanded.Count > 0)
            {
                groups.Add(expanded.Distinct().ToList());
            }
        }

        return groups;
    }

    private static bool ResourcePassesFilters(
        int resourceId,
        IReadOnlyList<AncestorFilterMatch> filters,
        AncestorExpansion expansion,
        IReadOnlyDictionary<int, int> typeByResourceId)
    {
        for (var i = 0; i < filters.Count; i++)
        {
            var filterMatch = filters[i];
            var eligibleAncestors = GetEligibleAncestors(
                resourceId,
                filterMatch.Filter,
                expansion,
                typeByResourceId);

            if (eligibleAncestors.Count == 0)
            {
                return false;
            }

            if (filterMatch.Filter.MatchAllAncestors)
            {
                foreach (var ancestorId in eligibleAncestors)
                {
                    if (!filterMatch.MatchingAncestors.Contains(ancestorId))
                    {
                        return false;
                    }
                }
            }
            else
            {
                var hasMatch = false;
                foreach (var ancestorId in eligibleAncestors)
                {
                    if (filterMatch.MatchingAncestors.Contains(ancestorId))
                    {
                        hasMatch = true;
                        break;
                    }
                }

                if (!hasMatch)
                {
                    return false;
                }
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
        var scope = NormalizeAncestorScope(filter.Scope) ?? "anyAncestor";
        var result = new HashSet<int>();

        switch (scope)
        {
            case "directParent":
                if (expansion.ParentsByChild.TryGetValue(resourceId, out var parents))
                {
                    foreach (var parent in parents)
                    {
                        if (typeByResourceId.TryGetValue(parent, out var typeId)
                            && typeId == filter.ResourceTypeId)
                        {
                            result.Add(parent);
                        }
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
                if (typeByResourceId.TryGetValue(ancestor, out var typeId)
                    && typeId == filter.ResourceTypeId)
                {
                    result.Add(ancestor);
                }
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
        if (!parentsByChild.TryGetValue(resourceId, out var parents))
        {
            return new HashSet<int>();
        }

        var visited = new HashSet<int>();
        var queue = new Queue<(int ResourceId, int Depth)>();
        foreach (var parent in parents)
        {
            queue.Enqueue((parent, 1));
        }

        var result = new HashSet<int>();
        int? matchDepth = null;

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (matchDepth.HasValue && depth > matchDepth.Value)
            {
                break;
            }

            if (!visited.Add(current))
            {
                continue;
            }

            if (typeByResourceId.TryGetValue(current, out var typeId) && typeId == resourceTypeId)
            {
                result.Add(current);
                matchDepth ??= depth;
                continue;
            }

            if (matchDepth.HasValue)
            {
                continue;
            }

            if (parentsByChild.TryGetValue(current, out var nextParents))
            {
                foreach (var parent in nextParents)
                {
                    queue.Enqueue((parent, depth + 1));
                }
            }
        }

        return result;
    }

    private static List<AncestorPropertyFilter> NormalizeAncestorFilters(
        IReadOnlyList<AncestorPropertyFilter> filters)
    {
        var result = new List<AncestorPropertyFilter>();
        for (var i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];
            var propertyIds = filter.PropertyIds?.Distinct().ToList() ?? new List<int>();
            result.Add(filter with
            {
                PropertyIds = propertyIds,
                MatchMode = NormalizeMatchMode(filter.MatchMode) ?? "or",
                Scope = NormalizeAncestorScope(filter.Scope) ?? "anyAncestor"
            });
        }

        return result;
    }

    private static List<int> ExpandRequiredAncestors(
        IReadOnlyList<int> requiredIds,
        AncestorExpansion expansion)
    {
        if (requiredIds.Count == 0 || expansion.AncestorMap.Count == 0)
        {
            return requiredIds.ToList();
        }

        var result = new HashSet<int>(requiredIds);
        for (var i = 0; i < requiredIds.Count; i++)
        {
            if (expansion.AncestorMap.TryGetValue(requiredIds[i], out var ancestors))
            {
                result.UnionWith(ancestors);
            }
        }

        var list = result.ToList();
        list.Sort();
        return list;
    }

    private AvailabilityResult ComputePerGroupAvailability(
        DatePeriod period,
        IReadOnlyList<int> requiredIds,
        IReadOnlyList<List<int>> orGroups,
        AncestorExpansion expansion,
        IReadOnlyList<int> allResourceIds,
        AvailabilityInputs inputs)
    {
        List<UtcSlot>? intersection = null;

        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            if (group.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            var union = UnionGroupAvailabilityWithAncestors(period, requiredIds, group, expansion, allResourceIds, inputs);
            if (union.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            intersection = intersection == null
                ? union
                : IntersectSlots(intersection, union);

            if (intersection.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }
        }

        if (intersection == null)
        {
            return new AvailabilityResult(Array.Empty<UtcSlot>());
        }

        var normalized = NormalizeSlots(intersection);
        return new AvailabilityResult(normalized);
    }

    private List<UtcSlot> UnionGroupAvailabilityWithAncestors(
        DatePeriod period,
        IReadOnlyList<int> requiredIds,
        IReadOnlyList<int> groupResourceIds,
        AncestorExpansion expansion,
        IReadOnlyList<int> allResourceIds,
        AvailabilityInputs inputs)
    {
        var slots = new List<UtcSlot>();
        for (var i = 0; i < groupResourceIds.Count; i++)
        {
            var resourceId = groupResourceIds[i];
            var required = new HashSet<int>(requiredIds) { resourceId };
            if (expansion.AncestorMap.TryGetValue(resourceId, out var ancestors))
            {
                required.UnionWith(ancestors);
            }

            var requiredList = required.ToList();
            requiredList.Sort();
            var query = new AvailabilityQuery(period, requiredList);
            var result = _engine.Compute(query, inputs);
            for (var s = 0; s < result.Slots.Count; s++)
            {
                var slot = result.Slots[s];
                slots.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, allResourceIds));
            }
        }

        return NormalizeSlots(slots);
    }

    private async Task<AncestorExpansion> BuildAncestorExpansionAsync(
        IReadOnlyCollection<int> resourceIds,
        IReadOnlyList<string>? relationTypes,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return AncestorExpansion.Empty;
        }

        var normalizedTypes = NormalizeRelationTypes(relationTypes);
        var parentsByChild = new Dictionary<int, HashSet<int>>();
        var pending = new HashSet<int>(resourceIds);
        var processed = new HashSet<int>();

        while (pending.Count > 0)
        {
            var batch = pending.Where(id => processed.Add(id)).ToList();
            pending.Clear();
            if (batch.Count == 0)
            {
                break;
            }

            var relations = await _dataSource
                .GetResourceRelationsAsync(batch, normalizedTypes, ct)
                .ConfigureAwait(false);

            for (var i = 0; i < relations.Count; i++)
            {
                var relation = relations[i];
                if (!parentsByChild.TryGetValue(relation.ChildResourceId, out var parents))
                {
                    parents = new HashSet<int>();
                    parentsByChild[relation.ChildResourceId] = parents;
                }

                if (parents.Add(relation.ParentResourceId) && !processed.Contains(relation.ParentResourceId))
                {
                    pending.Add(relation.ParentResourceId);
                }
            }
        }

        var ancestorMap = new Dictionary<int, HashSet<int>>();
        var allAncestors = new HashSet<int>();
        foreach (var resourceId in resourceIds)
        {
            var ancestors = ResolveAncestors(resourceId, parentsByChild, ancestorMap);
            if (ancestors.Count > 0)
            {
                allAncestors.UnionWith(ancestors);
            }
        }

        return new AncestorExpansion(ancestorMap, allAncestors, parentsByChild);
    }

    private static HashSet<int> ResolveAncestors(
        int resourceId,
        IReadOnlyDictionary<int, HashSet<int>> parentsByChild,
        IDictionary<int, HashSet<int>> cache)
    {
        if (cache.TryGetValue(resourceId, out var cached))
        {
            return cached;
        }

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

    private static IReadOnlyList<string>? NormalizeRelationTypes(IReadOnlyList<string>? relationTypes)
    {
        if (relationTypes == null || relationTypes.Count == 0)
        {
            return null;
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < relationTypes.Count; i++)
        {
            var value = relationTypes[i]?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            set.Add(value);
        }

        return set.Count == 0 ? null : set.ToList();
    }

    private static IReadOnlyList<UtcSlot> ApplySlotDuration(
        IReadOnlyList<UtcSlot> slots,
        AvailabilityComputeRequest request)
    {
        if (slots.Count == 0 || !request.SlotDurationMinutes.HasValue)
        {
            return slots;
        }

        var duration = TimeSpan.FromMinutes(request.SlotDurationMinutes.Value);
        var includeRemainder = request.IncludeRemainderSlot;
        if (duration <= TimeSpan.Zero)
        {
            return slots;
        }

        var result = new List<UtcSlot>();
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var length = slot.EndUtc - slot.StartUtc;
            if (length <= TimeSpan.Zero)
            {
                continue;
            }

            var fullCount = (int)(length.Ticks / duration.Ticks);
            for (var index = 0; index < fullCount; index++)
            {
                var start = slot.StartUtc.AddTicks(duration.Ticks * index);
                var end = start.Add(duration);
                result.Add(new UtcSlot(start, end, slot.ResourceIds));
            }

            var remainderTicks = length.Ticks - (duration.Ticks * fullCount);
            if (includeRemainder && remainderTicks > 0)
            {
                var remainderStart = slot.StartUtc.AddTicks(duration.Ticks * fullCount);
                var remainderEnd = slot.EndUtc;
                if (remainderEnd > remainderStart)
                {
                    result.Add(new UtcSlot(remainderStart, remainderEnd, slot.ResourceIds));
                }
            }
        }

        return result;
    }

    private static string? NormalizeAncestorMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return "perGroup";
        }

        mode = mode.Trim();

        if (mode.Equals("perGroup", StringComparison.OrdinalIgnoreCase))
        {
            return "perGroup";
        }

        return mode.Equals("global", StringComparison.OrdinalIgnoreCase)
            ? "global"
            : null;
    }

    private static string? NormalizeMatchMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return "or";
        }

        return mode.Equals("or", StringComparison.OrdinalIgnoreCase)
            ? "or"
            : mode.Equals("and", StringComparison.OrdinalIgnoreCase)
                ? "and"
                : null;
    }

    private static string? NormalizeAncestorScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return "anyAncestor";
        }

        return scope.Equals("anyAncestor", StringComparison.OrdinalIgnoreCase)
            ? "anyAncestor"
            : scope.Equals("directParent", StringComparison.OrdinalIgnoreCase)
                ? "directParent"
                : scope.Equals("nearestOfType", StringComparison.OrdinalIgnoreCase)
                    ? "nearestOfType"
                    : null;
    }

    private sealed record AncestorFilterResult(
        List<int> RequiredIds,
        List<List<int>> OrGroups,
        bool IsSatisfied);

    private sealed record AncestorFilterMatch(
        AncestorPropertyFilter Filter,
        HashSet<int> MatchingAncestors);

    private sealed record AncestorExpansion(
        Dictionary<int, HashSet<int>> AncestorMap,
        HashSet<int> AllAncestors,
        Dictionary<int, HashSet<int>> ParentsByChild)
    {
        public static readonly AncestorExpansion Empty = new(
            new Dictionary<int, HashSet<int>>(),
            new HashSet<int>(),
            new Dictionary<int, HashSet<int>>());
    }

    private async Task<List<int>> ExpandPropertyIdsAsync(int propertyId, CancellationToken ct)
    {
        var subtree = await _dataSource.ExpandPropertySubtreeAsync(propertyId, ct).ConfigureAwait(false);
        var ids = new List<int>(subtree.Count);
        for (var i = 0; i < subtree.Count; i++)
        {
            ids.Add(subtree[i].Id);
        }

        return ids;
    }

    private static AvailabilityExplanation BuildEmptyExplanation(
        AvailabilityComputation computation,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var fromUtc = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtcExclusive = DateTime.SpecifyKind(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        if (!computation.HasPositiveRules)
        {
            return new AvailabilityExplanation(
                "NoPositiveRule",
                ResourceId: null,
                FromUtc: fromUtc,
                ToUtc: toUtcExclusive,
                RuleId: null,
                BusyEventId: null,
                "No positive rules apply to the requested range.");
        }

        if (computation.HasBusySlots)
        {
            return new AvailabilityExplanation(
                "FullyBlockedByBusy",
                ResourceId: null,
                FromUtc: fromUtc,
                ToUtc: toUtcExclusive,
                RuleId: null,
                BusyEventId: null,
                "Busy events block availability in the requested range.");
        }

        if (computation.HasNegativeRules)
        {
            return new AvailabilityExplanation(
                "FullyBlockedByNegativeRule",
                ResourceId: null,
                FromUtc: fromUtc,
                ToUtc: toUtcExclusive,
                RuleId: null,
                BusyEventId: null,
                "Negative rules block availability in the requested range.");
        }

        return new AvailabilityExplanation(
            "PartiallyBlocked",
            ResourceId: null,
            FromUtc: fromUtc,
            ToUtc: toUtcExclusive,
            RuleId: null,
            BusyEventId: null,
            "Availability is blocked by rules or busy events.");
    }

    private static bool RuleAppliesToPeriod(RuleModel rule, DatePeriod period)
    {
        return rule.Kind switch
        {
            RuleKind.RecurringWeekly => WeeklyRuleApplies(rule, period),
            RuleKind.SingleDate => SingleDateRuleApplies(rule, period),
            RuleKind.Range => RangeRuleApplies(rule, period),
            _ => false
        };
    }

    private static bool WeeklyRuleApplies(RuleModel rule, DatePeriod period)
    {
        if (rule.DaysOfWeekMask == null)
        {
            return false;
        }

        var start = rule.FromDate ?? period.From;
        var end = rule.ToDate ?? period.To;
        if (end < period.From || start > period.To)
        {
            return false;
        }

        var from = start < period.From ? period.From : start;
        var to = end > period.To ? period.To : end;

        foreach (var day in EnumerateDays(from, to))
        {
            if (MatchesDayOfWeek(day.DayOfWeek, rule.DaysOfWeekMask.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SingleDateRuleApplies(RuleModel rule, DatePeriod period)
    {
        if (rule.SingleDate == null)
        {
            return false;
        }

        var date = rule.SingleDate.Value;
        return date >= period.From && date <= period.To;
    }

    private static bool RangeRuleApplies(RuleModel rule, DatePeriod period)
    {
        var start = rule.FromDate ?? period.From;
        var end = rule.ToDate ?? period.To;
        return !(end < period.From || start > period.To);
    }

    private static IEnumerable<DateOnly> EnumerateDays(DateOnly from, DateOnly to)
    {
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            yield return day;
        }
    }

    private static bool MatchesDayOfWeek(DayOfWeek dayOfWeek, int mask)
    {
        var bit = 1 << (int)dayOfWeek;
        return (mask & bit) == bit;
    }

    private static List<List<int>> NormalizeOrGroups(IReadOnlyList<IReadOnlyList<int>>? resourceOrGroups)
    {
        if (resourceOrGroups == null || resourceOrGroups.Count == 0)
        {
            return new List<List<int>>();
        }

        var result = new List<List<int>>();
        for (var i = 0; i < resourceOrGroups.Count; i++)
        {
            result.Add(resourceOrGroups[i].Distinct().ToList());
        }

        return result;
    }

    private static bool HasNonPositive(IReadOnlyList<int> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] <= 0)
            {
                return true;
            }
        }

        return false;
    }

    private AvailabilityResult ComputeOrOnlyAvailability(
        DatePeriod period,
        IReadOnlyList<List<int>> orGroups,
        IReadOnlyList<int> allResourceIds,
        AvailabilityInputs inputs)
    {
        List<UtcSlot>? intersection = null;

        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            if (group.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            var union = UnionGroupAvailability(period, group, inputs, allResourceIds);
            if (union.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            intersection = intersection == null
                ? union
                : IntersectSlots(intersection, union);

            if (intersection.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }
        }

        if (intersection == null)
        {
            return new AvailabilityResult(Array.Empty<UtcSlot>());
        }

        var normalized = NormalizeSlots(intersection);
        return new AvailabilityResult(normalized);
    }

    private List<UtcSlot> UnionGroupAvailability(
        DatePeriod period,
        IReadOnlyList<int> groupResourceIds,
        AvailabilityInputs inputs,
        IReadOnlyList<int> allResourceIds)
    {
        var slots = new List<UtcSlot>();
        for (var i = 0; i < groupResourceIds.Count; i++)
        {
            var resourceId = groupResourceIds[i];
            var query = new AvailabilityQuery(period, new List<int> { resourceId });
            var result = _engine.Compute(query, inputs);
            for (var s = 0; s < result.Slots.Count; s++)
            {
                var slot = result.Slots[s];
                slots.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, allResourceIds));
            }
        }

        return NormalizeSlots(slots);
    }

    private static List<UtcSlot> IntersectSlots(IReadOnlyList<UtcSlot> first, IReadOnlyList<UtcSlot> second)
    {
        var result = new List<UtcSlot>();
        var i = 0;
        var j = 0;
        while (i < first.Count && j < second.Count)
        {
            var start = first[i].StartUtc > second[j].StartUtc ? first[i].StartUtc : second[j].StartUtc;
            var end = first[i].EndUtc < second[j].EndUtc ? first[i].EndUtc : second[j].EndUtc;

            if (end > start)
            {
                result.Add(new UtcSlot(start, end, first[i].ResourceIds));
            }

            if (first[i].EndUtc <= second[j].EndUtc)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return result;
    }

    private static List<UtcSlot> NormalizeSlots(IReadOnlyList<UtcSlot> slots)
    {
        if (slots.Count == 0)
        {
            return new List<UtcSlot>();
        }

        var ordered = new List<UtcSlot>(slots);
        ordered.Sort(CompareSlotsByTime);

        var normalized = new List<UtcSlot> { ordered[0] };
        for (var index = 1; index < ordered.Count; index++)
        {
            var last = normalized[^1];
            var current = ordered[index];

            if (current.StartUtc <= last.EndUtc)
            {
                var end = current.EndUtc > last.EndUtc ? current.EndUtc : last.EndUtc;
                normalized[^1] = new UtcSlot(last.StartUtc, end, last.ResourceIds);
            }
            else
            {
                normalized.Add(current);
            }
        }

        return normalized;
    }

    private static int CompareSlotsByTime(UtcSlot? x, UtcSlot? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var startCompare = x.StartUtc.CompareTo(y.StartUtc);
        if (startCompare != 0)
        {
            return startCompare;
        }

        return x.EndUtc.CompareTo(y.EndUtc);
    }

    private sealed class AvailabilityComputation
    {
        public AvailabilityComputation(
            AvailabilityResult result,
            bool hasPositiveRules,
            bool hasNegativeRules,
            bool hasBusySlots)
        {
            Result = result;
            HasPositiveRules = hasPositiveRules;
            HasNegativeRules = hasNegativeRules;
            HasBusySlots = hasBusySlots;
        }

        public AvailabilityResult Result { get; }
        public bool HasPositiveRules { get; }
        public bool HasNegativeRules { get; }
        public bool HasBusySlots { get; }

        public static AvailabilityComputation Empty()
        {
            return new AvailabilityComputation(
                new AvailabilityResult(Array.Empty<UtcSlot>()),
                hasPositiveRules: false,
                hasNegativeRules: false,
                hasBusySlots: false);
        }
    }
}
