using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Core;
using Xunit;

namespace HelixScheduler.WebApi.Tests;

public sealed class AvailabilityScenarioTests
{
    private readonly AvailabilityService _service;

    public AvailabilityScenarioTests()
    {
        var dataSource = new DemoAvailabilityQueryService();
        var schemaSource = new DemoPropertySchemaQueryService();
        var schemaService = new PropertySchemaService(schemaSource);
        _service = new AvailabilityService(dataSource, dataSource, dataSource, schemaService, new AvailabilityEngine());
    }

    [Fact]
    public async Task OrGroups_Returns_Full_Slot_When_Any_Doctor_And_Any_Room()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            ResourceOrGroups: new[]
            {
                new[] { DemoAvailabilityQueryService.Doctor7Id, DemoAvailabilityQueryService.Doctor8Id },
                new[] { DemoAvailabilityQueryService.Room1Id, DemoAvailabilityQueryService.Room4Id }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);

        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task AncestorFilters_Location_Milan_Allows_SiteA()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: new[] { DemoAvailabilityQueryService.Room1Id },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: DemoPropertySchemaQueryService.SiteTypeId,
                    PropertyIds: new[] { DemoPropertySchemaQueryService.LocationMilanId })
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task AncestorFilters_Location_Milan_Rejects_SiteB()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: new[] { DemoAvailabilityQueryService.Room4Id },
            IncludeResourceAncestors: true,
            AncestorFilters: new[]
            {
                new AncestorPropertyFilter(
                    ResourceTypeId: DemoPropertySchemaQueryService.SiteTypeId,
                    PropertyIds: new[] { DemoPropertySchemaQueryService.LocationMilanId })
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task IncludeResourceAncestors_Applies_Negative_Site_Rule()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: new[] { DemoAvailabilityQueryService.Room2Id },
            IncludeResourceAncestors: true);

        var result = await _service.ComputeAsync(request, CancellationToken.None);

        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 15, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 16, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task RelationTypes_Restrict_Ancestor_Expansion()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: new[] { DemoAvailabilityQueryService.Doctor8Id },
            IncludeResourceAncestors: true,
            AncestorRelationTypes: new[] { "Contains" });

        var result = await _service.ComputeAsync(request, CancellationToken.None);

        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task RelationTypes_Allows_Negative_Site_Rule()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: new[] { DemoAvailabilityQueryService.Doctor8Id },
            IncludeResourceAncestors: true);

        var result = await _service.ComputeAsync(request, CancellationToken.None);

        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 15, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 16, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task OrGroups_With_Ancestors_PerGroup_Preserves_Result()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            ResourceOrGroups: new[]
            {
                new[] { DemoAvailabilityQueryService.Doctor7Id, DemoAvailabilityQueryService.Doctor8Id },
                new[] { DemoAvailabilityQueryService.Room1Id, DemoAvailabilityQueryService.Room4Id }
            },
            IncludeResourceAncestors: true,
            AncestorMode: "perGroup");

        var result = await _service.ComputeAsync(request, CancellationToken.None);

        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 15, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 16, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 16, 30, 0, DateTimeKind.Utc));
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 17, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task PropertyDescendants_Match_Imaging_Rooms()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.ImagingRootId },
                    MatchMode: "and",
                    IncludePropertyDescendants: true)
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.Room1Id,
                    DemoAvailabilityQueryService.Room2Id,
                    DemoAvailabilityQueryService.Room3Id,
                    DemoAvailabilityQueryService.Room4Id
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task PropertyDescendants_Off_Requires_Direct_Property()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.ImagingRootId },
                    MatchMode: "and")
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.Room1Id,
                    DemoAvailabilityQueryService.Room2Id,
                    DemoAvailabilityQueryService.Room3Id,
                    DemoAvailabilityQueryService.Room4Id
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task PropertyFilterGroups_Or_Allows_Either_Specialization()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.OphthalmologyId, DemoPropertySchemaQueryService.CardiologyId },
                    MatchMode: "or")
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.Doctor7Id,
                    DemoAvailabilityQueryService.Doctor8Id,
                    DemoAvailabilityQueryService.Doctor9Id
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task PropertyFilterGroups_And_Between_Groups_Reduces_To_SiteA()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.LocationMilanId, DemoPropertySchemaQueryService.LocationRomeId },
                    MatchMode: "or"),
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.AccreditationIsoId },
                    MatchMode: "and")
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.SiteAId,
                    DemoAvailabilityQueryService.SiteBId
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 15, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 16, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task PropertyFilterGroups_IncludeDescendants_Or_Matches_Imaging_Rooms()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.ImagingRootId },
                    MatchMode: "or",
                    IncludePropertyDescendants: true)
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.Room1Id,
                    DemoAvailabilityQueryService.Room2Id,
                    DemoAvailabilityQueryService.Room3Id,
                    DemoAvailabilityQueryService.Room4Id
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task PropertyFilterGroups_Duplicates_Do_Not_Change_Result()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.CardiologyId, DemoPropertySchemaQueryService.CardiologyId },
                    MatchMode: "or")
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.Doctor8Id,
                    DemoAvailabilityQueryService.Doctor9Id
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task PropertyFilterGroups_Or_With_Descendants_Union_Matches_Rooms()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.ImagingRootId, DemoPropertySchemaQueryService.OctId },
                    MatchMode: "or",
                    IncludePropertyDescendants: true)
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.Room1Id,
                    DemoAvailabilityQueryService.Room2Id,
                    DemoAvailabilityQueryService.Room3Id,
                    DemoAvailabilityQueryService.Room4Id
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.NotEmpty(result.Slots);
    }

    [Fact]
    public async Task PropertyFilterGroups_And_Within_Group_Requires_All_Properties()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.OphthalmologyId, DemoPropertySchemaQueryService.CardiologyId },
                    MatchMode: "and")
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.Doctor7Id,
                    DemoAvailabilityQueryService.Doctor8Id,
                    DemoAvailabilityQueryService.Doctor9Id
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task PropertyFilterGroups_Filter_Matches_Without_Legacy_Fallback()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.LocationMilanId },
                    MatchMode: "and")
            },
            ResourceOrGroups: new[]
            {
                new[]
                {
                    DemoAvailabilityQueryService.SiteAId,
                    DemoAvailabilityQueryService.SiteBId
                }
            });

        var result = await _service.ComputeAsync(request, CancellationToken.None);
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 15, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 12, 16, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 12, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task PropertyFilterGroups_Invalid_MatchMode_Throws()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 12),
            ToDate: new DateOnly(2026, 1, 12),
            RequiredResourceIds: Array.Empty<int>(),
            PropertyFilterGroups: new[]
            {
                new PropertyFilterGroup(
                    new[] { DemoPropertySchemaQueryService.OphthalmologyId },
                    MatchMode: "xor")
            },
            ResourceOrGroups: new[]
            {
                new[] { DemoAvailabilityQueryService.Doctor7Id }
            });

        await Assert.ThrowsAsync<AvailabilityRequestException>(() =>
            _service.ComputeAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SlotDuration_Emits_Remainder_When_Enabled()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 16),
            ToDate: new DateOnly(2026, 1, 16),
            RequiredResourceIds: new[] { DemoAvailabilityQueryService.Room3Id },
            SlotDurationMinutes: 60,
            IncludeRemainderSlot: true);

        var result = await _service.ComputeAsync(request, CancellationToken.None);

        Assert.Equal(2, result.Slots.Count);
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 16, 9, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 16, 10, 0, 0, DateTimeKind.Utc));
        Assert.Contains(result.Slots, slot =>
            slot.StartUtc == new DateTime(2026, 1, 16, 10, 0, 0, DateTimeKind.Utc) &&
            slot.EndUtc == new DateTime(2026, 1, 16, 10, 20, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task SlotDuration_Drops_Remainder_When_Disabled()
    {
        var request = new AvailabilityComputeRequest(
            FromDate: new DateOnly(2026, 1, 16),
            ToDate: new DateOnly(2026, 1, 16),
            RequiredResourceIds: new[] { DemoAvailabilityQueryService.Room3Id },
            SlotDurationMinutes: 60,
            IncludeRemainderSlot: false);

        var result = await _service.ComputeAsync(request, CancellationToken.None);

        Assert.Single(result.Slots);
        Assert.Equal(
            new DateTime(2026, 1, 16, 10, 0, 0, DateTimeKind.Utc),
            result.Slots[0].EndUtc);
    }

    private sealed class DemoAvailabilityQueryService : IAvailabilityComputeQueryService, IAvailabilityFilterQueryService, IAvailabilityAncestorQueryService
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
            [SiteAId] = new List<int> { DemoPropertySchemaQueryService.LocationMilanId, DemoPropertySchemaQueryService.AccreditationIsoId },
            [SiteBId] = new List<int> { DemoPropertySchemaQueryService.LocationRomeId },
            [Room1Id] = new List<int> { DemoPropertySchemaQueryService.OctId },
            [Room2Id] = new List<int> { DemoPropertySchemaQueryService.MriId },
            [Room3Id] = new List<int> { DemoPropertySchemaQueryService.UltrasoundId },
            [Room4Id] = new List<int> { DemoPropertySchemaQueryService.OctId },
            [Doctor7Id] = new List<int> { DemoPropertySchemaQueryService.OphthalmologyId },
            [Doctor8Id] = new List<int> { DemoPropertySchemaQueryService.CardiologyId },
            [Doctor9Id] = new List<int> { DemoPropertySchemaQueryService.CardiologyId }
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
            var nodes = DemoPropertySchemaQueryService.PropertyNodes;
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
        public const int SiteTypeId = 10;
        public const int FloorTypeId = 11;
        public const int RoomTypeId = 12;
        public const int DoctorTypeId = 13;

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
            new ResourceTypePropertyLink(DoctorTypeId, SpecializationRootId),
            new ResourceTypePropertyLink(RoomTypeId, RoomFeatureRootId),
            new ResourceTypePropertyLink(SiteTypeId, LocationRootId),
            new ResourceTypePropertyLink(SiteTypeId, AccreditationRootId)
        };

        private static readonly List<ResourceTypeAssignment> Assignments = new()
        {
            new ResourceTypeAssignment(DemoAvailabilityQueryService.SiteAId, SiteTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.SiteBId, SiteTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.FloorA1Id, FloorTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.FloorB1Id, FloorTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.Room1Id, RoomTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.Room2Id, RoomTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.Room3Id, RoomTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.Room4Id, RoomTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.Doctor7Id, DoctorTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.Doctor8Id, DoctorTypeId),
            new ResourceTypeAssignment(DemoAvailabilityQueryService.Doctor9Id, DoctorTypeId)
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
