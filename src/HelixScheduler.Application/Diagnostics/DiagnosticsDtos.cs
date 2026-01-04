namespace HelixScheduler.Application.Diagnostics;

public sealed record DbCounts(
    int Resources,
    int Relations,
    int Properties,
    int PropertyLinks,
    int Rules,
    int RuleResources,
    int BusyEvents,
    int BusyEventResources);
