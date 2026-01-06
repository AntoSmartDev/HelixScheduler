namespace HelixScheduler.Core;

/// <summary>
/// Inclusive date period (UTC dates).
/// </summary>
public readonly record struct DatePeriod
{
    /// <summary>
    /// Start date (inclusive).
    /// </summary>
    public DateOnly From { get; }
    /// <summary>
    /// End date (inclusive).
    /// </summary>
    public DateOnly To { get; }

    /// <summary>
    /// Builds a new date period.
    /// </summary>
    public DatePeriod(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("From must be less than or equal to To.", nameof(from));
        }

        From = from;
        To = to;
    }

    /// <summary>
    /// Enumerates all dates in the period, inclusive.
    /// </summary>
    public IEnumerable<DateOnly> EnumerateDays()
    {
        for (var date = From; date <= To; date = date.AddDays(1))
        {
            yield return date;
        }
    }
}
