namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public interface IBusyEventRepository
{
    Task<IReadOnlyList<BusyEventRow>> GetBusyAsync(
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
