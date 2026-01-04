using HelixScheduler.Infrastructure.Persistence.Entities;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public interface IRuleRepository
{
    Task<IReadOnlyList<Rules>> GetRulesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct);
}
