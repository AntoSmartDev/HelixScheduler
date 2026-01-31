namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public interface IRuleRepository
{
    Task<IReadOnlyList<RuleRow>> GetRulesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct);
}

public sealed record RuleRow(
    long Id,
    byte Kind,
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
    DateTime CreatedAtUtc,
    IReadOnlyList<int> ResourceIds);
