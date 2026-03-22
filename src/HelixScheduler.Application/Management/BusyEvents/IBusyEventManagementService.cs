using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.BusyEvents;

public interface IBusyEventManagementService
{
    Task<ManagementResult<BusyEventManagementDto>> RegisterBusyEventAsync(RegisterBusyEventCommand command, CancellationToken ct);
    Task<ManagementResult<IReadOnlyList<BusyEventManagementDto>>> RegisterBusyEventsAsync(RegisterBusyEventsCommand command, CancellationToken ct);
    Task<ManagementResult<BusyEventManagementDto>> UpsertBusyEventByExternalKeyAsync(UpsertBusyEventByExternalKeyCommand command, CancellationToken ct);
    Task<ManagementResult<BusyEventManagementDto>> UpdateBusyEventAsync(UpdateBusyEventCommand command, CancellationToken ct);
    Task<ManagementResult<BusyEventManagementDto>> GetBusyEventAsync(long busyEventId, CancellationToken ct);
    Task<IReadOnlyList<BusyEventManagementDto>> ListBusyEventsAsync(CancellationToken ct);
    Task<ManagementResult<BusyEventManagementDto>> CancelBusyEventAsync(long busyEventId, CancellationToken ct);
}
