namespace HelixScheduler.Application.ResourceCatalog.Management;

public sealed record AssignPropertyDefinitionsToResourceTypeCommand(
    int ResourceTypeId,
    IReadOnlyList<int> PropertyDefinitionIds);

public sealed record RemovePropertyDefinitionsFromResourceTypeCommand(
    int ResourceTypeId,
    IReadOnlyList<int> PropertyDefinitionIds);

public sealed record ResourceTypePropertySchemaManagementDto(
    int ResourceTypeId,
    IReadOnlyList<int> PropertyDefinitionIds);

public sealed record PropertyDefinitionManagementState(
    int Id,
    bool IsActive);
