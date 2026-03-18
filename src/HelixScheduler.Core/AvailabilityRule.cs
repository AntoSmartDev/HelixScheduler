namespace HelixScheduler.Core;

/// <summary>
/// Canonical scheduler rule input for the availability engine (UTC dates/times).
/// </summary>
public sealed class AvailabilityRule
{
    public long Id { get; }
    public RuleKind Kind { get; }
    public bool IsExclude { get; }
    public DateOnly? FromDate { get; }
    public DateOnly? ToDate { get; }
    public DateOnly? SingleDate { get; }
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public int? DaysOfWeekMask { get; }
    public int? DayOfMonth { get; }
    public int? IntervalDays { get; }
    public int ResourceId { get; }

    public AvailabilityRule(
        long id,
        RuleKind kind,
        bool isExclude,
        DateOnly? fromDate,
        DateOnly? toDate,
        DateOnly? singleDate,
        TimeSpan startTime,
        TimeSpan endTime,
        int? daysOfWeekMask,
        int? dayOfMonth,
        int? intervalDays,
        int resourceId)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("EndTime must be greater than StartTime.", nameof(endTime));
        }

        Id = id;
        Kind = kind;
        IsExclude = isExclude;
        FromDate = fromDate;
        ToDate = toDate;
        SingleDate = singleDate;
        StartTime = startTime;
        EndTime = endTime;
        DaysOfWeekMask = daysOfWeekMask;
        DayOfMonth = dayOfMonth;
        IntervalDays = intervalDays;
        ResourceId = resourceId;
    }
}
