using HelixScheduler.Core;
using Xunit;

namespace HelixScheduler.Core.Tests;

public sealed class ValueObjectsTests
{
    [Fact]
    public void TimeRange_Throws_When_End_Is_Not_Greater_Than_Start()
    {
        var start = TimeSpan.FromHours(9);
        var end = TimeSpan.FromHours(9);

        Assert.Throws<ArgumentException>(() => new TimeRange(start, end));
    }

    [Fact]
    public void TimeRange_Allows_End_Greater_Than_Start()
    {
        var start = TimeSpan.FromHours(9);
        var end = TimeSpan.FromHours(10);

        var range = new TimeRange(start, end);

        Assert.Equal(start, range.Start);
        Assert.Equal(end, range.End);
    }

    [Fact]
    public void TimeRange_Overlap_True_When_Intervals_Intersect()
    {
        var first = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12));
        var second = new TimeRange(TimeSpan.FromHours(11), TimeSpan.FromHours(13));

        Assert.True(first.Overlaps(second));
    }

    [Fact]
    public void TimeRange_Overlap_False_When_Touching_Boundary()
    {
        var first = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12));
        var second = new TimeRange(TimeSpan.FromHours(12), TimeSpan.FromHours(13));

        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void TimeRange_Intersect_Returns_Null_When_No_Overlap()
    {
        var first = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var second = new TimeRange(TimeSpan.FromHours(11), TimeSpan.FromHours(12));

        Assert.Null(first.Intersect(second));
    }

    [Fact]
    public void TimeRange_Intersect_Returns_Overlap_When_Partial()
    {
        var first = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12));
        var second = new TimeRange(TimeSpan.FromHours(11), TimeSpan.FromHours(13));

        var intersect = first.Intersect(second);

        Assert.NotNull(intersect);
        Assert.Equal(new TimeRange(TimeSpan.FromHours(11), TimeSpan.FromHours(12)), intersect.Value);
    }

    [Fact]
    public void TimeRange_Subtract_Returns_Original_When_No_Overlap()
    {
        var original = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var other = new TimeRange(TimeSpan.FromHours(11), TimeSpan.FromHours(12));

        var result = original.Subtract(other).ToList();

        Assert.Single(result);
        Assert.Equal(original, result[0]);
    }

    [Fact]
    public void TimeRange_Subtract_Removes_Contained_Interval()
    {
        var original = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12));
        var other = new TimeRange(TimeSpan.FromHours(10), TimeSpan.FromHours(11));

        var result = original.Subtract(other).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(10)), result[0]);
        Assert.Equal(new TimeRange(TimeSpan.FromHours(11), TimeSpan.FromHours(12)), result[1]);
    }

    [Fact]
    public void TimeRange_Subtract_Removes_Full_Overlap()
    {
        var original = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12));
        var other = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(12));

        var result = original.Subtract(other).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void DatePeriod_Throws_When_From_Is_After_To()
    {
        var from = new DateOnly(2025, 1, 2);
        var to = new DateOnly(2025, 1, 1);

        Assert.Throws<ArgumentException>(() => new DatePeriod(from, to));
    }

    [Fact]
    public void DatePeriod_Allows_Equal_From_And_To()
    {
        var day = new DateOnly(2025, 1, 1);

        var period = new DatePeriod(day, day);

        Assert.Equal(day, period.From);
        Assert.Equal(day, period.To);
    }

    [Fact]
    public void DatePeriod_EnumerateDays_Includes_Bounds()
    {
        var period = new DatePeriod(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 3));

        var days = period.EnumerateDays().ToList();

        Assert.Equal(3, days.Count);
        Assert.Equal(new DateOnly(2025, 1, 1), days[0]);
        Assert.Equal(new DateOnly(2025, 1, 3), days[2]);
    }

    [Fact]
    public void UtcSlot_Throws_When_End_Is_Not_Greater_Than_Start()
    {
        var start = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => new UtcSlot(start, end, new[] { 1 }));
    }

    [Fact]
    public void UtcSlot_Throws_When_DateTime_Is_Not_Utc()
    {
        var start = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Local);
        var end = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => new UtcSlot(start, end, new[] { 1 }));
    }

    [Fact]
    public void BusySlot_Throws_When_DateTime_Is_Not_Utc()
    {
        var start = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Local);
        var end = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => new BusySlot(1, start, end));
    }

    [Fact]
    public void UtcSlot_Throws_When_DateTime_Is_Unspecified()
    {
        var start = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => new UtcSlot(start, end, new[] { 1 }));
    }

    [Fact]
    public void BusySlot_Throws_When_DateTime_Is_Unspecified()
    {
        var start = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => new BusySlot(1, start, end));
    }

    [Fact]
    public void AvailabilityQuery_Throws_When_RequiredResourceIds_Is_Null()
    {
        var period = new DatePeriod(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2));

        Assert.Throws<ArgumentNullException>(() =>
            new AvailabilityQuery(period, null!, propertyFilters: null));
    }

    [Fact]
    public void AvailabilityQuery_Allows_Null_PropertyFilters()
    {
        var period = new DatePeriod(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 2));

        var query = new AvailabilityQuery(period, new[] { 1 }, propertyFilters: null);

        Assert.NotNull(query.PropertyFilters);
        Assert.Empty(query.PropertyFilters);
    }
}
