namespace HelixScheduler.Application.Management.Hierarchy;

public interface IHierarchyManagementStore
{
    Task<IReadOnlyList<HierarchyRelationDto>> ListRelationsAsync(CancellationToken ct);
    Task<bool> RelationExistsAsync(int parentResourceId, int childResourceId, string relationType, CancellationToken ct);
    Task<HierarchyResourceState?> FindResourceStateAsync(int resourceId, CancellationToken ct);
    Task<IReadOnlyList<HierarchyRelationDto>> GetRelationsByRelationTypeAsync(string relationType, CancellationToken ct);
    Task<HierarchyRelationDto> AddRelationAsync(int parentResourceId, int childResourceId, string relationType, CancellationToken ct);
    Task<HierarchyRelationDto?> RemoveRelationAsync(int parentResourceId, int childResourceId, string relationType, CancellationToken ct);
}
