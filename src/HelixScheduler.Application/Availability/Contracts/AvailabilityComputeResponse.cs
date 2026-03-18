using HelixScheduler.Core;

namespace HelixScheduler.Application.Availability;

public sealed record AvailabilityComputeResponse(
    IReadOnlyList<UtcSlot> Slots,
    IReadOnlyList<AvailabilityExplanation> Explanations);
