namespace HelixScheduler.Application.RuleManagement;

public interface IRuleManagementStore
{
    Task<RuleManagementDto?> FindByIdAsync(long ruleId, CancellationToken ct);
    Task<IReadOnlyList<RuleManagementDto>> ListAsync(CancellationToken ct);
    Task<RuleManagementDto> CreateAsync(Guid tenantId, RuleDefinition definition, DateTime createdAtUtc, CancellationToken ct);
    Task<RuleManagementDto> UpdateAsync(long ruleId, RuleDefinition definition, CancellationToken ct);
    Task<RuleManagementDto> SetActiveAsync(long ruleId, bool isActive, CancellationToken ct);
}
