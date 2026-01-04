using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Demo;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Seed;

public sealed class DemoSeedService : IDemoSeedService
{
    private const int SeedVersion = 2;
    private readonly SchedulerDbContext _dbContext;
    private readonly IDemoScenarioStore _store;
    private readonly IClock _clock;

    public DemoSeedService(
        SchedulerDbContext dbContext,
        IDemoScenarioStore store,
        IClock clock)
    {
        _dbContext = dbContext;
        _store = store;
        _clock = clock;
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
        await NormalizePropertyKeysAsync(ct).ConfigureAwait(false);
        await NormalizePropertyLabelsAsync(ct).ConfigureAwait(false);
        await CleanupPropertyDuplicatesAsync(ct).ConfigureAwait(false);
        await CleanupPropertyLinkDuplicatesAsync(ct).ConfigureAwait(false);

        var siteType = await EnsureResourceTypeAsync("Site", "Site", 1, ct);
        var roomType = await EnsureResourceTypeAsync("Room", "Room", 2, ct);
        var doctorType = await EnsureResourceTypeAsync("Doctor", "Doctor", 3, ct);

        var site = await EnsureResourceAsync("SITE-A", "Site A", capacity: 0, isSchedulable: false, siteType.Id, nowUtc, ct);
        var room1 = await EnsureResourceAsync("ROOM-1", "Room 1", capacity: 1, isSchedulable: true, roomType.Id, nowUtc, ct);
        var room2 = await EnsureResourceAsync("ROOM-2", "Room 2", capacity: 1, isSchedulable: true, roomType.Id, nowUtc, ct);
        var room3 = await EnsureResourceAsync("ROOM-3", "Room 3", capacity: 1, isSchedulable: true, roomType.Id, nowUtc, ct);
        var doctor7 = await EnsureResourceAsync("DOC-7", "Doctor 7", capacity: 1, isSchedulable: true, doctorType.Id, nowUtc, ct);
        var doctor8 = await EnsureResourceAsync("DOC-8", "Doctor 8", capacity: 1, isSchedulable: true, doctorType.Id, nowUtc, ct);

        await EnsureRelationAsync(site.Id, room1.Id, "Contains", ct);
        await EnsureRelationAsync(site.Id, room2.Id, "Contains", ct);
        await EnsureRelationAsync(site.Id, room3.Id, "Contains", ct);
        await EnsureRelationAsync(site.Id, doctor7.Id, "WorksIn", ct);
        await EnsureRelationAsync(site.Id, doctor8.Id, "WorksIn", ct);

        var specializationRoot = await EnsurePropertyAsync("Specialization", "Specialization", null, null, ct);
        var roomFeatureRoot = await EnsurePropertyAsync("RoomFeature", "RoomFeature", null, null, ct);
        var ophthalmology = await EnsurePropertyAsync("Specialization", "Ophthalmology", specializationRoot.Id, 1, ct);
        var cardiology = await EnsurePropertyAsync("Specialization", "Cardiology", specializationRoot.Id, 2, ct);
        var oct = await EnsurePropertyAsync("RoomFeature", "OCT", roomFeatureRoot.Id, 1, ct);

        await EnsureResourceTypePropertyAsync(doctorType.Id, specializationRoot.Id, ct);
        await EnsureResourceTypePropertyAsync(roomType.Id, roomFeatureRoot.Id, ct);

        await EnsurePropertyLinkAsync(doctor7.Id, ophthalmology.Id, ct);
        await EnsurePropertyLinkAsync(doctor8.Id, cardiology.Id, ct);
        await EnsurePropertyLinkAsync(room1.Id, oct.Id, ct);

        var periodEnd = baseDateUtc.AddDays(28);
        var mondayWednesday = (1 << (int)DayOfWeek.Monday) | (1 << (int)DayOfWeek.Wednesday);
        var tuesdayThursday = (1 << (int)DayOfWeek.Tuesday) | (1 << (int)DayOfWeek.Thursday);

        var room1Rule = await EnsureWeeklyRuleAsync(
            "Demo: Room 1 availability",
            new TimeOnly(14, 0),
            new TimeOnly(18, 0),
            mondayWednesday,
            baseDateUtc,
            periodEnd,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room1Rule.Id, room1.Id, ct);

        var doctor7Rule = await EnsureWeeklyRuleAsync(
            "Demo: Doctor 7 availability",
            new TimeOnly(14, 0),
            new TimeOnly(18, 0),
            mondayWednesday,
            baseDateUtc,
            periodEnd,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(doctor7Rule.Id, doctor7.Id, ct);

        var room2Rule = await EnsureWeeklyRuleAsync(
            "Demo: Room 2 availability",
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            tuesdayThursday,
            baseDateUtc,
            periodEnd,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room2Rule.Id, room2.Id, ct);

        var doctor8Rule = await EnsureWeeklyRuleAsync(
            "Demo: Doctor 8 availability",
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            tuesdayThursday,
            baseDateUtc,
            periodEnd,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(doctor8Rule.Id, doctor8.Id, ct);

        var doctor8BackupRule = await EnsureWeeklyRuleAsync(
            "Demo: Doctor 8 backup availability",
            new TimeOnly(14, 0),
            new TimeOnly(18, 0),
            mondayWednesday,
            baseDateUtc,
            periodEnd,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(doctor8BackupRule.Id, doctor8.Id, ct);

        var room3Rule = await EnsureWeeklyRuleAsync(
            "Demo: Room 3 availability",
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            (1 << (int)DayOfWeek.Friday),
            baseDateUtc,
            periodEnd,
            nowUtc,
            ct);
        await EnsureRuleResourceAsync(room3Rule.Id, room3.Id, ct);

        var busyDoctor = await EnsureBusyEventAsync(
            "Demo: Doctor 7 busy",
            ToUtc(baseDateUtc, new TimeOnly(15, 0)),
            ToUtc(baseDateUtc, new TimeOnly(16, 0)),
            nowUtc,
            ct);
        await EnsureBusyEventResourceAsync(busyDoctor.Id, doctor7.Id, ct);

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
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var rule = await _dbContext.Rules.FirstOrDefaultAsync(item => item.Title == title, ct)
            .ConfigureAwait(false);

        if (rule != null)
        {
            rule.Kind = 1;
            rule.IsExclude = false;
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
            Kind = 1,
            IsExclude = false,
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

    private async Task EnsureRuleResourceAsync(long ruleId, int resourceId, CancellationToken ct)
    {
        var exists = await _dbContext.RuleResources.AnyAsync(
            link => link.RuleId == ruleId && link.ResourceId == resourceId,
            ct).ConfigureAwait(false);

        if (!exists)
        {
            _dbContext.RuleResources.Add(new RuleResources
            {
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
                BusyEventId = busyEventId,
                ResourceId = resourceId
            });
        }
    }

    private Task CleanupPropertyLinkDuplicatesAsync(CancellationToken ct)
    {
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            WITH cte AS (
                SELECT ResourceId,
                       PropertyId,
                       ROW_NUMBER() OVER (PARTITION BY ResourceId, PropertyId ORDER BY ResourceId) AS rn
                FROM ResourcePropertyLinks
            )
            DELETE FROM cte WHERE rn > 1;
            """,
            ct);
    }

    private Task CleanupPropertyDuplicatesAsync(CancellationToken ct)
    {
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            WITH props AS (
                SELECT Id,
                       [Key],
                       Label,
                       ParentId,
                       MIN(Id) OVER (PARTITION BY [Key], Label, ParentId) AS CanonicalId,
                       ROW_NUMBER() OVER (PARTITION BY [Key], Label, ParentId ORDER BY Id) AS rn
                FROM ResourceProperties
            )
            DELETE rpl
            FROM ResourcePropertyLinks rpl
            INNER JOIN props p ON rpl.PropertyId = p.Id
            WHERE p.rn > 1
              AND EXISTS (
                  SELECT 1
                  FROM ResourcePropertyLinks rpl2
                  WHERE rpl2.ResourceId = rpl.ResourceId
                    AND rpl2.PropertyId = p.CanonicalId
              );

            WITH props AS (
                SELECT Id,
                       [Key],
                       Label,
                       ParentId,
                       MIN(Id) OVER (PARTITION BY [Key], Label, ParentId) AS CanonicalId,
                       ROW_NUMBER() OVER (PARTITION BY [Key], Label, ParentId ORDER BY Id) AS rn
                FROM ResourceProperties
            )
            UPDATE rpl
            SET PropertyId = p.CanonicalId
            FROM ResourcePropertyLinks rpl
            INNER JOIN props p ON rpl.PropertyId = p.Id
            WHERE p.rn > 1;

            WITH props AS (
                SELECT Id,
                       ROW_NUMBER() OVER (PARTITION BY [Key], Label, ParentId ORDER BY Id) AS rn
                FROM ResourceProperties
            )
            DELETE FROM props WHERE rn > 1;
            """,
            ct);
    }

    private Task NormalizePropertyKeysAsync(CancellationToken ct)
    {
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE child
            SET [Key] = parent.[Key]
            FROM ResourceProperties child
            INNER JOIN ResourceProperties parent ON child.ParentId = parent.Id
            WHERE child.ParentId IS NOT NULL
              AND child.[Key] <> parent.[Key];
            """,
            ct);
    }

    private Task NormalizePropertyLabelsAsync(CancellationToken ct)
    {
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE child
            SET Label = 'Ophthalmology'
            FROM ResourceProperties child
            INNER JOIN ResourceProperties parent ON child.ParentId = parent.Id
            WHERE parent.[Key] = 'Specialization'
              AND child.Label = 'Oculistica';
            """,
            ct);
    }
}
