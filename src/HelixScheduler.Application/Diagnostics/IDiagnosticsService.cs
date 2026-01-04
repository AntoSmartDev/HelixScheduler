namespace HelixScheduler.Application.Diagnostics;

public interface IDiagnosticsService
{
    Task<DbCounts> GetDbCountsAsync(CancellationToken ct);
    Task<IReadOnlyList<int>> GetPropertySubtreeAsync(int propertyId, CancellationToken ct);
}
