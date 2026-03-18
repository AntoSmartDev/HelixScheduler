using HelixScheduler.Application.Diagnostics;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.QueryServices;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Diagnostics;

public sealed class DiagnosticsService : IDiagnosticsService
{
    private readonly SchedulerDbContext _dbContext;
    private readonly PropertyHierarchyQueryService _propertyHierarchyQueryService;

    public DiagnosticsService(
        SchedulerDbContext dbContext,
        PropertyHierarchyQueryService propertyHierarchyQueryService)
    {
        _dbContext = dbContext;
        _propertyHierarchyQueryService = propertyHierarchyQueryService;
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
        var subtree = await _propertyHierarchyQueryService
            .ExpandPropertySubtreeAsync(propertyId, ct)
            .ConfigureAwait(false);
        return subtree.Select(node => node.Id).ToList();
    }
}
