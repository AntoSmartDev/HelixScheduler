namespace HelixScheduler.Infrastructure.Persistence.Seed;

public interface IDemoScenarioStore
{
    Task<DemoScenarioState?> GetAsync(CancellationToken ct);
    Task SaveAsync(DemoScenarioState state, CancellationToken ct);
}
