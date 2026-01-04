namespace HelixScheduler.Application.Availability;

public sealed record AvailabilityComputeRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<int> RequiredResourceIds,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<IReadOnlyList<int>>? ResourceOrGroups = null,
    bool IncludeDescendants = false,
    bool Explain = false);
