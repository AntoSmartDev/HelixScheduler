namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class ResourcePropertyLinks
{
    public Guid TenantId { get; set; }
    public int ResourceId { get; set; }
    public int PropertyId { get; set; }

    public Resources Resource { get; set; } = null!;
    public ResourceProperties Property { get; set; } = null!;
}
