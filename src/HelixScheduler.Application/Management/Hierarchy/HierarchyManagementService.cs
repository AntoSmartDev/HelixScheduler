using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Hierarchy;

public sealed class HierarchyManagementService : IHierarchyManagementService
{
    private const int RelationTypeMaxLength = 50;

    private readonly IHierarchyManagementStore _store;
    private readonly ITenantContext _tenantContext;

    public HierarchyManagementService(
        IHierarchyManagementStore store,
        ITenantContext tenantContext)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<ManagementResult<HierarchyRelationDto>> AddParentRelationAsync(
        AddParentRelationCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(tenantError);
        }

        var relationType = NormalizeRelationType(command.RelationType);
        var errors = ValidateCommand(command.ParentResourceId, command.ChildResourceId, relationType);
        if (errors.Count > 0)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(errors);
        }

        var parent = await _store.FindResourceStateAsync(command.ParentResourceId, ct).ConfigureAwait(false);
        if (parent == null)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(
                CreateError("hierarchy.parent-resource-not-found", ManagementErrorCategory.NotFound, $"Parent resource '{command.ParentResourceId}' was not found.", "parentResourceId"));
        }

        var child = await _store.FindResourceStateAsync(command.ChildResourceId, ct).ConfigureAwait(false);
        if (child == null)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(
                CreateError("hierarchy.child-resource-not-found", ManagementErrorCategory.NotFound, $"Child resource '{command.ChildResourceId}' was not found.", "childResourceId"));
        }

        if (!parent.IsActive || parent.IsArchived)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(
                CreateError("hierarchy.parent-resource-inactive", ManagementErrorCategory.InvalidOperation, "Parent resource must be active and not archived.", "parentResourceId"));
        }

        if (!child.IsActive || child.IsArchived)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(
                CreateError("hierarchy.child-resource-inactive", ManagementErrorCategory.InvalidOperation, "Child resource must be active and not archived.", "childResourceId"));
        }

        var exists = await _store.RelationExistsAsync(command.ParentResourceId, command.ChildResourceId, relationType!, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(
                CreateError("hierarchy.relation-already-exists", ManagementErrorCategory.Conflict, "Hierarchy relation already exists.", "relation"));
        }

        var sameTypeRelations = await _store.GetRelationsByRelationTypeAsync(relationType!, ct).ConfigureAwait(false);
        if (WouldIntroduceCycle(command.ParentResourceId, command.ChildResourceId, sameTypeRelations))
        {
            return ManagementResult<HierarchyRelationDto>.Failure(
                CreateError("hierarchy.cycle-detected", ManagementErrorCategory.InvalidOperation, "Hierarchy relation would introduce a cycle.", "relation"));
        }

        var added = await _store.AddRelationAsync(command.ParentResourceId, command.ChildResourceId, relationType!, ct)
            .ConfigureAwait(false);
        return ManagementResult<HierarchyRelationDto>.Success(added);
    }

    public async Task<ManagementResult<HierarchyRelationDto>> RemoveParentRelationAsync(
        RemoveParentRelationCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(tenantError);
        }

        var relationType = NormalizeRelationType(command.RelationType);
        var errors = ValidateCommand(command.ParentResourceId, command.ChildResourceId, relationType);
        if (errors.Count > 0)
        {
            return ManagementResult<HierarchyRelationDto>.Failure(errors);
        }

        var removed = await _store.RemoveRelationAsync(command.ParentResourceId, command.ChildResourceId, relationType!, ct)
            .ConfigureAwait(false);

        return removed == null
            ? ManagementResult<HierarchyRelationDto>.Failure(
                CreateError("hierarchy.relation-not-found", ManagementErrorCategory.NotFound, "Hierarchy relation was not found.", "relation"))
            : ManagementResult<HierarchyRelationDto>.Success(removed);
    }

    public async Task<IReadOnlyList<HierarchyRelationDto>> GetHierarchyRelationsAsync(CancellationToken ct)
    {
        if (ValidateTenantContext() != null)
        {
            return Array.Empty<HierarchyRelationDto>();
        }

        return await _store.ListRelationsAsync(ct).ConfigureAwait(false);
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static List<ManagementError> ValidateCommand(int parentResourceId, int childResourceId, string? relationType)
    {
        var errors = new List<ManagementError>();

        if (parentResourceId <= 0)
        {
            errors.Add(CreateError("hierarchy.parent-resource-required", ManagementErrorCategory.Validation, "ParentResourceId must be positive.", "parentResourceId"));
        }

        if (childResourceId <= 0)
        {
            errors.Add(CreateError("hierarchy.child-resource-required", ManagementErrorCategory.Validation, "ChildResourceId must be positive.", "childResourceId"));
        }

        if (parentResourceId > 0 && childResourceId > 0 && parentResourceId == childResourceId)
        {
            errors.Add(CreateError("hierarchy.self-parent-not-allowed", ManagementErrorCategory.Validation, "A resource cannot be parent of itself.", "relation"));
        }

        if (relationType == null)
        {
            errors.Add(CreateError("hierarchy.relation-type-invalid", ManagementErrorCategory.Validation, "RelationType is required.", "relationType"));
        }

        return errors;
    }

    private static string? NormalizeRelationType(string relationType)
    {
        if (string.IsNullOrWhiteSpace(relationType))
        {
            return null;
        }

        var normalized = relationType.Trim();
        return normalized.Length <= RelationTypeMaxLength ? normalized : null;
    }

    private static bool WouldIntroduceCycle(
        int parentResourceId,
        int childResourceId,
        IReadOnlyList<HierarchyRelationDto> relations)
    {
        var parentsByChild = new Dictionary<int, List<int>>();
        for (var i = 0; i < relations.Count; i++)
        {
            var relation = relations[i];
            if (!parentsByChild.TryGetValue(relation.ChildResourceId, out var parents))
            {
                parents = new List<int>();
                parentsByChild[relation.ChildResourceId] = parents;
            }

            parents.Add(relation.ParentResourceId);
        }

        if (!parentsByChild.TryGetValue(childResourceId, out var newParents))
        {
            newParents = new List<int>();
            parentsByChild[childResourceId] = newParents;
        }

        newParents.Add(parentResourceId);

        var toVisit = new Stack<int>();
        var visited = new HashSet<int>();
        toVisit.Push(parentResourceId);

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
                if (next == childResourceId)
                {
                    return true;
                }

                toVisit.Push(next);
            }
        }

        return false;
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
