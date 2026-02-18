namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class BusyEventResources
{
    public Guid TenantId { get; set; }
    public long BusyEventId { get; set; }
    public int ResourceId { get; set; }

    public BusyEvents BusyEvent { get; set; } = null!;
    public Resources Resource { get; set; } = null!;
}
