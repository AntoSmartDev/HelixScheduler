using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Tenancy;

public interface ITenantManagementService
{
    Task<ManagementResult<TenantManagementDto>> CreateTenantAsync(
        CreateTenantCommand command,
        CancellationToken ct);

    Task<ManagementResult<TenantManagementDto>> UpdateTenantAsync(
        UpdateTenantCommand command,
        CancellationToken ct);

    Task<ManagementResult<TenantManagementDto>> GetTenantAsync(
        Guid tenantId,
        CancellationToken ct);

    Task<IReadOnlyList<TenantManagementDto>> ListTenantsAsync(CancellationToken ct);

    Task<ManagementResult<TenantManagementDto>> ActivateTenantAsync(
        Guid tenantId,
        CancellationToken ct);

    Task<ManagementResult<TenantManagementDto>> DeactivateTenantAsync(
        Guid tenantId,
        CancellationToken ct);
}
