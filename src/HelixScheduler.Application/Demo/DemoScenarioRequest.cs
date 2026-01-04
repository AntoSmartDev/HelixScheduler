namespace HelixScheduler.Application.Demo;

public sealed record DemoScenarioRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<int> ResourceIds);
