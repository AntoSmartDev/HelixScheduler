namespace HelixScheduler.Application.BusyEventManagement;

public interface IBusyEventManagementStore
{
    Task<BusyEventManagementDto?> FindByIdAsync(long busyEventId, CancellationToken ct);
    Task<IReadOnlyList<BusyEventManagementDto>> ListAsync(CancellationToken ct);
    Task<BusyEventManagementDto> CreateAsync(Guid tenantId, BusyEventDefinition definition, DateTime createdAtUtc, CancellationToken ct);
    Task<BusyEventManagementDto> UpdateAsync(long busyEventId, BusyEventDefinition definition, CancellationToken ct);
    Task<BusyEventManagementDto> SetActiveAsync(long busyEventId, bool isActive, CancellationToken ct);
}
