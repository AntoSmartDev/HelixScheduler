namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class Resources
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSchedulable { get; set; }
    public int Capacity { get; set; }
    public int TypeId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ResourceTypes Type { get; set; } = null!;
    public ICollection<ResourceRelations> ParentRelations { get; } = new List<ResourceRelations>();
    public ICollection<ResourceRelations> ChildRelations { get; } = new List<ResourceRelations>();
    public ICollection<ResourcePropertyLinks> PropertyLinks { get; } = new List<ResourcePropertyLinks>();
    public ICollection<RuleResources> RuleResources { get; } = new List<RuleResources>();
    public ICollection<BusyEventResources> BusyEventResources { get; } = new List<BusyEventResources>();
}
