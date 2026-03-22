namespace HelixScheduler.Application.Management.Tenants;

public interface ITenantManagementStore
{
    Task<TenantManagementDto?> FindByIdAsync(Guid tenantId, CancellationToken ct);
    Task<TenantManagementDto?> FindByKeyAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<TenantManagementDto>> ListAsync(CancellationToken ct);
    Task<TenantManagementDto> CreateAsync(Guid tenantId, string key, string? label, DateTime createdAtUtc, CancellationToken ct);
    Task<TenantManagementDto> UpdateAsync(Guid tenantId, string key, string? label, CancellationToken ct);
    Task<TenantManagementDto> SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct);
}
