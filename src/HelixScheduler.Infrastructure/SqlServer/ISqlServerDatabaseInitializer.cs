namespace HelixScheduler.Infrastructure.SqlServer;

internal interface ISqlServerDatabaseInitializer
{
    Task MigrateAsync(CancellationToken ct);
}
