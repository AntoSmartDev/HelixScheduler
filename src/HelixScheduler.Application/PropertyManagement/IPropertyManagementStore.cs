namespace HelixScheduler.Application.PropertyManagement;

public interface IPropertyManagementStore
{
    Task<PropertyManagementDto?> FindByIdAsync(int propertyId, CancellationToken ct);
    Task<PropertyManagementDto?> FindByKeyAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<PropertyManagementDto>> FindByIdsAsync(IReadOnlyList<int> propertyIds, CancellationToken ct);
    Task<IReadOnlyList<PropertyManagementDto>> ListAsync(CancellationToken ct);
    Task<PropertyManagementDto> CreateAsync(Guid tenantId, string key, string label, int? sortOrder, CancellationToken ct);
    Task<PropertyManagementDto> UpdateAsync(int propertyId, string key, string label, int? sortOrder, CancellationToken ct);
    Task<PropertyManagementDto> SetActiveAsync(int propertyId, bool isActive, CancellationToken ct);
    Task<bool> HasChildPropertiesAsync(int propertyId, CancellationToken ct);
    Task<bool> HasResourceAssignmentsAsync(int propertyId, CancellationToken ct);
    Task<bool> HasTypeMappingsAsync(int propertyId, CancellationToken ct);
    Task<IReadOnlyList<PropertyHierarchyRelationDto>> ListRelationsAsync(CancellationToken ct);
    Task<bool> RelationExistsAsync(int parentPropertyId, int childPropertyId, CancellationToken ct);
    Task<IReadOnlyList<PropertyHierarchyRelationDto>> GetRelationsAsync(CancellationToken ct);
    Task<PropertyHierarchyRelationDto> AddParentRelationAsync(int parentPropertyId, int childPropertyId, CancellationToken ct);
    Task<PropertyHierarchyRelationDto?> RemoveParentRelationAsync(int parentPropertyId, int childPropertyId, CancellationToken ct);
}
