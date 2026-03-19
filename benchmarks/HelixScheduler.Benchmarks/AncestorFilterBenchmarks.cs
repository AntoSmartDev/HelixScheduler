using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.Orchestration;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Core;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(AncestorFilterBenchmarkConfig))]
public class AncestorFilterBenchmarks
{
    private AvailabilityService _service = null!;

    [ParamsSource(nameof(Scenarios))]
    public AncestorScenario Scenario { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        var queryService = new DemoAvailabilityQueryService();
        var schemaService = new PropertySchemaService(new DemoPropertySchemaQueryService());
        _service = new AvailabilityService(queryService, queryService, queryService, schemaService, new AvailabilityEngine());
    }

    [Benchmark]
    public Task<AvailabilityComputeResponse> Compute()
    {
        return _service.ComputeAsync(Scenario.BuildRequest(), CancellationToken.None);
    }

    public static IEnumerable<AncestorScenario> Scenarios()
    {
        var day = new DateOnly(2026, 1, 6);
        return
        [
            new AncestorScenario(
                "Ancestors_AnyAncestor",
                () => new AvailabilityComputeRequest(
                    day,
                    day,
                    RequiredResourceIds: [301],
                    IncludeResourceAncestors: true,
                    AncestorFilters:
                    [
                        new AncestorPropertyFilter(
                            ResourceTypeId: 4,
                            PropertyIds: [31],
                            Scope: "anyAncestor")
                    ])),
            new AncestorScenario(
                "Ancestors_NearestOfType",
                () => new AvailabilityComputeRequest(
                    day,
                    day,
                    RequiredResourceIds: [301],
                    IncludeResourceAncestors: true,
                    AncestorFilters:
                    [
                        new AncestorPropertyFilter(
                            ResourceTypeId: 1,
                            PropertyIds: [11],
                            Scope: "nearestOfType")
                    ])),
            new AncestorScenario(
                "Ancestors_MatchAllAncestors",
                () => new AvailabilityComputeRequest(
                    day,
                    day,
                    RequiredResourceIds: [401],
                    IncludeResourceAncestors: true,
                    AncestorFilters:
                    [
                        new AncestorPropertyFilter(
                            ResourceTypeId: 1,
                            PropertyIds: [11, 12],
                            MatchMode: "or",
                            MatchAllAncestors: true,
                            Scope: "anyAncestor")
                    ]))
        ];
    }

    public sealed record AncestorScenario(string Name, Func<AvailabilityComputeRequest> BuildRequest)
    {
        public override string ToString() => Name;
    }

    private sealed class AncestorFilterBenchmarkConfig : ManualConfig
    {
        public AncestorFilterBenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }

    private sealed class DemoAvailabilityQueryService :
        IAvailabilityComputeQueryService,
        IAvailabilityFilterQueryService,
        IAvailabilityAncestorQueryService
    {
        private static readonly List<ResourceRelationLink> Relations =
        [
            new(100, 101, "Contains"),
            new(101, 201, "Contains"),
            new(201, 301, "Contains"),
            new(100, 102, "Contains"),
            new(101, 401, "Contains"),
            new(102, 401, "Contains")
        ];

        private static readonly Dictionary<int, List<int>> PropertyLinks = new()
        {
            [101] = [11],
            [102] = [12],
            [201] = [21, 22],
            [100] = [31]
        };

        public Task<IReadOnlyList<RuleData>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var rules = new List<RuleData>(resourceIds.Count);
            foreach (var resourceId in resourceIds)
            {
                rules.Add(new RuleData(
                    resourceId,
                    (byte)RuleKind.SingleDate,
                    false,
                    null,
                    null,
                    new DateOnly(2026, 1, 6),
                    new TimeOnly(9, 0),
                    new TimeOnly(10, 0),
                    null,
                    null,
                    null,
                    [resourceId]));
            }

            return Task.FromResult<IReadOnlyList<RuleData>>(rules);
        }

        public Task<IReadOnlyDictionary<int, int>> GetResourceCapacitiesAsync(
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());
        }

        public Task<IReadOnlyList<BusyEventData>> GetBusyEventsAsync(
            DateTime fromUtc,
            DateTime toUtcExclusive,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<BusyEventData>>(Array.Empty<BusyEventData>());
        }

        public Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(int propertyId, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<PropertyNode>>(Array.Empty<PropertyNode>());
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            var ids = new HashSet<int>();
            foreach (var link in PropertyLinks)
            {
                if (link.Value.Any(propertyIds.Contains))
                {
                    ids.Add(link.Key);
                }
            }

            return Task.FromResult<IReadOnlyList<int>>(ids.ToList());
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            if (propertyIds.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
            }

            var required = new HashSet<int>(propertyIds);
            var ids = new List<int>();
            foreach (var link in PropertyLinks)
            {
                if (required.All(link.Value.Contains))
                {
                    ids.Add(link.Key);
                }
            }

            return Task.FromResult<IReadOnlyList<int>>(ids);
        }

        public async Task<IReadOnlyList<IReadOnlyList<int>>> GetResourceIdsByPropertySetsAsync(
            IReadOnlyList<IReadOnlyList<int>> propertySets,
            CancellationToken ct)
        {
            var result = new List<IReadOnlyList<int>>(propertySets.Count);
            for (var i = 0; i < propertySets.Count; i++)
            {
                result.Add(await GetResourceIdsByPropertiesAsync(propertySets[i].ToList(), ct).ConfigureAwait(false));
            }

            return result;
        }

        public Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsByTypesAsync(
            IReadOnlyList<string>? relationTypes,
            CancellationToken ct)
        {
            var query = Relations.AsEnumerable();
            if (relationTypes != null && relationTypes.Count > 0)
            {
                query = query.Where(relation => relationTypes.Contains(relation.RelationType));
            }

            return Task.FromResult<IReadOnlyList<ResourceRelationLink>>(query.ToList());
        }
    }

    private sealed class DemoPropertySchemaQueryService : IPropertySchemaQueryService
    {
        private static readonly List<PropertySchemaNode> PropertyNodes =
        [
            new(10, null, "Location", "Location", null),
            new(11, 10, "Location", "Milan", 1),
            new(12, 10, "Location", "Rome", 2),
            new(20, null, "FloorFeature", "FloorFeature", null),
            new(21, 20, "FloorFeature", "Sterile", 1),
            new(22, 20, "FloorFeature", "Quiet", 2),
            new(30, null, "RegionTag", "RegionTag", null),
            new(31, 30, "RegionTag", "North", 1)
        ];

        private static readonly List<ResourceTypePropertyLink> TypeLinks =
        [
            new(1, 10),
            new(3, 20),
            new(4, 30)
        ];

        private static readonly List<ResourceTypeAssignment> Assignments =
        [
            new(100, 4),
            new(101, 1),
            new(102, 1),
            new(201, 3),
            new(301, 2),
            new(401, 2)
        ];

        public Task<IReadOnlyList<PropertySchemaNode>> GetPropertyNodesAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<PropertySchemaNode>>(PropertyNodes);
        }

        public Task<IReadOnlyList<ResourceTypePropertyLink>> GetResourceTypePropertiesAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceTypePropertyLink>>(TypeLinks);
        }

        public Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceTypeAssignment>>(
                Assignments.Where(item => resourceIds.Contains(item.ResourceId)).ToList());
        }
    }
}
