namespace HelixScheduler.Infrastructure.Persistence.Entities;

public sealed class Tenants
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Label { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
