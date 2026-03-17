namespace HelixScheduler.Application.Availability;

public sealed record PropertyFilterGroup(
    IReadOnlyList<int> PropertyIds,
    string? MatchMode = null,
    bool IncludePropertyDescendants = false);
