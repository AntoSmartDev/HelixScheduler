namespace HelixScheduler.Application.PropertySchema;

public interface IPropertySchemaDataSource
{
    Task<IReadOnlyList<PropertySchemaNode>> GetPropertyNodesAsync(CancellationToken ct);
    Task<IReadOnlyList<ResourceTypePropertyLink>> GetResourceTypePropertiesAsync(CancellationToken ct);
    Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);
}
