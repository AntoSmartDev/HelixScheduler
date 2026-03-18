namespace HelixScheduler.Application.Availability;

public interface IAvailabilityFilterQueryService
{
    Task<IReadOnlyList<PropertyNode>> ExpandPropertySubtreeAsync(
        int propertyId,
        CancellationToken ct);

    Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
        IReadOnlyList<int> propertyIds,
        CancellationToken ct);

    Task<IReadOnlyList<int>> GetResourceIdsByAllPropertiesAsync(
        IReadOnlyList<int> propertyIds,
        CancellationToken ct);

    Task<IReadOnlyList<IReadOnlyList<int>>> GetResourceIdsByPropertySetsAsync(
        IReadOnlyList<IReadOnlyList<int>> propertySets,
        CancellationToken ct);
}
