using HelixScheduler.Application.Abstractions;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Seed;

internal sealed class DemoSeedScheduleBuilder
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public DemoSeedScheduleBuilder(SchedulerDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task EnsureScheduleAsync(
        DemoSeedCatalog catalog,
        DateOnly baseDateUtc,
        DateTime nowUtc,
        CancellationToken ct)
    {
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

        var siteAOpen = await EnsureWeeklyRuleAsync("Demo: Site A open hours", new TimeOnly(8, 0), new TimeOnly(20, 0), allWeek, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(siteAOpen.Id, catalog.Resources.SiteA.Id, ct).ConfigureAwait(false);

        var floorAOpen = await EnsureWeeklyRuleAsync("Demo: Floor A1 open hours", new TimeOnly(8, 0), new TimeOnly(20, 0), allWeek, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(floorAOpen.Id, catalog.Resources.FloorA1.Id, ct).ConfigureAwait(false);

        var siteBOpen = await EnsureWeeklyRuleAsync("Demo: Site B open hours", new TimeOnly(9, 0), new TimeOnly(17, 0), allWeek, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(siteBOpen.Id, catalog.Resources.SiteB.Id, ct).ConfigureAwait(false);

        var floorBOpen = await EnsureWeeklyRuleAsync("Demo: Floor B1 open hours", new TimeOnly(9, 0), new TimeOnly(17, 0), allWeek, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(floorBOpen.Id, catalog.Resources.FloorB1.Id, ct).ConfigureAwait(false);

        var siteAExclude = await EnsureWeeklyRuleAsync("Demo: Site A maintenance", new TimeOnly(15, 0), new TimeOnly(16, 0), BuildDaysMask(DayOfWeek.Wednesday), null, null, true, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(siteAExclude.Id, catalog.Resources.SiteA.Id, ct).ConfigureAwait(false);

        var room1Rule = await EnsureWeeklyRuleAsync("Demo: Room 1 availability", new TimeOnly(14, 0), new TimeOnly(18, 0), mondayWednesday, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(room1Rule.Id, catalog.Resources.Room1.Id, ct).ConfigureAwait(false);

        var doctor7Rule = await EnsureWeeklyRuleAsync("Demo: Doctor 7 availability", new TimeOnly(14, 0), new TimeOnly(18, 0), mondayWednesday, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(doctor7Rule.Id, catalog.Resources.Doctor7.Id, ct).ConfigureAwait(false);

        var room2Rule = await EnsureWeeklyRuleAsync("Demo: Room 2 availability", new TimeOnly(9, 0), new TimeOnly(13, 0), tuesdayThursday, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(room2Rule.Id, catalog.Resources.Room2.Id, ct).ConfigureAwait(false);

        var doctor8Rule = await EnsureWeeklyRuleAsync("Demo: Doctor 8 availability", new TimeOnly(9, 0), new TimeOnly(13, 0), tuesdayThursday, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(doctor8Rule.Id, catalog.Resources.Doctor8.Id, ct).ConfigureAwait(false);

        var doctor9Rule = await EnsureWeeklyRuleAsync("Demo: Doctor 9 availability", new TimeOnly(9, 0), new TimeOnly(13, 0), tuesdayThursday, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(doctor9Rule.Id, catalog.Resources.Doctor9.Id, ct).ConfigureAwait(false);

        var room3Rule = await EnsureWeeklyRuleAsync("Demo: Room 3 short session", new TimeOnly(9, 0), new TimeOnly(10, 20), fridayOnly, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(room3Rule.Id, catalog.Resources.Room3.Id, ct).ConfigureAwait(false);

        var room4Rule = await EnsureWeeklyRuleAsync("Demo: Room 4 availability", new TimeOnly(10, 0), new TimeOnly(15, 0), mondayWednesday, null, null, false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(room4Rule.Id, catalog.Resources.Room4.Id, ct).ConfigureAwait(false);

        var room2Single = await EnsureSingleDateRuleAsync("Demo: Room 2 single date", new TimeOnly(12, 0), new TimeOnly(14, 0), baseDateUtc.AddDays(1), false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(room2Single.Id, catalog.Resources.Room2.Id, ct).ConfigureAwait(false);

        var doctor7Range = await EnsureRangeRuleAsync("Demo: Doctor 7 range availability", new TimeOnly(12, 0), new TimeOnly(14, 0), baseDateUtc.AddDays(7), baseDateUtc.AddDays(10), false, nowUtc, ct).ConfigureAwait(false);
        await EnsureRuleResourceAsync(doctor7Range.Id, catalog.Resources.Doctor7.Id, ct).ConfigureAwait(false);

        var busyDoctor7 = await EnsureBusyEventAsync("Demo: Doctor 7 busy", ToUtc(baseDateUtc, new TimeOnly(15, 0)), ToUtc(baseDateUtc, new TimeOnly(16, 0)), nowUtc, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busyDoctor7.Id, catalog.Resources.Doctor7.Id, ct).ConfigureAwait(false);

        var busyDoctorRoom = await EnsureBusyEventAsync("Demo: Doctor 7 + Room 1 busy", ToUtc(baseDateUtc.AddDays(2), new TimeOnly(16, 30)), ToUtc(baseDateUtc.AddDays(2), new TimeOnly(17, 0)), nowUtc, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busyDoctorRoom.Id, catalog.Resources.Doctor7.Id, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busyDoctorRoom.Id, catalog.Resources.Room1.Id, ct).ConfigureAwait(false);

        var busyRoom2 = await EnsureBusyEventAsync("Demo: Room 2 busy", ToUtc(baseDateUtc.AddDays(1), new TimeOnly(10, 0)), ToUtc(baseDateUtc.AddDays(1), new TimeOnly(11, 0)), nowUtc, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busyRoom2.Id, catalog.Resources.Room2.Id, ct).ConfigureAwait(false);

        var busyDoctor8 = await EnsureBusyEventAsync("Demo: Doctor 8 busy", ToUtc(baseDateUtc.AddDays(3), new TimeOnly(10, 30)), ToUtc(baseDateUtc.AddDays(3), new TimeOnly(11, 0)), nowUtc, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busyDoctor8.Id, catalog.Resources.Doctor8.Id, ct).ConfigureAwait(false);

        var busySiteA = await EnsureBusyEventAsync("Demo: Site A busy", ToUtc(baseDateUtc.AddDays(1), new TimeOnly(10, 0)), ToUtc(baseDateUtc.AddDays(1), new TimeOnly(11, 0)), nowUtc, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busySiteA.Id, catalog.Resources.SiteA.Id, ct).ConfigureAwait(false);

        var busyFloorA = await EnsureBusyEventAsync("Demo: Floor A1 busy", ToUtc(baseDateUtc.AddDays(1), new TimeOnly(11, 0)), ToUtc(baseDateUtc.AddDays(1), new TimeOnly(12, 0)), nowUtc, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busyFloorA.Id, catalog.Resources.FloorA1.Id, ct).ConfigureAwait(false);

        var busyRoom3 = await EnsureBusyEventAsync("Demo: Room 3 busy", ToUtc(baseDateUtc.AddDays(4), new TimeOnly(9, 40)), ToUtc(baseDateUtc.AddDays(4), new TimeOnly(10, 0)), nowUtc, ct).ConfigureAwait(false);
        await EnsureBusyEventResourceAsync(busyRoom3.Id, catalog.Resources.Room3.Id, ct).ConfigureAwait(false);
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

    private async Task<Rules> EnsureWeeklyRuleAsync(string title, TimeOnly startTime, TimeOnly endTime, int daysOfWeekMask, DateOnly? fromDateUtc, DateOnly? toDateUtc, bool isExclude, DateTime nowUtc, CancellationToken ct)
    {
        var rule = await _dbContext.Rules.FirstOrDefaultAsync(item => item.Title == title, ct).ConfigureAwait(false);

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

    private async Task<Rules> EnsureSingleDateRuleAsync(string title, TimeOnly startTime, TimeOnly endTime, DateOnly singleDateUtc, bool isExclude, DateTime nowUtc, CancellationToken ct)
    {
        var rule = await _dbContext.Rules.FirstOrDefaultAsync(item => item.Title == title, ct).ConfigureAwait(false);

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

    private async Task<Rules> EnsureRangeRuleAsync(string title, TimeOnly startTime, TimeOnly endTime, DateOnly fromDateUtc, DateOnly toDateUtc, bool isExclude, DateTime nowUtc, CancellationToken ct)
    {
        var rule = await _dbContext.Rules.FirstOrDefaultAsync(item => item.Title == title, ct).ConfigureAwait(false);

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
        var exists = await _dbContext.RuleResources.AnyAsync(link => link.RuleId == ruleId && link.ResourceId == resourceId, ct).ConfigureAwait(false);

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

    private async Task<BusyEvents> EnsureBusyEventAsync(string title, DateTime startUtc, DateTime endUtc, DateTime nowUtc, CancellationToken ct)
    {
        var busyEvent = await _dbContext.BusyEvents.FirstOrDefaultAsync(item => item.Title == title, ct).ConfigureAwait(false);

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
        var exists = await _dbContext.BusyEventResources.AnyAsync(link => link.BusyEventId == busyEventId && link.ResourceId == resourceId, ct).ConfigureAwait(false);

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
}
