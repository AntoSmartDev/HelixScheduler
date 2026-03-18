using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.Demo;

public sealed class DemoReadService : IDemoReadService
{
    private readonly IAvailabilitySummaryQueryService _summaryQueryService;

    public DemoReadService(IAvailabilitySummaryQueryService summaryQueryService)
    {
        _summaryQueryService = summaryQueryService ?? throw new ArgumentNullException(nameof(summaryQueryService));
    }

    public async Task<DemoScenarioSummary> GetScenarioAsync(DemoScenarioRequest request, CancellationToken ct)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("fromDate must be less than or equal to toDate.", nameof(request));
        }

        var resourceIds = request.ResourceIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        var resources = await _summaryQueryService.GetResourcesAsync(onlySchedulable: false, ct).ConfigureAwait(false);

        if (resourceIds.Count == 0)
        {
            resourceIds = resources.Select(resource => resource.Id).ToList();
        }

        var rules = await _summaryQueryService.GetRuleSummariesAsync(
            request.FromDate,
            request.ToDate,
            resourceIds,
            ct).ConfigureAwait(false);

        var fromUtc = DateTime.SpecifyKind(request.FromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtcExclusive = DateTime.SpecifyKind(request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var busy = await _summaryQueryService.GetBusyEventSummariesAsync(
            fromUtc,
            toUtcExclusive,
            resourceIds,
            ct).ConfigureAwait(false);

        return new DemoScenarioSummary(resources, rules, busy);
    }
}
