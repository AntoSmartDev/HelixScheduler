namespace HelixScheduler.Application.Abstractions;

public sealed record TenantInfo(
    Guid Id,
    string Key,
    string? Label);
