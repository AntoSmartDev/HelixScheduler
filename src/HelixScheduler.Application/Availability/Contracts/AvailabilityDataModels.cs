namespace HelixScheduler.Application.Availability;

public sealed record RuleData(
    long Id,
    byte Kind,
    bool IsExclude,
    DateOnly? FromDateUtc,
    DateOnly? ToDateUtc,
    DateOnly? SingleDateUtc,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? DaysOfWeekMask,
    int? DayOfMonth,
    int? IntervalDays,
    IReadOnlyList<int> ResourceIds);

public sealed record BusyEventData(
    long Id,
    DateTime StartUtc,
    DateTime EndUtc,
    IReadOnlyList<int> ResourceIds);

public sealed record ResourceSummary(
    int Id,
    string? Code,
    string Name,
    bool IsSchedulable);

public sealed record RuleSummary(
    long Id,
    string? Title,
    byte Kind,
    bool IsExclude,
    DateOnly? FromDateUtc,
    DateOnly? ToDateUtc,
    DateOnly? SingleDateUtc,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? DaysOfWeekMask,
    IReadOnlyList<int> ResourceIds);

public sealed record BusyEventSummary(
    long Id,
    string? Title,
    DateTime StartUtc,
    DateTime EndUtc,
    IReadOnlyList<int> ResourceIds);

public sealed record PropertyNode(
    int Id,
    int? ParentId,
    string Key,
    string Label,
    int? SortOrder);
