using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.SqlServer;

internal sealed class SqlServerDatabaseInitializer : ISqlServerDatabaseInitializer
{
    private readonly SchedulerDbContext _dbContext;

    public SqlServerDatabaseInitializer(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task MigrateAsync(CancellationToken ct)
    {
        await _dbContext.Database.MigrateAsync(ct);
    }
}
