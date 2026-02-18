namespace HelixScheduler.Application.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantKey { get; }
    void SetTenant(Guid tenantId, string tenantKey);
}
