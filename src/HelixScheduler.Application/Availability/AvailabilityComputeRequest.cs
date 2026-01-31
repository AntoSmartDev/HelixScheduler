namespace HelixScheduler.Application.Availability;

public sealed record AvailabilityComputeRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<int> RequiredResourceIds,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<IReadOnlyList<int>>? ResourceOrGroups = null,
    bool IncludePropertyDescendants = false,
    bool Explain = false,
    bool IncludeResourceAncestors = false,
    IReadOnlyList<string>? AncestorRelationTypes = null,
    string? AncestorMode = null,
    int? SlotDurationMinutes = null,
    bool IncludeRemainderSlot = false,
    IReadOnlyList<AncestorPropertyFilter>? AncestorFilters = null);
