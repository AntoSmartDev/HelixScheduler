namespace HelixScheduler.Application.Abstractions;

public interface ITenantStore
{
    Task<TenantInfo?> FindByKeyAsync(string key, CancellationToken ct);
    Task<TenantInfo> EnsureDefaultAsync(CancellationToken ct);
}
