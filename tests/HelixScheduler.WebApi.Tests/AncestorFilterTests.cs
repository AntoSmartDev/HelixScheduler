using System.Linq;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Core;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class AncestorFilterTests
{
    private readonly AvailabilityService _service;

    public AncestorFilterTests()
    {
        var dataSource = new FakeAvailabilityDataSource();
        var schemaSource = new FakePropertySchemaDataSource();
        var schemaService = new PropertySchemaService(schemaSource);
        _service = new AvailabilityService(dataSource, schemaService, new AvailabilityEngineV1());
    }

    [Fact]
    public async Task AncestorFilters_Defaults_Allow_Or_Match()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { 301 },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: 1,
                    PropertyIds: new[] { 11, 12 })
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task AncestorFilters_And_Requires_All_Properties()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { 301 },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: 1,
                    PropertyIds: new[] { 11, 12 },
                    MatchMode: "and")
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task AncestorFilters_And_DirectParent_Passes()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { 301 },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: 3,
                    PropertyIds: new[] { 21, 22 },
                    MatchMode: "and",
                    Scope: "directParent")
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task AncestorFilters_DirectParent_Fails_When_Only_Ancestor()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { 301 },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: 4,
                    PropertyIds: new[] { 31 },
                    Scope: "directParent")
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task AncestorFilters_AnyAncestor_Passes()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { 301 },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: 4,
                    PropertyIds: new[] { 31 },
                    Scope: "anyAncestor")
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task AncestorFilters_NearestOfType_Passes()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { 301 },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: 1,
                    PropertyIds: new[] { 11 },
                    Scope: "nearestOfType")
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task AncestorFilters_Invalid_MatchMode_Throws()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { 301 },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: 1,
                    PropertyIds: new[] { 11 },
                    MatchMode: "xor")
            });

        await Assert.ThrowsAsync<AvailabilityRequestException>(() => _service.ComputeAsync(request, CancellationToken.None));
    }

    private sealed class FakeAvailabilityDataSource : IAvailabilityDataSource
    {
        private static readonly Dictionary<int, int> ResourceTypes = new()
        {
            [100] = 4,
            [101] = 1,
            [102] = 1,
            [201] = 3,
            [301] = 2,
            [302] = 2
        };

        private static readonly List<ResourceRelationLink> Relations = new()
        {
            new ResourceRelationLink(100, 101, "Contains"),
            new ResourceRelationLink(101, 201, "Contains"),
            new ResourceRelationLink(201, 301, "Contains"),
            new ResourceRelationLink(100, 102, "Contains"),
            new ResourceRelationLink(102, 302, "Contains")
        };

        private static readonly Dictionary<int, List<int>> PropertyLinks = new()
        {
            [101] = new List<int> { 11 },
            [102] = new List<int> { 12 },
            [201] = new List<int> { 21, 22 },
            [100] = new List<int> { 31 }
        };

        public Task<IReadOnlyList<RuleData>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var rules = new List<RuleData>();
            foreach (var resourceId in resourceIds)
            {
                rules.Add(new RuleData(
                    resourceId,
                    (byte)RuleKind.RecurringWeekly,
                    false,
                    null,
                    null,
                    null,
                    new TimeOnly(9, 0),
                    new TimeOnly(10, 0),
                    127,
                    null,
                    null,
                    new[] { resourceId }));
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

        public Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(
            int propertyId,
            CancellationToken ct)
        {
            var nodes = FakePropertySchemaDataSource.PropertyNodes;
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

        public Task<IReadOnlyList<ResourceSummary>> GetResourcesAsync(
            bool onlySchedulable,
            CancellationToken ct)
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

    private sealed class FakePropertySchemaDataSource : IPropertySchemaDataSource
    {
        public static readonly List<PropertySchemaNode> PropertyNodes = new()
        {
            new PropertySchemaNode(10, null, "Location", "Location", null),
            new PropertySchemaNode(11, 10, "Location", "Milan", 1),
            new PropertySchemaNode(12, 10, "Location", "Rome", 2),
            new PropertySchemaNode(20, null, "FloorFeature", "FloorFeature", null),
            new PropertySchemaNode(21, 20, "FloorFeature", "Sterile", 1),
            new PropertySchemaNode(22, 20, "FloorFeature", "Quiet", 2),
            new PropertySchemaNode(30, null, "RegionTag", "RegionTag", null),
            new PropertySchemaNode(31, 30, "RegionTag", "North", 1)
        };

        private static readonly List<ResourceTypePropertyLink> TypeLinks = new()
        {
            new ResourceTypePropertyLink(1, 10),
            new ResourceTypePropertyLink(3, 20),
            new ResourceTypePropertyLink(4, 30)
        };

        private static readonly List<ResourceTypeAssignment> Assignments = new()
        {
            new ResourceTypeAssignment(100, 4),
            new ResourceTypeAssignment(101, 1),
            new ResourceTypeAssignment(102, 1),
            new ResourceTypeAssignment(201, 3),
            new ResourceTypeAssignment(301, 2),
            new ResourceTypeAssignment(302, 2)
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
