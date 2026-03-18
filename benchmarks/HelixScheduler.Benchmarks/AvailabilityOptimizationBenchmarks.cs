using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;
using HelixScheduler.Core;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(OptimizationBenchmarkConfig))]
public class AvailabilityOptimizationBenchmarks
{
    private const int ResourceCount = 20;
    private const int AncestorAId = 101;
    private const int AncestorBId = 102;

    private readonly AvailabilityEngine _engine = new();
    private AvailabilityInputs _inputs = null!;
    private AvailabilityQuery _coreQuery = null!;
    private IReadOnlyList<int> _resourceIds = null!;
    private IReadOnlyList<List<int>> _orGroups = null!;
    private IReadOnlyDictionary<int, IReadOnlyCollection<int>> _ancestorMap = null!;
    private List<UtcSlot> _availableSlots = null!;
    private List<UtcSlot> _blockSlots = null!;

    [Params(100, 300)]
    public int RuleCount { get; set; }

    [Params(200, 500)]
    public int BusyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _resourceIds = Enumerable.Range(1, ResourceCount).ToArray();
        var allResourceIds = _resourceIds.Concat(new[] { AncestorAId, AncestorBId }).ToArray();
        var period = new DatePeriod(new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 16));
        _coreQuery = new AvailabilityQuery(period, allResourceIds);
        _inputs = BuildInputs(period, allResourceIds, RuleCount, BusyCount);
        _orGroups = new[]
        {
            _resourceIds.Take(10).ToList(),
            _resourceIds.Skip(10).Take(10).ToList()
        };
        _ancestorMap = BuildAncestorMap(_resourceIds);
        BuildSubtractData(out _availableSlots, out _blockSlots, BusyCount);
    }

    [Benchmark(Baseline = true, Description = "Core baseline (scan rules/busy, old subtract)")]
    public IReadOnlyDictionary<int, List<UtcSlot>> Core_Baseline()
    {
        return BaselineCoreBuilder.ComputePerResource(_coreQuery, _inputs);
    }

    [Benchmark(Description = "Core grouped rules/busy (old subtract)")]
    public IReadOnlyDictionary<int, List<UtcSlot>> Core_GroupedRulesBusy()
    {
        return GroupedCoreBuilder.ComputePerResource(_coreQuery, _inputs);
    }

    [Benchmark(Description = "Core current (grouped + new subtract)")]
    public IReadOnlyDictionary<int, List<UtcSlot>> Core_Current()
    {
        return _engine.ComputePerResourceAvailability(_coreQuery, _inputs);
    }

    [Benchmark(Description = "Application OR-only baseline (20 engine.Compute calls)")]
    public AvailabilityResult Application_OrOnly_Baseline()
    {
        return ApplicationPathComparer.ComputeOrOnlyBaseline(_engine, _coreQuery.Period, _orGroups, _resourceIds, _inputs);
    }

    [Benchmark(Description = "Application OR-only optimized (1 per-resource build, 0 engine.Compute calls)")]
    public AvailabilityResult Application_OrOnly_Optimized()
    {
        var perResource = _engine.ComputePerResourceAvailability(_coreQuery, _inputs);
        return ApplicationPathComparer.ComputeOrOnlyOptimized(_orGroups, _resourceIds, perResource);
    }

    [Benchmark(Description = "Application perGroup ancestors baseline (20 engine.Compute calls)")]
    public AvailabilityResult Application_PerGroupAncestors_Baseline()
    {
        return ApplicationPathComparer.ComputePerGroupAncestorsBaseline(
            _engine,
            _coreQuery.Period,
            _orGroups,
            _ancestorMap,
            _resourceIds,
            _inputs);
    }

    [Benchmark(Description = "Application perGroup ancestors optimized (1 per-resource build, 0 engine.Compute calls)")]
    public AvailabilityResult Application_PerGroupAncestors_Optimized()
    {
        var perResource = _engine.ComputePerResourceAvailability(_coreQuery, _inputs);
        return ApplicationPathComparer.ComputePerGroupAncestorsOptimized(
            _engine,
            _coreQuery.Period,
            _orGroups,
            _ancestorMap,
            _resourceIds,
            perResource);
    }

    [Benchmark(Description = "SubtractSlots baseline")]
    public List<UtcSlot> SubtractSlots_Baseline()
    {
        return SubtractComparer.Baseline(_availableSlots, _blockSlots);
    }

    [Benchmark(Description = "SubtractSlots current")]
    public List<UtcSlot> SubtractSlots_Current()
    {
        return SubtractComparer.Current(_availableSlots, _blockSlots);
    }

    private static AvailabilityInputs BuildInputs(
        DatePeriod period,
        IReadOnlyList<int> allResourceIds,
        int ruleCount,
        int busyCount)
    {
        var rules = new List<RuleModel>(ruleCount + 2);
        var random = new Random(1000 + ruleCount + busyCount);
        var daySpan = period.To.DayNumber - period.From.DayNumber;

        for (var i = 0; i < ruleCount; i++)
        {
            var resourceId = allResourceIds[i % allResourceIds.Count];
            var date = period.From.AddDays(i % (daySpan + 1));
            var startHour = 8 + (i % 7);
            var durationHours = (i % 3 == 0) ? 2 : 3;
            var isExclude = i % 5 == 0;
            rules.Add(new RuleModel(
                i + 1,
                RuleKind.SingleDate,
                isExclude,
                null,
                null,
                date,
                TimeSpan.FromHours(startHour),
                TimeSpan.FromHours(startHour + durationHours),
                null,
                null,
                null,
                resourceId));
        }

        rules.Add(new RuleModel(900001, RuleKind.SingleDate, false, null, null, period.From, TimeSpan.FromHours(8), TimeSpan.FromHours(18), null, null, null, AncestorAId));
        rules.Add(new RuleModel(900002, RuleKind.SingleDate, false, null, null, period.From, TimeSpan.FromHours(8), TimeSpan.FromHours(18), null, null, null, AncestorBId));

        var busy = new List<BusySlotModel>(busyCount);
        for (var i = 0; i < busyCount; i++)
        {
            var resourceId = allResourceIds[random.Next(allResourceIds.Count)];
            var day = period.From.AddDays(random.Next(daySpan + 1));
            var startHour = 8 + random.Next(9);
            var minute = random.Next(0, 2) * 30;
            var lengthMinutes = (random.Next(0, 3) + 1) * 30;
            var startUtc = DateTime.SpecifyKind(day.ToDateTime(new TimeOnly(startHour, minute)), DateTimeKind.Utc);
            busy.Add(new BusySlotModel(startUtc, startUtc.AddMinutes(lengthMinutes), resourceId));
        }

        return new AvailabilityInputs(rules, busy, new Dictionary<int, int>());
    }

    private static IReadOnlyDictionary<int, IReadOnlyCollection<int>> BuildAncestorMap(IReadOnlyList<int> resourceIds)
    {
        var map = new Dictionary<int, IReadOnlyCollection<int>>(resourceIds.Count);
        for (var i = 0; i < resourceIds.Count; i++)
        {
            map[resourceIds[i]] = new[] { i < 10 ? AncestorAId : AncestorBId };
        }

        return map;
    }

    private static void BuildSubtractData(out List<UtcSlot> available, out List<UtcSlot> blocks, int busyCount)
    {
        available = new List<UtcSlot>();
        var resourceIds = new[] { 1 };
        var baseDate = new DateTime(2026, 1, 12, 8, 0, 0, DateTimeKind.Utc);
        for (var day = 0; day < 10; day++)
        {
            available.Add(new UtcSlot(baseDate.AddDays(day), baseDate.AddDays(day).AddHours(10), resourceIds));
        }

        var random = new Random(7000 + busyCount);
        var temp = new List<UtcSlot>(busyCount);
        for (var i = 0; i < busyCount; i++)
        {
            var day = random.Next(0, 10);
            var startOffsetMinutes = random.Next(0, 19) * 30;
            var lengthMinutes = random.Next(1, 5) * 30;
            var start = baseDate.AddDays(day).AddMinutes(startOffsetMinutes);
            temp.Add(new UtcSlot(start, start.AddMinutes(lengthMinutes), resourceIds));
        }

        blocks = Normalize(temp, mergeByResources: true);
    }

    private static List<UtcSlot> Normalize(IReadOnlyList<UtcSlot> slots, bool mergeByResources)
    {
        if (slots.Count == 0)
        {
            return new List<UtcSlot>();
        }

        var ordered = new List<UtcSlot>(slots);
        ordered.Sort(static (a, b) =>
        {
            var startCompare = a.StartUtc.CompareTo(b.StartUtc);
            return startCompare != 0 ? startCompare : a.EndUtc.CompareTo(b.EndUtc);
        });

        var normalized = new List<UtcSlot> { ordered[0] };
        for (var i = 1; i < ordered.Count; i++)
        {
            var last = normalized[^1];
            var current = ordered[i];
            var canMerge = current.StartUtc <= last.EndUtc;
            if (mergeByResources)
            {
                canMerge = canMerge && last.ResourceIds.SequenceEqual(current.ResourceIds);
            }

            if (canMerge)
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

    private sealed class OptimizationBenchmarkConfig : ManualConfig
    {
        public OptimizationBenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }

    private static class ApplicationPathComparer
    {
        public static AvailabilityResult ComputeOrOnlyBaseline(
            AvailabilityEngine engine,
            DatePeriod period,
            IReadOnlyList<List<int>> orGroups,
            IReadOnlyList<int> allResourceIds,
            AvailabilityInputs inputs)
        {
            List<UtcSlot>? intersection = null;
            for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
            {
                var group = orGroups[groupIndex];
                var union = new List<UtcSlot>();
                for (var i = 0; i < group.Count; i++)
                {
                    var query = new AvailabilityQuery(period, new[] { group[i] });
                    var result = engine.Compute(query, inputs);
                    for (var s = 0; s < result.Slots.Count; s++)
                    {
                        var slot = result.Slots[s];
                        union.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, allResourceIds));
                    }
                }

                var normalizedUnion = Normalize(union, mergeByResources: false);
                intersection = intersection == null ? normalizedUnion : Intersect(intersection, normalizedUnion, allResourceIds);
            }

            return new AvailabilityResult(intersection ?? new List<UtcSlot>());
        }

        public static AvailabilityResult ComputeOrOnlyOptimized(
            IReadOnlyList<List<int>> orGroups,
            IReadOnlyList<int> allResourceIds,
            IReadOnlyDictionary<int, List<UtcSlot>> perResource)
        {
            List<UtcSlot>? intersection = null;
            for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
            {
                var group = orGroups[groupIndex];
                var union = new List<UtcSlot>();
                for (var i = 0; i < group.Count; i++)
                {
                    if (!perResource.TryGetValue(group[i], out var slots))
                    {
                        continue;
                    }

                    for (var s = 0; s < slots.Count; s++)
                    {
                        var slot = slots[s];
                        union.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, allResourceIds));
                    }
                }

                var normalizedUnion = Normalize(union, mergeByResources: false);
                intersection = intersection == null ? normalizedUnion : Intersect(intersection, normalizedUnion, allResourceIds);
            }

            return new AvailabilityResult(intersection ?? new List<UtcSlot>());
        }

        public static AvailabilityResult ComputePerGroupAncestorsBaseline(
            AvailabilityEngine engine,
            DatePeriod period,
            IReadOnlyList<List<int>> orGroups,
            IReadOnlyDictionary<int, IReadOnlyCollection<int>> ancestorMap,
            IReadOnlyList<int> allResourceIds,
            AvailabilityInputs inputs)
        {
            List<UtcSlot>? intersection = null;
            for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
            {
                var group = orGroups[groupIndex];
                var union = new List<UtcSlot>();
                for (var i = 0; i < group.Count; i++)
                {
                    var required = new HashSet<int> { group[i] };
                    required.UnionWith(ancestorMap[group[i]]);
                    var query = new AvailabilityQuery(period, required.OrderBy(id => id).ToArray());
                    var result = engine.Compute(query, inputs);
                    for (var s = 0; s < result.Slots.Count; s++)
                    {
                        var slot = result.Slots[s];
                        union.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, allResourceIds));
                    }
                }

                var normalizedUnion = Normalize(union, mergeByResources: false);
                intersection = intersection == null ? normalizedUnion : Intersect(intersection, normalizedUnion, allResourceIds);
            }

            return new AvailabilityResult(intersection ?? new List<UtcSlot>());
        }

        public static AvailabilityResult ComputePerGroupAncestorsOptimized(
            AvailabilityEngine engine,
            DatePeriod period,
            IReadOnlyList<List<int>> orGroups,
            IReadOnlyDictionary<int, IReadOnlyCollection<int>> ancestorMap,
            IReadOnlyList<int> allResourceIds,
            IReadOnlyDictionary<int, List<UtcSlot>> perResource)
        {
            List<UtcSlot>? intersection = null;
            for (var groupIndex = 0; groupIndex < orGroups.Count; groupIndex++)
            {
                var group = orGroups[groupIndex];
                var union = new List<UtcSlot>();
                for (var i = 0; i < group.Count; i++)
                {
                    var required = new HashSet<int> { group[i] };
                    required.UnionWith(ancestorMap[group[i]]);
                    var query = new AvailabilityQuery(period, required.OrderBy(id => id).ToArray());
                    var result = engine.ComposeAvailability(query, perResource);
                    for (var s = 0; s < result.Slots.Count; s++)
                    {
                        var slot = result.Slots[s];
                        union.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, allResourceIds));
                    }
                }

                var normalizedUnion = Normalize(union, mergeByResources: false);
                intersection = intersection == null ? normalizedUnion : Intersect(intersection, normalizedUnion, allResourceIds);
            }

            return new AvailabilityResult(intersection ?? new List<UtcSlot>());
        }

        private static List<UtcSlot> Intersect(IReadOnlyList<UtcSlot> first, IReadOnlyList<UtcSlot> second, IReadOnlyList<int> resourceIds)
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
                    result.Add(new UtcSlot(start, end, resourceIds));
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
    }

    private static class BaselineCoreBuilder
    {
        public static IReadOnlyDictionary<int, List<UtcSlot>> ComputePerResource(AvailabilityQuery query, AvailabilityInputs inputs)
        {
            var allResourceIds = query.AllResourceIds.Count > 0 ? query.AllResourceIds.ToArray() : query.RequiredResourceIds.ToArray();
            var perResource = new Dictionary<int, List<UtcSlot>>(allResourceIds.Length);
            foreach (var resourceId in allResourceIds)
            {
                var positive = new List<UtcSlot>();
                var negative = new List<UtcSlot>();
                for (var i = 0; i < inputs.Rules.Count; i++)
                {
                    var rule = inputs.Rules[i];
                    if (rule.ResourceId != resourceId)
                    {
                        continue;
                    }

                    var occurrences = SharedCoreLogic.GenerateOccurrences(rule, query.Period);
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

                if (positive.Count == 0)
                {
                    perResource[resourceId] = new List<UtcSlot>();
                    continue;
                }

                for (var i = 0; i < inputs.BusySlots.Count; i++)
                {
                    var busy = inputs.BusySlots[i];
                    if (busy.ResourceId == resourceId)
                    {
                        negative.Add(new UtcSlot(busy.StartUtc, busy.EndUtc, new[] { resourceId }));
                    }
                }

                var normalizedPositive = SharedCoreLogic.NormalizeSlots(positive);
                var normalizedNegative = SharedCoreLogic.NormalizeSlots(negative);
                perResource[resourceId] = SubtractComparer.Baseline(normalizedPositive, normalizedNegative);
            }

            return perResource;
        }
    }

    private static class GroupedCoreBuilder
    {
        public static IReadOnlyDictionary<int, List<UtcSlot>> ComputePerResource(AvailabilityQuery query, AvailabilityInputs inputs)
        {
            var rulesByResource = inputs.Rules.GroupBy(rule => rule.ResourceId).ToDictionary(group => group.Key, group => group.ToList());
            var busyByResource = inputs.BusySlots.GroupBy(busy => busy.ResourceId).ToDictionary(group => group.Key, group => group.ToList());
            var allResourceIds = query.AllResourceIds.Count > 0 ? query.AllResourceIds.ToArray() : query.RequiredResourceIds.ToArray();
            var perResource = new Dictionary<int, List<UtcSlot>>(allResourceIds.Length);
            foreach (var resourceId in allResourceIds)
            {
                var positive = new List<UtcSlot>();
                var negative = new List<UtcSlot>();
                if (rulesByResource.TryGetValue(resourceId, out var rules))
                {
                    for (var i = 0; i < rules.Count; i++)
                    {
                        var occurrences = SharedCoreLogic.GenerateOccurrences(rules[i], query.Period);
                        if (occurrences.Count == 0)
                        {
                            continue;
                        }

                        if (rules[i].IsExclude)
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

                if (busyByResource.TryGetValue(resourceId, out var busy))
                {
                    for (var i = 0; i < busy.Count; i++)
                    {
                        negative.Add(new UtcSlot(busy[i].StartUtc, busy[i].EndUtc, new[] { resourceId }));
                    }
                }

                var normalizedPositive = SharedCoreLogic.NormalizeSlots(positive);
                var normalizedNegative = SharedCoreLogic.NormalizeSlots(negative);
                perResource[resourceId] = SubtractComparer.Baseline(normalizedPositive, normalizedNegative);
            }

            return perResource;
        }
    }

    private static class SharedCoreLogic
    {
        public static List<UtcSlot> GenerateOccurrences(RuleModel rule, DatePeriod period)
        {
            return rule.Kind switch
            {
                RuleKind.RecurringWeekly => GenerateWeekly(rule, period),
                RuleKind.SingleDate => GenerateSingleDate(rule, period),
                RuleKind.Range => GenerateRange(rule, period),
                _ => throw new NotSupportedException()
            };
        }

        public static List<UtcSlot> NormalizeSlots(IReadOnlyList<UtcSlot> slots)
        {
            if (slots.Count == 0)
            {
                return new List<UtcSlot>();
            }

            var ordered = new List<UtcSlot>(slots);
            ordered.Sort(static (a, b) =>
            {
                var startCompare = a.StartUtc.CompareTo(b.StartUtc);
                return startCompare != 0 ? startCompare : a.EndUtc.CompareTo(b.EndUtc);
            });

            var normalized = new List<UtcSlot> { ordered[0] };
            for (var i = 1; i < ordered.Count; i++)
            {
                var last = normalized[^1];
                var current = ordered[i];
                if (current.StartUtc <= last.EndUtc && last.ResourceIds.SequenceEqual(current.ResourceIds))
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

        private static List<UtcSlot> GenerateWeekly(RuleModel rule, DatePeriod period)
        {
            if (rule.DaysOfWeekMask == null)
            {
                return new List<UtcSlot>();
            }

            var slots = new List<UtcSlot>();
            foreach (var day in period.EnumerateDays())
            {
                var bit = 1 << (int)day.DayOfWeek;
                if ((rule.DaysOfWeekMask.Value & bit) == bit)
                {
                    slots.Add(CreateSlot(day, rule));
                }
            }

            return slots;
        }

        private static List<UtcSlot> GenerateSingleDate(RuleModel rule, DatePeriod period)
        {
            if (rule.SingleDate == null || rule.SingleDate.Value < period.From || rule.SingleDate.Value > period.To)
            {
                return new List<UtcSlot>();
            }

            return new List<UtcSlot> { CreateSlot(rule.SingleDate.Value, rule) };
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
            for (var day = start; day <= end; day = day.AddDays(1))
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
    }

    private static class SubtractComparer
    {
        public static List<UtcSlot> Baseline(IReadOnlyList<UtcSlot> available, IReadOnlyList<UtcSlot> blocks)
        {
            if (blocks.Count == 0)
            {
                return new List<UtcSlot>(available);
            }

            var result = new List<UtcSlot>(available.Count);
            var segments = new List<UtcSlot>(1);
            var nextSegments = new List<UtcSlot>(2);
            for (var i = 0; i < available.Count; i++)
            {
                segments.Clear();
                segments.Add(available[i]);
                for (var b = 0; b < blocks.Count; b++)
                {
                    if (segments.Count == 0)
                    {
                        break;
                    }

                    nextSegments.Clear();
                    var block = blocks[b];
                    for (var s = 0; s < segments.Count; s++)
                    {
                        var segment = segments[s];
                        if (block.EndUtc <= segment.StartUtc || block.StartUtc >= segment.EndUtc)
                        {
                            nextSegments.Add(segment);
                            continue;
                        }

                        if (block.StartUtc <= segment.StartUtc && block.EndUtc >= segment.EndUtc)
                        {
                            continue;
                        }

                        if (block.StartUtc > segment.StartUtc)
                        {
                            nextSegments.Add(new UtcSlot(segment.StartUtc, block.StartUtc, segment.ResourceIds));
                        }

                        if (block.EndUtc < segment.EndUtc)
                        {
                            nextSegments.Add(new UtcSlot(block.EndUtc, segment.EndUtc, segment.ResourceIds));
                        }
                    }

                    var swap = segments;
                    segments = nextSegments;
                    nextSegments = swap;
                }

                result.AddRange(segments);
            }

            return result;
        }

        public static List<UtcSlot> Current(IReadOnlyList<UtcSlot> available, IReadOnlyList<UtcSlot> blocks)
        {
            if (blocks.Count == 0)
            {
                return new List<UtcSlot>(available);
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
    }
}
