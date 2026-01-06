namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class ResourceRelations
{
    public int ParentResourceId { get; set; }
    public int ChildResourceId { get; set; }
    public string RelationType { get; set; } = string.Empty;

    public Resources ParentResource { get; set; } = null!;
    public Resources ChildResource { get; set; } = null!;
}
