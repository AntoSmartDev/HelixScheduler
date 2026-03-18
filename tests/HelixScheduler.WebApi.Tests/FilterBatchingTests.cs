using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.Orchestration;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Core;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class FilterBatchingTests
{
    [Fact]
    public async Task PropertyFilterGroups_With_Descendants_Use_Batched_PropertySet_Query()
    {
        var dataSource = new CountingAvailabilityQueryService();
        var schemaService = new PropertySchemaService(new CountingPropertySchemaQueryService());
        var service = new AvailabilityService(dataSource, dataSource, dataSource, schemaService, new AvailabilityEngine());

        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { CountingPropertySchemaQueryService.SharedRootId, CountingPropertySchemaQueryService.SharedLeafAId },
                    MatchMode: "and",
                    IncludePropertyDescendants: true)
            },
            ResourceOrGroups: new[]
            {
                new[] { CountingAvailabilityQueryService.ResourceId }
            });

        await service.ComputeAsync(request, CancellationToken.None);

        Assert.Equal(1, dataSource.PropertySetBatchCalls);
        Assert.Equal(0, dataSource.SinglePropertyCalls);
    }

    [Fact]
    public async Task Shared_Subtree_Expansion_Is_Reused_Across_Property_And_Ancestor_Filters()
    {
        var dataSource = new CountingAvailabilityQueryService();
        var schemaService = new PropertySchemaService(new CountingPropertySchemaQueryService());
        var service = new AvailabilityService(dataSource, dataSource, dataSource, schemaService, new AvailabilityEngine());

        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { CountingAvailabilityQueryService.ResourceId },
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { CountingPropertySchemaQueryService.SharedRootId },
                    MatchMode: "and",
                    IncludePropertyDescendants: true)
            },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: CountingPropertySchemaQueryService.AncestorTypeId,
                    PropertyIds: new[] { CountingPropertySchemaQueryService.SharedRootId },
                    IncludePropertyDescendants: true)
            });

        var result = await service.ComputeAsync(request, CancellationToken.None);

        Assert.NotEmpty(result.Slots);
        Assert.Equal(1, dataSource.ExpandPropertySubtreeCalls);
    }

    [Fact]
    public async Task AncestorFilters_Use_Batched_PropertySet_Query()
    {
        var dataSource = new CountingAvailabilityQueryService();
        var schemaService = new PropertySchemaService(new CountingPropertySchemaQueryService());
        var service = new AvailabilityService(dataSource, dataSource, dataSource, schemaService, new AvailabilityEngine());

        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { CountingAvailabilityQueryService.ResourceId },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: CountingPropertySchemaQueryService.AncestorTypeId,
                    PropertyIds: new[] { CountingPropertySchemaQueryService.SharedLeafAId, CountingPropertySchemaQueryService.SharedLeafBId },
                    MatchMode: "and")
            });

        var result = await service.ComputeAsync(request, CancellationToken.None);

        Assert.NotEmpty(result.Slots);
        Assert.Equal(1, dataSource.PropertySetBatchCalls);
        Assert.Equal(0, dataSource.SinglePropertyCalls);
    }

    [Fact]
    public async Task IncludeResourceAncestors_Uses_Single_Load_For_Deep_Hierarchy()
    {
        var dataSource = new CountingAvailabilityQueryService();
        var schemaService = new PropertySchemaService(new CountingPropertySchemaQueryService());
        var service = new AvailabilityService(dataSource, dataSource, dataSource, schemaService, new AvailabilityEngine());

        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 6),
            ToDate: new DateOnly(2026, 1, 6),
            RequiredResourceIds: new[] { CountingAvailabilityQueryService.ResourceId },
            IncludeResourceAncestors: true);

        var result = await service.ComputeAsync(request, CancellationToken.None);

        Assert.NotEmpty(result.Slots);
        Assert.Equal(1, dataSource.RelationSingleLoadCalls);
        Assert.Equal(0, dataSource.RelationPerLevelCalls);
    }

    private sealed class CountingAvailabilityQueryService : IAvailabilityComputeQueryService, IAvailabilityFilterQueryService, IAvailabilityAncestorQueryService
    {
        public const int RootAncestorId = 202;
        public const int AncestorId = 201;
        public const int ResourceId = 301;

        public int ExpandPropertySubtreeCalls { get; private set; }
        public int SinglePropertyCalls { get; private set; }
        public int PropertySetBatchCalls { get; private set; }
        public int RelationPerLevelCalls { get; private set; }
        public int RelationSingleLoadCalls { get; private set; }

        private static readonly IReadOnlyList<ResourceRelationLink> Relations =
            new[]
            {
                new ResourceRelationLink(RootAncestorId, AncestorId, "Contains"),
                new ResourceRelationLink(AncestorId, ResourceId, "Contains")
            };

        private static readonly IReadOnlyDictionary<int, IReadOnlyList<int>> PropertyLinks =
            new Dictionary<int, IReadOnlyList<int>>
            {
                [AncestorId] = new[] { CountingPropertySchemaQueryService.SharedLeafAId, CountingPropertySchemaQueryService.SharedLeafBId },
                [ResourceId] = new[] { CountingPropertySchemaQueryService.SharedLeafAId }
            };

        public Task<IReadOnlyList<RuleData>> GetRulesAsync(
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var rules = new List<RuleData>(resourceIds.Count);
            for (var i = 0; i < resourceIds.Count; i++)
            {
                rules.Add(new RuleData(
                    resourceIds[i],
                    (byte)RuleKind.SingleDate,
                    false,
                    null,
                    null,
                    fromDateUtc,
                    new TimeOnly(9, 0),
                    new TimeOnly(10, 0),
                    null,
                    null,
                    null,
                    new[] { resourceIds[i] }));
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
            ExpandPropertySubtreeCalls++;
            if (propertyId != CountingPropertySchemaQueryService.SharedRootId)
            {
                return Task.FromResult<IReadOnlyList<PropertyNode>>(Array.Empty<PropertyNode>());
            }

            return Task.FromResult<IReadOnlyList<PropertyNode>>(new[]
            {
                new PropertyNode(CountingPropertySchemaQueryService.SharedRootId, null, "Shared", "Shared", null),
                new PropertyNode(CountingPropertySchemaQueryService.SharedLeafAId, CountingPropertySchemaQueryService.SharedRootId, "Shared", "Leaf A", 1),
                new PropertyNode(CountingPropertySchemaQueryService.SharedLeafBId, CountingPropertySchemaQueryService.SharedRootId, "Shared", "Leaf B", 2)
            });
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            SinglePropertyCalls++;
            return Task.FromResult<IReadOnlyList<int>>(ResolveMatches(propertyIds));
        }

        public Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
            IReadOnlyList<int> propertyIds,
            CancellationToken ct)
        {
            var required = new HashSet<int>(propertyIds);
            var matches = new List<int>();
            foreach (var link in PropertyLinks)
            {
                if (required.All(link.Value.Contains))
                {
                    matches.Add(link.Key);
                }
            }

            return Task.FromResult<IReadOnlyList<int>>(matches);
        }

        public Task<IReadOnlyList<IReadOnlyList<int>>> GetResourceIdsByPropertySetsAsync(
            IReadOnlyList<IReadOnlyList<int>> propertySets,
            CancellationToken ct)
        {
            PropertySetBatchCalls++;
            var result = new List<IReadOnlyList<int>>(propertySets.Count);
            for (var i = 0; i < propertySets.Count; i++)
            {
                result.Add(ResolveMatches(propertySets[i]));
            }

            return Task.FromResult<IReadOnlyList<IReadOnlyList<int>>>(result);
        }

        public Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsByTypesAsync(
            IReadOnlyList<string>? relationTypes,
            CancellationToken ct)
        {
            RelationSingleLoadCalls++;
            var result = Relations.AsEnumerable();
            if (relationTypes != null && relationTypes.Count > 0)
            {
                result = result.Where(relation => relationTypes.Contains(relation.RelationType));
            }

            return Task.FromResult<IReadOnlyList<ResourceRelationLink>>(result.ToList());
        }

        private static IReadOnlyList<int> ResolveMatches(IReadOnlyList<int> propertyIds)
        {
            var ids = new HashSet<int>();
            foreach (var link in PropertyLinks)
            {
                if (link.Value.Any(propertyIds.Contains))
                {
                    ids.Add(link.Key);
                }
            }

            return ids.ToList();
        }
    }

    private sealed class CountingPropertySchemaQueryService : IPropertySchemaQueryService
    {
        public const int AncestorTypeId = 1;
        public const int ResourceTypeId = 2;
        public const int SharedRootId = 10;
        public const int SharedLeafAId = 11;
        public const int SharedLeafBId = 12;

        public Task<IReadOnlyList<PropertySchemaNode>> GetPropertyNodesAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<PropertySchemaNode>>(new[]
            {
                new PropertySchemaNode(SharedRootId, null, "Shared", "Shared", null),
                new PropertySchemaNode(SharedLeafAId, SharedRootId, "Shared", "Leaf A", 1),
                new PropertySchemaNode(SharedLeafBId, SharedRootId, "Shared", "Leaf B", 2)
            });
        }

        public Task<IReadOnlyList<ResourceTypePropertyLink>> GetResourceTypePropertiesAsync(CancellationToken ct)
        {
            return Task.FromResult<IReadOnlyList<ResourceTypePropertyLink>>(new[]
            {
                new ResourceTypePropertyLink(AncestorTypeId, SharedRootId),
                new ResourceTypePropertyLink(ResourceTypeId, SharedRootId)
            });
        }

        public Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
            IReadOnlyList<int> resourceIds,
            CancellationToken ct)
        {
            var result = new List<ResourceTypeAssignment>();
            for (var i = 0; i < resourceIds.Count; i++)
            {
                if (resourceIds[i] == CountingAvailabilityQueryService.RootAncestorId
                    || resourceIds[i] == CountingAvailabilityQueryService.AncestorId)
                {
                    result.Add(new ResourceTypeAssignment(resourceIds[i], AncestorTypeId));
                }
                else if (resourceIds[i] == CountingAvailabilityQueryService.ResourceId)
                {
                    result.Add(new ResourceTypeAssignment(resourceIds[i], ResourceTypeId));
                }
            }

            return Task.FromResult<IReadOnlyList<ResourceTypeAssignment>>(result);
        }
    }
}


