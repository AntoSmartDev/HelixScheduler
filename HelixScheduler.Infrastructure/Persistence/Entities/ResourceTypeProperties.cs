namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class ResourceTypeProperties
{
    public int ResourceTypeId { get; set; }
    public int PropertyDefinitionId { get; set; }

    public ResourceTypes ResourceType { get; set; } = null!;
    public ResourceProperties PropertyDefinition { get; set; } = null!;
}
