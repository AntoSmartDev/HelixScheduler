using HelixScheduler.Application.Abstractions;

namespace HelixScheduler.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    private Guid _tenantId = Guid.Empty;
    private string _tenantKey = "default";

    public Guid TenantId => _tenantId;
    public string TenantKey => _tenantKey;

    public void SetTenant(Guid tenantId, string tenantKey)
    {
        _tenantId = tenantId;
        _tenantKey = tenantKey;
    }
}
