using HelixScheduler.Application.Diagnostics;
using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Diagnostics;

public sealed class DiagnosticsService : IDiagnosticsService
{
    private readonly SchedulerDbContext _dbContext;

    public DiagnosticsService(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DbCounts> GetDbCountsAsync(CancellationToken ct)
    {
        var resources = await _dbContext.Resources.CountAsync(ct);
        var relations = await _dbContext.ResourceRelations.CountAsync(ct);
        var properties = await _dbContext.ResourceProperties.CountAsync(ct);
        var propertyLinks = await _dbContext.ResourcePropertyLinks.CountAsync(ct);
        var rules = await _dbContext.Rules.CountAsync(ct);
        var ruleResources = await _dbContext.RuleResources.CountAsync(ct);
        var busyEvents = await _dbContext.BusyEvents.CountAsync(ct);
        var busyEventResources = await _dbContext.BusyEventResources.CountAsync(ct);

        return new DbCounts(
            resources,
            relations,
            properties,
            propertyLinks,
            rules,
            ruleResources,
            busyEvents,
            busyEventResources);
    }

    public async Task<IReadOnlyList<int>> GetPropertySubtreeAsync(int propertyId, CancellationToken ct)
    {
        var result = new List<int>();
        var seenIds = new HashSet<int>();
        var frontier = new List<int> { propertyId };
        var visited = new HashSet<int>();

        while (frontier.Count > 0)
        {
            var batch = frontier.Where(visited.Add).ToList();
            frontier.Clear();
            if (batch.Count == 0)
            {
                continue;
            }

            var nodes = await _dbContext.ResourceProperties
                .AsNoTracking()
                .Where(property => batch.Contains(property.Id) || (property.ParentId != null && batch.Contains(property.ParentId.Value)))
                .Select(property => new { property.Id, property.ParentId })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (seenIds.Add(node.Id))
                {
                    result.Add(node.Id);
                }

                if (node.ParentId.HasValue && batch.Contains(node.ParentId.Value))
                {
                    frontier.Add(node.Id);
                }
            }
        }

        result.Sort();
        return result;
    }
}
