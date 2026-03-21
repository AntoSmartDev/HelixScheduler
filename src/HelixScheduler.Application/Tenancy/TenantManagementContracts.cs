namespace HelixScheduler.Application.Tenancy;

public sealed record CreateTenantCommand(
    string Key,
    string? Label);

public sealed record UpdateTenantCommand(
    Guid TenantId,
    string Key,
    string? Label);

public sealed record TenantManagementDto(
    Guid Id,
    string Key,
    string? Label,
    bool IsActive,
    DateTime CreatedAtUtc);
