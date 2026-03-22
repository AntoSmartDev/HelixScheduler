using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Validation;

public interface ILegacyConsistencyService
{
    Task<ManagementResult<LegacyConsistencyReport>> GetLegacyConsistencyReportAsync(CancellationToken ct);
    Task<ManagementResult<LegacyConsistencyCleanupResult>> CleanupInactivePropertyReferencesAsync(CancellationToken ct);
}
