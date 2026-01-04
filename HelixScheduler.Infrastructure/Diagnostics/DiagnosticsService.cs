using HelixScheduler.Application.Diagnostics;
using HelixScheduler.Infrastructure.Persistence;
using HelixScheduler.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Diagnostics;

public sealed class DiagnosticsService : IDiagnosticsService
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IPropertyRepository _propertyRepository;

    public DiagnosticsService(
        SchedulerDbContext dbContext,
        IPropertyRepository propertyRepository)
    {
        _dbContext = dbContext;
        _propertyRepository = propertyRepository;
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
        var properties = await _propertyRepository.ExpandPropertySubtreeAsync(propertyId, ct);
        return properties.Select(property => property.Id).ToList();
    }
}
