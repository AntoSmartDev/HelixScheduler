namespace HelixScheduler.Application.Availability;

public sealed record AvailabilityExplanation(
    string Reason,
    int? ResourceId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    long? RuleId,
    long? BusyEventId,
    string Message);
