namespace HelixScheduler.Application.Availability;

public interface IAvailabilityDataSource
{
    Task<IReadOnlyList<RuleData>> GetRulesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);

    Task<IReadOnlyDictionary<int, int>> GetResourceCapacitiesAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);

    Task<IReadOnlyList<BusyEventData>> GetBusyEventsAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);

    Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(
        int propertyId,
        CancellationToken ct);

    Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
        IReadOnlyList<int> propertyIds,
        CancellationToken ct);

    Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
        IReadOnlyList<int> propertyIds,
        CancellationToken ct);

    Task<IReadOnlyList<ResourceSummary>> GetResourcesAsync(
        bool onlySchedulable,
        CancellationToken ct);

    Task<IReadOnlyList<RuleSummary>> GetRuleSummariesAsync(
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);

    Task<IReadOnlyList<BusyEventSummary>> GetBusyEventSummariesAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);

    Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsAsync(
        IReadOnlyList<int> childResourceIds,
        IReadOnlyList<string>? relationTypes,
        CancellationToken ct);
}
