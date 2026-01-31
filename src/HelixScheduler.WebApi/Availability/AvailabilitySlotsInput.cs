namespace HelixScheduler.WebApi.Availability;

public sealed record AvailabilitySlotsInput(
    DateOnly FromDate,
    DateOnly ToDate,
    List<int> ResourceIds,
    List<int> PropertyIds,
    List<List<int>> OrGroups,
    bool IncludePropertyDescendants,
    bool Explain,
    bool IncludeResourceAncestors,
    List<string> AncestorRelationTypes,
    string? AncestorMode);

