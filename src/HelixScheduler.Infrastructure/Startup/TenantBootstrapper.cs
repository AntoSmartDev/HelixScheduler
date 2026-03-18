using HelixScheduler.Application.Abstractions;

namespace HelixScheduler.Infrastructure.Startup;

internal sealed class TenantBootstrapper : ITenantBootstrapper
{
    private readonly ITenantStore _tenantStore;
    private readonly ITenantContext _tenantContext;

    public TenantBootstrapper(ITenantStore tenantStore, ITenantContext tenantContext)
    {
        _tenantStore = tenantStore;
        _tenantContext = tenantContext;
    }

    public async Task<TenantInfo> EnsureDefaultTenantAsync(CancellationToken ct)
    {
        var tenant = await _tenantStore.EnsureDefaultAsync(ct).ConfigureAwait(false);
        _tenantContext.SetTenant(tenant.Id, tenant.Key);
        return tenant;
    }
}
