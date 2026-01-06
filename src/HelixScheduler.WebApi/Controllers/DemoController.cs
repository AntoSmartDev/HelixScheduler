using HelixScheduler.Application.Demo;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.Controllers;

[ApiController]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    private readonly IDemoReadService _demoReadService;
    private readonly IDemoSeedService _demoSeedService;
    private readonly IHostEnvironment _environment;

    public DemoController(
        IDemoReadService demoReadService,
        IDemoSeedService demoSeedService,
        IHostEnvironment environment)
    {
        _demoReadService = demoReadService;
        _demoSeedService = demoSeedService;
        _environment = environment;
    }

    [HttpPost("summary")]
    public async Task<ActionResult<DemoScenarioSummary>> GetSummaryAsync(
        [FromBody] DemoScenarioRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var summary = await _demoReadService.GetScenarioAsync(request, ct);
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("reset")]
    public async Task<IActionResult> ResetAsync(CancellationToken ct = default)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        await _demoSeedService.ResetAsync(ct);
        return NoContent();
    }
}
