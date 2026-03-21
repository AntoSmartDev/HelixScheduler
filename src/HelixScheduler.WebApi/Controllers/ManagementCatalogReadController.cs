using HelixScheduler.Application.CatalogRead;
using HelixScheduler.Application.ManagementValidation;
using HelixScheduler.WebApi.Management;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Controllers;

[ApiController]
[Route("api/management")]
public sealed class ManagementCatalogReadController : ManagementControllerBase
{
    private readonly IManagementCatalogReadService _catalogReadService;
    private readonly IManagementValidationService _validationService;

    public ManagementCatalogReadController(
        IManagementCatalogReadService catalogReadService,
        IManagementValidationService validationService)
    {
        _catalogReadService = catalogReadService;
        _validationService = validationService;
    }

    [HttpGet("catalog/snapshot")]
    public async Task<ActionResult<SchedulerCatalogSnapshot>> GetSchedulerCatalogSnapshotAsync(CancellationToken ct = default)
    {
        return FromManagementResult(await _catalogReadService.GetSchedulerCatalogSnapshotAsync(ct));
    }

    [HttpPost("catalog/resource-configuration")]
    public async Task<ActionResult<ResourceConfigurationSnapshot>> GetResourceConfigurationSnapshotAsync(
        [FromBody] ResourceConfigurationSnapshotRequest request,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _catalogReadService.GetResourceConfigurationSnapshotAsync(request, ct));
    }

    [HttpGet("validation/tenant")]
    public Task<ManagementValidationResult> ValidateTenantModelAsync(CancellationToken ct = default)
    {
        return _validationService.ValidateTenantModelAsync(ct);
    }

    [HttpGet("validation/resources/{resourceId:int}")]
    public Task<ManagementValidationResult> ValidateResourceConfigurationAsync(
        int resourceId,
        CancellationToken ct = default)
    {
        return _validationService.ValidateResourceConfigurationAsync(resourceId, ct);
    }
}
