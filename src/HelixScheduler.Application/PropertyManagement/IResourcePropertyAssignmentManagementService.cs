using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.PropertyManagement;

public interface IResourcePropertyAssignmentManagementService
{
    Task<ManagementResult<ResourcePropertyAssignmentsDto>> AssignPropertiesToResourceAsync(AssignPropertiesToResourceCommand command, CancellationToken ct);
    Task<ManagementResult<ResourcePropertyAssignmentsDto>> RemovePropertiesFromResourceAsync(RemovePropertiesFromResourceCommand command, CancellationToken ct);
    Task<ManagementResult<ResourcePropertyAssignmentsDto>> GetResourcePropertiesAsync(int resourceId, CancellationToken ct);
}
