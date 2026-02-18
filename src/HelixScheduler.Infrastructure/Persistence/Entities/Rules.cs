namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class Rules
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public byte Kind { get; set; }
    public bool IsExclude { get; set; }
    public string? Title { get; set; }
    public DateOnly? FromDateUtc { get; set; }
    public DateOnly? ToDateUtc { get; set; }
    public DateOnly? SingleDateUtc { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? DaysOfWeekMask { get; set; }
    public int? DayOfMonth { get; set; }
    public int? IntervalDays { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<RuleResources> RuleResources { get; } = new List<RuleResources>();
}
