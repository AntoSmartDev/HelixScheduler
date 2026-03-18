namespace HelixScheduler.Infrastructure.Startup;

public interface IDemoSeedInitializer
{
    Task EnsureSeedAsync(CancellationToken ct);
}
