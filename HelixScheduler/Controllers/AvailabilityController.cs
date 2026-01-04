using HelixScheduler.Application.Availability;
using HelixScheduler.Availability;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.Controllers;

[ApiController]
[Route("api/availability")]
public sealed class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _service;
    private readonly AvailabilityQueryParser _parser = new();
    private readonly AvailabilityQueryValidator _validator = new();

    public AvailabilityController(IAvailabilityService service)
    {
        _service = service;
    }

    [HttpGet("slots")]
    public async Task<IActionResult> GetSlotsAsync(
        [FromQuery] AvailabilitySlotsQuery query,
        CancellationToken ct = default)
    {
        if (!_parser.TryParse(query, out var input, out var parseError))
        {
            return BadRequest(parseError);
        }

        if (!_validator.TryValidate(input, out var request, out var validationError))
        {
            return BadRequest(validationError);
        }

        return await ExecuteAsync(
            request,
            ct,
            response => input.Explain ? Ok(response) : Ok(response.Slots));
    }

    [HttpPost("compute")]
    public async Task<ActionResult<AvailabilityComputeResponse>> ComputeAsync(
        [FromBody] AvailabilityComputeRequest request,
        CancellationToken ct = default)
    {
        return await ExecuteComputeAsync(request, ct);
    }

    private async Task<IActionResult> ExecuteAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct,
        Func<AvailabilityComputeResponse, IActionResult> onSuccess)
    {
        try
        {
            var response = await _service.ComputeAsync(request, ct);
            return onSuccess(response);
        }
        catch (AvailabilityRequestException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<ActionResult<AvailabilityComputeResponse>> ExecuteComputeAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _service.ComputeAsync(request, ct);
            return Ok(response);
        }
        catch (AvailabilityRequestException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
