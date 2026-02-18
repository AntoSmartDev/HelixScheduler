namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class RuleResources
{
    public Guid TenantId { get; set; }
    public long RuleId { get; set; }
    public int ResourceId { get; set; }

    public Rules Rule { get; set; } = null!;
    public Resources Resource { get; set; } = null!;
}
