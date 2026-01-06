namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class ResourceProperties
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? SortOrder { get; set; }

    public ResourceProperties? Parent { get; set; }
    public ICollection<ResourceProperties> Children { get; } = new List<ResourceProperties>();
    public ICollection<ResourcePropertyLinks> PropertyLinks { get; } = new List<ResourcePropertyLinks>();
}
