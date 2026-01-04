namespace HelixScheduler.Application.Demo;

public interface IDemoSeedService
{
    Task EnsureSeedAsync(CancellationToken ct);
    Task ResetAsync(CancellationToken ct);
}
