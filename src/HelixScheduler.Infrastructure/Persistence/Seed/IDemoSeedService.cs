namespace HelixScheduler.Infrastructure.Persistence.Seed;

public interface IDemoSeedService
{
    Task EnsureSeedAsync(CancellationToken ct);
    Task ResetAsync(CancellationToken ct);
}
