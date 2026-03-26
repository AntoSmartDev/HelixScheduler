namespace HelixScheduler.Infrastructure.Startup;

public interface IStartupInitializer
{
    Task EnsureDemoSeedAsync(CancellationToken ct);
}
