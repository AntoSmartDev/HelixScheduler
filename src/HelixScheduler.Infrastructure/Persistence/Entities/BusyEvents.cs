namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class BusyEvents
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public string? Title { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? EventType { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<BusyEventResources> BusyEventResources { get; } = new List<BusyEventResources>();
}
