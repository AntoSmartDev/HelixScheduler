using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class ResourceRepository : IResourceRepository
{
    private readonly SchedulerDbContext _dbContext;

    public ResourceRepository(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetCapacitiesAsync(
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var capacities = await _dbContext.Resources
            .AsNoTracking()
            .Where(resource => resourceIds.Contains(resource.Id))
            .Select(resource => new { resource.Id, resource.Capacity })
            .ToListAsync(ct);

        var result = new Dictionary<int, int>(capacities.Count);
        for (var i = 0; i < capacities.Count; i++)
        {
            var entry = capacities[i];
            result[entry.Id] = entry.Capacity;
        }

        return result;
    }
}
