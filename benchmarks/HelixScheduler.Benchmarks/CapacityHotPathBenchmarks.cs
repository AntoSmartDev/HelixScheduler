using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;
using HelixScheduler.Core;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(CapacityHotPathBenchmarkConfig))]
public class CapacityHotPathBenchmarks
{
    private readonly AvailabilityEngine _engine = new();
    private AvailabilityQuery _query = null!;
    private AvailabilityInputs _inputs = null!;
    private IReadOnlyList<BusySlot> _busySlots = null!;

    [Params(2, 3)]
    public int Capacity { get; set; }

    [Params(100, 500)]
    public int BusyCount { get; set; }

    [Params("DenseOverlap", "SteppedOverlap")]
    public string Pattern { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        const int resourceId = 1;
        var day = new DateOnly(2026, 1, 12);
        var period = new DatePeriod(day, day);
        _query = new AvailabilityQuery(period, new[] { resourceId });
        _busySlots = BuildBusySlots(resourceId, day, BusyCount, Pattern);
        _inputs = new AvailabilityInputs(
            new[]
            {
                new AvailabilityRule(
                    1,
                    RuleKind.SingleDate,
                    false,
                    null,
                    null,
                    day,
                    TimeSpan.FromHours(8),
                    TimeSpan.FromHours(18),
                    null,
                    null,
                    null,
                    resourceId)
            },
            _busySlots,
            new Dictionary<int, int> { [resourceId] = Capacity });
    }

    [Benchmark(Baseline = true, Description = "BuildCapacityBlocks current")]
    public List<UtcSlot> BuildCapacityBlocks_Current()
    {
        return CapacityPathComparer.BuildCapacityBlocksCurrent(_busySlots, 1, Capacity);
    }

    [Benchmark(Description = "BuildCapacityBlocks presized candidate")]
    public List<UtcSlot> BuildCapacityBlocks_PresizedCandidate()
    {
        return CapacityPathComparer.BuildCapacityBlocksPresized(_busySlots, 1, Capacity);
    }

    [Benchmark(Description = "Engine compute current")]
    public AvailabilityResult Engine_Compute()
    {
        return _engine.Compute(_query, _inputs);
    }

    private static IReadOnlyList<BusySlot> BuildBusySlots(int resourceId, DateOnly day, int busyCount, string pattern)
    {
        var busy = new List<BusySlot>(busyCount);
        for (var i = 0; i < busyCount; i++)
        {
            var start = pattern == "DenseOverlap"
                ? CreateUtc(day, 8, (i % 20) * 5)
                : CreateUtc(day, 8 + ((i / 12) % 8), (i % 4) * 10);
            var durationMinutes = pattern == "DenseOverlap"
                ? 90 + (i % 3) * 15
                : 45 + (i % 5) * 15;
            busy.Add(new BusySlot(start, start.AddMinutes(durationMinutes), resourceId));
        }

        return busy;
    }

    private static DateTime CreateUtc(DateOnly day, int hour, int minute)
    {
        var normalizedHour = hour + (minute / 60);
        var normalizedMinute = minute % 60;
        return DateTime.SpecifyKind(day.ToDateTime(new TimeOnly(normalizedHour, normalizedMinute)), DateTimeKind.Utc);
    }

    private sealed class CapacityHotPathBenchmarkConfig : ManualConfig
    {
        public CapacityHotPathBenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }

    private static class CapacityPathComparer
    {
        public static List<UtcSlot> BuildCapacityBlocksCurrent(
            IReadOnlyList<BusySlot> busySlots,
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
            return BuildBlocksFromEdges(edges, resourceId, capacity);
        }

        public static List<UtcSlot> BuildCapacityBlocksPresized(
            IReadOnlyList<BusySlot> busySlots,
            int resourceId,
            int capacity)
        {
            var edges = new List<BusyEdge>(busySlots.Count * 2);
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
            return BuildBlocksFromEdges(edges, resourceId, capacity);
        }

        private static List<UtcSlot> BuildBlocksFromEdges(
            List<BusyEdge> edges,
            int resourceId,
            int capacity)
        {
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
    }
}
