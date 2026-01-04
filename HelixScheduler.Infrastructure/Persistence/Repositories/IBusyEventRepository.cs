using HelixScheduler.Infrastructure.Persistence.Entities;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public interface IBusyEventRepository
{
    Task<IReadOnlyList<BusyEvents>> GetBusyAsync(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct);
}
