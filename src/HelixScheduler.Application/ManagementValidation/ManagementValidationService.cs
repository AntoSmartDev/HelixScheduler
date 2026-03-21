using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.PropertySchema;

namespace HelixScheduler.Application.ManagementValidation;

public sealed class ManagementValidationService : IManagementValidationService
{
    private readonly IManagementValidationStore _store;
    private readonly IPropertySchemaService _propertySchemaService;
    private readonly ITenantContext _tenantContext;

    public ManagementValidationService(
        IManagementValidationStore store,
        IPropertySchemaService propertySchemaService,
        ITenantContext tenantContext)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _propertySchemaService = propertySchemaService ?? throw new ArgumentNullException(nameof(propertySchemaService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ManagementValidationResult> ValidateTenantModelAsync(CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return Invalid(tenantError);
        }

        var snapshot = await _store.LoadTenantSnapshotAsync(ct).ConfigureAwait(false);
        var findings = new List<ManagementError>();

        ValidateResources(snapshot, findings);
        ValidateResourceRelations(snapshot, findings);
        ValidateProperties(snapshot, findings);
        ValidateRuleBindings(snapshot, findings);
        ValidateBusyBindings(snapshot, findings);
        await ValidatePropertyAssignmentsCompatibilityAsync(snapshot, findings, ct).ConfigureAwait(false);

        return Complete(findings);
    }

    public async Task<ManagementValidationResult> ValidateResourceConfigurationAsync(int resourceId, CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return Invalid(tenantError);
        }

        if (resourceId <= 0)
        {
            return Invalid(CreateError("resource.id.required", ManagementErrorCategory.Validation, "ResourceId must be positive.", "resourceId"));
        }

        var snapshot = await _store.LoadTenantSnapshotAsync(ct).ConfigureAwait(false);
        var findings = new List<ManagementError>();
        var resource = snapshot.Resources.FirstOrDefault(item => item.Id == resourceId);
        if (resource == null)
        {
            return Invalid(CreateError("resource.not-found", ManagementErrorCategory.NotFound, $"Resource '{resourceId}' was not found.", "resourceId"));
        }

        var type = snapshot.ResourceTypes.FirstOrDefault(item => item.Id == resource.TypeId);
        if (type == null)
        {
            findings.Add(CreateError("validation.resource.type-missing", ManagementErrorCategory.NotFound, $"Resource '{resourceId}' references missing type '{resource.TypeId}'.", "resource.typeId"));
        }
        else if (!type.IsActive && resource.IsActive && !resource.IsArchived)
        {
            findings.Add(CreateError("validation.resource.type-inactive", ManagementErrorCategory.InvalidOperation, $"Active resource '{resourceId}' references inactive type '{resource.TypeId}'.", "resource.typeId"));
        }

        if (resource.IsActive && resource.IsArchived)
        {
            findings.Add(CreateError("validation.resource.lifecycle-conflict", ManagementErrorCategory.InvalidOperation, $"Resource '{resourceId}' cannot be active and archived at the same time.", "resource.lifecycle"));
        }

        foreach (var relation in snapshot.ResourceRelations.Where(item => item.ParentResourceId == resourceId || item.ChildResourceId == resourceId))
        {
            var otherId = relation.ParentResourceId == resourceId ? relation.ChildResourceId : relation.ParentResourceId;
            var other = snapshot.Resources.FirstOrDefault(item => item.Id == otherId);
            if (other == null)
            {
                findings.Add(CreateError("validation.resource.relation-resource-missing", ManagementErrorCategory.NotFound, $"Resource relation references missing resource '{otherId}'.", "resource.relations"));
                continue;
            }

            if (!other.IsActive || other.IsArchived)
            {
                findings.Add(CreateError("validation.resource.relation-resource-inactive", ManagementErrorCategory.InvalidOperation, $"Resource '{resourceId}' is linked to inactive or archived resource '{otherId}'.", "resource.relations"));
            }
        }

        foreach (var binding in snapshot.RuleResourceBindings.Where(item => item.ResourceId == resourceId && item.RuleIsActive))
        {
            if (!resource.IsActive || resource.IsArchived)
            {
                findings.Add(CreateError("validation.resource.active-rule-on-inactive-resource", ManagementErrorCategory.InvalidOperation, $"Active rule '{binding.RuleId}' targets inactive or archived resource '{resourceId}'.", "resource.rules"));
            }
        }

        foreach (var binding in snapshot.BusyEventResourceBindings.Where(item => item.ResourceId == resourceId && item.BusyEventIsActive))
        {
            if (!resource.IsActive || resource.IsArchived)
            {
                findings.Add(CreateError("validation.resource.active-busy-on-inactive-resource", ManagementErrorCategory.InvalidOperation, $"Active busy event '{binding.BusyEventId}' targets inactive or archived resource '{resourceId}'.", "resource.busyEvents"));
            }
        }

        var assignedPropertyIds = snapshot.ResourcePropertyAssignments
            .Where(item => item.ResourceId == resourceId)
            .Select(item => item.PropertyId)
            .Distinct()
            .ToList();

        foreach (var propertyId in assignedPropertyIds)
        {
            var property = snapshot.Properties.FirstOrDefault(item => item.Id == propertyId);
            if (property == null)
            {
                findings.Add(CreateError("validation.resource.assigned-property-missing", ManagementErrorCategory.NotFound, $"Resource '{resourceId}' references missing property '{propertyId}'.", "resource.properties"));
            }
            else if (!property.IsActive)
            {
                findings.Add(CreateError("validation.resource.assigned-property-inactive", ManagementErrorCategory.InvalidOperation, $"Resource '{resourceId}' references inactive property '{propertyId}'.", "resource.properties"));
            }
        }

        if (assignedPropertyIds.Count > 0)
        {
            try
            {
                await _propertySchemaService.ValidatePropertyFiltersForTypeAsync(resource.TypeId, assignedPropertyIds, ct).ConfigureAwait(false);
            }
            catch (AvailabilityRequestException ex)
            {
                findings.Add(CreateError("validation.resource.property-type-incompatibility", ManagementErrorCategory.Validation, ex.Message, "resource.properties"));
            }
        }

        return Complete(findings);
    }

    private static void ValidateResources(TenantValidationSnapshot snapshot, List<ManagementError> findings)
    {
        for (var i = 0; i < snapshot.Resources.Count; i++)
        {
            var resource = snapshot.Resources[i];
            var type = snapshot.ResourceTypes.FirstOrDefault(item => item.Id == resource.TypeId);
            if (type == null)
            {
                findings.Add(CreateError("validation.resource.type-missing", ManagementErrorCategory.NotFound, $"Resource '{resource.Id}' references missing type '{resource.TypeId}'.", "resources"));
                continue;
            }

            if (resource.IsActive && !resource.IsArchived && !type.IsActive)
            {
                findings.Add(CreateError("validation.resource.type-inactive", ManagementErrorCategory.InvalidOperation, $"Active resource '{resource.Id}' references inactive type '{resource.TypeId}'.", "resources"));
            }

            if (resource.IsActive && resource.IsArchived)
            {
                findings.Add(CreateError("validation.resource.lifecycle-conflict", ManagementErrorCategory.InvalidOperation, $"Resource '{resource.Id}' cannot be active and archived at the same time.", "resources"));
            }
        }
    }

    private static void ValidateResourceRelations(TenantValidationSnapshot snapshot, List<ManagementError> findings)
    {
        for (var i = 0; i < snapshot.ResourceRelations.Count; i++)
        {
            var relation = snapshot.ResourceRelations[i];
            var parent = snapshot.Resources.FirstOrDefault(item => item.Id == relation.ParentResourceId);
            var child = snapshot.Resources.FirstOrDefault(item => item.Id == relation.ChildResourceId);

            if (parent == null || child == null)
            {
                findings.Add(CreateError("validation.hierarchy.resource-missing", ManagementErrorCategory.NotFound, $"Hierarchy relation '{relation.ParentResourceId}->{relation.ChildResourceId}' references a missing resource.", "hierarchy"));
                continue;
            }

            if (!parent.IsActive || parent.IsArchived || !child.IsActive || child.IsArchived)
            {
                findings.Add(CreateError("validation.hierarchy.resource-inactive", ManagementErrorCategory.InvalidOperation, $"Hierarchy relation '{relation.ParentResourceId}->{relation.ChildResourceId}' references inactive or archived resources.", "hierarchy"));
            }
        }

        foreach (var group in snapshot.ResourceRelations.GroupBy(item => item.RelationType))
        {
            if (HasCycle(group.Select(item => (item.ParentResourceId, item.ChildResourceId))))
            {
                findings.Add(CreateError("validation.hierarchy.cycle-detected", ManagementErrorCategory.InvalidOperation, $"Hierarchy relation type '{group.Key}' contains a cycle.", "hierarchy"));
            }
        }
    }

    private static void ValidateProperties(TenantValidationSnapshot snapshot, List<ManagementError> findings)
    {
        for (var i = 0; i < snapshot.Properties.Count; i++)
        {
            var property = snapshot.Properties[i];
            if (property.ParentId == null)
            {
                continue;
            }

            var parent = snapshot.Properties.FirstOrDefault(item => item.Id == property.ParentId.Value);
            if (parent == null)
            {
                findings.Add(CreateError("validation.property.parent-missing", ManagementErrorCategory.NotFound, $"Property '{property.Id}' references missing parent '{property.ParentId.Value}'.", "properties"));
                continue;
            }

            if (property.IsActive && !parent.IsActive)
            {
                findings.Add(CreateError("validation.property.active-child-inactive-parent", ManagementErrorCategory.InvalidOperation, $"Active property '{property.Id}' references inactive parent '{parent.Id}'.", "properties"));
            }
        }

        if (HasCycle(snapshot.Properties.Where(item => item.ParentId != null).Select(item => (item.ParentId!.Value, item.Id))))
        {
            findings.Add(CreateError("validation.property.cycle-detected", ManagementErrorCategory.InvalidOperation, "Property tree contains a cycle.", "properties"));
        }
    }

    private static void ValidateRuleBindings(TenantValidationSnapshot snapshot, List<ManagementError> findings)
    {
        for (var i = 0; i < snapshot.RuleResourceBindings.Count; i++)
        {
            var binding = snapshot.RuleResourceBindings[i];
            if (!binding.RuleIsActive)
            {
                continue;
            }

            var resource = snapshot.Resources.FirstOrDefault(item => item.Id == binding.ResourceId);
            if (resource == null)
            {
                findings.Add(CreateError("validation.rule.resource-missing", ManagementErrorCategory.NotFound, $"Active rule '{binding.RuleId}' references missing resource '{binding.ResourceId}'.", "rules"));
                continue;
            }

            if (!resource.IsActive || resource.IsArchived)
            {
                findings.Add(CreateError("validation.rule.resource-inactive", ManagementErrorCategory.InvalidOperation, $"Active rule '{binding.RuleId}' references inactive or archived resource '{binding.ResourceId}'.", "rules"));
            }
        }
    }

    private static void ValidateBusyBindings(TenantValidationSnapshot snapshot, List<ManagementError> findings)
    {
        for (var i = 0; i < snapshot.BusyEventResourceBindings.Count; i++)
        {
            var binding = snapshot.BusyEventResourceBindings[i];
            if (!binding.BusyEventIsActive)
            {
                continue;
            }

            var resource = snapshot.Resources.FirstOrDefault(item => item.Id == binding.ResourceId);
            if (resource == null)
            {
                findings.Add(CreateError("validation.busy-event.resource-missing", ManagementErrorCategory.NotFound, $"Active busy event '{binding.BusyEventId}' references missing resource '{binding.ResourceId}'.", "busyEvents"));
                continue;
            }

            if (!resource.IsActive || resource.IsArchived)
            {
                findings.Add(CreateError("validation.busy-event.resource-inactive", ManagementErrorCategory.InvalidOperation, $"Active busy event '{binding.BusyEventId}' references inactive or archived resource '{binding.ResourceId}'.", "busyEvents"));
            }
        }
    }

    private async Task ValidatePropertyAssignmentsCompatibilityAsync(
        TenantValidationSnapshot snapshot,
        List<ManagementError> findings,
        CancellationToken ct)
    {
        var propertyMap = snapshot.Properties.ToDictionary(item => item.Id, item => item);

        foreach (var resource in snapshot.Resources)
        {
            var propertyIds = snapshot.ResourcePropertyAssignments
                .Where(item => item.ResourceId == resource.Id)
                .Select(item => item.PropertyId)
                .Distinct()
                .ToList();

            for (var i = 0; i < propertyIds.Count; i++)
            {
                if (!propertyMap.TryGetValue(propertyIds[i], out var property))
                {
                    findings.Add(CreateError("validation.resource.assigned-property-missing", ManagementErrorCategory.NotFound, $"Resource '{resource.Id}' references missing property '{propertyIds[i]}'.", "resourceProperties"));
                    continue;
                }

                if (!property.IsActive)
                {
                    findings.Add(CreateError("validation.resource.assigned-property-inactive", ManagementErrorCategory.InvalidOperation, $"Resource '{resource.Id}' references inactive property '{property.Id}'.", "resourceProperties"));
                }
            }

            if (propertyIds.Count == 0)
            {
                continue;
            }

            try
            {
                await _propertySchemaService.ValidatePropertyFiltersForTypeAsync(resource.TypeId, propertyIds, ct).ConfigureAwait(false);
            }
            catch (AvailabilityRequestException ex)
            {
                findings.Add(CreateError("validation.resource.property-type-incompatibility", ManagementErrorCategory.Validation, ex.Message, $"resource:{resource.Id}.properties"));
            }
        }
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static bool HasCycle(IEnumerable<(int ParentId, int ChildId)> edges)
    {
        var childrenByParent = new Dictionary<int, List<int>>();
        var nodes = new HashSet<int>();

        foreach (var (parentId, childId) in edges)
        {
            nodes.Add(parentId);
            nodes.Add(childId);
            if (!childrenByParent.TryGetValue(parentId, out var children))
            {
                children = new List<int>();
                childrenByParent[parentId] = children;
            }

            children.Add(childId);
        }

        var visiting = new HashSet<int>();
        var visited = new HashSet<int>();

        foreach (var node in nodes)
        {
            if (Visit(node, childrenByParent, visiting, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Visit(
        int node,
        IReadOnlyDictionary<int, List<int>> childrenByParent,
        HashSet<int> visiting,
        HashSet<int> visited)
    {
        if (visited.Contains(node))
        {
            return false;
        }

        if (!visiting.Add(node))
        {
            return true;
        }

        if (childrenByParent.TryGetValue(node, out var children))
        {
            for (var i = 0; i < children.Count; i++)
            {
                if (Visit(children[i], childrenByParent, visiting, visited))
                {
                    return true;
                }
            }
        }

        visiting.Remove(node);
        visited.Add(node);
        return false;
    }

    private static ManagementValidationResult Invalid(params ManagementError[] findings)
    {
        return new ManagementValidationResult(false, findings);
    }

    private static ManagementValidationResult Complete(IReadOnlyList<ManagementError> findings)
    {
        return new ManagementValidationResult(findings.Count == 0, findings);
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
