namespace HelixScheduler.Application.Startup;

public interface IStartupInitializer
{
    Task EnsureDemoSeedAsync(CancellationToken ct);
}
