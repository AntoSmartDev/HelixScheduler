using HelixScheduler.Application.Management.Tenants;
using HelixScheduler.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using TenantEntity = HelixScheduler.Infrastructure.Persistence.Entities.Tenants;

namespace HelixScheduler.Infrastructure.Persistence.Stores.Management.Tenants;

public sealed class TenantManagementStore : ITenantManagementStore
{
    private readonly SchedulerDbContext _dbContext;

    public TenantManagementStore(SchedulerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantManagementDto?> FindByIdAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == tenantId, ct)
            .ConfigureAwait(false);

        return tenant == null ? null : Map(tenant);
    }

    public async Task<TenantManagementDto?> FindByKeyAsync(string key, CancellationToken ct)
    {
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Key == key, ct)
            .ConfigureAwait(false);

        return tenant == null ? null : Map(tenant);
    }

    public async Task<IReadOnlyList<TenantManagementDto>> ListAsync(CancellationToken ct)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .OrderBy(item => item.Key)
            .Select(item => new TenantManagementDto(
                item.Id,
                item.Key,
                item.Label,
                item.IsActive,
                item.CreatedAtUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<TenantManagementDto> CreateAsync(
        Guid tenantId,
        string key,
        string? label,
        DateTime createdAtUtc,
        CancellationToken ct)
    {
        var tenant = new TenantEntity
        {
            Id = tenantId,
            Key = key,
            Label = label,
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };

        _dbContext.Tenants.Add(tenant);
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return Map(tenant);
    }

    public async Task<TenantManagementDto> UpdateAsync(
        Guid tenantId,
        string key,
        string? label,
        CancellationToken ct)
    {
        var tenant = await _dbContext.Tenants
            .FirstAsync(item => item.Id == tenantId, ct)
            .ConfigureAwait(false);

        tenant.Key = key;
        tenant.Label = label;

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(tenant);
    }

    public async Task<TenantManagementDto> SetActiveAsync(Guid tenantId, bool isActive, CancellationToken ct)
    {
        var tenant = await _dbContext.Tenants
            .FirstAsync(item => item.Id == tenantId, ct)
            .ConfigureAwait(false);

        tenant.IsActive = isActive;

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return Map(tenant);
    }

    private static TenantManagementDto Map(TenantEntity tenant)
    {
        return new TenantManagementDto(
            tenant.Id,
            tenant.Key,
            tenant.Label,
            tenant.IsActive,
            tenant.CreatedAtUtc);
    }
}
