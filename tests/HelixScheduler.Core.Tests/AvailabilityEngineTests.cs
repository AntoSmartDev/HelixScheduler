using HelixScheduler.Core;
using Xunit;

namespace HelixScheduler.Core.Tests;

public sealed class AvailabilityEngineTests
{
    [Fact]
    public void Busy_On_Doctor_Does_Not_Reduce_Room_Availability()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 12));
        var roomId = 1;
        var doctorId = 7;

        var rules = new[]
        {
            new RuleModel(
                1,
                RuleKind.RecurringWeekly,
                isExclude: false,
                fromDate: null,
                toDate: null,
                singleDate: null,
                startTime: TimeSpan.FromHours(14),
                endTime: TimeSpan.FromHours(18),
                daysOfWeekMask: DaysMask(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday),
                dayOfMonth: null,
                intervalDays: null,
                resourceId: doctorId),
            new RuleModel(
                2,
                RuleKind.RecurringWeekly,
                isExclude: false,
                fromDate: null,
                toDate: null,
                singleDate: null,
                startTime: TimeSpan.FromHours(14),
                endTime: TimeSpan.FromHours(18),
                daysOfWeekMask: DaysMask(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday),
                dayOfMonth: null,
                intervalDays: null,
                resourceId: roomId)
        };

        var busyDoctor = new BusySlotModel(
            new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc),
            doctorId);

        var query = new AvailabilityQuery(period, new[] { doctorId, roomId });
        var inputs = new AvailabilityInputs(rules, new[] { busyDoctor });
        var result = engine.Compute(query, inputs);

        Assert.Equal(4, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 14, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 14, 0, 0, DateTimeKind.Utc), result.Slots[3].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 18, 0, 0, DateTimeKind.Utc), result.Slots[3].EndUtc);
    }

    [Fact]
    public void Busy_On_Doctor_And_Room_Blocks_Both()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 12), new DateOnly(2025, 3, 12));
        var roomId = 1;
        var doctorId = 7;

        var rules = new[]
        {
            new RuleModel(
                1,
                RuleKind.RecurringWeekly,
                isExclude: false,
                fromDate: null,
                toDate: null,
                singleDate: null,
                startTime: TimeSpan.FromHours(14),
                endTime: TimeSpan.FromHours(18),
                daysOfWeekMask: DaysMask(DayOfWeek.Wednesday),
                dayOfMonth: null,
                intervalDays: null,
                resourceId: doctorId),
            new RuleModel(
                2,
                RuleKind.RecurringWeekly,
                isExclude: false,
                fromDate: null,
                toDate: null,
                singleDate: null,
                startTime: TimeSpan.FromHours(14),
                endTime: TimeSpan.FromHours(18),
                daysOfWeekMask: DaysMask(DayOfWeek.Wednesday),
                dayOfMonth: null,
                intervalDays: null,
                resourceId: roomId)
        };

        var busyDoctor = new BusySlotModel(
            new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc),
            doctorId);

        var busyRoom = new BusySlotModel(
            new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc),
            roomId);

        var query = new AvailabilityQuery(period, new[] { doctorId, roomId });
        var inputs = new AvailabilityInputs(rules, new[] { busyDoctor, busyRoom });
        var result = engine.Compute(query, inputs);

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 12, 14, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 18, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Busy_Trim_Splits_Slot_And_Ignores_Outside_Window()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = new RuleModel(
            1,
            RuleKind.SingleDate,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: new DateOnly(2025, 3, 10),
            startTime: TimeSpan.FromHours(9),
            endTime: TimeSpan.FromHours(12),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var busyInside = new BusySlotModel(
            new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc),
            resourceId);

        var busyOutside = new BusySlotModel(
            new DateTime(2025, 3, 10, 6, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 7, 0, 0, DateTimeKind.Utc),
            resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var inputs = new AvailabilityInputs(new[] { rule }, new[] { busyInside, busyOutside });
        var result = engine.Compute(query, inputs);

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Busy_Touching_Boundary_Does_Not_Reduce_Availability()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = new RuleModel(
            1,
            RuleKind.SingleDate,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: new DateOnly(2025, 3, 10),
            startTime: TimeSpan.FromHours(9),
            endTime: TimeSpan.FromHours(12),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var busy = new BusySlotModel(
            new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 13, 0, 0, DateTimeKind.Utc),
            resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var inputs = new AvailabilityInputs(new[] { rule }, new[] { busy });
        var result = engine.Compute(query, inputs);

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Contiguous_Slots_Merge_When_Same_Resources()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var ruleA = new RuleModel(
            1,
            RuleKind.SingleDate,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: new DateOnly(2025, 3, 10),
            startTime: TimeSpan.FromHours(14),
            endTime: TimeSpan.FromHours(15),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var ruleB = new RuleModel(
            2,
            RuleKind.SingleDate,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: new DateOnly(2025, 3, 10),
            startTime: TimeSpan.FromHours(15),
            endTime: TimeSpan.FromHours(16),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var inputs = new AvailabilityInputs(new[] { ruleA, ruleB }, Array.Empty<BusySlotModel>());
        var result = engine.Compute(query, inputs);

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 14, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Unsupported_RuleKind_Throws_NotSupported()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = new RuleModel(
            99,
            RuleKind.Monthly,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: null,
            startTime: TimeSpan.FromHours(9),
            endTime: TimeSpan.FromHours(10),
            daysOfWeekMask: null,
            dayOfMonth: 10,
            intervalDays: null,
            resourceId: resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var inputs = new AvailabilityInputs(new[] { rule }, Array.Empty<BusySlotModel>());

        Assert.Throws<NotSupportedException>(() => engine.Compute(query, inputs));
    }

    [Fact]
    public void Negative_Rule_Subtracts_From_Positive_Range()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var positive = new SchedulingRule(
            SchedulingRuleKind.Range,
            isExclude: false,
            fromDateUtc: new DateOnly(2025, 3, 10),
            toDateUtc: new DateOnly(2025, 3, 10),
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var negative = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: true,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(12), TimeSpan.FromHours(13)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { positive, negative }, Array.Empty<BusySlot>());

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 13, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Intersection_Produces_Only_Overlapping_Window()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var roomId = 1;
        var doctorId = 7;

        var roomRule = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(17)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { roomId });

        var doctorRule = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(13), TimeSpan.FromHours(18)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { doctorId });

        var query = new AvailabilityQuery(period, new[] { roomId, doctorId });
        var result = engine.Compute(query, new[] { roomRule, doctorRule }, Array.Empty<BusySlot>());

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 13, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 17, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void BusySlot_Splits_Availability_Within_Day()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var busy = new BusySlot(
            resourceId,
            new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc));

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, new[] { busy });

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Repeating_Rule_Respects_Interval_And_Period()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 16));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.Repeating,
            isExclude: false,
            fromDateUtc: new DateOnly(2025, 3, 9),
            toDateUtc: new DateOnly(2025, 3, 16),
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(8), TimeSpan.FromHours(9)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: 2,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 11, 8, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 15, 9, 0, 0, DateTimeKind.Utc), result.Slots[2].EndUtc);
    }

    [Fact]
    public void Repeating_Rule_Includes_Start_When_Aligned()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 16));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.Repeating,
            isExclude: false,
            fromDateUtc: new DateOnly(2025, 3, 10),
            toDateUtc: new DateOnly(2025, 3, 16),
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(8), TimeSpan.FromHours(9)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: 2,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.Equal(4, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 16, 9, 0, 0, DateTimeKind.Utc), result.Slots[3].EndUtc);
    }

    [Fact]
    public void Monthly_Rule_Selects_Day_Of_Month()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 1, 1), new DateOnly(2025, 3, 31));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.Monthly,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(10), TimeSpan.FromHours(12)),
            daysOfWeekMask: null,
            dayOfMonth: 15,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 15, 12, 0, 0, DateTimeKind.Utc), result.Slots[2].EndUtc);
    }

    [Fact]
    public void Weekly_Rule_Respects_DaysOfWeekMask()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 16));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.Weekly,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
            daysOfWeekMask: DaysMask(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday),
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 14, 10, 0, 0, DateTimeKind.Utc), result.Slots[2].EndUtc);
    }

    [Fact]
    public void Intersection_Empty_When_No_Overlap()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceA = 1;
        var resourceB = 2;

        var ruleA = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceA });

        var ruleB = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(11), TimeSpan.FromHours(12)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceB });

        var query = new AvailabilityQuery(period, new[] { resourceA, resourceB });
        var result = engine.Compute(query, new[] { ruleA, ruleB }, Array.Empty<BusySlot>());

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void Intersection_Full_Overlap_When_Identical()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceA = 1;
        var resourceB = 2;

        var ruleA = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceA });

        var ruleB = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceB });

        var query = new AvailabilityQuery(period, new[] { resourceA, resourceB });
        var result = engine.Compute(query, new[] { ruleA, ruleB }, Array.Empty<BusySlot>());

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Negative_And_Busy_Combine_To_Exclude_Multiple_Segments()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var positive = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var negative = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: true,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(12), TimeSpan.FromHours(13)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var busy = new BusySlot(
            resourceId,
            new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc));

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { positive, negative }, new[] { busy });

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 13, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc), result.Slots[2].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc), result.Slots[2].EndUtc);
    }

    [Fact]
    public void Range_Rule_Handles_Open_Ended_Periods()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 12));
        var resourceId = 1;

        var openStart = new SchedulingRule(
            SchedulingRuleKind.Range,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: new DateOnly(2025, 3, 11),
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(8), TimeSpan.FromHours(9)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var openEnd = new SchedulingRule(
            SchedulingRuleKind.Range,
            isExclude: false,
            fromDateUtc: new DateOnly(2025, 3, 11),
            toDateUtc: null,
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(15), TimeSpan.FromHours(16)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { openStart, openEnd }, Array.Empty<BusySlot>());

        Assert.Equal(4, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 16, 0, 0, DateTimeKind.Utc), result.Slots[3].EndUtc);
    }

    [Fact]
    public void Negative_Rule_Can_Remove_All_Availability()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var positive = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var negative = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: true,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { positive, negative }, Array.Empty<BusySlot>());

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void BusySlot_Touching_Boundary_Does_Not_Reduce()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var busy = new BusySlot(
            resourceId,
            new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 13, 0, 0, DateTimeKind.Utc));

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, new[] { busy });

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Monthly_Rule_Ignores_Invalid_Day_Of_Month()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 2, 1), new DateOnly(2025, 2, 28));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.Monthly,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(10), TimeSpan.FromHours(11)),
            daysOfWeekMask: null,
            dayOfMonth: 31,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void Weekly_Rule_With_Null_Mask_Produces_No_Slots()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 16));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.Weekly,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void Repeating_Rule_With_Invalid_Interval_Produces_No_Slots()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 16));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.Repeating,
            isExclude: false,
            fromDateUtc: new DateOnly(2025, 3, 10),
            toDateUtc: new DateOnly(2025, 3, 16),
            singleDateUtc: null,
            timeRange: new TimeRange(TimeSpan.FromHours(8), TimeSpan.FromHours(9)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: 0,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void Engine_Emits_Utc_Slots()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new[] { rule }, Array.Empty<BusySlot>());

        Assert.All(result.Slots, slot =>
        {
            Assert.Equal(DateTimeKind.Utc, slot.StartUtc.Kind);
            Assert.Equal(DateTimeKind.Utc, slot.EndUtc.Kind);
        });
    }

    [Fact]
    public void OrGroup_Produces_Union_For_Group()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var requiredId = 9;
        var resourceA = 1;
        var resourceB = 2;

        var rules = new[]
        {
            CreateSingleDateRule(requiredId, 8, 18),
            CreateSingleDateRule(resourceA, 9, 10),
            CreateSingleDateRule(resourceB, 11, 12)
        };

        var query = new AvailabilityQuery(
            period,
            new[] { requiredId },
            resourceOrGroups: new[] { new[] { resourceA, resourceB } });

        var result = engine.Compute(query, rules, Array.Empty<BusySlot>());

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Required_And_OrGroup_Intersect_Correctly()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var requiredId = 9;
        var resourceA = 1;
        var resourceB = 2;

        var rules = new[]
        {
            CreateSingleDateRule(requiredId, 9, 11),
            CreateSingleDateRule(resourceA, 8, 10),
            CreateSingleDateRule(resourceB, 10, 12)
        };

        var query = new AvailabilityQuery(
            period,
            new[] { requiredId },
            resourceOrGroups: new[] { new[] { resourceA, resourceB } });

        var result = engine.Compute(query, rules, Array.Empty<BusySlot>());

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Multiple_OrGroups_Are_Intersected()
    {
        var engine = new AvailabilityEngine();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var requiredId = 9;
        var resourceA = 1;
        var resourceB = 2;
        var resourceC = 3;

        var rules = new[]
        {
            CreateSingleDateRule(requiredId, 8, 18),
            CreateSingleDateRule(resourceA, 9, 12),
            CreateSingleDateRule(resourceB, 9, 12),
            CreateSingleDateRule(resourceC, 10, 11)
        };

        var query = new AvailabilityQuery(
            period,
            new[] { requiredId },
            resourceOrGroups: new[]
            {
                new[] { resourceA, resourceB },
                new[] { resourceC }
            });

        var result = engine.Compute(query, rules, Array.Empty<BusySlot>());

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    private static int DaysMask(params DayOfWeek[] days)
    {
        var mask = 0;
        foreach (var day in days)
        {
            mask |= 1 << (int)day;
        }

        return mask;
    }

    private static SchedulingRule CreateSingleDateRule(int resourceId, int startHour, int endHour)
    {
        return new SchedulingRule(
            SchedulingRuleKind.SingleDate,
            isExclude: false,
            fromDateUtc: null,
            toDateUtc: null,
            singleDateUtc: new DateOnly(2025, 3, 10),
            timeRange: new TimeRange(TimeSpan.FromHours(startHour), TimeSpan.FromHours(endHour)),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceIds: new[] { resourceId });
    }
}
