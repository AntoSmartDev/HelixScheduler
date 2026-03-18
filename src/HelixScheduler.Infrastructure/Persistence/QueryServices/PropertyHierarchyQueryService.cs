using HelixScheduler.Application.Availability;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.QueryServices;

public sealed class PropertyHierarchyQueryService
{
    private readonly SchedulerDbContext _dbContext;

    public PropertyHierarchyQueryService(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(int propertyId, CancellationToken ct)
    {
        var result = new List<PropertyNode>();
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
                .Select(property => new PropertyNode(
                    property.Id,
                    property.ParentId,
                    property.Key,
                    property.Label,
                    property.SortOrder))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (seenIds.Add(node.Id))
                {
                    result.Add(node);
                }

                if (node.ParentId.HasValue && batch.Contains(node.ParentId.Value))
                {
                    frontier.Add(node.Id);
                }
            }
        }

        result.Sort((left, right) => left.Id.CompareTo(right.Id));
        return result;
    }
}
