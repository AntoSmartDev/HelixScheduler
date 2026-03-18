namespace HelixScheduler.WebApi.Availability;

public sealed record AvailabilitySlotsInput(
    DateOnly FromDate,
    DateOnly ToDate,
    List<int> ResourceIds,
    List<List<int>> OrGroups,
    bool Explain,
    bool IncludeResourceAncestors,
    List<string> AncestorRelationTypes,
    string? AncestorMode);
