using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class RuleRepository : IRuleRepository
{
    private readonly SchedulerDbContext _dbContext;

    public RuleRepository(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Rules>> GetRulesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyCollection<int> resourceIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<Rules>();
        }

        var query = _dbContext.Rules
            .AsNoTracking()
            .Where(rule => rule.RuleResources.Any(link => resourceIds.Contains(link.ResourceId)))
            .Where(rule =>
                (rule.SingleDateUtc != null && rule.SingleDateUtc >= fromDateUtc && rule.SingleDateUtc <= toDateUtc) ||
                (rule.FromDateUtc != null && rule.ToDateUtc != null && rule.FromDateUtc <= toDateUtc && rule.ToDateUtc >= fromDateUtc) ||
                (rule.FromDateUtc != null && rule.ToDateUtc == null && rule.FromDateUtc <= toDateUtc) ||
                (rule.FromDateUtc == null && rule.ToDateUtc != null && rule.ToDateUtc >= fromDateUtc) ||
                (rule.FromDateUtc == null && rule.ToDateUtc == null && rule.SingleDateUtc == null));

        return await query
            .Include(rule => rule.RuleResources.Where(link => resourceIds.Contains(link.ResourceId)))
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
