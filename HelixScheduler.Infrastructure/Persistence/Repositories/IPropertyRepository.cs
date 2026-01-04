using HelixScheduler.Infrastructure.Persistence.Entities;

namespace HelixScheduler.Infrastructure.Persistence.Repositories;

public interface IPropertyRepository
{
    Task<IReadOnlyList<ResourceProperties>> ExpandPropertySubtreeAsync(
        int propertyId,
        CancellationToken ct);

    Task<IReadOnlyList<int>> GetResourceIdsByPropertiesAsync(
        IReadOnlyCollection<int> propertyIds,
        CancellationToken ct);
}
