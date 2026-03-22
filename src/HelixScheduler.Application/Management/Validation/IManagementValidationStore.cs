namespace HelixScheduler.Application.Management.Validation;

public interface IManagementValidationStore
{
    Task<TenantValidationSnapshot> LoadTenantSnapshotAsync(CancellationToken ct);
}
