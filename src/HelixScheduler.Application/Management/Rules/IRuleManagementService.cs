using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Rules;

public interface IRuleManagementService
{
    Task<ManagementResult<RuleManagementDto>> CreateRuleAsync(CreateRuleCommand command, CancellationToken ct);
    Task<ManagementResult<RuleManagementDto>> UpdateRuleAsync(UpdateRuleCommand command, CancellationToken ct);
    Task<ManagementResult<RuleManagementDto>> GetRuleAsync(long ruleId, CancellationToken ct);
    Task<IReadOnlyList<RuleManagementDto>> ListRulesAsync(CancellationToken ct);
    Task<ManagementResult<RuleManagementDto>> DeactivateRuleAsync(long ruleId, CancellationToken ct);
}
