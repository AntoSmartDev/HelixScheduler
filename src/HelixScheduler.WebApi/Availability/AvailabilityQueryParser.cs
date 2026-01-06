using System.Globalization;

namespace HelixScheduler.WebApi.Availability;

public sealed class AvailabilityQueryParser
{
    private static readonly string[] DateFormats = { "yyyy-MM-dd" };

    public bool TryParse(
        AvailabilitySlotsQuery query,
        out AvailabilitySlotsInput input,
        out string error)
    {
        error = string.Empty;
        input = null!;

        if (!TryParseDate(query.FromDate, out var from))
        {
            error = "fromDate is required and must be yyyy-MM-dd.";
            return false;
        }

        if (!TryParseDate(query.ToDate, out var to))
        {
            error = "toDate is required and must be yyyy-MM-dd.";
            return false;
        }

        if (!TryParseCsvInts(query.ResourceIds, out var resourceIds))
        {
            error = "resourceIds is required and must be a comma-separated list of integers.";
            return false;
        }

        if (!TryParseCsvInts(query.PropertyIds, out var propertyIds))
        {
            error = "propertyIds must be a comma-separated list of integers.";
            return false;
        }

        if (!TryParseOrGroups(query.OrGroups, out var orGroups, out var orGroupsError))
        {
            error = orGroupsError;
            return false;
        }

        input = new AvailabilitySlotsInput(
            from,
            to,
            resourceIds,
            propertyIds,
            orGroups,
            query.IncludeDescendants,
            query.Explain);
        return true;
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        date = default;
        return false;
    }

    private static bool TryParseCsvInts(string? csv, out List<int> values)
    {
        values = new List<int>();
        if (string.IsNullOrWhiteSpace(csv))
        {
            return true;
        }

        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                values = new List<int>();
                return false;
            }

            values.Add(parsed);
        }

        return true;
    }

    private static bool TryParseOrGroups(
        string? value,
        out List<List<int>> groups,
        out string error)
    {
        groups = new List<List<int>>();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var groupParts = value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var groupPart in groupParts)
        {
            var parts = groupPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                error = "orGroups contains an empty group.";
                return false;
            }

            var group = new List<int>();
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    error = "orGroups must contain only integers.";
                    return false;
                }

                group.Add(parsed);
            }

            groups.Add(group);
        }

        return true;
    }
}

