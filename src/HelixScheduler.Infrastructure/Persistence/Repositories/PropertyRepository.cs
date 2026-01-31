using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class PropertyRepository : IPropertyRepository
{
    private readonly SchedulerDbContext _dbContext;

    public PropertyRepository(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ResourceProperties>> ExpandPropertySubtreeAsync(
        int propertyId,
        CancellationToken ct)
    {
        const string sql = """
            WITH PropertyTree AS (
                SELECT Id, ParentId, [Key], Label, SortOrder
                FROM ResourceProperties
                WHERE Id = {0}
                UNION ALL
                SELECT rp.Id, rp.ParentId, rp.[Key], rp.Label, rp.SortOrder
                FROM ResourceProperties rp
                INNER JOIN PropertyTree pt ON rp.ParentId = pt.Id
            )
            SELECT Id, ParentId, [Key], Label, SortOrder
            FROM PropertyTree
            """;

        return await _dbContext.ResourceProperties
            .FromSqlRaw(sql, propertyId)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
        IReadOnlyCollection<int> propertyIds,
        CancellationToken ct)
    {
        if (propertyIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        return await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .Where(link => propertyIds.Contains(link.PropertyId))
            .Select(link => link.ResourceId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
        IReadOnlyCollection<int> propertyIds,
        CancellationToken ct)
    {
        if (propertyIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        var distinctIds = propertyIds.Distinct().ToList();
        var requiredCount = distinctIds.Count;
        if (requiredCount == 1)
        {
            return await GetResourceIdsByPropertiesAsync(distinctIds, ct).ConfigureAwait(false);
        }

        return await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .Where(link => distinctIds.Contains(link.PropertyId))
            .GroupBy(link => link.ResourceId)
            .Where(group => group.Select(link => link.PropertyId).Distinct().Count() == requiredCount)
            .Select(group => group.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
