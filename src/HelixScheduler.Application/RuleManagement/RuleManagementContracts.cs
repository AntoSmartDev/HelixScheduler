namespace HelixScheduler.Application.RuleManagement;

public enum RuleShape : byte
{
    Weekly = 1,
    SingleDate = 2,
    Range = 3,
    Monthly = 4,
    Repeating = 5
}

public sealed record RuleDefinition(
    RuleShape Shape,
    bool IsExclude,
    IReadOnlyList<int> ResourceIds,
    string? Title,
    DateOnly? FromDateUtc,
    DateOnly? ToDateUtc,
    DateOnly? SingleDateUtc,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? DaysOfWeekMask,
    int? DayOfMonth,
    int? IntervalDays);

public sealed record CreateRuleCommand(RuleDefinition Definition);

public sealed record UpdateRuleCommand(
    long RuleId,
    RuleDefinition Definition);

public sealed record RuleManagementDto(
    long Id,
    RuleShape Shape,
    bool IsExclude,
    string? Title,
    DateOnly? FromDateUtc,
    DateOnly? ToDateUtc,
    DateOnly? SingleDateUtc,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? DaysOfWeekMask,
    int? DayOfMonth,
    int? IntervalDays,
    IReadOnlyList<int> ResourceIds,
    bool IsActive,
    DateTime CreatedAtUtc);
