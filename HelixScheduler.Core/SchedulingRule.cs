namespace HelixScheduler.Core;

/// <summary>
/// Scheduler rule for the core engine (UTC dates/times).
/// </summary>
public sealed class SchedulingRule
{
    /// <summary>
    /// Rule kind.
    /// </summary>
    public SchedulingRuleKind Kind { get; }
    /// <summary>
    /// True for negative rules.
    /// </summary>
    public bool IsExclude { get; }
    /// <summary>
    /// Optional range start (UTC date).
    /// </summary>
    public DateOnly? FromDateUtc { get; }
    /// <summary>
    /// Optional range end (UTC date).
    /// </summary>
    public DateOnly? ToDateUtc { get; }
    /// <summary>
    /// Optional single date (UTC date).
    /// </summary>
    public DateOnly? SingleDateUtc { get; }
    /// <summary>
    /// Time range in UTC.
    /// </summary>
    public TimeRange TimeRange { get; }
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
    /// Resource ids bound to the rule.
    /// </summary>
    public IReadOnlyList<int> ResourceIds { get; }

    /// <summary>
    /// Builds a new scheduling rule.
    /// </summary>
    public SchedulingRule(
        SchedulingRuleKind kind,
        bool isExclude,
        DateOnly? fromDateUtc,
        DateOnly? toDateUtc,
        DateOnly? singleDateUtc,
        TimeRange timeRange,
        int? daysOfWeekMask,
        int? dayOfMonth,
        int? intervalDays,
        IReadOnlyList<int> resourceIds)
    {
        ResourceIds = resourceIds ?? throw new ArgumentNullException(nameof(resourceIds));
        Kind = kind;
        IsExclude = isExclude;
        FromDateUtc = fromDateUtc;
        ToDateUtc = toDateUtc;
        SingleDateUtc = singleDateUtc;
        TimeRange = timeRange;
        DaysOfWeekMask = daysOfWeekMask;
        DayOfMonth = dayOfMonth;
        IntervalDays = intervalDays;
    }
}
