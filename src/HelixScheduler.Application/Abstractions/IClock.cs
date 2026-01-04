namespace HelixScheduler.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
