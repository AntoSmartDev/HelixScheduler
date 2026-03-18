namespace HelixScheduler.Infrastructure.Startup;

public interface IDatabaseInitializer
{
    Task MigrateAsync(CancellationToken ct);
}
