namespace HelixScheduler.Application.Management.Validation;

public interface IManagementValidationStore
{
    Task<TenantValidationSnapshot> LoadTenantSnapshotAsync(CancellationToken ct);
    Task<LegacyPropertyReferenceSnapshot> LoadInactivePropertyReferenceSnapshotAsync(CancellationToken ct);
    Task<LegacyPropertyReferenceCleanupResult> RemoveInactivePropertyReferencesAsync(CancellationToken ct);
}
