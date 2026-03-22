namespace HelixScheduler.Application.Management.Properties;

public interface IResourcePropertyAssignmentManagementStore
{
    Task<bool> AssignmentExistsAsync(int resourceId, int propertyId, CancellationToken ct);
    Task<IReadOnlyList<PropertyManagementDto>> ListAssignedPropertiesAsync(int resourceId, CancellationToken ct);
    Task AddAssignmentsAsync(Guid tenantId, int resourceId, IReadOnlyList<int> propertyIds, CancellationToken ct);
    Task RemoveAssignmentsAsync(int resourceId, IReadOnlyList<int> propertyIds, CancellationToken ct);
}
