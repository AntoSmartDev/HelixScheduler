using HelixScheduler.Application.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Controllers;

[ApiController]
[Route("api/diag")]
public sealed class DiagController : ControllerBase
{
    private readonly IDiagnosticsService _diagnosticsService;

    public DiagController(
        IDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    [HttpGet("db")]
    public async Task<ActionResult<DbCounts>> GetCountsAsync(CancellationToken ct)
    {
        return await _diagnosticsService.GetDbCountsAsync(ct);
    }

    [HttpGet("property-subtree")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetPropertySubtreeAsync(
        [FromQuery] int propertyId,
        CancellationToken ct)
    {
        var properties = await _diagnosticsService.GetPropertySubtreeAsync(propertyId, ct);
        return properties.ToList();
    }
}

