using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.CatalogRead;

public interface IManagementCatalogReadService
{
    Task<ManagementResult<SchedulerCatalogSnapshot>> GetSchedulerCatalogSnapshotAsync(CancellationToken ct);

    Task<ManagementResult<ResourceConfigurationSnapshot>> GetResourceConfigurationSnapshotAsync(
        ResourceConfigurationSnapshotRequest request,
        CancellationToken ct);
}
