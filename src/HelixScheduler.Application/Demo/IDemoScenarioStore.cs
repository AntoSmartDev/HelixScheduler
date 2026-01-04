namespace HelixScheduler.Application.Demo;

public interface IDemoScenarioStore
{
    Task<DemoScenarioState?> GetAsync(CancellationToken ct);
    Task SaveAsync(DemoScenarioState state, CancellationToken ct);
}
