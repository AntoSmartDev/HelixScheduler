namespace HelixScheduler.Application.Availability;

public interface IAvailabilityAncestorQueryService
{
    Task<IReadOnlyList<ResourceRelationLink>> GetResourceRelationsByTypesAsync(
        IReadOnlyList<string>? relationTypes,
        CancellationToken ct);
}
