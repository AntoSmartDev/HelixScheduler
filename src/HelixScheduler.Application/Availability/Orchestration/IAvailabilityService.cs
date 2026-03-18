using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.Availability.Orchestration;

public interface IAvailabilityService
{
    Task<AvailabilityComputeResponse> ComputeAsync(
        AvailabilityComputeRequest request,
        CancellationToken ct);
}
