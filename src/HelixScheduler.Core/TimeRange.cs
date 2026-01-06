namespace HelixScheduler.Core;

/// <summary>
/// Time-of-day range (UTC).
/// </summary>
public readonly record struct TimeRange
{
    /// <summary>
    /// Start time (inclusive).
    /// </summary>
    public TimeSpan Start { get; }
    /// <summary>
    /// End time (exclusive).
    /// </summary>
    public TimeSpan End { get; }

    /// <summary>
    /// Builds a new time range.
    /// </summary>
    public TimeRange(TimeSpan start, TimeSpan end)
    {
        if (end <= start)
        {
            throw new ArgumentException("End must be greater than Start.", nameof(end));
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Returns true if the ranges overlap.
    /// </summary>
    public bool Overlaps(TimeRange other)
    {
        return Start < other.End && End > other.Start;
    }

    /// <summary>
    /// Returns the intersection of two ranges, or null if disjoint.
    /// </summary>
    public TimeRange? Intersect(TimeRange other)
    {
        if (!Overlaps(other))
        {
            return null;
        }

        var start = Start > other.Start ? Start : other.Start;
        var end = End < other.End ? End : other.End;
        return new TimeRange(start, end);
    }

    /// <summary>
    /// Subtracts another range from the current range.
    /// </summary>
    public IEnumerable<TimeRange> Subtract(TimeRange other)
    {
        if (!Overlaps(other))
        {
            yield return this;
            yield break;
        }

        if (other.Start <= Start && other.End >= End)
        {
            yield break;
        }

        if (other.Start > Start)
        {
            yield return new TimeRange(Start, other.Start);
        }

        if (other.End < End)
        {
            yield return new TimeRange(other.End, End);
        }
    }
}
