using HelixScheduler.Application.Availability;
using HelixScheduler.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class AvailabilityDataSource : IAvailabilityDataSource
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IRuleRepository _ruleRepository;
    private readonly IBusyEventRepository _busyEventRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IResourceRepository _resourceRepository;

    public AvailabilityDataSource(
        SchedulerDbContext dbContext,
        IRuleRepository ruleRepository,
        IBusyEventRepository busyEventRepository,
        IPropertyRepository propertyRepository,
        IResourceRepository resourceRepository)
    {
        _dbContext = dbContext;
        _ruleRepository = ruleRepository;
        _busyEventRepository = busyEventRepository;
        _propertyRepository = propertyRepository;
        _resourceRepository = resourceRepository;
    }

    public async Task<IReadOnlyList<RuleData>> GetRulesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        var rules = await _ruleRepository
            .GetRulesAsync(fromDateUtc, toDateUtc, resourceIds, ct)
            .ConfigureAwait(false);

        var result = new List<RuleData>(rules.Count);
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            result.Add(new RuleData(
                rule.Id,
                rule.Kind,
                rule.IsExclude,
                rule.FromDateUtc,
                rule.ToDateUtc,
                rule.SingleDateUtc,
                rule.StartTime,
                rule.EndTime,
                rule.DaysOfWeekMask,
                rule.DayOfMonth,
                rule.IntervalDays,
                rule.ResourceIds));
        }

        return result;
    }

    public Task<IReadOnlyDictionary<int, int>> GetResourceCapacitiesAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        return _resourceRepository.GetCapacitiesAsync(resourceIds, ct);
    }

    public async Task<IReadOnlyList<BusyEventData>> GetBusyEventsAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        var busyEvents = await _busyEventRepository
            .GetBusyAsync(fromUtc, toUtcExclusive, resourceIds, ct)
            .ConfigureAwait(false);

        var result = new List<BusyEventData>(busyEvents.Count);
        for (var i = 0; i < busyEvents.Count; i++)
        {
            var busy = busyEvents[i];
            result.Add(new BusyEventData(
                busy.Id,
                busy.StartUtc,
                busy.EndUtc,
                busy.ResourceIds));
        }

        return result;
    }

    public async Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(
        int propertyId,
        CancellationToken ct)
    {
        var properties = await _propertyRepository
            .ExpandPropertySubtreeAsync(propertyId, ct)
            .ConfigureAwait(false);

        return properties
            .Select(property => new PropertyNode(
                property.Id,
                property.ParentId,
                property.Key,
                property.Label,
                property.SortOrder))
            .ToList();
    }

    public Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
        IReadOnlyList<int> propertyIds,
        CancellationToken ct)
    {
        return _propertyRepository.GetResourceIdsByPropertiesAsync(propertyIds, ct);
    }

    public Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
        IReadOnlyList<int> propertyIds,
        CancellationToken ct)
    {
        return _propertyRepository.GetResourceIdsByAllPropertiesAsync(propertyIds, ct);
    }

    public async Task<IReadOnlyList<ResourceSummary>> GetResourcesAsync(
        bool onlySchedulable,
        CancellationToken ct)
    {
        var query = _dbContext.Resources.AsNoTracking();
        if (onlySchedulable)
        {
            query = query.Where(resource => resource.IsSchedulable);
        }

        var resources = await query
            .OrderBy(resource => resource.Name)
            .Select(resource => new ResourceSummary(
                resource.Id,
                resource.Code,
                resource.Name,
                resource.IsSchedulable))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return resources;
    }

    public async Task<IReadOnlyList<RuleSummary>> GetRuleSummariesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        var rules = await _ruleRepository
            .GetRulesAsync(fromDateUtc, toDateUtc, resourceIds, ct)
            .ConfigureAwait(false);

        var result = new List<RuleSummary>(rules.Count);
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            result.Add(new RuleSummary(
                rule.Id,
                rule.Title,
                rule.Kind,
                rule.IsExclude,
                rule.FromDateUtc,
                rule.ToDateUtc,
                rule.SingleDateUtc,
                rule.StartTime,
                rule.EndTime,
                rule.DaysOfWeekMask,
                rule.ResourceIds));
        }

        return result;
    }

    public async Task<IReadOnlyList<BusyEventSummary>> GetBusyEventSummariesAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        var busyEvents = await _busyEventRepository
            .GetBusyAsync(fromUtc, toUtcExclusive, resourceIds, ct)
            .ConfigureAwait(false);

        var result = new List<BusyEventSummary>(busyEvents.Count);
        for (var i = 0; i < busyEvents.Count; i++)
        {
            var busy = busyEvents[i];
            var startUtc = DateTime.SpecifyKind(busy.StartUtc, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(busy.EndUtc, DateTimeKind.Utc);

            result.Add(new BusyEventSummary(
                busy.Id,
                busy.Title,
                startUtc,
                endUtc,
                busy.ResourceIds));
        }

        return result;
    }

    public async Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsAsync(
        IReadOnlyList<int> childResourceIds,
        IReadOnlyList<string>? relationTypes,
        CancellationToken ct)
    {
        if (childResourceIds.Count == 0)
        {
            return Array.Empty<ResourceRelationLink>();
        }

        var query = _dbContext.ResourceRelations.AsNoTracking()
            .Where(relation => childResourceIds.Contains(relation.ChildResourceId));

        if (relationTypes != null && relationTypes.Count > 0)
        {
            query = query.Where(relation => relationTypes.Contains(relation.RelationType));
        }

        var relations = await query
            .Select(relation => new ResourceRelationLink(
                relation.ParentResourceId,
                relation.ChildResourceId,
                relation.RelationType))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return relations;
    }
}
