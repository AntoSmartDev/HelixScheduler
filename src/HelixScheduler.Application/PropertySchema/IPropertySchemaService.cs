namespace HelixScheduler.Application.PropertySchema;

public interface IPropertySchemaService
{
    Task<PropertySchemaResponse> GetSchemaAsync(CancellationToken ct);

    Task ValidatePropertyFiltersAsync(
        IReadOnlyList<int> resourceIds,
        IReadOnlyList<int> propertyIds,
        CancellationToken ct);

    Task ValidatePropertyFiltersForTypeAsync(
        int resourceTypeId,
        IReadOnlyList<int> propertyIds,
        CancellationToken ct);

    Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);
}
