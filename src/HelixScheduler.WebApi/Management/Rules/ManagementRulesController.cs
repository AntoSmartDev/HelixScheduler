using HelixScheduler.Application.Management.Rules;
using HelixScheduler.WebApi.Management;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Controllers;

[ApiController]
[Route("api/management/rules")]
public sealed class ManagementRulesController : ManagementControllerBase
{
    private readonly IRuleManagementService _service;

    public ManagementRulesController(IRuleManagementService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<RuleManagementDto>> CreateAsync([FromBody] CreateRuleCommand command, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.CreateRuleAsync(command, ct));
    }

    [HttpPut]
    public async Task<ActionResult<RuleManagementDto>> UpdateAsync([FromBody] UpdateRuleCommand command, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.UpdateRuleAsync(command, ct));
    }

    [HttpGet]
    public Task<IReadOnlyList<RuleManagementDto>> ListAsync(CancellationToken ct = default)
    {
        return _service.ListRulesAsync(ct);
    }

    [HttpGet("{ruleId:long}")]
    public async Task<ActionResult<RuleManagementDto>> GetAsync(long ruleId, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.GetRuleAsync(ruleId, ct));
    }

    [HttpPost("{ruleId:long}/deactivate")]
    public async Task<ActionResult<RuleManagementDto>> DeactivateAsync(long ruleId, CancellationToken ct = default)
    {
        return FromManagementResult(await _service.DeactivateRuleAsync(ruleId, ct));
    }
}
