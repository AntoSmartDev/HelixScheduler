namespace HelixScheduler.WebApi.Management;

public sealed record ManagementErrorResponse(
    string Code,
    string Category,
    string Message,
    string Target);

public sealed record ManagementFailureResponse(
    IReadOnlyList<ManagementErrorResponse> Errors);
