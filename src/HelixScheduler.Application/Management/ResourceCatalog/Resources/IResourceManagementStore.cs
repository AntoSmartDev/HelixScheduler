namespace HelixScheduler.Application.Management.ResourceCatalog;

public interface IResourceManagementStore
{
    Task<ResourceManagementDto?> FindByIdAsync(int resourceId, CancellationToken ct);
    Task<IReadOnlyList<ResourceManagementDto>> ListAsync(CancellationToken ct);
    Task<ResourceManagementDto> CreateAsync(Guid tenantId, string? code, string name, bool isSchedulable, int capacity, int typeId, DateTime createdAtUtc, CancellationToken ct);
    Task<ResourceManagementDto> UpdateAsync(int resourceId, string? code, string name, bool isSchedulable, int capacity, int typeId, CancellationToken ct);
    Task<ResourceManagementDto> SetActiveAsync(int resourceId, bool isActive, CancellationToken ct);
    Task<ResourceManagementDto> ArchiveAsync(int resourceId, CancellationToken ct);
}
