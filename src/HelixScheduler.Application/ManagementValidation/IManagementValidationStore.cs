namespace HelixScheduler.Application.ManagementValidation;

public interface IManagementValidationStore
{
    Task<TenantValidationSnapshot> LoadTenantSnapshotAsync(CancellationToken ct);
}
