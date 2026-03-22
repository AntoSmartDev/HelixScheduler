using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.Management.ResourceCatalog;

namespace HelixScheduler.Application.Management.Properties;

public sealed class ResourcePropertyAssignmentManagementService : IResourcePropertyAssignmentManagementService
{
    private readonly IResourcePropertyAssignmentManagementStore _assignmentStore;
    private readonly IPropertyManagementStore _propertyStore;
    private readonly IResourceManagementStore _resourceStore;
    private readonly IPropertySchemaService _propertySchemaService;
    private readonly ITenantContext _tenantContext;

    public ResourcePropertyAssignmentManagementService(
        IResourcePropertyAssignmentManagementStore assignmentStore,
        IPropertyManagementStore propertyStore,
        IResourceManagementStore resourceStore,
        IPropertySchemaService propertySchemaService,
        ITenantContext tenantContext)
    {
        _assignmentStore = assignmentStore ?? throw new ArgumentNullException(nameof(assignmentStore));
        _propertyStore = propertyStore ?? throw new ArgumentNullException(nameof(propertyStore));
        _resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
        _propertySchemaService = propertySchemaService ?? throw new ArgumentNullException(nameof(propertySchemaService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ManagementResult<ResourcePropertyAssignmentsDto>> AssignPropertiesToResourceAsync(
        AssignPropertiesToResourceCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(tenantError);
        }

        var propertyIds = NormalizePropertyIds(command.PropertyIds);
        var errors = ValidateCommand(command.ResourceId, propertyIds);
        if (errors.Count > 0)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(errors);
        }

        var resource = await _resourceStore.FindByIdAsync(command.ResourceId, ct).ConfigureAwait(false);
        if (resource == null)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                CreateError("resource.not-found", ManagementErrorCategory.NotFound, $"Resource '{command.ResourceId}' was not found.", "resourceId"));
        }

        if (!resource.IsActive || resource.IsArchived)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                CreateError("resource.inactive", ManagementErrorCategory.InvalidOperation, "Resource must be active and not archived.", "resourceId"));
        }

        var properties = await _propertyStore.FindByIdsAsync(propertyIds, ct).ConfigureAwait(false);
        if (properties.Count != propertyIds.Count)
        {
            var missingId = propertyIds.First(id => properties.All(property => property.Id != id));
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                CreateError("property.not-found", ManagementErrorCategory.NotFound, $"Property '{missingId}' was not found.", "propertyIds"));
        }

        for (var i = 0; i < properties.Count; i++)
        {
            if (!properties[i].IsActive)
            {
                return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                    CreateError("property.inactive", ManagementErrorCategory.InvalidOperation, $"Property '{properties[i].Id}' is inactive.", "propertyIds"));
            }
        }

        for (var i = 0; i < propertyIds.Count; i++)
        {
            var exists = await _assignmentStore.AssignmentExistsAsync(command.ResourceId, propertyIds[i], ct).ConfigureAwait(false);
            if (exists)
            {
                return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                    CreateError("property.assignment.already-exists", ManagementErrorCategory.Conflict, "Resource-property assignment already exists.", "propertyIds"));
            }
        }

        try
        {
            await _propertySchemaService.ValidatePropertyFiltersForTypeAsync(resource.TypeId, propertyIds, ct).ConfigureAwait(false);
        }
        catch (AvailabilityRequestException ex)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                CreateError("property.assignment.type-incompatibility", ManagementErrorCategory.Validation, ex.Message, "propertyIds"));
        }

        await _assignmentStore.AddAssignmentsAsync(_tenantContext.TenantId, command.ResourceId, propertyIds, ct).ConfigureAwait(false);
        var assigned = await _assignmentStore.ListAssignedPropertiesAsync(command.ResourceId, ct).ConfigureAwait(false);
        return ManagementResult<ResourcePropertyAssignmentsDto>.Success(new ResourcePropertyAssignmentsDto(command.ResourceId, assigned));
    }

    public async Task<ManagementResult<ResourcePropertyAssignmentsDto>> RemovePropertiesFromResourceAsync(
        RemovePropertiesFromResourceCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(tenantError);
        }

        var propertyIds = NormalizePropertyIds(command.PropertyIds);
        var errors = ValidateCommand(command.ResourceId, propertyIds);
        if (errors.Count > 0)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(errors);
        }

        var resource = await _resourceStore.FindByIdAsync(command.ResourceId, ct).ConfigureAwait(false);
        if (resource == null)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                CreateError("resource.not-found", ManagementErrorCategory.NotFound, $"Resource '{command.ResourceId}' was not found.", "resourceId"));
        }

        for (var i = 0; i < propertyIds.Count; i++)
        {
            var exists = await _assignmentStore.AssignmentExistsAsync(command.ResourceId, propertyIds[i], ct).ConfigureAwait(false);
            if (!exists)
            {
                return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                    CreateError("property.assignment.not-found", ManagementErrorCategory.NotFound, "Resource-property assignment was not found.", "propertyIds"));
            }
        }

        await _assignmentStore.RemoveAssignmentsAsync(command.ResourceId, propertyIds, ct).ConfigureAwait(false);
        var assigned = await _assignmentStore.ListAssignedPropertiesAsync(command.ResourceId, ct).ConfigureAwait(false);
        return ManagementResult<ResourcePropertyAssignmentsDto>.Success(new ResourcePropertyAssignmentsDto(command.ResourceId, assigned));
    }

    public async Task<ManagementResult<ResourcePropertyAssignmentsDto>> GetResourcePropertiesAsync(
        int resourceId,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(tenantError);
        }

        if (resourceId <= 0)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                CreateError("resource.id.required", ManagementErrorCategory.Validation, "ResourceId must be positive.", "resourceId"));
        }

        var resource = await _resourceStore.FindByIdAsync(resourceId, ct).ConfigureAwait(false);
        if (resource == null)
        {
            return ManagementResult<ResourcePropertyAssignmentsDto>.Failure(
                CreateError("resource.not-found", ManagementErrorCategory.NotFound, $"Resource '{resourceId}' was not found.", "resourceId"));
        }

        var assigned = await _assignmentStore.ListAssignedPropertiesAsync(resourceId, ct).ConfigureAwait(false);
        return ManagementResult<ResourcePropertyAssignmentsDto>.Success(new ResourcePropertyAssignmentsDto(resourceId, assigned));
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static List<int> NormalizePropertyIds(IReadOnlyList<int> propertyIds)
    {
        return propertyIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static List<ManagementError> ValidateCommand(int resourceId, IReadOnlyList<int> propertyIds)
    {
        var errors = new List<ManagementError>();

        if (resourceId <= 0)
        {
            errors.Add(CreateError("resource.id.required", ManagementErrorCategory.Validation, "ResourceId must be positive.", "resourceId"));
        }

        if (propertyIds.Count == 0)
        {
            errors.Add(CreateError("property.assignment.ids.required", ManagementErrorCategory.Validation, "At least one PropertyId is required.", "propertyIds"));
        }

        return errors;
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
