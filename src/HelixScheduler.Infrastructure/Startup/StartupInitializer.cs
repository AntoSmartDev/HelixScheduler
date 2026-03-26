namespace HelixScheduler.Infrastructure.Startup;

public sealed class StartupInitializer : IStartupInitializer
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly ITenantBootstrapper _tenantBootstrapper;
    private readonly IDemoSeedInitializer _demoSeedInitializer;

    public StartupInitializer(
        IDatabaseInitializer databaseInitializer,
        ITenantBootstrapper tenantBootstrapper,
        IDemoSeedInitializer demoSeedInitializer)
    {
        _databaseInitializer = databaseInitializer;
        _tenantBootstrapper = tenantBootstrapper;
        _demoSeedInitializer = demoSeedInitializer;
    }

    public async Task EnsureDemoSeedAsync(CancellationToken ct)
    {
        await _databaseInitializer.MigrateAsync(ct).ConfigureAwait(false);
        await _tenantBootstrapper.EnsureDefaultTenantAsync(ct).ConfigureAwait(false);
        await _demoSeedInitializer.EnsureSeedAsync(ct).ConfigureAwait(false);
    }
}
