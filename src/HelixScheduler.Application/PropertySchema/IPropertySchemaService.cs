namespace HelixScheduler.Application.PropertySchema;

public interface IPropertySchemaService
{
    Task<PropertySchemaResponse> GetSchemaAsync(CancellationToken ct);

    Task ValidatePropertyFiltersAsync(
        IReadOnlyList<int> resourceIds,
        IReadOnlyList<int> propertyIds,
        CancellationToken ct);
}
