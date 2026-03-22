using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Properties;

public interface IPropertyManagementService
{
    Task<ManagementResult<PropertyManagementDto>> CreatePropertyAsync(CreatePropertyCommand command, CancellationToken ct);
    Task<ManagementResult<PropertyManagementDto>> UpdatePropertyAsync(UpdatePropertyCommand command, CancellationToken ct);
    Task<ManagementResult<PropertyManagementDto>> GetPropertyAsync(int propertyId, CancellationToken ct);
    Task<IReadOnlyList<PropertyManagementDto>> ListPropertiesAsync(CancellationToken ct);
    Task<ManagementResult<PropertyManagementDto>> ActivatePropertyAsync(int propertyId, CancellationToken ct);
    Task<ManagementResult<PropertyManagementDto>> DeactivatePropertyAsync(int propertyId, CancellationToken ct);
    Task<ManagementResult<PropertyHierarchyRelationDto>> AddPropertyParentRelationAsync(AddPropertyParentRelationCommand command, CancellationToken ct);
    Task<ManagementResult<PropertyHierarchyRelationDto>> RemovePropertyParentRelationAsync(RemovePropertyParentRelationCommand command, CancellationToken ct);
    Task<IReadOnlyList<PropertyHierarchyRelationDto>> GetPropertyRelationsAsync(CancellationToken ct);
}
