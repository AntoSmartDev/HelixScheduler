namespace HelixScheduler.Application.PropertySchema;

public interface IPropertySchemaQueryService
{
    Task<IReadOnlyList<PropertySchemaNode>> GetPropertyNodesAsync(CancellationToken ct);
    Task<IReadOnlyList<ResourceTypePropertyLink>> GetResourceTypePropertiesAsync(CancellationToken ct);
    Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);
}
