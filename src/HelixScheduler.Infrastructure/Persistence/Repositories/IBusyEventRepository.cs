namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public interface IBusyEventRepository
{
    Task<IReadOnlyList<BusyEventRow>> GetBusyAsync(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct);

    Task<IReadOnlyList<BusyEventComputeRow>> GetBusyForComputeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct);
}

public sealed record BusyEventRow(
    long Id,
    string? Title,
    DateTime StartUtc,
    DateTime EndUtc,
    string? EventType,
    DateTime CreatedAtUtc,
    IReadOnlyList<int> ResourceIds);

public sealed record BusyEventComputeRow(
    long Id,
    DateTime StartUtc,
    DateTime EndUtc,
    IReadOnlyList<int> ResourceIds);
