using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Tenants;

public sealed class TenantManagementService : ITenantManagementService
{
    private const int TenantKeyMaxLength = 64;
    private const int TenantLabelMaxLength = 128;

    private readonly ITenantManagementStore _store;
    private readonly IClock _clock;

    public TenantManagementService(ITenantManagementStore store, IClock clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ManagementResult<TenantManagementDto>> CreateTenantAsync(
        CreateTenantCommand command,
        CancellationToken ct)
    {
        var errors = ValidateKeyAndLabel(command.Key, command.Label);
        if (errors.Count > 0)
        {
            return ManagementResult<TenantManagementDto>.Failure(errors);
        }

        var normalizedKey = NormalizeKey(command.Key);
        var existing = await _store.FindByKeyAsync(normalizedKey, ct).ConfigureAwait(false);
        if (existing != null)
        {
            return ManagementResult<TenantManagementDto>.Failure(
                CreateError("tenant.key.duplicate", ManagementErrorCategory.Conflict, "Tenant key already exists.", "key"));
        }

        var tenant = await _store.CreateAsync(
            Guid.NewGuid(),
            normalizedKey,
            NormalizeLabel(command.Label),
            _clock.UtcNow,
            ct).ConfigureAwait(false);

        return ManagementResult<TenantManagementDto>.Success(tenant);
    }

    public async Task<ManagementResult<TenantManagementDto>> UpdateTenantAsync(
        UpdateTenantCommand command,
        CancellationToken ct)
    {
        var errors = ValidateKeyAndLabel(command.Key, command.Label);
        if (command.TenantId == Guid.Empty)
        {
            errors.Add(CreateError("tenant.id.required", ManagementErrorCategory.Validation, "TenantId is required.", "tenantId"));
        }

        if (errors.Count > 0)
        {
            return ManagementResult<TenantManagementDto>.Failure(errors);
        }

        var existing = await _store.FindByIdAsync(command.TenantId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(command.TenantId);
        }

        var normalizedKey = NormalizeKey(command.Key);
        var byKey = await _store.FindByKeyAsync(normalizedKey, ct).ConfigureAwait(false);
        if (byKey != null && byKey.Id != command.TenantId)
        {
            return ManagementResult<TenantManagementDto>.Failure(
                CreateError("tenant.key.duplicate", ManagementErrorCategory.Conflict, "Tenant key already exists.", "key"));
        }

        var tenant = await _store.UpdateAsync(
            command.TenantId,
            normalizedKey,
            NormalizeLabel(command.Label),
            ct).ConfigureAwait(false);

        return ManagementResult<TenantManagementDto>.Success(tenant);
    }

    public async Task<ManagementResult<TenantManagementDto>> GetTenantAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
        {
            return ManagementResult<TenantManagementDto>.Failure(
                CreateError("tenant.id.required", ManagementErrorCategory.Validation, "TenantId is required.", "tenantId"));
        }

        var tenant = await _store.FindByIdAsync(tenantId, ct).ConfigureAwait(false);
        return tenant == null
            ? NotFound(tenantId)
            : ManagementResult<TenantManagementDto>.Success(tenant);
    }

    public Task<IReadOnlyList<TenantManagementDto>> ListTenantsAsync(CancellationToken ct)
    {
        return _store.ListAsync(ct);
    }

    public Task<ManagementResult<TenantManagementDto>> ActivateTenantAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        return SetActiveAsync(tenantId, true, ct);
    }

    public Task<ManagementResult<TenantManagementDto>> DeactivateTenantAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        return SetActiveAsync(tenantId, false, ct);
    }

    private async Task<ManagementResult<TenantManagementDto>> SetActiveAsync(
        Guid tenantId,
        bool isActive,
        CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
        {
            return ManagementResult<TenantManagementDto>.Failure(
                CreateError("tenant.id.required", ManagementErrorCategory.Validation, "TenantId is required.", "tenantId"));
        }

        var existing = await _store.FindByIdAsync(tenantId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(tenantId);
        }

        if (existing.IsActive == isActive)
        {
            return ManagementResult<TenantManagementDto>.Failure(
                CreateError(
                    isActive ? "tenant.lifecycle.already-active" : "tenant.lifecycle.already-inactive",
                    ManagementErrorCategory.InvalidOperation,
                    isActive ? "Tenant is already active." : "Tenant is already inactive.",
                    "tenantId"));
        }

        if (!isActive && string.Equals(existing.Key, TenantConstants.DefaultTenantKey, StringComparison.OrdinalIgnoreCase))
        {
            return ManagementResult<TenantManagementDto>.Failure(
                CreateError(
                    "tenant.lifecycle.default-deactivation-forbidden",
                    ManagementErrorCategory.InvalidOperation,
                    "Default tenant cannot be deactivated.",
                    "tenantId"));
        }

        var tenant = await _store.SetActiveAsync(tenantId, isActive, ct).ConfigureAwait(false);
        return ManagementResult<TenantManagementDto>.Success(tenant);
    }

    private static ManagementResult<TenantManagementDto> NotFound(Guid tenantId)
    {
        return ManagementResult<TenantManagementDto>.Failure(
            CreateError("tenant.not-found", ManagementErrorCategory.NotFound, $"Tenant '{tenantId}' was not found.", "tenantId"));
    }

    private static List<ManagementError> ValidateKeyAndLabel(string key, string? label)
    {
        var errors = new List<ManagementError>();
        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add(CreateError("tenant.key.required", ManagementErrorCategory.Validation, "Tenant key is required.", "key"));
        }
        else if (key.Trim().Length > TenantKeyMaxLength)
        {
            errors.Add(CreateError("tenant.key.too-long", ManagementErrorCategory.Validation, $"Tenant key must be at most {TenantKeyMaxLength} characters.", "key"));
        }

        if (!string.IsNullOrWhiteSpace(label) && label.Trim().Length > TenantLabelMaxLength)
        {
            errors.Add(CreateError("tenant.label.too-long", ManagementErrorCategory.Validation, $"Tenant label must be at most {TenantLabelMaxLength} characters.", "label"));
        }

        return errors;
    }

    private static string NormalizeKey(string key) => key.Trim();

    private static string? NormalizeLabel(string? label)
    {
        return string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }

    private static ManagementError CreateError(
        string code,
        ManagementErrorCategory category,
        string message,
        string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
