namespace HelixScheduler.Application.Availability;

public interface IAvailabilitySummaryQueryService
{
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
}
