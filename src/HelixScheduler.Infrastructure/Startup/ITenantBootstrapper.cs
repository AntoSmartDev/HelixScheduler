using HelixScheduler.Application.Abstractions;

namespace HelixScheduler.Infrastructure.Startup;

public interface ITenantBootstrapper
{
    Task<TenantInfo> EnsureDefaultTenantAsync(CancellationToken ct);
}
