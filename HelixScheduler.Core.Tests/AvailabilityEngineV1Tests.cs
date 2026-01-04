using HelixScheduler.Core;
using Xunit;

namespace HelixScheduler.Core.Tests;

public sealed class AvailabilityEngineV1Tests
{
    [Fact]
    public void R3_Busy_Doctor_Does_Not_Reduce_Room_And_Intersection_Splits()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var doctorId = 7;
        var roomId = 1;

        var rules = new[]
        {
            WeeklyRule(1, doctorId, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday),
            WeeklyRule(2, roomId, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday)
        };

        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc),
                doctorId)
        };

        var query = new AvailabilityQuery(period, new[] { doctorId, roomId });
        var result = engine.Compute(query, new AvailabilityInputs(rules, busy));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 14, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void R3_Busy_Multi_Resource_Splits_Intersection()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 12), new DateOnly(2025, 3, 12));
        var doctorId = 7;
        var roomId = 1;

        var rules = new[]
        {
            WeeklyRule(1, doctorId, DayOfWeek.Wednesday),
            WeeklyRule(2, roomId, DayOfWeek.Wednesday)
        };

        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc),
                doctorId),
            new BusySlotModel(
                new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc),
                roomId)
        };

        var query = new AvailabilityQuery(period, new[] { doctorId, roomId });
        var result = engine.Compute(query, new AvailabilityInputs(rules, busy));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 12, 14, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 14, 30, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 15, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 18, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Busy_Overlap_Trimming_And_Outside_Window()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = SingleDateRule(1, resourceId, new DateOnly(2025, 3, 10), 14, 18);
        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc),
                resourceId),
            new BusySlotModel(
                new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 19, 0, 0, DateTimeKind.Utc),
                resourceId),
            new BusySlotModel(
                new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                resourceId)
        };

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, busy));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 14, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 15, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Contiguous_Slots_Merge_For_Same_Resources()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rules = new[]
        {
            SingleDateRule(1, resourceId, new DateOnly(2025, 3, 10), 14, 15),
            SingleDateRule(2, resourceId, new DateOnly(2025, 3, 10), 15, 16)
        };

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(rules, Array.Empty<BusySlotModel>()));

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 14, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 16, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Negative_Only_Yields_No_Availability()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var negative = new RuleModel(
            1,
            RuleKind.SingleDate,
            isExclude: true,
            fromDate: null,
            toDate: null,
            singleDate: new DateOnly(2025, 3, 10),
            startTime: TimeSpan.FromHours(9),
            endTime: TimeSpan.FromHours(10),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { negative }, Array.Empty<BusySlotModel>()));

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void Busy_Can_Remove_Entire_Window()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = SingleDateRule(1, resourceId, new DateOnly(2025, 3, 10), 14, 18);
        var busy = new BusySlotModel(
            new DateTime(2025, 3, 10, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 10, 18, 0, 0, DateTimeKind.Utc),
            resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, new[] { busy }));

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void Range_Rule_Is_Clipped_To_Query_Period()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 12));
        var resourceId = 1;

        var rule = new RuleModel(
            1,
            RuleKind.Range,
            isExclude: false,
            fromDate: new DateOnly(2025, 3, 5),
            toDate: new DateOnly(2025, 3, 20),
            singleDate: null,
            startTime: TimeSpan.FromHours(8),
            endTime: TimeSpan.FromHours(9),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, Array.Empty<BusySlotModel>()));

        Assert.Equal(3, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 12, 9, 0, 0, DateTimeKind.Utc), result.Slots[2].EndUtc);
    }

    [Fact]
    public void Rules_Outside_Period_Are_Ignored()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 12));
        var resourceId = 1;

        var rule = new RuleModel(
            1,
            RuleKind.SingleDate,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: new DateOnly(2025, 3, 20),
            startTime: TimeSpan.FromHours(9),
            endTime: TimeSpan.FromHours(10),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, Array.Empty<BusySlotModel>()));

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void Weekly_Rule_With_Null_Mask_Yields_No_Slots()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 16));
        var resourceId = 1;

        var rule = new RuleModel(
            1,
            RuleKind.RecurringWeekly,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: null,
            startTime: TimeSpan.FromHours(9),
            endTime: TimeSpan.FromHours(10),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);

        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, Array.Empty<BusySlotModel>()));

        Assert.Empty(result.Slots);
    }

    [Fact]
    public void OrGroup_Produces_Union_For_Group()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var requiredId = 9;
        var resourceA = 1;
        var resourceB = 2;

        var rules = new[]
        {
            SingleDateRule(1, requiredId, new DateOnly(2025, 3, 10), 8, 18),
            SingleDateRule(2, resourceA, new DateOnly(2025, 3, 10), 9, 10),
            SingleDateRule(3, resourceB, new DateOnly(2025, 3, 10), 11, 12)
        };

        var query = new AvailabilityQuery(
            period,
            new[] { requiredId },
            resourceOrGroups: new[] { new[] { resourceA, resourceB } });

        var result = engine.Compute(query, new AvailabilityInputs(rules, Array.Empty<BusySlotModel>()));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 12, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Required_And_OrGroup_Intersect_Correctly()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var requiredId = 9;
        var resourceA = 1;
        var resourceB = 2;

        var rules = new[]
        {
            SingleDateRule(1, requiredId, new DateOnly(2025, 3, 10), 9, 11),
            SingleDateRule(2, resourceA, new DateOnly(2025, 3, 10), 8, 10),
            SingleDateRule(3, resourceB, new DateOnly(2025, 3, 10), 10, 12)
        };

        var query = new AvailabilityQuery(
            period,
            new[] { requiredId },
            resourceOrGroups: new[] { new[] { resourceA, resourceB } });

        var result = engine.Compute(query, new AvailabilityInputs(rules, Array.Empty<BusySlotModel>()));

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    private static RuleModel WeeklyRule(long id, int resourceId, params DayOfWeek[] days)
    {
        return new RuleModel(
            id,
            RuleKind.RecurringWeekly,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: null,
            startTime: TimeSpan.FromHours(14),
            endTime: TimeSpan.FromHours(18),
            daysOfWeekMask: DaysMask(days),
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);
    }

    private static RuleModel SingleDateRule(long id, int resourceId, DateOnly date, int startHour, int endHour)
    {
        return new RuleModel(
            id,
            RuleKind.SingleDate,
            isExclude: false,
            fromDate: null,
            toDate: null,
            singleDate: date,
            startTime: TimeSpan.FromHours(startHour),
            endTime: TimeSpan.FromHours(endHour),
            daysOfWeekMask: null,
            dayOfMonth: null,
            intervalDays: null,
            resourceId: resourceId);
    }

    private static int DaysMask(params DayOfWeek[] days)
    {
        var mask = 0;
        for (var i = 0; i < days.Length; i++)
        {
            mask |= 1 << (int)days[i];
        }

        return mask;
    }
}
