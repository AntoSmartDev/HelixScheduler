using HelixScheduler.Application.Demo;
using HelixScheduler.Application.Startup;
using HelixScheduler.Application.Abstractions;
using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Startup;

public sealed class StartupInitializer : IStartupInitializer
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IDemoSeedService _seedService;
    private readonly ITenantStore _tenantStore;
    private readonly ITenantContext _tenantContext;

    public StartupInitializer(
        SchedulerDbContext dbContext,
        IDemoSeedService seedService,
        ITenantStore tenantStore,
        ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _seedService = seedService;
        _tenantStore = tenantStore;
        _tenantContext = tenantContext;
    }

    public async Task EnsureDemoSeedAsync(CancellationToken ct)
    {
        await _dbContext.Database.MigrateAsync(ct);
        var defaultTenant = await _tenantStore.EnsureDefaultAsync(ct).ConfigureAwait(false);
        _tenantContext.SetTenant(defaultTenant.Id, defaultTenant.Key);
        await _seedService.EnsureSeedAsync(ct);
    }
}
