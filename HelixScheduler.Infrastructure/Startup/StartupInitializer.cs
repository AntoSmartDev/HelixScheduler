using HelixScheduler.Application.Demo;
using HelixScheduler.Application.Startup;
using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Startup;

public sealed class StartupInitializer : IStartupInitializer
{
    private readonly SchedulerDbContext _dbContext;
    private readonly IDemoSeedService _seedService;

    public StartupInitializer(SchedulerDbContext dbContext, IDemoSeedService seedService)
    {
        _dbContext = dbContext;
        _seedService = seedService;
    }

    public async Task EnsureDemoSeedAsync(CancellationToken ct)
    {
        await _dbContext.Database.MigrateAsync(ct);
        await _seedService.EnsureSeedAsync(ct);
    }
}
