using HelixScheduler.Application.Abstractions;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Stores;

public sealed class TenantStore : ITenantStore
{
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
            .FirstOrDefaultAsync(item => item.Key == key && item.IsActive, ct)
            .ConfigureAwait(false);

        return tenant == null ? null : new TenantInfo(tenant.Id, tenant.Key, tenant.Label, tenant.IsActive);
    }

    public async Task<TenantInfo> EnsureDefaultAsync(CancellationToken ct)
    {
        var existing = await _dbContext.Tenants
            .FirstOrDefaultAsync(item => item.Key == TenantConstants.DefaultTenantKey, ct)
            .ConfigureAwait(false);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            return new TenantInfo(existing.Id, existing.Key, existing.Label, existing.IsActive);
        }

        var tenant = new Tenants
        {
            Id = Guid.NewGuid(),
            Key = TenantConstants.DefaultTenantKey,
            Label = "Default",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return new TenantInfo(tenant.Id, tenant.Key, tenant.Label, tenant.IsActive);
    }
}
