using HelixScheduler.Application.BusyEventManagement;
using HelixScheduler.WebApi.Management;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Controllers;

[ApiController]
[Route("api/management/busy-events")]
public sealed class ManagementBusyEventsController : ManagementControllerBase
{
    private readonly IBusyEventManagementService _service;

    public ManagementBusyEventsController(IBusyEventManagementService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<BusyEventManagementDto>> RegisterAsync(
        [FromBody] RegisterBusyEventCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _service.RegisterBusyEventAsync(command, ct));
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<BusyEventManagementDto>>> RegisterBulkAsync(
        [FromBody] RegisterBusyEventsCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _service.RegisterBusyEventsAsync(command, ct));
    }

    [HttpPut("upsert")]
    public async Task<ActionResult<BusyEventManagementDto>> UpsertByExternalKeyAsync(
        [FromBody] UpsertBusyEventByExternalKeyCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _service.UpsertBusyEventByExternalKeyAsync(command, ct));
    }

    [HttpPut]
    public async Task<ActionResult<BusyEventManagementDto>> UpdateAsync(
        [FromBody] UpdateBusyEventCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _service.UpdateBusyEventAsync(command, ct));
    }

    [HttpGet]
    public Task<IReadOnlyList<BusyEventManagementDto>> ListAsync(CancellationToken ct = default)
    {
        return _service.ListBusyEventsAsync(ct);
    }

    [HttpGet("{busyEventId:long}")]
    public async Task<ActionResult<BusyEventManagementDto>> GetAsync(long busyEventId, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.GetBusyEventAsync(busyEventId, ct));
    }

    [HttpPost("{busyEventId:long}/cancel")]
    public async Task<ActionResult<BusyEventManagementDto>> CancelAsync(long busyEventId, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.CancelBusyEventAsync(busyEventId, ct));
    }
}
