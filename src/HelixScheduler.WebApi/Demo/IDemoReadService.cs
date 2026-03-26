namespace HelixScheduler.WebApi.Demo;

public interface IDemoReadService
{
    Task<DemoScenarioSummary> GetScenarioAsync(DemoScenarioRequest request, CancellationToken ct);
}
