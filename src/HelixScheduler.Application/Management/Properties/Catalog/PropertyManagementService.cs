using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Properties;

public sealed class PropertyManagementService : IPropertyManagementService
{
    private const int PropertyKeyMaxLength = 100;
    private const int PropertyLabelMaxLength = 200;

    private readonly IPropertyManagementStore _store;
    private readonly ITenantContext _tenantContext;

    public PropertyManagementService(
        IPropertyManagementStore store,
        ITenantContext tenantContext)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ManagementResult<PropertyManagementDto>> CreatePropertyAsync(
        CreatePropertyCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<PropertyManagementDto>.Failure(tenantError);
        }

        var errors = ValidatePropertyPayload(command.Key, command.Label);
        if (errors.Count > 0)
        {
            return ManagementResult<PropertyManagementDto>.Failure(errors);
        }

        var normalizedKey = NormalizeKey(command.Key);
        var existing = await _store.FindByKeyAsync(normalizedKey, ct).ConfigureAwait(false);
        if (existing != null)
        {
            return ManagementResult<PropertyManagementDto>.Failure(
                CreateError("property.key.duplicate", ManagementErrorCategory.Conflict, "Property key already exists.", "key"));
        }

        var created = await _store.CreateAsync(
            _tenantContext.TenantId,
            normalizedKey,
            NormalizeLabel(command.Label),
            command.SortOrder,
            ct).ConfigureAwait(false);

        return ManagementResult<PropertyManagementDto>.Success(created);
    }

    public async Task<ManagementResult<PropertyManagementDto>> UpdatePropertyAsync(
        UpdatePropertyCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<PropertyManagementDto>.Failure(tenantError);
        }

        var errors = ValidatePropertyPayload(command.Key, command.Label);
        if (command.PropertyId <= 0)
        {
            errors.Add(CreateError("property.id.required", ManagementErrorCategory.Validation, "PropertyId must be positive.", "propertyId"));
        }

        if (errors.Count > 0)
        {
            return ManagementResult<PropertyManagementDto>.Failure(errors);
        }

        var existing = await _store.FindByIdAsync(command.PropertyId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(command.PropertyId);
        }

        var normalizedKey = NormalizeKey(command.Key);
        var byKey = await _store.FindByKeyAsync(normalizedKey, ct).ConfigureAwait(false);
        if (byKey != null && byKey.Id != command.PropertyId)
        {
            return ManagementResult<PropertyManagementDto>.Failure(
                CreateError("property.key.duplicate", ManagementErrorCategory.Conflict, "Property key already exists.", "key"));
        }

        var updated = await _store.UpdateAsync(
            command.PropertyId,
            normalizedKey,
            NormalizeLabel(command.Label),
            command.SortOrder,
            ct).ConfigureAwait(false);

        return ManagementResult<PropertyManagementDto>.Success(updated);
    }

    public async Task<ManagementResult<PropertyManagementDto>> GetPropertyAsync(
        int propertyId,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<PropertyManagementDto>.Failure(tenantError);
        }

        if (propertyId <= 0)
        {
            return ManagementResult<PropertyManagementDto>.Failure(
                CreateError("property.id.required", ManagementErrorCategory.Validation, "PropertyId must be positive.", "propertyId"));
        }

        var existing = await _store.FindByIdAsync(propertyId, ct).ConfigureAwait(false);
        return existing == null
            ? NotFound(propertyId)
            : ManagementResult<PropertyManagementDto>.Success(existing);
    }

    public async Task<IReadOnlyList<PropertyManagementDto>> ListPropertiesAsync(CancellationToken ct)
    {
        if (ValidateTenantContext() != null)
        {
            return Array.Empty<PropertyManagementDto>();
        }

        return await _store.ListAsync(ct).ConfigureAwait(false);
    }

    public Task<ManagementResult<PropertyManagementDto>> ActivatePropertyAsync(
        int propertyId,
        CancellationToken ct)
    {
        return SetActiveAsync(propertyId, true, ct);
    }

    public Task<ManagementResult<PropertyManagementDto>> DeactivatePropertyAsync(
        int propertyId,
        CancellationToken ct)
    {
        return SetActiveAsync(propertyId, false, ct);
    }

    public async Task<ManagementResult<PropertyHierarchyRelationDto>> AddPropertyParentRelationAsync(
        AddPropertyParentRelationCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(tenantError);
        }

        var errors = ValidateRelationCommand(command.ParentPropertyId, command.ChildPropertyId);
        if (errors.Count > 0)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(errors);
        }

        var parent = await _store.FindByIdAsync(command.ParentPropertyId, ct).ConfigureAwait(false);
        if (parent == null)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(
                CreateError("property.parent.not-found", ManagementErrorCategory.NotFound, $"Parent property '{command.ParentPropertyId}' was not found.", "parentPropertyId"));
        }

        var child = await _store.FindByIdAsync(command.ChildPropertyId, ct).ConfigureAwait(false);
        if (child == null)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(
                CreateError("property.child.not-found", ManagementErrorCategory.NotFound, $"Child property '{command.ChildPropertyId}' was not found.", "childPropertyId"));
        }

        if (!parent.IsActive)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(
                CreateError("property.parent.inactive", ManagementErrorCategory.InvalidOperation, "Parent property must be active.", "parentPropertyId"));
        }

        if (!child.IsActive)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(
                CreateError("property.child.inactive", ManagementErrorCategory.InvalidOperation, "Child property must be active.", "childPropertyId"));
        }

        var exists = await _store.RelationExistsAsync(command.ParentPropertyId, command.ChildPropertyId, ct).ConfigureAwait(false);
        if (exists)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(
                CreateError("property.relation.already-exists", ManagementErrorCategory.Conflict, "Property parent relation already exists.", "relation"));
        }

        var relations = await _store.GetRelationsAsync(ct).ConfigureAwait(false);
        if (WouldIntroduceCycle(command.ParentPropertyId, command.ChildPropertyId, relations))
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(
                CreateError("property.tree.cycle-detected", ManagementErrorCategory.InvalidOperation, "Property relation would introduce a cycle.", "relation"));
        }

        var added = await _store.AddParentRelationAsync(command.ParentPropertyId, command.ChildPropertyId, ct).ConfigureAwait(false);
        return ManagementResult<PropertyHierarchyRelationDto>.Success(added);
    }

    public async Task<ManagementResult<PropertyHierarchyRelationDto>> RemovePropertyParentRelationAsync(
        RemovePropertyParentRelationCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(tenantError);
        }

        var errors = ValidateRelationCommand(command.ParentPropertyId, command.ChildPropertyId);
        if (errors.Count > 0)
        {
            return ManagementResult<PropertyHierarchyRelationDto>.Failure(errors);
        }

        var removed = await _store.RemoveParentRelationAsync(command.ParentPropertyId, command.ChildPropertyId, ct).ConfigureAwait(false);
        return removed == null
            ? ManagementResult<PropertyHierarchyRelationDto>.Failure(
                CreateError("property.relation.not-found", ManagementErrorCategory.NotFound, "Property parent relation was not found.", "relation"))
            : ManagementResult<PropertyHierarchyRelationDto>.Success(removed);
    }

    public async Task<IReadOnlyList<PropertyHierarchyRelationDto>> GetPropertyRelationsAsync(CancellationToken ct)
    {
        if (ValidateTenantContext() != null)
        {
            return Array.Empty<PropertyHierarchyRelationDto>();
        }

        return await _store.ListRelationsAsync(ct).ConfigureAwait(false);
    }

    private async Task<ManagementResult<PropertyManagementDto>> SetActiveAsync(
        int propertyId,
        bool isActive,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<PropertyManagementDto>.Failure(tenantError);
        }

        if (propertyId <= 0)
        {
            return ManagementResult<PropertyManagementDto>.Failure(
                CreateError("property.id.required", ManagementErrorCategory.Validation, "PropertyId must be positive.", "propertyId"));
        }

        var existing = await _store.FindByIdAsync(propertyId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(propertyId);
        }

        if (existing.IsActive == isActive)
        {
            return ManagementResult<PropertyManagementDto>.Failure(
                CreateError(
                    isActive ? "property.lifecycle.already-active" : "property.lifecycle.already-inactive",
                    ManagementErrorCategory.InvalidOperation,
                    isActive ? "Property is already active." : "Property is already inactive.",
                    "propertyId"));
        }

        if (!isActive)
        {
            if (await _store.HasChildPropertiesAsync(propertyId, ct).ConfigureAwait(false))
            {
                return ManagementResult<PropertyManagementDto>.Failure(
                    CreateError("property.lifecycle.child-properties-exist", ManagementErrorCategory.InvalidOperation, "Property cannot be deactivated while child properties still reference it.", "propertyId"));
            }

            if (await _store.HasTypeMappingsAsync(propertyId, ct).ConfigureAwait(false))
            {
                return ManagementResult<PropertyManagementDto>.Failure(
                    CreateError("property.lifecycle.type-mappings-exist", ManagementErrorCategory.InvalidOperation, "Property cannot be deactivated while resource type mappings still reference it.", "propertyId"));
            }

            if (await _store.HasResourceAssignmentsAsync(propertyId, ct).ConfigureAwait(false))
            {
                return ManagementResult<PropertyManagementDto>.Failure(
                    CreateError("property.lifecycle.resource-assignments-exist", ManagementErrorCategory.InvalidOperation, "Property cannot be deactivated while resource assignments still reference it.", "propertyId"));
            }
        }

        var updated = await _store.SetActiveAsync(propertyId, isActive, ct).ConfigureAwait(false);
        return ManagementResult<PropertyManagementDto>.Success(updated);
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static List<ManagementError> ValidatePropertyPayload(string key, string label)
    {
        var errors = new List<ManagementError>();

        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add(CreateError("property.key.required", ManagementErrorCategory.Validation, "Property key is required.", "key"));
        }
        else if (key.Trim().Length > PropertyKeyMaxLength)
        {
            errors.Add(CreateError("property.key.too-long", ManagementErrorCategory.Validation, $"Property key must be at most {PropertyKeyMaxLength} characters.", "key"));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add(CreateError("property.label.required", ManagementErrorCategory.Validation, "Property label is required.", "label"));
        }
        else if (label.Trim().Length > PropertyLabelMaxLength)
        {
            errors.Add(CreateError("property.label.too-long", ManagementErrorCategory.Validation, $"Property label must be at most {PropertyLabelMaxLength} characters.", "label"));
        }

        return errors;
    }

    private static List<ManagementError> ValidateRelationCommand(int parentPropertyId, int childPropertyId)
    {
        var errors = new List<ManagementError>();

        if (parentPropertyId <= 0)
        {
            errors.Add(CreateError("property.parent.required", ManagementErrorCategory.Validation, "ParentPropertyId must be positive.", "parentPropertyId"));
        }

        if (childPropertyId <= 0)
        {
            errors.Add(CreateError("property.child.required", ManagementErrorCategory.Validation, "ChildPropertyId must be positive.", "childPropertyId"));
        }

        if (parentPropertyId > 0 && childPropertyId > 0 && parentPropertyId == childPropertyId)
        {
            errors.Add(CreateError("property.tree.self-parent-not-allowed", ManagementErrorCategory.Validation, "A property cannot be parent of itself.", "relation"));
        }

        return errors;
    }

    private static bool WouldIntroduceCycle(
        int parentPropertyId,
        int childPropertyId,
        IReadOnlyList<PropertyHierarchyRelationDto> relations)
    {
        var parentsByChild = new Dictionary<int, List<int>>();
        for (var i = 0; i < relations.Count; i++)
        {
            var relation = relations[i];
            if (!parentsByChild.TryGetValue(relation.ChildPropertyId, out var parents))
            {
                parents = new List<int>();
                parentsByChild[relation.ChildPropertyId] = parents;
            }

            parents.Add(relation.ParentPropertyId);
        }

        if (!parentsByChild.TryGetValue(childPropertyId, out var newParents))
        {
            newParents = new List<int>();
            parentsByChild[childPropertyId] = newParents;
        }

        newParents.Add(parentPropertyId);

        var toVisit = new Stack<int>();
        var visited = new HashSet<int>();
        toVisit.Push(parentPropertyId);

        while (toVisit.Count > 0)
        {
            var current = toVisit.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (!parentsByChild.TryGetValue(current, out var parents))
            {
                continue;
            }

            for (var i = 0; i < parents.Count; i++)
            {
                var next = parents[i];
                if (next == childPropertyId)
                {
                    return true;
                }

                toVisit.Push(next);
            }
        }

        return false;
    }

    private static string NormalizeKey(string key) => key.Trim();
    private static string NormalizeLabel(string label) => label.Trim();

    private static ManagementResult<PropertyManagementDto> NotFound(int propertyId)
    {
        return ManagementResult<PropertyManagementDto>.Failure(
            CreateError("property.not-found", ManagementErrorCategory.NotFound, $"Property '{propertyId}' was not found.", "propertyId"));
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
