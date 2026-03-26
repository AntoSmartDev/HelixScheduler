namespace HelixScheduler.Infrastructure.Persistence.Seed;

public sealed record DemoScenarioState(
    DateOnly BaseDateUtc,
    int SeedVersion,
    DateTime UpdatedAtUtc);
