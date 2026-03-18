using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.QueryServices;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.QueryServices;

public sealed class AvailabilityFilterQueryService : IAvailabilityFilterQueryService
{
    private readonly SchedulerDbContext _dbContext;
    private readonly PropertyHierarchyQueryService _propertyHierarchyQueryService;

    public AvailabilityFilterQueryService(
        SchedulerDbContext dbContext,
        PropertyHierarchyQueryService propertyHierarchyQueryService)
    {
        _dbContext = dbContext;
        _propertyHierarchyQueryService = propertyHierarchyQueryService;
    }

    public async Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(int propertyId, CancellationToken ct)
    {
        return await _propertyHierarchyQueryService
            .ExpandPropertySubtreeAsync(propertyId, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
        IReadOnlyList<int> propertyIds,
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
        IReadOnlyList<int> propertyIds,
        CancellationToken ct)
    {
        if (propertyIds.Count == 0)
        {
            return Array.Empty<int>();
        }

        var distinctIds = propertyIds.Distinct().ToList();
        if (distinctIds.Count == 1)
        {
            return await GetResourceIdsByPropertiesAsync(distinctIds, ct).ConfigureAwait(false);
        }

        return await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .Where(link => distinctIds.Contains(link.PropertyId))
            .GroupBy(link => link.ResourceId)
            .Where(group => group.Select(link => link.PropertyId).Distinct().Count() == distinctIds.Count)
            .Select(group => group.Key)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IReadOnlyList<int>>> GetResourceIdsByPropertySetsAsync(
        IReadOnlyList<IReadOnlyList<int>> propertySets,
        CancellationToken ct)
    {
        if (propertySets.Count == 0)
        {
            return Array.Empty<IReadOnlyList<int>>();
        }

        var normalizedSets = new List<List<int>>(propertySets.Count);
        var allPropertyIds = new HashSet<int>();
        for (var i = 0; i < propertySets.Count; i++)
        {
            var set = propertySets[i];
            if (set == null || set.Count == 0)
            {
                normalizedSets.Add(new List<int>());
                continue;
            }

            var distinct = set.Distinct().ToList();
            normalizedSets.Add(distinct);
            for (var j = 0; j < distinct.Count; j++)
            {
                allPropertyIds.Add(distinct[j]);
            }
        }

        if (allPropertyIds.Count == 0)
        {
            return normalizedSets.Select(_ => (IReadOnlyList<int>)Array.Empty<int>()).ToList();
        }

        var links = await _dbContext.ResourcePropertyLinks
            .AsNoTracking()
            .Where(link => allPropertyIds.Contains(link.PropertyId))
            .Select(link => new { link.PropertyId, link.ResourceId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resourcesByPropertyId = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (!resourcesByPropertyId.TryGetValue(link.PropertyId, out var resources))
            {
                resources = new HashSet<int>();
                resourcesByPropertyId[link.PropertyId] = resources;
            }

            resources.Add(link.ResourceId);
        }

        var result = new List<IReadOnlyList<int>>(normalizedSets.Count);
        for (var i = 0; i < normalizedSets.Count; i++)
        {
            var set = normalizedSets[i];
            if (set.Count == 0)
            {
                result.Add(Array.Empty<int>());
                continue;
            }

            var matched = new HashSet<int>();
            for (var j = 0; j < set.Count; j++)
            {
                if (resourcesByPropertyId.TryGetValue(set[j], out var resources))
                {
                    matched.UnionWith(resources);
                }
            }

            var ids = matched.ToList();
            ids.Sort();
            result.Add(ids);
        }

        return result;
    }
}

