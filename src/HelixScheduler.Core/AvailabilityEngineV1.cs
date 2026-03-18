namespace HelixScheduler.Core;

/// <summary>
/// Availability engine v1 based on RuleModel and BusySlotModel inputs.
/// </summary>
public sealed class AvailabilityEngineV1
{
    public IReadOnlyDictionary<int, List<UtcSlot>> ComputePerResourceAvailability(
        AvailabilityQuery query,
        AvailabilityInputs inputs)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        if (query.RequiredResourceIds.Count == 0)
        {
            return new Dictionary<int, List<UtcSlot>>();
        }

        var allResourceIds = query.AllResourceIds.Count > 0
            ? query.AllResourceIds.ToArray()
            : query.RequiredResourceIds.ToArray();
        var perResource = new Dictionary<int, List<UtcSlot>>(allResourceIds.Length);
        var rulesByResource = GroupRulesByResource(inputs.Rules);
        var busyByResource = GroupBusyByResource(inputs.BusySlots);

        foreach (var resourceId in allResourceIds)
        {
            var positive = new List<UtcSlot>();
            var negative = new List<UtcSlot>();

            if (rulesByResource.TryGetValue(resourceId, out var resourceRules))
            {
                for (var i = 0; i < resourceRules.Count; i++)
                {
                    var rule = resourceRules[i];
                    var occurrences = GenerateOccurrences(rule, query.Period);
                    if (occurrences.Count == 0)
                    {
                        continue;
                    }

                    if (rule.IsExclude)
                    {
                        negative.AddRange(occurrences);
                    }
                    else
                    {
                        positive.AddRange(occurrences);
                    }
                }
            }

            if (positive.Count == 0)
            {
                perResource[resourceId] = new List<UtcSlot>();
                continue;
            }

            var capacity = ResolveCapacity(inputs.ResourceCapacities, resourceId);
            if (busyByResource.TryGetValue(resourceId, out var resourceBusy))
            {
                if (capacity <= 1)
                {
                    for (var i = 0; i < resourceBusy.Count; i++)
                    {
                        var busy = resourceBusy[i];
                        negative.Add(new UtcSlot(busy.StartUtc, busy.EndUtc, new[] { resourceId }));
                    }
                }
                else
                {
                    var capacityBlocks = BuildCapacityBlocks(resourceBusy, resourceId, capacity);
                    if (capacityBlocks.Count > 0)
                    {
                        negative.AddRange(capacityBlocks);
                    }
                }
            }

            var normalizedPositive = SlotComposition.Normalize(positive, mergeByResources: true);
            var normalizedNegative = SlotComposition.Normalize(negative, mergeByResources: true);
            perResource[resourceId] = SubtractSlots(normalizedPositive, normalizedNegative);
        }

        return perResource;
    }

    public AvailabilityResult ComposeAvailability(
        AvailabilityQuery query,
        IReadOnlyDictionary<int, List<UtcSlot>> perResource)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (perResource == null) throw new ArgumentNullException(nameof(perResource));

        if (query.RequiredResourceIds.Count == 0)
        {
            return new AvailabilityResult(Array.Empty<UtcSlot>());
        }

        var requiredIds = query.RequiredResourceIds.ToArray();
        var allResourceIds = query.AllResourceIds.Count > 0
            ? query.AllResourceIds.ToArray()
            : requiredIds;

        if (!perResource.TryGetValue(requiredIds[0], out var first))
        {
            return new AvailabilityResult(Array.Empty<UtcSlot>());
        }

        var intersection = first;
        for (var index = 1; index < requiredIds.Length; index++)
        {
            if (!perResource.TryGetValue(requiredIds[index], out var resourceSlots))
            {
                return new AvailabilityResult(Array.Empty<UtcSlot>());
            }

            intersection = SlotComposition.IntersectByTime(intersection, resourceSlots);
            if (intersection.Count == 0)
            {
                break;
            }
        }

        for (var groupIndex = 0; groupIndex < query.ResourceOrGroups.Count; groupIndex++)
        {
            var group = query.ResourceOrGroups[groupIndex];
            if (group.Count == 0)
            {
                intersection = new List<UtcSlot>();
                break;
            }

            var union = SlotComposition.UnionByTime(group, perResource);
            if (union.Count == 0)
            {
                intersection = new List<UtcSlot>();
                break;
            }

            intersection = SlotComposition.IntersectByTime(intersection, union);
            if (intersection.Count == 0)
            {
                break;
            }
        }

        var resultSlots = new List<UtcSlot>(intersection.Count);
        for (var i = 0; i < intersection.Count; i++)
        {
            resultSlots.Add(new UtcSlot(intersection[i].StartUtc, intersection[i].EndUtc, allResourceIds));
        }

        resultSlots = SlotComposition.Normalize(resultSlots, mergeByResources: true);
        return new AvailabilityResult(resultSlots);
    }

    /// <summary>
    /// Computes availability by intersecting per-resource availability and OR groups.
    /// </summary>
    /// <param name="query">Availability query (UTC period, resource selection).</param>
    /// <param name="inputs">Normalized rules, busy slots, and capacities (UTC).</param>
    public AvailabilityResult Compute(AvailabilityQuery query, AvailabilityInputs inputs)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var perResource = ComputePerResourceAvailability(query, inputs);
        return ComposeAvailability(query, perResource);
    }

    private static List<UtcSlot> GenerateOccurrences(RuleModel rule, DatePeriod period)
    {
        return rule.Kind switch
        {
            RuleKind.RecurringWeekly => GenerateWeekly(rule, period),
            RuleKind.SingleDate => GenerateSingleDate(rule, period),
            RuleKind.Range => GenerateRange(rule, period),
            _ => throw new NotSupportedException($"RuleKind {rule.Kind} is not supported in v1.")
        };
    }

    private static List<UtcSlot> GenerateWeekly(RuleModel rule, DatePeriod period)
    {
        if (rule.DaysOfWeekMask == null)
        {
            return new List<UtcSlot>();
        }

        var slots = new List<UtcSlot>();
        foreach (var day in period.EnumerateDays())
        {
            if (MatchesDayOfWeek(day.DayOfWeek, rule.DaysOfWeekMask.Value))
            {
                slots.Add(CreateSlot(day, rule));
            }
        }

        return slots;
    }

    private static List<UtcSlot> GenerateSingleDate(RuleModel rule, DatePeriod period)
    {
        if (rule.SingleDate == null)
        {
            return new List<UtcSlot>();
        }

        var date = rule.SingleDate.Value;
        if (date < period.From || date > period.To)
        {
            return new List<UtcSlot>();
        }

        return new List<UtcSlot> { CreateSlot(date, rule) };
    }

    private static List<UtcSlot> GenerateRange(RuleModel rule, DatePeriod period)
    {
        var from = rule.FromDate ?? period.From;
        var to = rule.ToDate ?? period.To;

        if (from > period.To || to < period.From)
        {
            return new List<UtcSlot>();
        }

        var start = from < period.From ? period.From : from;
        var end = to > period.To ? period.To : to;

        var slots = new List<UtcSlot>();
        foreach (var day in EnumerateDays(start, end))
        {
            slots.Add(CreateSlot(day, rule));
        }

        return slots;
    }

    private static UtcSlot CreateSlot(DateOnly date, RuleModel rule)
    {
        var start = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromTimeSpan(rule.StartTime)), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromTimeSpan(rule.EndTime)), DateTimeKind.Utc);
        return new UtcSlot(start, end, new[] { rule.ResourceId });
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

    private static List<UtcSlot> SubtractSlots(List<UtcSlot> available, List<UtcSlot> blocks)
    {
        if (blocks.Count == 0)
        {
            return available;
        }

        var result = new List<UtcSlot>(available.Count);
        var blockIndex = 0;

        for (var i = 0; i < available.Count; i++)
        {
            var slot = available[i];

            while (blockIndex < blocks.Count && blocks[blockIndex].EndUtc <= slot.StartUtc)
            {
                blockIndex++;
            }

            var currentStart = slot.StartUtc;
            var scanIndex = blockIndex;

            while (scanIndex < blocks.Count)
            {
                var block = blocks[scanIndex];
                if (block.StartUtc >= slot.EndUtc)
                {
                    break;
                }

                if (block.EndUtc <= currentStart)
                {
                    scanIndex++;
                    continue;
                }

                if (block.StartUtc > currentStart)
                {
                    result.Add(new UtcSlot(currentStart, block.StartUtc, slot.ResourceIds));
                }

                if (block.EndUtc >= slot.EndUtc)
                {
                    currentStart = slot.EndUtc;
                    break;
                }

                currentStart = block.EndUtc;
                scanIndex++;
            }

            if (currentStart < slot.EndUtc)
            {
                result.Add(new UtcSlot(currentStart, slot.EndUtc, slot.ResourceIds));
            }
        }

        return result;
    }

    private static int ResolveCapacity(IReadOnlyDictionary<int, int> capacities, int resourceId)
    {
        if (capacities.Count == 0)
        {
            return 1;
        }

        if (capacities.TryGetValue(resourceId, out var capacity))
        {
            return capacity < 1 ? 1 : capacity;
        }

        return 1;
    }

    private static List<UtcSlot> BuildCapacityBlocks(
        IReadOnlyList<BusySlotModel> busySlots,
        int resourceId,
        int capacity)
    {
        var edges = new List<BusyEdge>();
        for (var i = 0; i < busySlots.Count; i++)
        {
            var busy = busySlots[i];
            edges.Add(new BusyEdge(busy.StartUtc, 1));
            edges.Add(new BusyEdge(busy.EndUtc, -1));
        }

        if (edges.Count == 0)
        {
            return new List<UtcSlot>();
        }

        edges.Sort(BusyEdgeComparer.Instance);

        var blocks = new List<UtcSlot>();
        var occupancy = 0;

        for (var index = 0; index < edges.Count; index++)
        {
            var current = edges[index].Timestamp;
            var delta = 0;

            var nextIndex = index;
            while (nextIndex < edges.Count && edges[nextIndex].Timestamp == current)
            {
                delta += edges[nextIndex].Delta;
                nextIndex++;
            }

            occupancy += delta;
            if (nextIndex >= edges.Count)
            {
                break;
            }

            var next = edges[nextIndex].Timestamp;
            if (next > current && occupancy >= capacity)
            {
                blocks.Add(new UtcSlot(current, next, new[] { resourceId }));
            }

            index = nextIndex - 1;
        }

        return blocks;
    }

    private static Dictionary<int, List<RuleModel>> GroupRulesByResource(IReadOnlyList<RuleModel> rules)
    {
        var grouped = new Dictionary<int, List<RuleModel>>();
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (!grouped.TryGetValue(rule.ResourceId, out var resourceRules))
            {
                resourceRules = new List<RuleModel>();
                grouped[rule.ResourceId] = resourceRules;
            }

            resourceRules.Add(rule);
        }

        return grouped;
    }

    private static Dictionary<int, List<BusySlotModel>> GroupBusyByResource(IReadOnlyList<BusySlotModel> busySlots)
    {
        var grouped = new Dictionary<int, List<BusySlotModel>>();
        for (var i = 0; i < busySlots.Count; i++)
        {
            var busy = busySlots[i];
            if (!grouped.TryGetValue(busy.ResourceId, out var resourceBusy))
            {
                resourceBusy = new List<BusySlotModel>();
                grouped[busy.ResourceId] = resourceBusy;
            }

            resourceBusy.Add(busy);
        }

        return grouped;
    }

    private readonly struct BusyEdge
    {
        public BusyEdge(DateTime timestamp, int delta)
        {
            Timestamp = timestamp;
            Delta = delta;
        }

        public DateTime Timestamp { get; }
        public int Delta { get; }
    }

    private sealed class BusyEdgeComparer : IComparer<BusyEdge>
    {
        public static BusyEdgeComparer Instance { get; } = new();

        public int Compare(BusyEdge x, BusyEdge y)
        {
            var timeCompare = x.Timestamp.CompareTo(y.Timestamp);
            if (timeCompare != 0)
            {
                return timeCompare;
            }

            return x.Delta.CompareTo(y.Delta);
        }
    }

    private sealed class SlotTimeComparer : IComparer<UtcSlot>
    {
        public static SlotTimeComparer Instance { get; } = new();

        public int Compare(UtcSlot? x, UtcSlot? y)
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
    }
}
