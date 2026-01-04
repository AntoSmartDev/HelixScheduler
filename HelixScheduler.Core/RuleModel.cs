namespace HelixScheduler.Core;

/// <summary>
/// Scheduler rule input model for v1 (UTC dates/times).
/// </summary>
public sealed class RuleModel
{
    /// <summary>
    /// Rule identifier (opaque to the core).
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Rule kind.
    /// </summary>
    public RuleKind Kind { get; }
    /// <summary>
    /// True for negative rules.
    /// </summary>
    public bool IsExclude { get; }
    /// <summary>
    /// Optional range start (UTC date).
    /// </summary>
    public DateOnly? FromDate { get; }
    /// <summary>
    /// Optional range end (UTC date).
    /// </summary>
    public DateOnly? ToDate { get; }
    /// <summary>
    /// Optional single date (UTC date).
    /// </summary>
    public DateOnly? SingleDate { get; }
    /// <summary>
    /// Start time of the rule (UTC time-of-day).
    /// </summary>
    public TimeSpan StartTime { get; }
    /// <summary>
    /// End time of the rule (UTC time-of-day).
    /// </summary>
    public TimeSpan EndTime { get; }
    /// <summary>
    /// Day-of-week bitmask (Sunday=0).
    /// </summary>
    public int? DaysOfWeekMask { get; }
    /// <summary>
    /// Day of month (1-31) for monthly rules.
    /// </summary>
    public int? DayOfMonth { get; }
    /// <summary>
    /// Interval days for repeating rules.
    /// </summary>
    public int? IntervalDays { get; }
    /// <summary>
    /// Resource id bound to the rule.
    /// </summary>
    public int ResourceId { get; }

    /// <summary>
    /// Builds a new rule model.
    /// </summary>
    public RuleModel(
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
