namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class ResourceTypes
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? SortOrder { get; set; }

    public ICollection<Resources> Resources { get; } = new List<Resources>();
    public ICollection<ResourceTypeProperties> ResourceTypeProperties { get; } = new List<ResourceTypeProperties>();
}
