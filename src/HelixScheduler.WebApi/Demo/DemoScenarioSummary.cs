using HelixScheduler.Application.Availability;

namespace HelixScheduler.WebApi.Demo;

public sealed record DemoScenarioSummary(
    IReadOnlyList<ResourceSummary> Resources,
    IReadOnlyList<RuleSummary> Rules,
    IReadOnlyList<BusyEventSummary> BusyEvents);
