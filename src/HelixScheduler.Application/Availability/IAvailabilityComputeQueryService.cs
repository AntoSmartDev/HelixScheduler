namespace HelixScheduler.Application.Availability;

public interface IAvailabilityComputeQueryService
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
}
