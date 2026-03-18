using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Startup;

internal sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly SchedulerDbContext _dbContext;

    public DatabaseInitializer(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task MigrateAsync(CancellationToken ct)
    {
        await _dbContext.Database.MigrateAsync(ct);
    }
}
