namespace HelixScheduler.Application.ResourceCatalog.Management;

public interface IResourceTypeManagementStore
{
    Task<ResourceTypeManagementDto?> FindByIdAsync(int resourceTypeId, CancellationToken ct);
    Task<ResourceTypeManagementDto?> FindByKeyAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<ResourceTypeManagementDto>> ListAsync(CancellationToken ct);
    Task<ResourceTypeManagementDto> CreateAsync(Guid tenantId, string key, string label, int? sortOrder, CancellationToken ct);
    Task<ResourceTypeManagementDto> UpdateAsync(int resourceTypeId, string key, string label, int? sortOrder, CancellationToken ct);
    Task<ResourceTypeManagementDto> SetActiveAsync(int resourceTypeId, bool isActive, CancellationToken ct);
    Task<bool> HasActiveResourcesAsync(int resourceTypeId, CancellationToken ct);
}
