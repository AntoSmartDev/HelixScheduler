using HelixScheduler.Application.Abstractions;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public sealed class TenantStore : ITenantStore
{
    public const string DefaultTenantKey = "default";
    private readonly SchedulerDbContext _dbContext;

    public TenantStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantInfo?> FindByKeyAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Key == key, ct)
            .ConfigureAwait(false);

        return tenant == null ? null : new TenantInfo(tenant.Id, tenant.Key, tenant.Label);
    }

    public async Task<TenantInfo> EnsureDefaultAsync(CancellationToken ct)
    {
        var existing = await FindByKeyAsync(DefaultTenantKey, ct).ConfigureAwait(false);
        if (existing != null)
        {
            return existing;
        }

        var tenant = new Tenants
        {
            Id = Guid.NewGuid(),
            Key = DefaultTenantKey,
            Label = "Default",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return new TenantInfo(tenant.Id, tenant.Key, tenant.Label);
    }
}
