namespace HelixScheduler.Availability;

public sealed class AvailabilitySlotsQuery
{
    public string? FromDate { get; init; }
    public string? ToDate { get; init; }
    public string? ResourceIds { get; init; }
    public string? PropertyIds { get; init; }
    public string? OrGroups { get; init; }
    public bool Explain { get; init; }
    public bool IncludeDescendants { get; init; }
}
