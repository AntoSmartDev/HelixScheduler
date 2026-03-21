using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.ResourceCatalog.Management;

public interface IResourceTypeManagementService
{
    Task<ManagementResult<ResourceTypeManagementDto>> CreateResourceTypeAsync(
        CreateResourceTypeCommand command,
        CancellationToken ct);

    Task<ManagementResult<ResourceTypeManagementDto>> UpdateResourceTypeAsync(
        UpdateResourceTypeCommand command,
        CancellationToken ct);

    Task<ManagementResult<ResourceTypeManagementDto>> GetResourceTypeAsync(
        int resourceTypeId,
        CancellationToken ct);

    Task<IReadOnlyList<ResourceTypeManagementDto>> ListResourceTypesAsync(CancellationToken ct);

    Task<ManagementResult<ResourceTypeManagementDto>> ActivateResourceTypeAsync(
        int resourceTypeId,
        CancellationToken ct);

    Task<ManagementResult<ResourceTypeManagementDto>> DeactivateResourceTypeAsync(
        int resourceTypeId,
        CancellationToken ct);
}
