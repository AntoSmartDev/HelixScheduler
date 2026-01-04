namespace HelixScheduler.Availability;

public sealed record AvailabilitySlotsInput(
    DateOnly FromDate,
    DateOnly ToDate,
    List<int> ResourceIds,
    List<int> PropertyIds,
    List<List<int>> OrGroups,
    bool IncludeDescendants,
    bool Explain);
