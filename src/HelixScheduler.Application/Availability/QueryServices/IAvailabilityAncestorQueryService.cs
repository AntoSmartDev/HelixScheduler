using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.Availability.QueryServices;

public interface IAvailabilityAncestorQueryService
{
    Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsByTypesAsync(
        IReadOnlyList<string>? relationTypes,
        CancellationToken ct);
}
