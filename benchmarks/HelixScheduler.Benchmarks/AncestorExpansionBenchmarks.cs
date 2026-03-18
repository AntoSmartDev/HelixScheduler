using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(AncestorExpansionBenchmarkConfig))]
public class AncestorExpansionBenchmarks
{
    private List<int> _leafResourceIds = null!;
    private List<ResourceRelationEdge> _relations = null!;

    [Params(4, 8)]
    public int Depth { get; set; }

    [Params(3, 5)]
    public int LeafCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_leafResourceIds, _relations) = BuildHierarchy(Depth, LeafCount);
    }

    [Benchmark(Baseline = true, Description = "Ancestor expansion baseline (per-level relation fetch)")]
    public int Baseline_PerLevel()
    {
        var parentsByChild = new Dictionary<int, HashSet<int>>();
        var pending = new HashSet<int>(_leafResourceIds);
        var processed = new HashSet<int>();

        while (pending.Count > 0)
        {
            var batch = pending.Where(id => processed.Add(id)).ToList();
            pending.Clear();
            if (batch.Count == 0)
            {
                break;
            }

            var relations = FilterByChildren(_relations, batch);
            for (var i = 0; i < relations.Count; i++)
            {
                var relation = relations[i];
                if (!parentsByChild.TryGetValue(relation.ChildId, out var parents))
                {
                    parents = new HashSet<int>();
                    parentsByChild[relation.ChildId] = parents;
                }

                if (parents.Add(relation.ParentId) && !processed.Contains(relation.ParentId))
                {
                    pending.Add(relation.ParentId);
                }
            }
        }

        return CountAncestors(_leafResourceIds, parentsByChild);
    }

    [Benchmark(Description = "Ancestor expansion current (single-load graph traversal)")]
    public int Current_SingleLoad()
    {
        var reachable = BuildReachableAncestorParents(_leafResourceIds, _relations);
        return CountAncestors(_leafResourceIds, reachable);
    }

    private static List<ResourceRelationEdge> FilterByChildren(
        IReadOnlyList<ResourceRelationEdge> relations,
        IReadOnlyCollection<int> childIds)
    {
        var result = new List<ResourceRelationEdge>();
        for (var i = 0; i < relations.Count; i++)
        {
            var relation = relations[i];
            if (childIds.Contains(relation.ChildId))
            {
                result.Add(relation);
            }
        }

        return result;
    }

    private static Dictionary<int, HashSet<int>> BuildReachableAncestorParents(
        IReadOnlyCollection<int> resourceIds,
        IReadOnlyList<ResourceRelationEdge> relations)
    {
        var fullParentsByChild = new Dictionary<int, List<int>>();
        for (var i = 0; i < relations.Count; i++)
        {
            var relation = relations[i];
            if (!fullParentsByChild.TryGetValue(relation.ChildId, out var parents))
            {
                parents = new List<int>();
                fullParentsByChild[relation.ChildId] = parents;
            }

            if (!parents.Contains(relation.ParentId))
            {
                parents.Add(relation.ParentId);
            }
        }

        var reachable = new Dictionary<int, HashSet<int>>();
        var pending = new Queue<int>();
        var visited = new HashSet<int>();

        foreach (var resourceId in resourceIds)
        {
            if (visited.Add(resourceId))
            {
                pending.Enqueue(resourceId);
            }
        }

        while (pending.Count > 0)
        {
            var childId = pending.Dequeue();
            if (!fullParentsByChild.TryGetValue(childId, out var parents))
            {
                continue;
            }

            if (!reachable.TryGetValue(childId, out var reachableParents))
            {
                reachableParents = new HashSet<int>();
                reachable[childId] = reachableParents;
            }

            for (var i = 0; i < parents.Count; i++)
            {
                var parentId = parents[i];
                reachableParents.Add(parentId);
                if (visited.Add(parentId))
                {
                    pending.Enqueue(parentId);
                }
            }
        }

        return reachable;
    }

    private static int CountAncestors(
        IReadOnlyList<int> resourceIds,
        IReadOnlyDictionary<int, HashSet<int>> parentsByChild)
    {
        var cache = new Dictionary<int, HashSet<int>>();
        var total = 0;
        for (var i = 0; i < resourceIds.Count; i++)
        {
            total += ResolveAncestors(resourceIds[i], parentsByChild, cache).Count;
        }

        return total;
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

    private static (List<int> Leaves, List<ResourceRelationEdge> Relations) BuildHierarchy(int depth, int leafCount)
    {
        var relations = new List<ResourceRelationEdge>();
        var leaves = new List<int>(leafCount);
        var nextId = 1;

        for (var leafIndex = 0; leafIndex < leafCount; leafIndex++)
        {
            var childId = nextId++;
            leaves.Add(childId);

            for (var level = 0; level < depth; level++)
            {
                var parentId = nextId++;
                relations.Add(new ResourceRelationEdge(parentId, childId));
                childId = parentId;
            }
        }

        return (leaves, relations);
    }

    private sealed record ResourceRelationEdge(int ParentId, int ChildId);

    private sealed class AncestorExpansionBenchmarkConfig : ManualConfig
    {
        public AncestorExpansionBenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }
}
