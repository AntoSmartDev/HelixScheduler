using HelixScheduler.Core;

namespace HelixScheduler.Application.Availability;

public sealed class AvailabilityService : IAvailabilityService
{
    private readonly IAvailabilityComputeQueryService _computeQueryService;
    private readonly AvailabilityRequestValidator _requestValidator;
    private readonly AvailabilityPropertyFilterEvaluator _propertyFilterEvaluator;
    private readonly AvailabilityAncestorHandler _ancestorHandler;
    private readonly AvailabilityEngine _engine;

    public AvailabilityService(
        IAvailabilityComputeQueryService computeQueryService,
        IAvailabilityFilterQueryService filterQueryService,
        IAvailabilityAncestorQueryService ancestorQueryService,
        PropertySchema.IPropertySchemaService propertySchemaService,
        AvailabilityEngine engine)
    {
        _computeQueryService = computeQueryService ?? throw new ArgumentNullException(nameof(computeQueryService));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));

        _propertyFilterEvaluator = new AvailabilityPropertyFilterEvaluator(
            filterQueryService ?? throw new ArgumentNullException(nameof(filterQueryService)));
        _requestValidator = new AvailabilityRequestValidator(
            propertySchemaService ?? throw new ArgumentNullException(nameof(propertySchemaService)));
        _ancestorHandler = new AvailabilityAncestorHandler(
            ancestorQueryService ?? throw new ArgumentNullException(nameof(ancestorQueryService)),
            propertySchemaService,
            _propertyFilterEvaluator);
    }

    public async Task<AvailabilityComputeResponse> ComputeAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        await _requestValidator.ValidateAsync(request, ct).ConfigureAwait(false);

        var computation = await ComputeAvailabilityAsync(request, ct).ConfigureAwait(false);
        var effectiveSlots = AvailabilitySlotPostProcessor.ApplySlotDuration(computation.Result.Slots, request);
        if (!request.Explain)
        {
            return new AvailabilityComputeResponse(
                effectiveSlots,
                Array.Empty<AvailabilityExplanation>());
        }

        var explanations = new List<AvailabilityExplanation>();
        if (effectiveSlots.Count == 0 && computation.Result.Slots.Count == 0)
        {
            explanations.Add(AvailabilitySlotPostProcessor.BuildEmptyExplanation(
                computation.Result,
                computation.HasPositiveRules,
                computation.HasNegativeRules,
                computation.HasBusySlots,
                request.FromDate,
                request.ToDate));
        }

        return new AvailabilityComputeResponse(effectiveSlots, explanations);
    }

    private async Task<AvailabilityComputation> ComputeAvailabilityAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        var requiredIds = request.RequiredResourceIds.Distinct().ToList();
        requiredIds.Sort();

        var orGroups = AvailabilityRequestNormalization.NormalizeOrGroups(request.ResourceOrGroups);
        var filteredResourceIds = new HashSet<int>(requiredIds);
        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            for (var i = 0; i < orGroups[groupIndex].Count; i++)
            {
                filteredResourceIds.Add(orGroups[groupIndex][i]);
            }
        }

        HashSet<int>? propertyFiltered = null;
        var propertyExecutionContext = new PropertyFilterExecutionContext();
        var propertyGroups = _propertyFilterEvaluator.NormalizePropertyFilterGroups(request);
        if (propertyGroups.Count > 0)
        {
            for (var i = 0; i < propertyGroups.Count; i++)
            {
                var groupMatch = await _propertyFilterEvaluator.EvaluatePropertyFilterGroupAsync(
                    propertyGroups[i],
                    propertyExecutionContext,
                    ct).ConfigureAwait(false);

                if (groupMatch.Count == 0)
                {
                    return AvailabilityComputation.Empty();
                }

                if (propertyFiltered == null)
                {
                    propertyFiltered = groupMatch;
                }
                else
                {
                    propertyFiltered.IntersectWith(groupMatch);
                    if (propertyFiltered.Count == 0)
                    {
                        return AvailabilityComputation.Empty();
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
            ? await _ancestorHandler.BuildAncestorExpansionAsync(filteredResourceIds, request.AncestorRelationTypes, ct)
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
            var filterResult = await _ancestorHandler.ApplyAncestorFiltersAsync(
                filteredRequiredIds,
                filteredOrGroups,
                ancestorExpansion,
                request.AncestorFilters,
                propertyExecutionContext,
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
            var ancestorIds = _ancestorHandler.CollectAncestors(filteredResourceIds, ancestorExpansion);
            filteredResourceIds.UnionWith(ancestorIds);
        }

        var resourceIdList = filteredResourceIds.ToList();
        resourceIdList.Sort();

        var rules = await _computeQueryService.GetRulesAsync(
            request.FromDate,
            request.ToDate,
            resourceIdList,
            ct).ConfigureAwait(false);

        var resourceCapacities = await _computeQueryService
            .GetResourceCapacitiesAsync(resourceIdList, ct)
            .ConfigureAwait(false);

        var availabilityRules = new List<AvailabilityRule>();
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

                var availabilityRule = new AvailabilityRule(
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

                if (!availabilityRule.IsExclude && AvailabilityRuleApplicability.RuleAppliesToPeriod(availabilityRule, period))
                {
                    hasPositive = true;
                }

                if (availabilityRule.IsExclude && AvailabilityRuleApplicability.RuleAppliesToPeriod(availabilityRule, period))
                {
                    hasNegative = true;
                }

                availabilityRules.Add(availabilityRule);
            }
        }

        var fromUtc = DateTime.SpecifyKind(request.FromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtcExclusive = DateTime.SpecifyKind(request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var busyEvents = await _computeQueryService
            .GetBusyEventsAsync(fromUtc, toUtcExclusive, resourceIdList, ct)
            .ConfigureAwait(false);

        var busySlots = new List<BusySlot>();
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
                busySlots.Add(new BusySlot(startUtc, endUtc, resourceId));
            }
        }

        var inputs = new AvailabilityInputs(availabilityRules, busySlots, resourceCapacities);
        var ancestorMode = AvailabilityRequestNormalization.NormalizeAncestorMode(request.AncestorMode) ?? "perGroup";
        IReadOnlyDictionary<int, List<UtcSlot>>? perResourceAvailability = null;

        if (includeAncestors && ancestorMode == "perGroup")
        {
            perResourceAvailability = BuildPerResourceAvailability(period, resourceIdList, inputs);
            var requiredWithAncestors = _ancestorHandler.ExpandRequiredAncestors(filteredRequiredIds, ancestorExpansion);
            if (filteredOrGroups.Count == 0)
            {
                var query = new AvailabilityQuery(period, requiredWithAncestors);
                var result = _engine.ComposeAvailability(query, perResourceAvailability);
                return new AvailabilityComputation(result, hasPositive, hasNegative, busySlots.Count > 0);
            }

            var resultWithAncestors = ComputePerGroupAvailability(
                period,
                requiredWithAncestors,
                filteredOrGroups,
                ancestorExpansion,
                resourceIdList,
                perResourceAvailability);
            return new AvailabilityComputation(resultWithAncestors, hasPositive, hasNegative, busySlots.Count > 0);
        }

        var globalRequired = includeAncestors
            ? _ancestorHandler.ExpandRequiredAncestors(filteredRequiredIds, ancestorExpansion)
            : filteredRequiredIds;

        if (globalRequired.Count == 0 && filteredOrGroups.Count > 0)
        {
            perResourceAvailability = BuildPerResourceAvailability(period, resourceIdList, inputs);
            var result = ComputeOrOnlyAvailability(period, filteredOrGroups, resourceIdList, perResourceAvailability);
            return new AvailabilityComputation(result, hasPositive, hasNegative, busySlots.Count > 0);
        }

        var queryWithRequired = new AvailabilityQuery(period, globalRequired, resourceOrGroups: filteredOrGroups);
        var resultWithRequired = _engine.Compute(queryWithRequired, inputs);

        return new AvailabilityComputation(resultWithRequired, hasPositive, hasNegative, busySlots.Count > 0);
    }

    private AvailabilityResult ComputePerGroupAvailability(
        DatePeriod period,
        IReadOnlyList<int> requiredIds,
        IReadOnlyList<List<int>> orGroups,
        AncestorExpansion expansion,
        IReadOnlyList<int> allResourceIds,
        IReadOnlyDictionary<int, List<UtcSlot>> perResourceAvailability)
    {
        List<UtcSlot>? intersection = null;

        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            if (group.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            var union = UnionGroupAvailabilityWithAncestors(
                period,
                requiredIds,
                group,
                expansion,
                allResourceIds,
                perResourceAvailability);
            if (union.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            intersection = intersection == null
                ? union
                : SlotComposition.IntersectByTime(intersection, union, allResourceIds);

            if (intersection.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }
        }

        if (intersection == null)
        {
            return new AvailabilityResult(Array.Empty<UtcSlot>());
        }

        var normalized = SlotComposition.Normalize(intersection, mergeByResources: false);
        return new AvailabilityResult(normalized);
    }

    private List<UtcSlot> UnionGroupAvailabilityWithAncestors(
        DatePeriod period,
        IReadOnlyList<int> requiredIds,
        IReadOnlyList<int> groupResourceIds,
        AncestorExpansion expansion,
        IReadOnlyList<int> allResourceIds,
        IReadOnlyDictionary<int, List<UtcSlot>> perResourceAvailability)
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
            var result = _engine.ComposeAvailability(query, perResourceAvailability);
            slots.AddRange(SlotComposition.Project(result.Slots, allResourceIds));
        }

        return SlotComposition.Normalize(slots, mergeByResources: false);
    }

    private AvailabilityResult ComputeOrOnlyAvailability(
        DatePeriod period,
        IReadOnlyList<List<int>> orGroups,
        IReadOnlyList<int> allResourceIds,
        IReadOnlyDictionary<int, List<UtcSlot>> perResourceAvailability)
    {
        List<UtcSlot>? intersection = null;

        for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
        {
            var group = orGroups[groupIndex];
            if (group.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            var union = UnionGroupAvailability(group, allResourceIds, perResourceAvailability);
            if (union.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            intersection = intersection == null
                ? union
                : SlotComposition.IntersectByTime(intersection, union, allResourceIds);

            if (intersection.Count == 0)
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }
        }

        if (intersection == null)
        {
            return new AvailabilityResult(Array.Empty<UtcSlot>());
        }

        var normalized = SlotComposition.Normalize(intersection, mergeByResources: false);
        return new AvailabilityResult(normalized);
    }

    private List<UtcSlot> UnionGroupAvailability(
        IReadOnlyList<int> groupResourceIds,
        IReadOnlyList<int> allResourceIds,
        IReadOnlyDictionary<int, List<UtcSlot>> perResourceAvailability)
    {
        return SlotComposition.UnionByTime(groupResourceIds, perResourceAvailability, allResourceIds);
    }

    private IReadOnlyDictionary<int, List<UtcSlot>> BuildPerResourceAvailability(
        DatePeriod period,
        IReadOnlyList<int> allResourceIds,
        AvailabilityInputs inputs)
    {
        if (allResourceIds.Count == 0)
        {
            return new Dictionary<int, List<UtcSlot>>();
        }

        var query = new AvailabilityQuery(period, allResourceIds);
        return _engine.ComputePerResourceAvailability(query, inputs);
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
