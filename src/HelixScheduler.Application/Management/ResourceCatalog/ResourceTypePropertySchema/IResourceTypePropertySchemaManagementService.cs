using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.ResourceCatalog;

public interface IResourceTypePropertySchemaManagementService
{
    Task<ManagementResult<ResourceTypePropertySchemaManagementDto>> AssignPropertyDefinitionsAsync(
        AssignPropertyDefinitionsToResourceTypeCommand command,
        CancellationToken ct);

    Task<ManagementResult<ResourceTypePropertySchemaManagementDto>> RemovePropertyDefinitionsAsync(
        RemovePropertyDefinitionsFromResourceTypeCommand command,
        CancellationToken ct);

    Task<ManagementResult<ResourceTypePropertySchemaManagementDto>> GetPropertyDefinitionsAsync(
        int resourceTypeId,
        CancellationToken ct);
}
