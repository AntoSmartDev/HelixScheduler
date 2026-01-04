using HelixScheduler.Core;
using Xunit;

namespace HelixScheduler.Core.Tests;

public sealed class AvailabilityEngineV1CapacityTests
{
    [Fact]
    public void Capacity1_Busy_Blocks_As_Before()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 1;

        var rule = SingleDateRule(1, resourceId, new DateOnly(2025, 3, 10), 9, 11);
        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                resourceId)
        };

        var capacities = new Dictionary<int, int> { [resourceId] = 1 };
        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, busy, capacities));

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Capacity2_SingleBusy_Does_Not_Block()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 2;

        var rule = SingleDateRule(1, resourceId, new DateOnly(2025, 3, 10), 9, 11);
        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                resourceId)
        };

        var capacities = new Dictionary<int, int> { [resourceId] = 2 };
        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, busy, capacities));

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
    }

    [Fact]
    public void Capacity2_DoubleBusy_Overlap_Blocks_Overlap()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 3;

        var rule = SingleDateRule(1, resourceId, new DateOnly(2025, 3, 10), 9, 11);
        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                resourceId),
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 30, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                resourceId)
        };

        var capacities = new Dictionary<int, int> { [resourceId] = 2 };
        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, busy, capacities));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 30, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void Capacity3_DenseBusy_Blocks_Only_Max_Overlap()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var resourceId = 4;

        var rule = SingleDateRule(1, resourceId, new DateOnly(2025, 3, 10), 9, 11);
        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                resourceId),
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 30, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                resourceId),
            new BusySlotModel(
                new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                resourceId)
        };

        var capacities = new Dictionary<int, int> { [resourceId] = 3 };
        var query = new AvailabilityQuery(period, new[] { resourceId });
        var result = engine.Compute(query, new AvailabilityInputs(new[] { rule }, busy, capacities));

        Assert.Equal(2, result.Slots.Count);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 10, 30, 0, DateTimeKind.Utc), result.Slots[1].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[1].EndUtc);
    }

    [Fact]
    public void OrGroup_Union_Uses_Capacity_Per_Resource()
    {
        var engine = new AvailabilityEngineV1();
        var period = new DatePeriod(new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 10));
        var requiredId = 10;
        var resourceA = 11;
        var resourceB = 12;

        var rules = new[]
        {
            SingleDateRule(1, requiredId, new DateOnly(2025, 3, 10), 9, 11),
            SingleDateRule(2, resourceA, new DateOnly(2025, 3, 10), 9, 11),
            SingleDateRule(3, resourceB, new DateOnly(2025, 3, 10), 9, 11)
        };

        var busy = new[]
        {
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                resourceA),
            new BusySlotModel(
                new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                resourceB)
        };

        var capacities = new Dictionary<int, int> { [resourceB] = 2 };
        var query = new AvailabilityQuery(
            period,
            new[] { requiredId },
            resourceOrGroups: new[] { new[] { resourceA, resourceB } });
        var result = engine.Compute(query, new AvailabilityInputs(rules, busy, capacities));

        Assert.Single(result.Slots);
        Assert.Equal(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc), result.Slots[0].StartUtc);
        Assert.Equal(new DateTime(2025, 3, 10, 11, 0, 0, DateTimeKind.Utc), result.Slots[0].EndUtc);
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
}
