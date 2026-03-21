using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.ResourceCatalog.Management;

namespace HelixScheduler.Application.RuleManagement;

public sealed class RuleManagementService : IRuleManagementService
{
    private const int TitleMaxLength = 300;

    private readonly IRuleManagementStore _store;
    private readonly IResourceManagementStore _resourceStore;
    private readonly ITenantContext _tenantContext;
    private readonly IClock _clock;

    public RuleManagementService(
        IRuleManagementStore store,
        IResourceManagementStore resourceStore,
        ITenantContext tenantContext,
        IClock clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ManagementResult<RuleManagementDto>> CreateRuleAsync(
        CreateRuleCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<RuleManagementDto>.Failure(tenantError);
        }

        var normalized = NormalizeDefinition(command.Definition);
        var errors = ValidateDefinition(normalized);
        if (errors.Count > 0)
        {
            return ManagementResult<RuleManagementDto>.Failure(errors);
        }

        var resourceErrors = await ValidateResourcesAsync(normalized.ResourceIds, ct).ConfigureAwait(false);
        if (resourceErrors.Count > 0)
        {
            return ManagementResult<RuleManagementDto>.Failure(resourceErrors);
        }

        var created = await _store.CreateAsync(_tenantContext.TenantId, normalized, _clock.UtcNow, ct).ConfigureAwait(false);
        return ManagementResult<RuleManagementDto>.Success(created);
    }

    public async Task<ManagementResult<RuleManagementDto>> UpdateRuleAsync(
        UpdateRuleCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<RuleManagementDto>.Failure(tenantError);
        }

        var normalized = NormalizeDefinition(command.Definition);
        var errors = ValidateDefinition(normalized);
        if (command.RuleId <= 0)
        {
            errors.Add(CreateError("rule.id.required", ManagementErrorCategory.Validation, "RuleId must be positive.", "ruleId"));
        }

        if (errors.Count > 0)
        {
            return ManagementResult<RuleManagementDto>.Failure(errors);
        }

        var existing = await _store.FindByIdAsync(command.RuleId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(command.RuleId);
        }

        if (!existing.IsActive)
        {
            return ManagementResult<RuleManagementDto>.Failure(
                CreateError("rule.lifecycle.inactive", ManagementErrorCategory.InvalidOperation, "Inactive rules cannot be updated.", "ruleId"));
        }

        var resourceErrors = await ValidateResourcesAsync(normalized.ResourceIds, ct).ConfigureAwait(false);
        if (resourceErrors.Count > 0)
        {
            return ManagementResult<RuleManagementDto>.Failure(resourceErrors);
        }

        var updated = await _store.UpdateAsync(command.RuleId, normalized, ct).ConfigureAwait(false);
        return ManagementResult<RuleManagementDto>.Success(updated);
    }

    public async Task<ManagementResult<RuleManagementDto>> GetRuleAsync(long ruleId, CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<RuleManagementDto>.Failure(tenantError);
        }

        if (ruleId <= 0)
        {
            return ManagementResult<RuleManagementDto>.Failure(
                CreateError("rule.id.required", ManagementErrorCategory.Validation, "RuleId must be positive.", "ruleId"));
        }

        var existing = await _store.FindByIdAsync(ruleId, ct).ConfigureAwait(false);
        return existing == null
            ? NotFound(ruleId)
            : ManagementResult<RuleManagementDto>.Success(existing);
    }

    public async Task<IReadOnlyList<RuleManagementDto>> ListRulesAsync(CancellationToken ct)
    {
        if (ValidateTenantContext() != null)
        {
            return Array.Empty<RuleManagementDto>();
        }

        return await _store.ListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ManagementResult<RuleManagementDto>> DeactivateRuleAsync(long ruleId, CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<RuleManagementDto>.Failure(tenantError);
        }

        if (ruleId <= 0)
        {
            return ManagementResult<RuleManagementDto>.Failure(
                CreateError("rule.id.required", ManagementErrorCategory.Validation, "RuleId must be positive.", "ruleId"));
        }

        var existing = await _store.FindByIdAsync(ruleId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(ruleId);
        }

        if (!existing.IsActive)
        {
            return ManagementResult<RuleManagementDto>.Failure(
                CreateError("rule.lifecycle.already-inactive", ManagementErrorCategory.InvalidOperation, "Rule is already inactive.", "ruleId"));
        }

        var updated = await _store.SetActiveAsync(ruleId, false, ct).ConfigureAwait(false);
        return ManagementResult<RuleManagementDto>.Success(updated);
    }

    private async Task<List<ManagementError>> ValidateResourcesAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        var errors = new List<ManagementError>();

        for (var i = 0; i < resourceIds.Count; i++)
        {
            var resourceId = resourceIds[i];
            var resource = await _resourceStore.FindByIdAsync(resourceId, ct).ConfigureAwait(false);
            if (resource == null)
            {
                errors.Add(CreateError("rule.resource.not-found", ManagementErrorCategory.NotFound, $"Resource '{resourceId}' was not found.", "resourceIds"));
                continue;
            }

            if (!resource.IsActive || resource.IsArchived)
            {
                errors.Add(CreateError("rule.resource.inactive", ManagementErrorCategory.InvalidOperation, $"Resource '{resourceId}' must be active and not archived.", "resourceIds"));
            }
        }

        return errors;
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static RuleDefinition NormalizeDefinition(RuleDefinition definition)
    {
        var normalized = definition with
        {
            ResourceIds = definition.ResourceIds.Where(id => id > 0).Distinct().OrderBy(id => id).ToList(),
            Title = string.IsNullOrWhiteSpace(definition.Title) ? null : definition.Title.Trim()
        };

        return normalized.Shape switch
        {
            RuleShape.Weekly => normalized with
            {
                FromDateUtc = null,
                ToDateUtc = null,
                SingleDateUtc = null,
                DayOfMonth = null,
                IntervalDays = null
            },
            RuleShape.SingleDate => normalized with
            {
                FromDateUtc = null,
                ToDateUtc = null,
                DaysOfWeekMask = null,
                DayOfMonth = null,
                IntervalDays = null
            },
            RuleShape.Range => normalized with
            {
                SingleDateUtc = null,
                DaysOfWeekMask = null,
                DayOfMonth = null,
                IntervalDays = null
            },
            RuleShape.Monthly => normalized with
            {
                FromDateUtc = null,
                ToDateUtc = null,
                SingleDateUtc = null,
                DaysOfWeekMask = null,
                IntervalDays = null
            },
            RuleShape.Repeating => normalized with
            {
                SingleDateUtc = null,
                DaysOfWeekMask = null,
                DayOfMonth = null
            },
            _ => normalized
        };
    }

    private static List<ManagementError> ValidateDefinition(RuleDefinition definition)
    {
        var errors = new List<ManagementError>();

        if (!Enum.IsDefined(definition.Shape))
        {
            errors.Add(CreateError("rule.shape.invalid", ManagementErrorCategory.Validation, "Rule shape is invalid.", "shape"));
        }

        if (definition.ResourceIds.Count == 0)
        {
            errors.Add(CreateError("rule.resources.required", ManagementErrorCategory.Validation, "At least one target resource is required.", "resourceIds"));
        }

        if (definition.Title != null && definition.Title.Length > TitleMaxLength)
        {
            errors.Add(CreateError("rule.title.too-long", ManagementErrorCategory.Validation, $"Rule title must be at most {TitleMaxLength} characters.", "title"));
        }

        if (definition.EndTime <= definition.StartTime)
        {
            errors.Add(CreateError("rule.time-range.invalid", ManagementErrorCategory.Validation, "EndTime must be greater than StartTime.", "timeRange"));
        }

        switch (definition.Shape)
        {
            case RuleShape.Weekly:
                if (definition.DaysOfWeekMask == null || definition.DaysOfWeekMask <= 0 || definition.DaysOfWeekMask > 127)
                {
                    errors.Add(CreateError("rule.days-of-week.invalid", ManagementErrorCategory.Validation, "DaysOfWeekMask is required and must be valid for weekly rules.", "daysOfWeekMask"));
                }

                break;

            case RuleShape.Monthly:
                if (definition.DayOfMonth == null || definition.DayOfMonth < 1 || definition.DayOfMonth > 31)
                {
                    errors.Add(CreateError("rule.day-of-month.invalid", ManagementErrorCategory.Validation, "DayOfMonth is required and must be between 1 and 31.", "dayOfMonth"));
                }

                break;

            case RuleShape.SingleDate:
                if (definition.SingleDateUtc == null)
                {
                    errors.Add(CreateError("rule.single-date.required", ManagementErrorCategory.Validation, "SingleDateUtc is required for single-date rules.", "singleDateUtc"));
                }

                break;

            case RuleShape.Range:
                if (definition.FromDateUtc == null && definition.ToDateUtc == null)
                {
                    errors.Add(CreateError("rule.date-range.required", ManagementErrorCategory.Validation, "At least one range boundary is required for range rules.", "dateRange"));
                }
                else if (definition.FromDateUtc != null && definition.ToDateUtc != null && definition.FromDateUtc > definition.ToDateUtc)
                {
                    errors.Add(CreateError("rule.date-range.invalid", ManagementErrorCategory.Validation, "FromDateUtc must be before or equal to ToDateUtc.", "dateRange"));
                }

                break;

            case RuleShape.Repeating:
                if (definition.FromDateUtc == null)
                {
                    errors.Add(CreateError("rule.from-date.required", ManagementErrorCategory.Validation, "FromDateUtc is required for repeating rules.", "fromDateUtc"));
                }

                if (definition.IntervalDays == null || definition.IntervalDays <= 0)
                {
                    errors.Add(CreateError("rule.interval-days.invalid", ManagementErrorCategory.Validation, "IntervalDays is required and must be greater than zero.", "intervalDays"));
                }

                break;
        }

        return errors;
    }

    private static ManagementResult<RuleManagementDto> NotFound(long ruleId)
    {
        return ManagementResult<RuleManagementDto>.Failure(
            CreateError("rule.not-found", ManagementErrorCategory.NotFound, $"Rule '{ruleId}' was not found.", "ruleId"));
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
