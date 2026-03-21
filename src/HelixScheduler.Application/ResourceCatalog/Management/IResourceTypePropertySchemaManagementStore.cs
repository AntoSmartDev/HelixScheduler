namespace HelixScheduler.Application.ResourceCatalog.Management;

public interface IResourceTypePropertySchemaManagementStore
{
    Task<IReadOnlyList<PropertyDefinitionManagementState>> GetPropertyDefinitionsAsync(
        IReadOnlyList<int> propertyDefinitionIds,
        CancellationToken ct);

    Task<IReadOnlyList<int>> ListAssignedPropertyDefinitionIdsAsync(
        int resourceTypeId,
        CancellationToken ct);

    Task AddAssignmentsAsync(
        Guid tenantId,
        int resourceTypeId,
        IReadOnlyList<int> propertyDefinitionIds,
        CancellationToken ct);

    Task RemoveAssignmentsAsync(
        int resourceTypeId,
        IReadOnlyList<int> propertyDefinitionIds,
        CancellationToken ct);
}
