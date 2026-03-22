using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Hierarchy;

public interface IHierarchyManagementService
{
    Task<ManagementResult<HierarchyRelationDto>> AddParentRelationAsync(
        AddParentRelationCommand command,
        CancellationToken ct);

    Task<ManagementResult<HierarchyRelationDto>> RemoveParentRelationAsync(
        RemoveParentRelationCommand command,
        CancellationToken ct);

    Task<IReadOnlyList<HierarchyRelationDto>> GetHierarchyRelationsAsync(CancellationToken ct);
}
