using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Validation;

public sealed class LegacyConsistencyService : ILegacyConsistencyService
{
    private readonly IManagementValidationService _managementValidationService;
    private readonly IManagementValidationStore _store;
    private readonly ITenantContext _tenantContext;

    public LegacyConsistencyService(
        IManagementValidationService managementValidationService,
        IManagementValidationStore store,
        ITenantContext tenantContext)
    {
        _managementValidationService = managementValidationService ?? throw new ArgumentNullException(nameof(managementValidationService));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ManagementResult<LegacyConsistencyReport>> GetLegacyConsistencyReportAsync(CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<LegacyConsistencyReport>.Failure(tenantError);
        }

        var report = await BuildReportAsync(ct).ConfigureAwait(false);
        return ManagementResult<LegacyConsistencyReport>.Success(report);
    }

    public async Task<ManagementResult<LegacyConsistencyCleanupResult>> CleanupInactivePropertyReferencesAsync(CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<LegacyConsistencyCleanupResult>.Failure(tenantError);
        }

        var cleanup = await _store.RemoveInactivePropertyReferencesAsync(ct).ConfigureAwait(false);
        var reportAfter = await BuildReportAsync(ct).ConfigureAwait(false);

        return ManagementResult<LegacyConsistencyCleanupResult>.Success(
            new LegacyConsistencyCleanupResult(
                cleanup.RemovedResourcePropertyAssignments,
                cleanup.RemovedResourceTypePropertyMappings,
                reportAfter));
    }

    private async Task<LegacyConsistencyReport> BuildReportAsync(CancellationToken ct)
    {
        var validation = await _managementValidationService.ValidateTenantModelAsync(ct).ConfigureAwait(false);
        var references = await _store.LoadInactivePropertyReferenceSnapshotAsync(ct).ConfigureAwait(false);

        return new LegacyConsistencyReport(
            validation,
            new LegacyConsistencyRepairPreview(
                references.InactiveResourcePropertyAssignments,
                references.InactiveResourceTypePropertyMappings));
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? new ManagementError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }
}
