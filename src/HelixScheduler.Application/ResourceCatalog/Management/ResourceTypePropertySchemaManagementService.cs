using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.ResourceCatalog.Management;

public sealed class ResourceTypePropertySchemaManagementService : IResourceTypePropertySchemaManagementService
{
    private readonly IResourceTypeManagementStore _resourceTypeStore;
    private readonly IResourceTypePropertySchemaManagementStore _store;
    private readonly ITenantContext _tenantContext;

    public ResourceTypePropertySchemaManagementService(
        IResourceTypeManagementStore resourceTypeStore,
        IResourceTypePropertySchemaManagementStore store,
        ITenantContext tenantContext)
    {
        _resourceTypeStore = resourceTypeStore ?? throw new ArgumentNullException(nameof(resourceTypeStore));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ManagementResult<ResourceTypePropertySchemaManagementDto>> AssignPropertyDefinitionsAsync(
        AssignPropertyDefinitionsToResourceTypeCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(tenantError);
        }

        var validationErrors = ValidateCommand(command.ResourceTypeId, command.PropertyDefinitionIds);
        if (validationErrors.Count > 0)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(validationErrors);
        }

        var resourceType = await _resourceTypeStore.FindByIdAsync(command.ResourceTypeId, ct).ConfigureAwait(false);
        if (resourceType == null)
        {
            return NotFound(command.ResourceTypeId);
        }

        var normalizedIds = command.PropertyDefinitionIds.Distinct().OrderBy(static id => id).ToArray();
        var definitions = await _store.GetPropertyDefinitionsAsync(normalizedIds, ct).ConfigureAwait(false);

        foreach (var propertyDefinitionId in normalizedIds)
        {
            var definition = definitions.FirstOrDefault(state => state.Id == propertyDefinitionId);
            if (definition == null)
            {
                return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(
                    CreateError(
                        "resource-type.property-definition.not-found",
                        ManagementErrorCategory.NotFound,
                        $"Property definition '{propertyDefinitionId}' was not found.",
                        "propertyDefinitionIds"));
            }

            if (!definition.IsActive)
            {
                return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(
                    CreateError(
                        "resource-type.property-definition.inactive",
                        ManagementErrorCategory.InvalidOperation,
                        $"Property definition '{propertyDefinitionId}' is inactive.",
                        "propertyDefinitionIds"));
            }
        }

        var currentIds = await _store.ListAssignedPropertyDefinitionIdsAsync(command.ResourceTypeId, ct).ConfigureAwait(false);
        var duplicateId = normalizedIds.FirstOrDefault(currentIds.Contains);
        if (duplicateId != 0)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(
                CreateError(
                    "resource-type.property-definition.duplicate",
                    ManagementErrorCategory.Conflict,
                    $"Property definition '{duplicateId}' is already assigned to the resource type.",
                    "propertyDefinitionIds"));
        }

        await _store.AddAssignmentsAsync(_tenantContext.TenantId, command.ResourceTypeId, normalizedIds, ct).ConfigureAwait(false);
        return await GetPropertyDefinitionsAsync(command.ResourceTypeId, ct).ConfigureAwait(false);
    }

    public async Task<ManagementResult<ResourceTypePropertySchemaManagementDto>> RemovePropertyDefinitionsAsync(
        RemovePropertyDefinitionsFromResourceTypeCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(tenantError);
        }

        var validationErrors = ValidateCommand(command.ResourceTypeId, command.PropertyDefinitionIds);
        if (validationErrors.Count > 0)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(validationErrors);
        }

        var resourceType = await _resourceTypeStore.FindByIdAsync(command.ResourceTypeId, ct).ConfigureAwait(false);
        if (resourceType == null)
        {
            return NotFound(command.ResourceTypeId);
        }

        var normalizedIds = command.PropertyDefinitionIds.Distinct().OrderBy(static id => id).ToArray();
        var currentIds = await _store.ListAssignedPropertyDefinitionIdsAsync(command.ResourceTypeId, ct).ConfigureAwait(false);
        var missingId = normalizedIds.FirstOrDefault(id => !currentIds.Contains(id));
        if (missingId != 0)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(
                CreateError(
                    "resource-type.property-definition.not-assigned",
                    ManagementErrorCategory.InvalidOperation,
                    $"Property definition '{missingId}' is not assigned to the resource type.",
                    "propertyDefinitionIds"));
        }

        await _store.RemoveAssignmentsAsync(command.ResourceTypeId, normalizedIds, ct).ConfigureAwait(false);
        return await GetPropertyDefinitionsAsync(command.ResourceTypeId, ct).ConfigureAwait(false);
    }

    public async Task<ManagementResult<ResourceTypePropertySchemaManagementDto>> GetPropertyDefinitionsAsync(
        int resourceTypeId,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(tenantError);
        }

        if (resourceTypeId <= 0)
        {
            return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(
                CreateError("resource-type.id.required", ManagementErrorCategory.Validation, "ResourceTypeId must be positive.", "resourceTypeId"));
        }

        var resourceType = await _resourceTypeStore.FindByIdAsync(resourceTypeId, ct).ConfigureAwait(false);
        if (resourceType == null)
        {
            return NotFound(resourceTypeId);
        }

        var propertyDefinitionIds = await _store.ListAssignedPropertyDefinitionIdsAsync(resourceTypeId, ct).ConfigureAwait(false);
        return ManagementResult<ResourceTypePropertySchemaManagementDto>.Success(
            new ResourceTypePropertySchemaManagementDto(resourceTypeId, propertyDefinitionIds));
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static List<ManagementError> ValidateCommand(int resourceTypeId, IReadOnlyList<int> propertyDefinitionIds)
    {
        var errors = new List<ManagementError>();

        if (resourceTypeId <= 0)
        {
            errors.Add(CreateError("resource-type.id.required", ManagementErrorCategory.Validation, "ResourceTypeId must be positive.", "resourceTypeId"));
        }

        if (propertyDefinitionIds.Count == 0)
        {
            errors.Add(CreateError(
                "resource-type.property-definition.required",
                ManagementErrorCategory.Validation,
                "At least one property definition id is required.",
                "propertyDefinitionIds"));
            return errors;
        }

        if (propertyDefinitionIds.Any(static id => id <= 0))
        {
            errors.Add(CreateError(
                "resource-type.property-definition.invalid-id",
                ManagementErrorCategory.Validation,
                "Property definition ids must be positive.",
                "propertyDefinitionIds"));
        }

        if (propertyDefinitionIds.Count != propertyDefinitionIds.Distinct().Count())
        {
            errors.Add(CreateError(
                "resource-type.property-definition.duplicate-id",
                ManagementErrorCategory.Validation,
                "Property definition ids must be unique within the command.",
                "propertyDefinitionIds"));
        }

        return errors;
    }

    private static ManagementResult<ResourceTypePropertySchemaManagementDto> NotFound(int resourceTypeId)
    {
        return ManagementResult<ResourceTypePropertySchemaManagementDto>.Failure(
            CreateError("resource-type.not-found", ManagementErrorCategory.NotFound, $"Resource type '{resourceTypeId}' was not found.", "resourceTypeId"));
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
