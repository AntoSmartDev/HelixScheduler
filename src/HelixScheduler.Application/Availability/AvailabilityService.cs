using HelixScheduler.Core;

namespace HelixScheduler.Application.Availability;

public sealed class AvailabilityService : IAvailabilityService
{
    private const int MaxRangeDays = 31;
    private const int MaxRequiredResources = 10;
    private const int MaxOrGroups = 5;
    private const int MaxOrGroupItems = 10;
    private const int MaxTotalResources = 20;

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

        var computation = await ComputeAvailabilityAsync(request, ct).ConfigureAwait(false);
        if (!request.Explain)
        {
            return new AvailabilityComputeResponse(
                computation.Result.Slots,
                Array.Empty<AvailabilityExplanation>());
        }

        var explanations = new List<AvailabilityExplanation>();
        if (computation.Result.Slots.Count == 0)
        {
            explanations.Add(BuildEmptyExplanation(computation, request.FromDate, request.ToDate));
        }

        return new AvailabilityComputeResponse(computation.Result.Slots, explanations);
    }

    private async Task ValidatePropertySchemaAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        if (request.PropertyIds == null || request.PropertyIds.Count == 0)
        {
            return;
        }

        var resourceIds = request.RequiredResourceIds.Distinct().ToList();
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
            resourceIds,
            request.PropertyIds,
            ct).ConfigureAwait(false);
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
        var propertyIds = request.PropertyIds ?? Array.Empty<int>();
        if (propertyIds.Count > 0)
        {
            foreach (var propertyId in propertyIds)
            {
                var effectivePropertyIds = request.IncludeDescendants
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

        var filteredRequiredIds = propertyFiltered == null
            ? requiredIds
            : requiredIds.Where(filteredResourceIds.Contains).ToList();
        filteredRequiredIds.Sort();

        var filteredOrGroups = new List<List<int>>();
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
        if (filteredRequiredIds.Count == 0 && filteredOrGroups.Count > 0)
        {
            var result = ComputeOrOnlyAvailability(period, filteredOrGroups, resourceIdList, inputs);
            return new AvailabilityComputation(result, hasPositive, hasNegative, busySlots.Count > 0);
        }

        var query = new AvailabilityQuery(period, filteredRequiredIds, resourceOrGroups: filteredOrGroups);
        var resultWithRequired = _engine.Compute(query, inputs);

        return new AvailabilityComputation(resultWithRequired, hasPositive, hasNegative, busySlots.Count > 0);
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
