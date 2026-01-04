using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;
using HelixScheduler.Core;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(BenchmarkConfig))]
public class AvailabilityBenchmarks
{
    private AvailabilityEngine _engine = null!;
    private AvailabilityQuery _query = null!;
    private List<SchedulingRule> _rules = null!;
    private List<BusySlot> _busySlots = null!;

    [ParamsSource(nameof(Scenarios))]
    public BenchmarkScenario Scenario { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engine = new AvailabilityEngine();
        BuildScenario(Scenario, out _query, out _rules, out _busySlots);
    }

    [Benchmark]
    public AvailabilityResult Compute()
    {
        return _engine.Compute(_query, _rules, _busySlots);
    }

    public static IEnumerable<BenchmarkScenario> Scenarios()
    {
        return new[]
        {
            new BenchmarkScenario("D1_R1_OR0_BusyLow", 1, 1, 0, 0, BusyDensity.Low),
            new BenchmarkScenario("D7_R2_OR1_BusyMed", 7, 2, 1, 3, BusyDensity.Medium),
            new BenchmarkScenario("D14_R4_OR2_BusyHigh", 14, 4, 2, 3, BusyDensity.High),
            new BenchmarkScenario("D31_R2_OR2_BusyMed", 31, 2, 2, 5, BusyDensity.Medium),
            new BenchmarkScenario("D7_R1_OR0_BusyHigh", 7, 1, 0, 0, BusyDensity.High),
            new BenchmarkScenario("D14_R4_OR1_BusyLow", 14, 4, 1, 4, BusyDensity.Low)
        };
    }

    private static void BuildScenario(
        BenchmarkScenario scenario,
        out AvailabilityQuery query,
        out List<SchedulingRule> rules,
        out List<BusySlot> busySlots)
    {
        var totalResources = scenario.RequiredCount + (scenario.OrGroupCount * scenario.OrGroupSize);
        var resourceIds = Enumerable.Range(1, totalResources).ToArray();

        var from = new DateOnly(2026, 3, 9);
        var to = from.AddDays(scenario.Days - 1);
        var period = new DatePeriod(from, to);

        var requiredIds = resourceIds.Take(scenario.RequiredCount).ToList();
        var orGroups = new List<IReadOnlyList<int>>();
        var cursor = scenario.RequiredCount;
        for (var groupIndex = 0; groupIndex < scenario.OrGroupCount; groupIndex++)
        {
            var group = resourceIds.Skip(cursor).Take(scenario.OrGroupSize).ToList();
            orGroups.Add(group);
            cursor += scenario.OrGroupSize;
        }

        query = new AvailabilityQuery(period, requiredIds, resourceOrGroups: orGroups);

        rules = BuildRules(resourceIds, period);
        busySlots = BuildBusySlots(resourceIds, period, scenario.BusyDensity, scenario.Seed);
    }

    private static List<SchedulingRule> BuildRules(IReadOnlyList<int> resourceIds, DatePeriod period)
    {
        var rules = new List<SchedulingRule>();
        var weekdays = DaysMask(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);

        for (var i = 0; i < resourceIds.Count; i++)
        {
            var resourceId = resourceIds[i];
            rules.Add(new SchedulingRule(
                SchedulingRuleKind.Weekly,
                isExclude: false,
                fromDateUtc: period.From,
                toDateUtc: period.To,
                singleDateUtc: null,
                timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(17)),
                daysOfWeekMask: weekdays,
                dayOfMonth: null,
                intervalDays: null,
                resourceIds: new[] { resourceId }));

            if (i % 4 == 0)
            {
                rules.Add(new SchedulingRule(
                    SchedulingRuleKind.Weekly,
                    isExclude: true,
                    fromDateUtc: period.From,
                    toDateUtc: period.To,
                    singleDateUtc: null,
                    timeRange: new TimeRange(TimeSpan.FromHours(12), TimeSpan.FromHours(13)),
                    daysOfWeekMask: weekdays,
                    dayOfMonth: null,
                    intervalDays: null,
                    resourceIds: new[] { resourceId }));
            }

            if (i % 5 == 0)
            {
                rules.Add(new SchedulingRule(
                    SchedulingRuleKind.SingleDate,
                    isExclude: true,
                    fromDateUtc: null,
                    toDateUtc: null,
                    singleDateUtc: period.From.AddDays(Math.Min(2, period.To.DayNumber - period.From.DayNumber)),
                    timeRange: new TimeRange(TimeSpan.FromHours(15), TimeSpan.FromHours(16)),
                    daysOfWeekMask: null,
                    dayOfMonth: null,
                    intervalDays: null,
                    resourceIds: new[] { resourceId }));
            }
        }

        return rules;
    }

    private static List<BusySlot> BuildBusySlots(
        IReadOnlyList<int> resourceIds,
        DatePeriod period,
        BusyDensity density,
        int seed)
    {
        var busySlots = new List<BusySlot>();
        var random = new Random(seed);
        var probability = density switch
        {
            BusyDensity.Low => 0.10,
            BusyDensity.Medium => 0.30,
            BusyDensity.High => 0.60,
            _ => 0.10
        };

        foreach (var resourceId in resourceIds)
        {
            foreach (var day in period.EnumerateDays())
            {
                if (random.NextDouble() >= probability)
                {
                    continue;
                }

                var startHour = 9 + random.Next(0, 6);
                var start = DateTime.SpecifyKind(day.ToDateTime(new TimeOnly(startHour, 0)), DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(day.ToDateTime(new TimeOnly(startHour, 30)), DateTimeKind.Utc);
                busySlots.Add(new BusySlot(resourceId, start, end));
            }
        }

        return busySlots;
    }

    private static int DaysMask(params DayOfWeek[] days)
    {
        var mask = 0;
        for (var i = 0; i < days.Length; i++)
        {
            mask |= 1 << (int)days[i];
        }

        return mask;
    }

    public sealed record BenchmarkScenario(
        string Name,
        int Days,
        int RequiredCount,
        int OrGroupCount,
        int OrGroupSize,
        BusyDensity BusyDensity)
    {
        public int Seed => Name.GetHashCode(StringComparison.Ordinal);
        public override string ToString() => Name;
    }

    public enum BusyDensity
    {
        Low,
        Medium,
        High
    }

    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }
}
