namespace HelixScheduler.Application.Management;

public sealed record ManagementResult<T>(
    T? Value,
    IReadOnlyList<ManagementError> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public static ManagementResult<T> Success(T value)
    {
        return new ManagementResult<T>(value, Array.Empty<ManagementError>());
    }

    public static ManagementResult<T> Failure(params ManagementError[] errors)
    {
        return new ManagementResult<T>(default, errors);
    }

    public static ManagementResult<T> Failure(IReadOnlyList<ManagementError> errors)
    {
        return new ManagementResult<T>(default, errors);
    }
}
