using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Order;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Core;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[Config(typeof(AppBenchmarkConfig))]
public class ApplicationBenchmarks
{
    private AvailabilityService _service = null!;
    private AvailabilityComputeRequest _request = null!;

    [ParamsSource(nameof(Scenarios))]
    public AppScenario Scenario { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        var dataSource = new DemoAvailabilityDataSource();
        var schemaSource = new DemoPropertySchemaDataSource();
        var schemaService = new PropertySchemaService(schemaSource);
        _service = new AvailabilityService(dataSource, schemaService, new AvailabilityEngineV1());
        _request = Scenario.BuildRequest();
    }

    [Benchmark]
    public async Task Compute()
    {
        await _service.ComputeAsync(_request, CancellationToken.None);
    }

    public static IEnumerable<AppScenario> Scenarios()
    {
        var monday = new DateOnly(2026, 1, 12);
        var friday = new DateOnly(2026, 1, 16);
        return new[]
        {
            new AppScenario(
                "App_OR_Ancestors",
                () => new AvailabilityComputeRequest(
                    monday,
                    monday,
                    Array.Empty<int>(),
                    ResourceOrGroups: new[]
                    {
                        new[] { DemoAvailabilityDataSource.Doctor7Id, DemoAvailabilityDataSource.Doctor8Id },
                        new[] { DemoAvailabilityDataSource.Room1Id, DemoAvailabilityDataSource.Room4Id }
                    },
                    IncludeResourceAncestors: true)),
            new AppScenario(
                "App_PropertyDescendants",
                () => new AvailabilityComputeRequest(
                    monday,
                    monday,
                    Array.Empty<int>(),
                    PropertyIds: new[] { DemoPropertySchemaDataSource.ImagingRootId },
                    ResourceOrGroups: new[]
                    {
                        new[]
                        {
                            DemoAvailabilityDataSource.Room1Id,
                            DemoAvailabilityDataSource.Room2Id,
                            DemoAvailabilityDataSource.Room3Id,
                            DemoAvailabilityDataSource.Room4Id
                        }
                    },
                    IncludePropertyDescendants: true)),
            new AppScenario(
                "App_SlotDuration",
                () => new AvailabilityComputeRequest(
                    friday,
                    friday,
                    new[] { DemoAvailabilityDataSource.Room3Id },
                    SlotDurationMinutes: 60,
                    IncludeRemainderSlot: true))
        };
    }

    public sealed record AppScenario(string Name, Func<AvailabilityComputeRequest> BuildRequest)
    {
        public override string ToString() => Name;
    }

    private sealed class AppBenchmarkConfig : ManualConfig
    {
        public AppBenchmarkConfig()
        {
            AddColumn(StatisticColumn.P50, StatisticColumn.P95);
            AddExporter(MarkdownExporter.GitHub, CsvExporter.Default, HtmlExporter.Default);
        }
    }

    private sealed class DemoAvailabilityDataSource : IAvailabilityDataSource
    {
        public const int SiteAId = 1;
        public const int SiteBId = 2;
        public const int FloorA1Id = 3;
        public const int FloorB1Id = 4;
        public const int Room1Id = 11;
        public const int Room2Id = 12;
        public const int Room3Id = 13;
        public const int Room4Id = 14;
        public const int Doctor7Id = 21;
        public const int Doctor8Id = 22;
        public const int Doctor9Id = 23;

        private static readonly List<ResourceRelationLink> Relations = new()
        {
            new ResourceRelationLink(SiteAId, FloorA1Id, "Contains"),
            new ResourceRelationLink(FloorA1Id, Room1Id, "Contains"),
            new ResourceRelationLink(FloorA1Id, Room2Id, "Contains"),
            new ResourceRelationLink(SiteAId, Room3Id, "Contains"),
            new ResourceRelationLink(SiteBId, FloorB1Id, "Contains"),
            new ResourceRelationLink(FloorB1Id, Room4Id, "Contains"),
            new ResourceRelationLink(SiteAId, Doctor7Id, "WorksIn"),
            new ResourceRelationLink(SiteAId, Doctor8Id, "WorksIn"),
            new ResourceRelationLink(SiteBId, Doctor9Id, "WorksIn")
        };

        private static readonly List<RuleData> Rules = new()
        {
            new RuleData(100, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { SiteAId }),
            new RuleData(101, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { FloorA1Id }),
            new RuleData(1, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { Room1Id }),
            new RuleData(2, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { Room2Id }),
            new RuleData(3, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 16),
                new TimeOnly(9, 0), new TimeOnly(10, 20), null, null, null, new[] { Room3Id }),
            new RuleData(4, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { Room4Id }),
            new RuleData(5, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { Doctor7Id }),
            new RuleData(6, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { Doctor8Id }),
            new RuleData(7, (byte)RuleKind.SingleDate, false, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(14, 0), new TimeOnly(18, 0), null, null, null, new[] { Doctor9Id }),
            new RuleData(8, (byte)RuleKind.SingleDate, true, null, null, new DateOnly(2026, 1, 12),
                new TimeOnly(15, 0), new TimeOnly(16, 0), null, null, null, new[] { SiteAId })
        };

        private static readonly List<BusyEventData> BusyEvents = new()
        {
            new BusyEventData(1, new DateTime(2026, 1, 12, 16, 30, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 12, 17, 0, 0, DateTimeKind.Utc),
                new[] { Room1Id, Doctor7Id })
        };

        private static readonly Dictionary<int, List<int>> PropertyLinks = new()
        {
            [SiteAId] = new List<int> { DemoPropertySchemaDataSource.LocationMilanId, DemoPropertySchemaDataSource.AccreditationIsoId },
            [SiteBId] = new List<int> { DemoPropertySchemaDataSource.LocationRomeId },
            [Room1Id] = new List<int> { DemoPropertySchemaDataSource.OctId },
            [Room2Id] = new List<int> { DemoPropertySchemaDataSource.MriId },
            [Room3Id] = new List<int> { DemoPropertySchemaDataSource.UltrasoundId },
            [Room4Id] = new List<int> { DemoPropertySchemaDataSource.OctId },
            [Doctor7Id] = new List<int> { DemoPropertySchemaDataSource.OphthalmologyId },
            [Doctor8Id] = new List<int> { DemoPropertySchemaDataSource.CardiologyId },
            [Doctor9Id] = new List<int> { DemoPropertySchemaDataSource.CardiologyId }
        };

        public Task<IReadOnlyList<RuleData>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var matches = Rules.Where(rule =>
                rule.ResourceIds.Any(resourceIds.Contains) &&
                (rule.SingleDateUtc == null ||
                 (rule.SingleDateUtc.Value >= fromDateUtc && rule.SingleDateUtc.Value <= toDateUtc)));
            return Task.FromResult<IReadOnlyList<RuleData>>(matches.ToList());
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
            var matches = BusyEvents.Where(evt =>
                evt.ResourceIds.Any(resourceIds.Contains) &&
                evt.EndUtc > fromUtc &&
                evt.StartUtc < toUtcExclusive);
            return Task.FromResult<IReadOnlyList<BusyEventData>>(matches.ToList());
        }

        public Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(int propertyId, CancellationToken ct)
        {
            var nodes = DemoPropertySchemaDataSource.PropertyNodes;
            var result = new List<PropertyNode>();
            var stack = new Stack<int>();
            stack.Push(propertyId);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var node = nodes.FirstOrDefault(item => item.Id == current);
                if (node == null)
                {
                    continue;
                }
                result.Add(new PropertyNode(node.Id, node.ParentId, node.Key, node.Label, node.SortOrder));
                foreach (var child in nodes.Where(item => item.ParentId == current))
                {
                    stack.Push(child.Id);
                }
            }

            return Task.FromResult<IReadOnlyList<PropertyNode>>(result);
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
                var matched = 0;
                foreach (var propertyId in required)
                {
                    if (link.Value.Contains(propertyId))
                    {
                        matched++;
                    }
                }

                if (matched == required.Count)
                {
                    ids.Add(link.Key);
                }
            }

            return Task.FromResult<IReadOnlyList<int>>(ids);
        }

        public Task<IReadOnlyList<ResourceSummary>> GetResourcesAsync(bool onlySchedulable, CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceSummary>>(Array.Empty<ResourceSummary>());
        }

        public Task<IReadOnlyList<RuleSummary>> GetRuleSummariesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<RuleSummary>>(Array.Empty<RuleSummary>());
        }

        public Task<IReadOnlyList<BusyEventSummary>> GetBusyEventSummariesAsync(
            DateTime fromUtc,
            DateTime toUtcExclusive,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<BusyEventSummary>>(Array.Empty<BusyEventSummary>());
        }

        public Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsAsync(
            IReadOnlyList<int> childResourceIds,
            IReadOnlyList<string>? relationTypes,
            CancellationToken ct)
        {
            var query = Relations.Where(relation => childResourceIds.Contains(relation.ChildResourceId));
            if (relationTypes != null && relationTypes.Count > 0)
            {
                query = query.Where(relation => relationTypes.Contains(relation.RelationType));
            }

            return Task.FromResult<IReadOnlyList<ResourceRelationLink>>(query.ToList());
        }
    }

    private sealed class DemoPropertySchemaDataSource : IPropertySchemaDataSource
    {
        public const int SpecializationRootId = 100;
        public const int OphthalmologyId = 101;
        public const int CardiologyId = 102;

        public const int RoomFeatureRootId = 110;
        public const int ImagingRootId = 111;
        public const int OctId = 112;
        public const int MriId = 113;
        public const int UltrasoundId = 114;

        public const int LocationRootId = 120;
        public const int LocationMilanId = 121;
        public const int LocationRomeId = 122;

        public const int AccreditationRootId = 130;
        public const int AccreditationIsoId = 131;

        public static readonly List<PropertySchemaNode> PropertyNodes = new()
        {
            new PropertySchemaNode(SpecializationRootId, null, "Specialization", "Specialization", null),
            new PropertySchemaNode(OphthalmologyId, SpecializationRootId, "Specialization", "Ophthalmology", 1),
            new PropertySchemaNode(CardiologyId, SpecializationRootId, "Specialization", "Cardiology", 2),
            new PropertySchemaNode(RoomFeatureRootId, null, "RoomFeature", "RoomFeature", null),
            new PropertySchemaNode(ImagingRootId, RoomFeatureRootId, "RoomFeature", "Imaging", 1),
            new PropertySchemaNode(OctId, ImagingRootId, "RoomFeature", "OCT", 1),
            new PropertySchemaNode(MriId, ImagingRootId, "RoomFeature", "MRI", 2),
            new PropertySchemaNode(UltrasoundId, ImagingRootId, "RoomFeature", "Ultrasound", 3),
            new PropertySchemaNode(LocationRootId, null, "Location", "Location", null),
            new PropertySchemaNode(LocationMilanId, LocationRootId, "Location", "Milan", 1),
            new PropertySchemaNode(LocationRomeId, LocationRootId, "Location", "Rome", 2),
            new PropertySchemaNode(AccreditationRootId, null, "Accreditation", "Accreditation", null),
            new PropertySchemaNode(AccreditationIsoId, AccreditationRootId, "Accreditation", "ISO 9001", 1)
        };

        private static readonly List<ResourceTypePropertyLink> TypeLinks = new()
        {
            new ResourceTypePropertyLink(13, SpecializationRootId),
            new ResourceTypePropertyLink(12, RoomFeatureRootId),
            new ResourceTypePropertyLink(10, LocationRootId),
            new ResourceTypePropertyLink(10, AccreditationRootId)
        };

        private static readonly List<ResourceTypeAssignment> Assignments = new()
        {
            new ResourceTypeAssignment(DemoAvailabilityDataSource.SiteAId, 10),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.SiteBId, 10),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.FloorA1Id, 11),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.FloorB1Id, 11),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.Room1Id, 12),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.Room2Id, 12),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.Room3Id, 12),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.Room4Id, 12),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.Doctor7Id, 13),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.Doctor8Id, 13),
            new ResourceTypeAssignment(DemoAvailabilityDataSource.Doctor9Id, 13)
        };

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
            var result = Assignments.Where(item => resourceIds.Contains(item.ResourceId)).ToList();
            return Task.FromResult<IReadOnlyList<ResourceTypeAssignment>>(result);
        }
    }
}
