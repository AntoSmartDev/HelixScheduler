using System.Net.Mime;
using System.Text.Json;
using HelixScheduler.Application.Abstractions;
using HelixScheduler.Infrastructure.Persistence.Repositories;

namespace HelixScheduler.WebApi.Tenancy;

public sealed class TenantResolutionMiddleware
{
    private const string TenantHeader = "X-Tenant";
    private const string TenantHeaderAlt = "X-Helix-Tenant";
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantStore tenantStore, ITenantContext tenantContext)
    {
        var tenantKey = ResolveTenantKey(context.Request);
        TenantInfo? tenant;
        if (string.IsNullOrWhiteSpace(tenantKey) || tenantKey == TenantStore.DefaultTenantKey)
        {
            tenant = await tenantStore.EnsureDefaultAsync(context.RequestAborted).ConfigureAwait(false);
        }
        else
        {
            tenant = await tenantStore.FindByKeyAsync(tenantKey, context.RequestAborted).ConfigureAwait(false);
        }

        if (tenant == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = "Tenant not found.", tenant = tenantKey }),
                context.RequestAborted);
            return;
        }

        tenantContext.SetTenant(tenant.Id, tenant.Key);
        await _next(context);
    }

    private static string? ResolveTenantKey(HttpRequest request)
    {
        var header = request.Headers[TenantHeaderAlt].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header))
        {
            header = request.Headers[TenantHeader].FirstOrDefault();
        }

        return header?.Trim();
    }
}
