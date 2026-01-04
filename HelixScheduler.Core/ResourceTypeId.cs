using System.Globalization;

namespace HelixScheduler.Core;

/// <summary>
/// Strongly-typed resource type identifier.
/// </summary>
public readonly struct ResourceTypeId : IEquatable<ResourceTypeId>
{
    public ResourceTypeId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "ResourceTypeId must be positive.");
        }

        Value = value;
    }

    public int Value { get; }

    public bool Equals(ResourceTypeId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ResourceTypeId other && Equals(other);
    public override int GetHashCode() => Value;
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static bool operator ==(ResourceTypeId left, ResourceTypeId right) => left.Equals(right);
    public static bool operator !=(ResourceTypeId left, ResourceTypeId right) => !left.Equals(right);
}
