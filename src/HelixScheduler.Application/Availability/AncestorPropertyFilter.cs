namespace HelixScheduler.Application.Availability;

public sealed record AncestorPropertyFilter(
    int ResourceTypeId,
    IReadOnlyList<int> PropertyIds,
    bool IncludePropertyDescendants = false,
    string? MatchMode = null,
    string? Scope = null,
    bool MatchAllAncestors = false);
