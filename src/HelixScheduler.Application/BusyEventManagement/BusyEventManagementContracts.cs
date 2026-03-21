namespace HelixScheduler.Application.BusyEventManagement;

public sealed record BusyEventDefinition(
    IReadOnlyList<int> ResourceIds,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Title,
    string? EventType,
    string? ExternalKey);

public sealed record RegisterBusyEventCommand(BusyEventDefinition Definition);
public sealed record RegisterBusyEventsCommand(IReadOnlyList<BusyEventDefinition> Definitions);
public sealed record UpsertBusyEventByExternalKeyCommand(string ExternalKey, BusyEventDefinition Definition);

public sealed record UpdateBusyEventCommand(
    long BusyEventId,
    BusyEventDefinition Definition);

public sealed record BusyEventManagementDto(
    long Id,
    string? Title,
    DateTime StartUtc,
    DateTime EndUtc,
    string? EventType,
    string? ExternalKey,
    IReadOnlyList<int> ResourceIds,
    bool IsActive,
    DateTime CreatedAtUtc);
