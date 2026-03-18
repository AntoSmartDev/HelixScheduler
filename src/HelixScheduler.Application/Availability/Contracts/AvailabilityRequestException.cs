namespace HelixScheduler.Application.Availability;

public sealed class AvailabilityRequestException : Exception
{
    public AvailabilityRequestException(string message)
        : base(message)
    {
    }
}
