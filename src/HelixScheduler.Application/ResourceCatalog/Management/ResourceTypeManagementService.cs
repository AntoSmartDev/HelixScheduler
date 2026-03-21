using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.ResourceCatalog.Management;

public sealed class ResourceTypeManagementService : IResourceTypeManagementService
{
    private const int ResourceTypeKeyMaxLength = 100;
    private const int ResourceTypeLabelMaxLength = 200;

    private readonly IResourceTypeManagementStore _store;
    private readonly ITenantContext _tenantContext;

    public ResourceTypeManagementService(
        IResourceTypeManagementStore store,
        ITenantContext tenantContext)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ManagementResult<ResourceTypeManagementDto>> CreateResourceTypeAsync(
        CreateResourceTypeCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(tenantError);
        }

        var errors = ValidateKeyAndLabel(command.Key, command.Label);
        if (errors.Count > 0)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(errors);
        }

        var normalizedKey = NormalizeKey(command.Key);
        var existing = await _store.FindByKeyAsync(normalizedKey, ct).ConfigureAwait(false);
        if (existing != null)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(
                CreateError("resource-type.key.duplicate", ManagementErrorCategory.Conflict, "Resource type key already exists.", "key"));
        }

        var created = await _store.CreateAsync(
            _tenantContext.TenantId,
            normalizedKey,
            NormalizeLabel(command.Label),
            command.SortOrder,
            ct).ConfigureAwait(false);

        return ManagementResult<ResourceTypeManagementDto>.Success(created);
    }

    public async Task<ManagementResult<ResourceTypeManagementDto>> UpdateResourceTypeAsync(
        UpdateResourceTypeCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(tenantError);
        }

        var errors = ValidateKeyAndLabel(command.Key, command.Label);
        if (command.ResourceTypeId <= 0)
        {
            errors.Add(CreateError("resource-type.id.required", ManagementErrorCategory.Validation, "ResourceTypeId must be positive.", "resourceTypeId"));
        }

        if (errors.Count > 0)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(errors);
        }

        var existing = await _store.FindByIdAsync(command.ResourceTypeId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(command.ResourceTypeId);
        }

        var normalizedKey = NormalizeKey(command.Key);
        var byKey = await _store.FindByKeyAsync(normalizedKey, ct).ConfigureAwait(false);
        if (byKey != null && byKey.Id != command.ResourceTypeId)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(
                CreateError("resource-type.key.duplicate", ManagementErrorCategory.Conflict, "Resource type key already exists.", "key"));
        }

        var updated = await _store.UpdateAsync(
            command.ResourceTypeId,
            normalizedKey,
            NormalizeLabel(command.Label),
            command.SortOrder,
            ct).ConfigureAwait(false);

        return ManagementResult<ResourceTypeManagementDto>.Success(updated);
    }

    public async Task<ManagementResult<ResourceTypeManagementDto>> GetResourceTypeAsync(
        int resourceTypeId,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(tenantError);
        }

        if (resourceTypeId <= 0)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(
                CreateError("resource-type.id.required", ManagementErrorCategory.Validation, "ResourceTypeId must be positive.", "resourceTypeId"));
        }

        var existing = await _store.FindByIdAsync(resourceTypeId, ct).ConfigureAwait(false);
        return existing == null
            ? NotFound(resourceTypeId)
            : ManagementResult<ResourceTypeManagementDto>.Success(existing);
    }

    public async Task<IReadOnlyList<ResourceTypeManagementDto>> ListResourceTypesAsync(CancellationToken ct)
    {
        if (ValidateTenantContext() != null)
        {
            return Array.Empty<ResourceTypeManagementDto>();
        }

        return await _store.ListAsync(ct).ConfigureAwait(false);
    }

    public Task<ManagementResult<ResourceTypeManagementDto>> ActivateResourceTypeAsync(
        int resourceTypeId,
        CancellationToken ct)
    {
        return SetActiveAsync(resourceTypeId, true, ct);
    }

    public Task<ManagementResult<ResourceTypeManagementDto>> DeactivateResourceTypeAsync(
        int resourceTypeId,
        CancellationToken ct)
    {
        return SetActiveAsync(resourceTypeId, false, ct);
    }

    private async Task<ManagementResult<ResourceTypeManagementDto>> SetActiveAsync(
        int resourceTypeId,
        bool isActive,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(tenantError);
        }

        if (resourceTypeId <= 0)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(
                CreateError("resource-type.id.required", ManagementErrorCategory.Validation, "ResourceTypeId must be positive.", "resourceTypeId"));
        }

        var existing = await _store.FindByIdAsync(resourceTypeId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(resourceTypeId);
        }

        if (existing.IsActive == isActive)
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(
                CreateError(
                    isActive ? "resource-type.lifecycle.already-active" : "resource-type.lifecycle.already-inactive",
                    ManagementErrorCategory.InvalidOperation,
                    isActive ? "Resource type is already active." : "Resource type is already inactive.",
                    "resourceTypeId"));
        }

        if (!isActive && await _store.HasActiveResourcesAsync(resourceTypeId, ct).ConfigureAwait(false))
        {
            return ManagementResult<ResourceTypeManagementDto>.Failure(
                CreateError(
                    "resource-type.lifecycle.active-resources-exist",
                    ManagementErrorCategory.InvalidOperation,
                    "Resource type cannot be deactivated while active resources still reference it.",
                    "resourceTypeId"));
        }

        var updated = await _store.SetActiveAsync(resourceTypeId, isActive, ct).ConfigureAwait(false);
        return ManagementResult<ResourceTypeManagementDto>.Success(updated);
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static List<ManagementError> ValidateKeyAndLabel(string key, string label)
    {
        var errors = new List<ManagementError>();
        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add(CreateError("resource-type.key.required", ManagementErrorCategory.Validation, "Resource type key is required.", "key"));
        }
        else if (key.Trim().Length > ResourceTypeKeyMaxLength)
        {
            errors.Add(CreateError("resource-type.key.too-long", ManagementErrorCategory.Validation, $"Resource type key must be at most {ResourceTypeKeyMaxLength} characters.", "key"));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add(CreateError("resource-type.label.required", ManagementErrorCategory.Validation, "Resource type label is required.", "label"));
        }
        else if (label.Trim().Length > ResourceTypeLabelMaxLength)
        {
            errors.Add(CreateError("resource-type.label.too-long", ManagementErrorCategory.Validation, $"Resource type label must be at most {ResourceTypeLabelMaxLength} characters.", "label"));
        }

        return errors;
    }

    private static string NormalizeKey(string key) => key.Trim();
    private static string NormalizeLabel(string label) => label.Trim();

    private static ManagementResult<ResourceTypeManagementDto> NotFound(int resourceTypeId)
    {
        return ManagementResult<ResourceTypeManagementDto>.Failure(
            CreateError("resource-type.not-found", ManagementErrorCategory.NotFound, $"Resource type '{resourceTypeId}' was not found.", "resourceTypeId"));
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
