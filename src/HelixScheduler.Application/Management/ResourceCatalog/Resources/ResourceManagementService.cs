using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.ResourceCatalog;

public sealed class ResourceManagementService : IResourceManagementService
{
    private const int ResourceCodeMaxLength = 64;
    private const int ResourceNameMaxLength = 200;

    private readonly IResourceManagementStore _store;
    private readonly IResourceTypeManagementStore _resourceTypeStore;
    private readonly ITenantContext _tenantContext;
    private readonly IClock _clock;

    public ResourceManagementService(
        IResourceManagementStore store,
        IResourceTypeManagementStore resourceTypeStore,
        ITenantContext tenantContext,
        IClock clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _resourceTypeStore = resourceTypeStore ?? throw new ArgumentNullException(nameof(resourceTypeStore));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ManagementResult<ResourceManagementDto>> CreateResourceAsync(
        CreateResourceCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceManagementDto>.Failure(tenantError);
        }

        var errors = ValidateResourcePayload(command.Code, command.Name, command.Capacity, command.TypeId);
        if (errors.Count > 0)
        {
            return ManagementResult<ResourceManagementDto>.Failure(errors);
        }

        var resourceType = await _resourceTypeStore.FindByIdAsync(command.TypeId, ct).ConfigureAwait(false);
        if (resourceType == null)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.type.not-found", ManagementErrorCategory.NotFound, $"Resource type '{command.TypeId}' was not found.", "typeId"));
        }

        if (!resourceType.IsActive)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.type.inactive", ManagementErrorCategory.InvalidOperation, "Resource type is inactive.", "typeId"));
        }

        var created = await _store.CreateAsync(
            _tenantContext.TenantId,
            NormalizeCode(command.Code),
            NormalizeName(command.Name),
            command.IsSchedulable,
            command.Capacity,
            command.TypeId,
            _clock.UtcNow,
            ct).ConfigureAwait(false);

        return ManagementResult<ResourceManagementDto>.Success(created);
    }

    public async Task<ManagementResult<ResourceManagementDto>> UpdateResourceAsync(
        UpdateResourceCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceManagementDto>.Failure(tenantError);
        }

        var errors = ValidateResourcePayload(command.Code, command.Name, command.Capacity, command.TypeId);
        if (command.ResourceId <= 0)
        {
            errors.Add(CreateError("resource.id.required", ManagementErrorCategory.Validation, "ResourceId must be positive.", "resourceId"));
        }

        if (errors.Count > 0)
        {
            return ManagementResult<ResourceManagementDto>.Failure(errors);
        }

        var existing = await _store.FindByIdAsync(command.ResourceId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(command.ResourceId);
        }

        if (existing.IsArchived)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.lifecycle.archived", ManagementErrorCategory.InvalidOperation, "Archived resources cannot be updated.", "resourceId"));
        }

        var resourceType = await _resourceTypeStore.FindByIdAsync(command.TypeId, ct).ConfigureAwait(false);
        if (resourceType == null)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.type.not-found", ManagementErrorCategory.NotFound, $"Resource type '{command.TypeId}' was not found.", "typeId"));
        }

        if (!resourceType.IsActive)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.type.inactive", ManagementErrorCategory.InvalidOperation, "Resource type is inactive.", "typeId"));
        }

        var updated = await _store.UpdateAsync(
            command.ResourceId,
            NormalizeCode(command.Code),
            NormalizeName(command.Name),
            command.IsSchedulable,
            command.Capacity,
            command.TypeId,
            ct).ConfigureAwait(false);

        return ManagementResult<ResourceManagementDto>.Success(updated);
    }

    public async Task<ManagementResult<ResourceManagementDto>> GetResourceAsync(
        int resourceId,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceManagementDto>.Failure(tenantError);
        }

        if (resourceId <= 0)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.id.required", ManagementErrorCategory.Validation, "ResourceId must be positive.", "resourceId"));
        }

        var existing = await _store.FindByIdAsync(resourceId, ct).ConfigureAwait(false);
        return existing == null
            ? NotFound(resourceId)
            : ManagementResult<ResourceManagementDto>.Success(existing);
    }

    public async Task<IReadOnlyList<ResourceManagementDto>> ListResourcesAsync(CancellationToken ct)
    {
        if (ValidateTenantContext() != null)
        {
            return Array.Empty<ResourceManagementDto>();
        }

        return await _store.ListAsync(ct).ConfigureAwait(false);
    }

    public Task<ManagementResult<ResourceManagementDto>> ActivateResourceAsync(
        int resourceId,
        CancellationToken ct)
    {
        return SetActiveAsync(resourceId, true, ct);
    }

    public Task<ManagementResult<ResourceManagementDto>> DeactivateResourceAsync(
        int resourceId,
        CancellationToken ct)
    {
        return SetActiveAsync(resourceId, false, ct);
    }

    public async Task<ManagementResult<ResourceManagementDto>> ArchiveResourceAsync(
        int resourceId,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceManagementDto>.Failure(tenantError);
        }

        if (resourceId <= 0)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.id.required", ManagementErrorCategory.Validation, "ResourceId must be positive.", "resourceId"));
        }

        var existing = await _store.FindByIdAsync(resourceId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(resourceId);
        }

        if (existing.IsArchived)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.lifecycle.already-archived", ManagementErrorCategory.InvalidOperation, "Resource is already archived.", "resourceId"));
        }

        var archived = await _store.ArchiveAsync(resourceId, ct).ConfigureAwait(false);
        return ManagementResult<ResourceManagementDto>.Success(archived);
    }

    private async Task<ManagementResult<ResourceManagementDto>> SetActiveAsync(
        int resourceId,
        bool isActive,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceManagementDto>.Failure(tenantError);
        }

        if (resourceId <= 0)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.id.required", ManagementErrorCategory.Validation, "ResourceId must be positive.", "resourceId"));
        }

        var existing = await _store.FindByIdAsync(resourceId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(resourceId);
        }

        if (existing.IsArchived)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError("resource.lifecycle.archived", ManagementErrorCategory.InvalidOperation, "Archived resources cannot change activation state.", "resourceId"));
        }

        if (existing.IsActive == isActive)
        {
            return ManagementResult<ResourceManagementDto>.Failure(
                CreateError(
                    isActive ? "resource.lifecycle.already-active" : "resource.lifecycle.already-inactive",
                    ManagementErrorCategory.InvalidOperation,
                    isActive ? "Resource is already active." : "Resource is already inactive.",
                    "resourceId"));
        }

        if (isActive)
        {
            var resourceType = await _resourceTypeStore.FindByIdAsync(existing.TypeId, ct).ConfigureAwait(false);
            if (resourceType == null)
            {
                return ManagementResult<ResourceManagementDto>.Failure(
                    CreateError("resource.type.not-found", ManagementErrorCategory.NotFound, $"Resource type '{existing.TypeId}' was not found.", "typeId"));
            }

            if (!resourceType.IsActive)
            {
                return ManagementResult<ResourceManagementDto>.Failure(
                    CreateError("resource.type.inactive", ManagementErrorCategory.InvalidOperation, "Resource type is inactive.", "typeId"));
            }
        }

        var updated = await _store.SetActiveAsync(resourceId, isActive, ct).ConfigureAwait(false);
        return ManagementResult<ResourceManagementDto>.Success(updated);
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static List<ManagementError> ValidateResourcePayload(string? code, string name, int capacity, int typeId)
    {
        var errors = new List<ManagementError>();

        if (!string.IsNullOrWhiteSpace(code) && code.Trim().Length > ResourceCodeMaxLength)
        {
            errors.Add(CreateError("resource.code.too-long", ManagementErrorCategory.Validation, $"Resource code must be at most {ResourceCodeMaxLength} characters.", "code"));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(CreateError("resource.name.required", ManagementErrorCategory.Validation, "Resource name is required.", "name"));
        }
        else if (name.Trim().Length > ResourceNameMaxLength)
        {
            errors.Add(CreateError("resource.name.too-long", ManagementErrorCategory.Validation, $"Resource name must be at most {ResourceNameMaxLength} characters.", "name"));
        }

        if (capacity < 1)
        {
            errors.Add(CreateError("resource.capacity.invalid", ManagementErrorCategory.Validation, "Resource capacity must be at least 1.", "capacity"));
        }

        if (typeId <= 0)
        {
            errors.Add(CreateError("resource.type.required", ManagementErrorCategory.Validation, "TypeId must be positive.", "typeId"));
        }

        return errors;
    }

    private static string NormalizeName(string name) => name.Trim();

    private static string? NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim();
    }

    private static ManagementResult<ResourceManagementDto> NotFound(int resourceId)
    {
        return ManagementResult<ResourceManagementDto>.Failure(
            CreateError("resource.not-found", ManagementErrorCategory.NotFound, $"Resource '{resourceId}' was not found.", "resourceId"));
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
