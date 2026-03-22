using HelixScheduler.Application.Management.Tenants;
using HelixScheduler.WebApi.Management;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Controllers;

[ApiController]
[Route("api/management/tenants")]
public sealed class ManagementTenantsController : ManagementControllerBase
{
    private readonly ITenantManagementService _service;

    public ManagementTenantsController(ITenantManagementService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<TenantManagementDto>> CreateAsync(
        [FromBody] CreateTenantCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _service.CreateTenantAsync(command, ct));
    }

    [HttpPut]
    public async Task<ActionResult<TenantManagementDto>> UpdateAsync(
        [FromBody] UpdateTenantCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _service.UpdateTenantAsync(command, ct));
    }

    [HttpGet]
    public Task<IReadOnlyList<TenantManagementDto>> ListAsync(CancellationToken ct = default)
    {
        return _service.ListTenantsAsync(ct);
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<ActionResult<TenantManagementDto>> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.GetTenantAsync(tenantId, ct));
    }

    [HttpPost("{tenantId:guid}/activate")]
    public async Task<ActionResult<TenantManagementDto>> ActivateAsync(Guid tenantId, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.ActivateTenantAsync(tenantId, ct));
    }

    [HttpPost("{tenantId:guid}/deactivate")]
    public async Task<ActionResult<TenantManagementDto>> DeactivateAsync(Guid tenantId, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.DeactivateTenantAsync(tenantId, ct));
    }
}
