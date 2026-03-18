using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.QueryServices;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.QueryServices;

public sealed class AvailabilityAncestorQueryService : IAvailabilityAncestorQueryService
{
    private readonly SchedulerDbContext _dbContext;

    public AvailabilityAncestorQueryService(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsByTypesAsync(
        IReadOnlyList<string>? relationTypes,
        CancellationToken ct)
    {
        var query = _dbContext.ResourceRelations.AsNoTracking();

        if (relationTypes != null && relationTypes.Count > 0)
        {
            query = query.Where(relation => relationTypes.Contains(relation.RelationType));
        }

        return await query
            .Select(relation => new ResourceRelationLink(
                relation.ParentResourceId,
                relation.ChildResourceId,
                relation.RelationType))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}

