namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public interface IResourceRepository
{
    Task<IReadOnlyDictionary<int, int>> GetCapacitiesAsync(
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct);
}
