namespace HelixScheduler.Application.Management;

public sealed record ManagementError(
    string Code,
    ManagementErrorCategory Category,
    string Message,
    string Target);
