using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.Demo;

public sealed record DemoScenarioSummary(
    IReadOnlyList<ResourceSummary> Resources,
    IReadOnlyList<RuleSummary> Rules,
    IReadOnlyList<BusyEventSummary> BusyEvents);
