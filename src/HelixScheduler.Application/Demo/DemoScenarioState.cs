namespace HelixScheduler.Application.Demo;

public sealed record DemoScenarioState(
    DateOnly BaseDateUtc,
    int SeedVersion,
    DateTime UpdatedAtUtc);
