using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Demo;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Seed;

public sealed class DemoSeedService : IDemoSeedService
{
    private const int SeedVersion = 6;
    private readonly SchedulerDbContext _dbContext;
    private readonly IDemoScenarioStore _store;
    private readonly IClock _clock;
    private readonly ITenantContext _tenantContext;

    public DemoSeedService(
        SchedulerDbContext dbContext,
        IDemoScenarioStore store,
        IClock clock,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _store = store;
        _clock = clock;
        _tenantContext = tenantContext;
    }

    public async Task EnsureSeedAsync(CancellationToken ct)
    {
        var state = await _store.GetAsync(ct).ConfigureAwait(false);
        if (state != null && state.SeedVersion == SeedVersion)
        {
            return;
        }

        var baseDate = state?.BaseDateUtc ?? ComputeBaseDateUtc(_clock.UtcNow);
        await ApplySeedAsync(baseDate, ct).ConfigureAwait(false);
    }

    public async Task ResetAsync(CancellationToken ct)
    {
        var baseDate = ComputeBaseDateUtc(_clock.UtcNow);
        await ApplySeedAsync(baseDate, ct).ConfigureAwait(false);
    }

    private async Task ApplySeedAsync(DateOnly baseDateUtc, CancellationToken ct)
    {
        var nowUtc = _clock.UtcNow;
        await CleanupDemoRulesAndBusyAsync(ct).ConfigureAwait(false);
        await CleanupDemoPropertyTreeAsync(ct).ConfigureAwait(false);
        await CleanupDemoPropertyLinksAsync(ct).ConfigureAwait(false);
        await CleanupDemoTypeMappingsAsync(ct).ConfigureAwait(false);
        await CleanupDemoRelationsAsync(ct).ConfigureAwait(false);

        var siteType = await EnsureResourceTypeAsync("Site", "Site", 1, ct);
        var floorType = await EnsureResourceTypeAsync("Floor", "Floor", 2, ct);
        var roomType = await EnsureResourceTypeAsync("Room", "Room", 3, ct);
        var doctorType = await EnsureResourceTypeAsync("Doctor", "Doctor", 4, ct);

        var siteA = await EnsureResourceAsync("SITE-A", "Site A", capacity: 1, isSchedulable: false, siteType.Id, nowUtc, ct);
        var siteB = await EnsureResourceAsync("SITE-B", "Site B", capacity: 1, isSchedulable: false, siteType.Id, nowUtc, ct);
        var floorA1 = await EnsureResourceAsync("FLOOR-A1", "Floor A1", capacity: 1, isSchedulable: false, floorType.Id, nowUtc, ct);
        var floorB1 = await EnsureResourceAsync("FLOOR-B1", "Floor B1", capacity: 1, isSchedulable: false, floorType.Id, nowUtc, ct);
        var room1 = await EnsureResourceAsync("ROOM-1", "Room 1", capacity: 1, isSchedulable: true, roomType.Id, nowUtc, ct);
        var room2 = await EnsureResourceAsync("ROOM-2", "Room 2", capacity: 2, isSchedulable: true, roomType.Id, nowUtc, ct);
        var room3 = await EnsureResourceAsync("ROOM-3", "Room 3", capacity: 1, isSchedulable: true, roomType.Id, nowUtc, ct);
        var room4 = await EnsureResourceAsync("ROOM-4", "Room 4", capacity: 1, isSchedulable: true, roomType.Id, nowUtc, ct);
        var doctor7 = await EnsureResourceAsync("DOC-7", "Doctor 7", capacity: 1, isSchedulable: true, doctorType.Id, nowUtc, ct);
        var doctor8 = await EnsureResourceAsync("DOC-8", "Doctor 8", capacity: 1, isSchedulable: true, doctorType.Id, nowUtc, ct);
        var doctor9 = await EnsureResourceAsync("DOC-9", "Doctor 9", capacity: 1, isSchedulable: true, doctorType.Id, nowUtc, ct);

        await EnsureRelationAsync(siteA.Id, floorA1.Id, "Contains", ct);
        await EnsureRelationAsync(floorA1.Id, room1.Id, "Contains", ct);
        await EnsureRelationAsync(floorA1.Id, room2.Id, "Contains", ct);
        await EnsureRelationAsync(siteA.Id, room3.Id, "Contains", ct);
        await EnsureRelationAsync(siteB.Id, floorB1.Id, "Contains", ct);
        await EnsureRelationAsync(floorB1.Id, room4.Id, "Contains", ct);
        await EnsureRelationAsync(siteA.Id, doctor7.Id, "WorksIn", ct);
        await EnsureRelationAsync(siteA.Id, doctor8.Id, "WorksIn", ct);
        await EnsureRelationAsync(siteB.Id, doctor9.Id, "WorksIn", ct);

        var specializationRoot = await EnsurePropertyAsync("Specialization", "Specialization", null, null, ct);
        var roomFeatureRoot = await EnsurePropertyAsync("RoomFeature", "RoomFeature", null, null, ct);
        var locationRoot = await EnsurePropertyAsync("Location", "Location", null, null, ct);
        var accreditationRoot = await EnsurePropertyAsync("Accreditation", "Accreditation", null, null, ct);

        var ophthalmology = await EnsurePropertyAsync("Specialization", "Ophthalmology", specializationRoot.Id, 1, ct);
        var retina = await EnsurePropertyAsync("Specialization", "Retina", ophthalmology.Id, 1, ct);
        var cardiology = await EnsurePropertyAsync("Specialization", "Cardiology", specializationRoot.Id, 2, ct);
        var interventionalCardiology = await EnsurePropertyAsync(
            "Specialization",
            "Interventional Cardiology",
            cardiology.Id,
            1,
            ct);
        var imaging = await EnsurePropertyAsync("RoomFeature", "Imaging", roomFeatureRoot.Id, 1, ct);
        var oct = await EnsurePropertyAsync("RoomFeature", "OCT", imaging.Id, 1, ct);
        var mri = await EnsurePropertyAsync("RoomFeature", "MRI", imaging.Id, 2, ct);
        var ultrasound = await EnsurePropertyAsync("RoomFeature", "Ultrasound", imaging.Id, 3, ct);
        var milan = await EnsurePropertyAsync("Location", "Milan", locationRoot.Id, 1, ct);
        var rome = await EnsurePropertyAsync("Location", "Rome", locationRoot.Id, 2, ct);
        var iso9001 = await EnsurePropertyAsync("Accreditation", "ISO 9001", accreditationRoot.Id, 1, ct);
        var soc2 = await EnsurePropertyAsync("Accreditation", "SOC2", accreditationRoot.Id, 2, ct);

        await EnsureResourceTypePropertyAsync(doctorType.Id, specializationRoot.Id, ct);
        await EnsureResourceTypePropertyAsync(roomType.Id, roomFeatureRoot.Id, ct);
        await EnsureResourceTypePropertyAsync(siteType.Id, locationRoot.Id, ct);
        await EnsureResourceTypePropertyAsync(siteType.Id, accreditationRoot.Id, ct);

        await EnsurePropertyLinkAsync(doctor7.Id, retina.Id, ct);
        await EnsurePropertyLinkAsync(doctor8.Id, cardiology.Id, ct);
        await EnsurePropertyLinkAsync(doctor9.Id, interventionalCardiology.Id, ct);
        await EnsurePropertyLinkAsync(room1.Id, oct.Id, ct);
        await EnsurePropertyLinkAsync(room2.Id, mri.Id, ct);
        await EnsurePropertyLinkAsync(room3.Id, ultrasound.Id, ct);
        await EnsurePropertyLinkAsync(room4.Id, oct.Id, ct);
        await EnsurePropertyLinkAsync(siteA.Id, milan.Id, ct);
        await EnsurePropertyLinkAsync(siteA.Id, iso9001.Id, ct);
        await EnsurePropertyLinkAsync(siteA.Id, soc2.Id, ct);
        await EnsurePropertyLinkAsync(siteB.Id, rome.Id, ct);
        await EnsurePropertyLinkAsync(siteB.Id, iso9001.Id, ct);

        var mondayWednesday = BuildDaysMask(DayOfWeek.Monday, DayOfWeek.Wednesday);
        var tuesdayThursday = BuildDaysMask(DayOfWeek.Tuesday, DayOfWeek.Thursday);
        var fridayOnly = BuildDaysMask(DayOfWeek.Friday);
        var allWeek = BuildDaysMask(
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday);

        var siteAOpen = await EnsureWeeklyRuleAsync(
            "Demo: Site A open hours",
            new TimeOnly(8, 0),
            new TimeOnly(20, 0),
            allWeek,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(siteAOpen.Id, siteA.Id, ct);

        var floorAOpen = await EnsureWeeklyRuleAsync(
            "Demo: Floor A1 open hours",
            new TimeOnly(8, 0),
            new TimeOnly(20, 0),
            allWeek,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(floorAOpen.Id, floorA1.Id, ct);

        var siteBOpen = await EnsureWeeklyRuleAsync(
            "Demo: Site B open hours",
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            allWeek,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(siteBOpen.Id, siteB.Id, ct);

        var floorBOpen = await EnsureWeeklyRuleAsync(
            "Demo: Floor B1 open hours",
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            allWeek,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(floorBOpen.Id, floorB1.Id, ct);

        var siteAExclude = await EnsureWeeklyRuleAsync(
            "Demo: Site A maintenance",
            new TimeOnly(15, 0),
            new TimeOnly(16, 0),
            BuildDaysMask(DayOfWeek.Wednesday),
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: true,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(siteAExclude.Id, siteA.Id, ct);

        var room1Rule = await EnsureWeeklyRuleAsync(
            "Demo: Room 1 availability",
            new TimeOnly(14, 0),
            new TimeOnly(18, 0),
            mondayWednesday,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room1Rule.Id, room1.Id, ct);

        var doctor7Rule = await EnsureWeeklyRuleAsync(
            "Demo: Doctor 7 availability",
            new TimeOnly(14, 0),
            new TimeOnly(18, 0),
            mondayWednesday,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(doctor7Rule.Id, doctor7.Id, ct);

        var room2Rule = await EnsureWeeklyRuleAsync(
            "Demo: Room 2 availability",
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            tuesdayThursday,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room2Rule.Id, room2.Id, ct);

        var doctor8Rule = await EnsureWeeklyRuleAsync(
            "Demo: Doctor 8 availability",
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            tuesdayThursday,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(doctor8Rule.Id, doctor8.Id, ct);

        var doctor9Rule = await EnsureWeeklyRuleAsync(
            "Demo: Doctor 9 availability",
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            tuesdayThursday,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(doctor9Rule.Id, doctor9.Id, ct);

        var room3Rule = await EnsureWeeklyRuleAsync(
            "Demo: Room 3 short session",
            new TimeOnly(9, 0),
            new TimeOnly(10, 20),
            fridayOnly,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room3Rule.Id, room3.Id, ct);

        var room4Rule = await EnsureWeeklyRuleAsync(
            "Demo: Room 4 availability",
            new TimeOnly(10, 0),
            new TimeOnly(15, 0),
            mondayWednesday,
            fromDateUtc: null,
            toDateUtc: null,
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room4Rule.Id, room4.Id, ct);

        var room2Single = await EnsureSingleDateRuleAsync(
            "Demo: Room 2 single date",
            new TimeOnly(12, 0),
            new TimeOnly(14, 0),
            baseDateUtc.AddDays(1),
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room2Single.Id, room2.Id, ct);

        var doctor7Range = await EnsureRangeRuleAsync(
            "Demo: Doctor 7 range availability",
            new TimeOnly(12, 0),
            new TimeOnly(14, 0),
            baseDateUtc.AddDays(7),
            baseDateUtc.AddDays(10),
            isExclude: false,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(doctor7Range.Id, doctor7.Id, ct);

        var busyDoctor7 = await EnsureBusyEventAsync(
            "Demo: Doctor 7 busy",
            ToUtc(baseDateUtc, new TimeOnly(15, 0)),
            ToUtc(baseDateUtc, new TimeOnly(16, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busyDoctor7.Id, doctor7.Id, ct);

        var busyDoctorRoom = await EnsureBusyEventAsync(
            "Demo: Doctor 7 + Room 1 busy",
            ToUtc(baseDateUtc.AddDays(2), new TimeOnly(16, 30)),
            ToUtc(baseDateUtc.AddDays(2), new TimeOnly(17, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busyDoctorRoom.Id, doctor7.Id, ct);
        await EnsureBusyEventResourceAsync(busyDoctorRoom.Id, room1.Id, ct);

        var busyRoom2 = await EnsureBusyEventAsync(
            "Demo: Room 2 busy",
            ToUtc(baseDateUtc.AddDays(1), new TimeOnly(10, 0)),
            ToUtc(baseDateUtc.AddDays(1), new TimeOnly(11, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busyRoom2.Id, room2.Id, ct);

        var busyDoctor8 = await EnsureBusyEventAsync(
            "Demo: Doctor 8 busy",
            ToUtc(baseDateUtc.AddDays(3), new TimeOnly(10, 30)),
            ToUtc(baseDateUtc.AddDays(3), new TimeOnly(11, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busyDoctor8.Id, doctor8.Id, ct);

        var busySiteA = await EnsureBusyEventAsync(
            "Demo: Site A busy",
            ToUtc(baseDateUtc.AddDays(1), new TimeOnly(10, 0)),
            ToUtc(baseDateUtc.AddDays(1), new TimeOnly(11, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busySiteA.Id, siteA.Id, ct);

        var busyFloorA = await EnsureBusyEventAsync(
            "Demo: Floor A1 busy",
            ToUtc(baseDateUtc.AddDays(1), new TimeOnly(11, 0)),
            ToUtc(baseDateUtc.AddDays(1), new TimeOnly(12, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busyFloorA.Id, floorA1.Id, ct);

        var busyRoom3 = await EnsureBusyEventAsync(
            "Demo: Room 3 busy",
            ToUtc(baseDateUtc.AddDays(4), new TimeOnly(9, 40)),
            ToUtc(baseDateUtc.AddDays(4), new TimeOnly(10, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busyRoom3.Id, room3.Id, ct);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        var state = new DemoScenarioState(baseDateUtc, SeedVersion, nowUtc);
        await _store.SaveAsync(state, ct).ConfigureAwait(false);
    }

    private static DateOnly ComputeBaseDateUtc(DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        var diff = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-diff);
    }

    private static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        return DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Utc);
    }

    private static int BuildDaysMask(params DayOfWeek[] days)
    {
        var mask = 0;
        for (var i = 0; i < days.Length; i++)
        {
            mask |= 1 << (int)days[i];
        }

        return mask;
    }

    private async Task<Resources> EnsureResourceAsync(
        string code,
        string name,
        int capacity,
        bool isSchedulable,
        int typeId,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(item => item.Name == name, ct)
            .ConfigureAwait(false);

        if (resource != null)
        {
            resource.Code = code;
            resource.IsSchedulable = isSchedulable;
            resource.Capacity = capacity < 1 ? 1 : capacity;
            resource.TypeId = typeId;
            return resource;
        }

        resource = new Resources
        {
            TenantId = _tenantContext.TenantId,
            Code = code,
            Name = name,
            IsSchedulable = isSchedulable,
            Capacity = capacity < 1 ? 1 : capacity,
            TypeId = typeId,
            CreatedAtUtc = nowUtc
        };

        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return resource;
    }

    private async Task<ResourceTypes> EnsureResourceTypeAsync(
        string key,
        string label,
        int? sortOrder,
        CancellationToken ct)
    {
        var type = await _dbContext.ResourceTypes
            .FirstOrDefaultAsync(item => item.Key == key, ct)
            .ConfigureAwait(false);

        if (type != null)
        {
            type.Label = label;
            type.SortOrder = sortOrder;
            return type;
        }

        type = new ResourceTypes
        {
            TenantId = _tenantContext.TenantId,
            Key = key,
            Label = label,
            SortOrder = sortOrder
        };

        _dbContext.ResourceTypes.Add(type);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return type;
    }

    private async Task EnsureResourceTypePropertyAsync(
        int resourceTypeId,
        int propertyDefinitionId,
        CancellationToken ct)
    {
        var exists = await _dbContext.ResourceTypeProperties.AnyAsync(
            link => link.ResourceTypeId == resourceTypeId && link.PropertyDefinitionId == propertyDefinitionId,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.ResourceTypeProperties.Add(new ResourceTypeProperties
            {
                TenantId = _tenantContext.TenantId,
                ResourceTypeId = resourceTypeId,
                PropertyDefinitionId = propertyDefinitionId
            });
        }
    }

    private async Task EnsureRelationAsync(
        int parentId,
        int childId,
        string relationType,
        CancellationToken ct)
    {
        var exists = await _dbContext.ResourceRelations.AnyAsync(
            relation => relation.ParentResourceId == parentId
                        && relation.ChildResourceId == childId
                        && relation.RelationType == relationType,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.ResourceRelations.Add(new ResourceRelations
            {
                TenantId = _tenantContext.TenantId,
                ParentResourceId = parentId,
                ChildResourceId = childId,
                RelationType = relationType
            });
        }
    }

    private async Task<ResourceProperties> EnsurePropertyAsync(
        string key,
        string label,
        int? parentId,
        int? sortOrder,
        CancellationToken ct)
    {
        var property = await _dbContext.ResourceProperties
            .FirstOrDefaultAsync(item => item.Key == key && item.Label == label && item.ParentId == parentId, ct)
            .ConfigureAwait(false);

        if (property != null)
        {
            property.SortOrder = sortOrder;
            return property;
        }

        property = new ResourceProperties
        {
            TenantId = _tenantContext.TenantId,
            Key = key,
            Label = label,
            ParentId = parentId,
            SortOrder = sortOrder
        };

        _dbContext.ResourceProperties.Add(property);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return property;
    }

    private async Task EnsurePropertyLinkAsync(int resourceId, int propertyId, CancellationToken ct)
    {
        var exists = await _dbContext.ResourcePropertyLinks.AnyAsync(
            link => link.ResourceId == resourceId && link.PropertyId == propertyId,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.ResourcePropertyLinks.Add(new ResourcePropertyLinks
            {
                TenantId = _tenantContext.TenantId,
                ResourceId = resourceId,
                PropertyId = propertyId
            });
        }
    }

    private async Task<Rules> EnsureWeeklyRuleAsync(
        string title,
        TimeOnly startTime,
        TimeOnly endTime,
        int daysOfWeekMask,
        DateOnly? fromDateUtc,
        DateOnly? toDateUtc,
        bool isExclude,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var rule = await _dbContext.Rules.FirstOrDefaultAsync(item => item.Title == title, ct)
            .ConfigureAwait(false);

        if (rule != null)
        {
            rule.Kind = 1;
            rule.IsExclude = isExclude;
            rule.StartTime = startTime;
            rule.EndTime = endTime;
            rule.DaysOfWeekMask = daysOfWeekMask;
            rule.FromDateUtc = fromDateUtc;
            rule.ToDateUtc = toDateUtc;
            rule.SingleDateUtc = null;
            return rule;
        }

        rule = new Rules
        {
            TenantId = _tenantContext.TenantId,
            Kind = 1,
            IsExclude = isExclude,
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            DaysOfWeekMask = daysOfWeekMask,
            FromDateUtc = fromDateUtc,
            ToDateUtc = toDateUtc,
            CreatedAtUtc = nowUtc
        };

        _dbContext.Rules.Add(rule);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return rule;
    }

    private async Task<Rules> EnsureSingleDateRuleAsync(
        string title,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly singleDateUtc,
        bool isExclude,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var rule = await _dbContext.Rules.FirstOrDefaultAsync(item => item.Title == title, ct)
            .ConfigureAwait(false);

        if (rule != null)
        {
            rule.Kind = 2;
            rule.IsExclude = isExclude;
            rule.StartTime = startTime;
            rule.EndTime = endTime;
            rule.DaysOfWeekMask = null;
            rule.FromDateUtc = null;
            rule.ToDateUtc = null;
            rule.SingleDateUtc = singleDateUtc;
            return rule;
        }

        rule = new Rules
        {
            TenantId = _tenantContext.TenantId,
            Kind = 2,
            IsExclude = isExclude,
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            DaysOfWeekMask = null,
            FromDateUtc = null,
            ToDateUtc = null,
            SingleDateUtc = singleDateUtc,
            CreatedAtUtc = nowUtc
        };

        _dbContext.Rules.Add(rule);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return rule;
    }

    private async Task<Rules> EnsureRangeRuleAsync(
        string title,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        bool isExclude,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var rule = await _dbContext.Rules.FirstOrDefaultAsync(item => item.Title == title, ct)
            .ConfigureAwait(false);

        if (rule != null)
        {
            rule.Kind = 3;
            rule.IsExclude = isExclude;
            rule.StartTime = startTime;
            rule.EndTime = endTime;
            rule.DaysOfWeekMask = null;
            rule.FromDateUtc = fromDateUtc;
            rule.ToDateUtc = toDateUtc;
            rule.SingleDateUtc = null;
            return rule;
        }

        rule = new Rules
        {
            TenantId = _tenantContext.TenantId,
            Kind = 3,
            IsExclude = isExclude,
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            DaysOfWeekMask = null,
            FromDateUtc = fromDateUtc,
            ToDateUtc = toDateUtc,
            SingleDateUtc = null,
            CreatedAtUtc = nowUtc
        };

        _dbContext.Rules.Add(rule);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return rule;
    }

    private async Task EnsureRuleResourceAsync(long ruleId, int resourceId, CancellationToken ct)
    {
        var exists = await _dbContext.RuleResources.AnyAsync(
            link => link.RuleId == ruleId && link.ResourceId == resourceId,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.RuleResources.Add(new RuleResources
            {
                TenantId = _tenantContext.TenantId,
                RuleId = ruleId,
                ResourceId = resourceId
            });
        }
    }

    private async Task<BusyEvents> EnsureBusyEventAsync(
        string title,
        DateTime startUtc,
        DateTime endUtc,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var busyEvent = await _dbContext.BusyEvents.FirstOrDefaultAsync(item => item.Title == title, ct)
            .ConfigureAwait(false);

        if (busyEvent != null)
        {
            busyEvent.StartUtc = startUtc;
            busyEvent.EndUtc = endUtc;
            return busyEvent;
        }

        busyEvent = new BusyEvents
        {
            TenantId = _tenantContext.TenantId,
            Title = title,
            StartUtc = startUtc,
            EndUtc = endUtc,
            CreatedAtUtc = nowUtc
        };

        _dbContext.BusyEvents.Add(busyEvent);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return busyEvent;
    }

    private async Task EnsureBusyEventResourceAsync(long busyEventId, int resourceId, CancellationToken ct)
    {
        var exists = await _dbContext.BusyEventResources.AnyAsync(
            link => link.BusyEventId == busyEventId && link.ResourceId == resourceId,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.BusyEventResources.Add(new BusyEventResources
            {
                TenantId = _tenantContext.TenantId,
                BusyEventId = busyEventId,
                ResourceId = resourceId
            });
        }
    }

    private Task CleanupDemoRulesAndBusyAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rr
            FROM RuleResources rr
            INNER JOIN Rules r ON rr.RuleId = r.Id
            WHERE r.Title LIKE 'Demo:%' AND r.TenantId = {0};

            DELETE FROM Rules WHERE Title LIKE 'Demo:%' AND TenantId = {0};

            DELETE ber
            FROM BusyEventResources ber
            INNER JOIN BusyEvents b ON ber.BusyEventId = b.Id
            WHERE b.Title LIKE 'Demo:%' AND b.TenantId = {0};

            DELETE FROM BusyEvents WHERE Title LIKE 'Demo:%' AND TenantId = {0};
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupDemoPropertyTreeAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rpl
            FROM ResourcePropertyLinks rpl
            INNER JOIN ResourceProperties p ON rpl.PropertyId = p.Id
            WHERE p.TenantId = {0}
              AND (p.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               OR EXISTS (
                    SELECT 1
                    FROM ResourceProperties parent
                    WHERE parent.Id = p.ParentId
                      AND parent.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               ));

            DELETE p
            FROM ResourceProperties p
            WHERE p.TenantId = {0}
              AND (p.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               OR EXISTS (
                    SELECT 1
                    FROM ResourceProperties parent
                    WHERE parent.Id = p.ParentId
                      AND parent.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               ));
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupDemoTypeMappingsAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rtp
            FROM ResourceTypeProperties rtp
            INNER JOIN ResourceTypes rt ON rtp.ResourceTypeId = rt.Id
            WHERE rt.TenantId = {0}
              AND rt.[Key] IN ('Doctor', 'Room', 'Site', 'Floor');
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupDemoPropertyLinksAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rpl
            FROM ResourcePropertyLinks rpl
            INNER JOIN Resources r ON rpl.ResourceId = r.Id
            WHERE r.TenantId = {0}
              AND (r.Code IN ('SITE-A', 'SITE-B', 'FLOOR-A1', 'FLOOR-B1', 'ROOM-1', 'ROOM-2', 'ROOM-3', 'ROOM-4', 'DOC-7', 'DOC-8', 'DOC-9')
               OR r.Name IN ('Site A', 'Site B', 'Floor A1', 'Floor B1', 'Room 1', 'Room 2', 'Room 3', 'Room 4', 'Doctor 7', 'Doctor 8', 'Doctor 9'));
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupDemoRelationsAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM ResourceRelations
            WHERE TenantId = {0}
              AND (
                ParentResourceId IN (
                  SELECT Id FROM Resources
                  WHERE TenantId = {0}
                    AND (Code IN ('SITE-A', 'SITE-B', 'FLOOR-A1', 'FLOOR-B1', 'ROOM-1', 'ROOM-2', 'ROOM-3', 'ROOM-4', 'DOC-7', 'DOC-8', 'DOC-9')
                     OR Name IN ('Site A', 'Site B', 'Floor A1', 'Floor B1', 'Room 1', 'Room 2', 'Room 3', 'Room 4', 'Doctor 7', 'Doctor 8', 'Doctor 9')
                    )
                )
                OR ChildResourceId IN (
                  SELECT Id FROM Resources
                  WHERE TenantId = {0}
                    AND (Code IN ('SITE-A', 'SITE-B', 'FLOOR-A1', 'FLOOR-B1', 'ROOM-1', 'ROOM-2', 'ROOM-3', 'ROOM-4', 'DOC-7', 'DOC-8', 'DOC-9')
                     OR Name IN ('Site A', 'Site B', 'Floor A1', 'Floor B1', 'Room 1', 'Room 2', 'Room 3', 'Room 4', 'Doctor 7', 'Doctor 8', 'Doctor 9')
                    )
                )
              );
            """,
            new object[] { tenantId },
            ct);
    }
}
