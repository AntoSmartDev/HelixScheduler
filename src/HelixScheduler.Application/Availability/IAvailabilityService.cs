namespace HelixScheduler.Application.Availability;

public interface IAvailabilityService
{
    Task<AvailabilityComputeResponse> ComputeAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct);
}
