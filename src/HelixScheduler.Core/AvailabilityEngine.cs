namespace HelixScheduler.Core;

/// <summary>
/// Availability engine based on SchedulingRule and BusySlot inputs.
/// </summary>
public sealed class AvailabilityEngine
{
    /// <summary>
    /// Computes availability by intersecting per-resource availability and OR groups.
    /// </summary>
    /// <param name="query">Availability query (UTC period, resource selection).</param>
    /// <param name="rules">Scheduler rules in UTC.</param>
    /// <param name="busySlots">Busy slots in UTC.</param>
    public AvailabilityResult Compute(
        AvailabilityQuery query,
        IReadOnlyList<SchedulingRule> rules,
        IReadOnlyList<BusySlot> busySlots)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (rules == null) throw new ArgumentNullException(nameof(rules));
        if (busySlots == null) throw new ArgumentNullException(nameof(busySlots));

        if (query.RequiredResourceIds.Count == 0)
        {
            return new AvailabilityResult(Array.Empty<UtcSlot>());
        }

        var allResourceIds = query.AllResourceIds.Count > 0
            ? query.AllResourceIds
            : query.RequiredResourceIds.ToList();
        var perResourceAvailability = new Dictionary<int, List<UtcSlot>>();

        foreach (var resourceId in allResourceIds)
        {
            var positiveRules = rules
                .Where(rule => !rule.IsExclude && rule.ResourceIds.Contains(resourceId))
                .ToList();

            var negativeRules = rules
                .Where(rule => rule.IsExclude && rule.ResourceIds.Contains(resourceId))
                .ToList();

            var availability = GenerateRuleSlots(positiveRules, query.Period, resourceId);
            if (availability.Count == 0)
            {
                perResourceAvailability[resourceId] = availability;
                continue;
            }

            var blocks = GenerateRuleSlots(negativeRules, query.Period, resourceId);
            blocks.AddRange(busySlots
                .Where(slot => slot.ResourceId == resourceId)
                .Select(slot => new UtcSlot(slot.StartUtc, slot.EndUtc, new[] { resourceId })));

            availability = SubtractSlots(availability, blocks);
            availability = SlotComposition.Normalize(availability, mergeByResources: true);

            perResourceAvailability[resourceId] = availability;
        }

        var requiredIds = query.RequiredResourceIds.ToArray();
        var intersection = perResourceAvailability[requiredIds[0]];
        for (var index = 1; index < requiredIds.Length; index++)
        {
            intersection = SlotComposition.IntersectByTime(intersection, perResourceAvailability[requiredIds[index]]);
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

            var union = SlotComposition.UnionByTime(group, perResourceAvailability);
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

        var resultResourceIds = allResourceIds.ToArray();
        var resultSlots = intersection
            .Select(slot => new UtcSlot(slot.StartUtc, slot.EndUtc, resultResourceIds))
            .ToList();

        resultSlots = SlotComposition.Normalize(resultSlots, mergeByResources: true);

        return new AvailabilityResult(resultSlots);
    }

    private static List<UtcSlot> GenerateRuleSlots(
        IReadOnlyList<SchedulingRule> rules,
        DatePeriod period,
        int resourceId)
    {
        var slots = new List<UtcSlot>();
        foreach (var rule in rules)
        {
            slots.AddRange(GenerateRuleSlots(rule, period, resourceId));
        }

        return slots;
    }

    private static IEnumerable<UtcSlot> GenerateRuleSlots(
        SchedulingRule rule,
        DatePeriod period,
        int resourceId)
    {
        return rule.Kind switch
        {
            SchedulingRuleKind.SingleDate => GenerateSingleDate(rule, period, resourceId),
            SchedulingRuleKind.Weekly => GenerateWeekly(rule, period, resourceId),
            SchedulingRuleKind.Range => GenerateRange(rule, period, resourceId),
            SchedulingRuleKind.Monthly => GenerateMonthly(rule, period, resourceId),
            SchedulingRuleKind.Repeating => GenerateRepeating(rule, period, resourceId),
            _ => Array.Empty<UtcSlot>()
        };
    }

    private static IEnumerable<UtcSlot> GenerateSingleDate(
        SchedulingRule rule,
        DatePeriod period,
        int resourceId)
    {
        if (rule.SingleDateUtc == null)
        {
            return Array.Empty<UtcSlot>();
        }

        var date = rule.SingleDateUtc.Value;
        if (date < period.From || date > period.To)
        {
            return Array.Empty<UtcSlot>();
        }

        return new[] { CreateSlot(date, rule.TimeRange, resourceId) };
    }

    private static IEnumerable<UtcSlot> GenerateWeekly(
        SchedulingRule rule,
        DatePeriod period,
        int resourceId)
    {
        if (rule.DaysOfWeekMask == null)
        {
            return Array.Empty<UtcSlot>();
        }

        var slots = new List<UtcSlot>();
        foreach (var date in EnumerateDates(period.From, period.To))
        {
            if (MatchesDayOfWeek(date.DayOfWeek, rule.DaysOfWeekMask.Value))
            {
                slots.Add(CreateSlot(date, rule.TimeRange, resourceId));
            }
        }

        return slots;
    }

    private static IEnumerable<UtcSlot> GenerateRange(
        SchedulingRule rule,
        DatePeriod period,
        int resourceId)
    {
        var from = rule.FromDateUtc ?? period.From;
        var to = rule.ToDateUtc ?? period.To;

        if (from > period.To || to < period.From)
        {
            return Array.Empty<UtcSlot>();
        }

        var start = from < period.From ? period.From : from;
        var end = to > period.To ? period.To : to;

        var slots = new List<UtcSlot>();
        foreach (var date in EnumerateDates(start, end))
        {
            slots.Add(CreateSlot(date, rule.TimeRange, resourceId));
        }

        return slots;
    }

    private static IEnumerable<UtcSlot> GenerateMonthly(
        SchedulingRule rule,
        DatePeriod period,
        int resourceId)
    {
        if (rule.DayOfMonth == null)
        {
            return Array.Empty<UtcSlot>();
        }

        var slots = new List<UtcSlot>();
        foreach (var date in EnumerateDates(period.From, period.To))
        {
            if (date.Day == rule.DayOfMonth.Value)
            {
                slots.Add(CreateSlot(date, rule.TimeRange, resourceId));
            }
        }

        return slots;
    }

    private static IEnumerable<UtcSlot> GenerateRepeating(
        SchedulingRule rule,
        DatePeriod period,
        int resourceId)
    {
        if (rule.IntervalDays == null || rule.IntervalDays <= 0)
        {
            return Array.Empty<UtcSlot>();
        }

        var start = rule.FromDateUtc ?? period.From;
        var end = rule.ToDateUtc ?? period.To;

        if (end < period.From || start > period.To)
        {
            return Array.Empty<UtcSlot>();
        }

        if (start < period.From)
        {
            var delta = period.From.DayNumber - start.DayNumber;
            var skip = delta / rule.IntervalDays.Value;
            start = start.AddDays(skip * rule.IntervalDays.Value);
        }

        var slots = new List<UtcSlot>();
        for (var date = start; date <= end && date <= period.To; date = date.AddDays(rule.IntervalDays.Value))
        {
            if (date >= period.From)
            {
                slots.Add(CreateSlot(date, rule.TimeRange, resourceId));
            }
        }

        return slots;
    }

    private static UtcSlot CreateSlot(DateOnly date, TimeRange range, int resourceId)
    {
        var start = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromTimeSpan(range.Start)), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromTimeSpan(range.End)), DateTimeKind.Utc);
        return new UtcSlot(start, end, new[] { resourceId });
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            yield return date;
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

        var result = new List<UtcSlot>();
        var orderedBlocks = blocks
            .OrderBy(slot => slot.StartUtc)
            .ThenBy(slot => slot.EndUtc)
            .ToList();

        foreach (var slot in SlotComposition.Normalize(available, mergeByResources: true))
        {
            var segments = new List<UtcSlot> { slot };
            foreach (var block in orderedBlocks)
            {
                segments = segments
                    .SelectMany(segment => SubtractSegment(segment, block))
                    .ToList();
                if (segments.Count == 0)
                {
                    break;
                }
            }

            result.AddRange(segments);
        }

        return SlotComposition.Normalize(result, mergeByResources: true);
    }

    private static IEnumerable<UtcSlot> SubtractSegment(UtcSlot segment, UtcSlot block)
    {
        if (block.EndUtc <= segment.StartUtc || block.StartUtc >= segment.EndUtc)
        {
            yield return segment;
            yield break;
        }

        if (block.StartUtc <= segment.StartUtc && block.EndUtc >= segment.EndUtc)
        {
            yield break;
        }

        if (block.StartUtc > segment.StartUtc)
        {
            yield return new UtcSlot(segment.StartUtc, block.StartUtc, segment.ResourceIds);
        }

        if (block.EndUtc < segment.EndUtc)
        {
            yield return new UtcSlot(block.EndUtc, segment.EndUtc, segment.ResourceIds);
        }
    }

}