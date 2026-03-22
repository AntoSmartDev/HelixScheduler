using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.ResourceCatalog;

public interface IResourceManagementService
{
    Task<ManagementResult<ResourceManagementDto>> CreateResourceAsync(
        CreateResourceCommand command,
        CancellationToken ct);

    Task<ManagementResult<ResourceManagementDto>> UpdateResourceAsync(
        UpdateResourceCommand command,
        CancellationToken ct);

    Task<ManagementResult<ResourceManagementDto>> GetResourceAsync(
        int resourceId,
        CancellationToken ct);

    Task<IReadOnlyList<ResourceManagementDto>> ListResourcesAsync(CancellationToken ct);

    Task<ManagementResult<ResourceManagementDto>> ActivateResourceAsync(
        int resourceId,
        CancellationToken ct);

    Task<ManagementResult<ResourceManagementDto>> DeactivateResourceAsync(
        int resourceId,
        CancellationToken ct);

    Task<ManagementResult<ResourceManagementDto>> ArchiveResourceAsync(
        int resourceId,
        CancellationToken ct);
}
