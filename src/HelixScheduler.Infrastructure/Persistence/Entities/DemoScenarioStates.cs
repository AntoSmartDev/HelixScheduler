namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class DemoScenarioStates
{
    public int Id { get; set; }
    public DateTime BaseDateUtc { get; set; }
    public int SeedVersion { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
