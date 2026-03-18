using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.Availability.Validation;

internal static class AvailabilityRequestNormalization
{
    public static string? NormalizeAncestorMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return "perGroup";
        mode = mode.Trim();
        if (mode.Equals("perGroup", StringComparison.OrdinalIgnoreCase)) return "perGroup";
        return mode.Equals("global", StringComparison.OrdinalIgnoreCase) ? "global" : null;
    }

    public static string? NormalizeMatchMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return "or";
        return mode.Equals("or", StringComparison.OrdinalIgnoreCase) ? "or" : mode.Equals("and", StringComparison.OrdinalIgnoreCase) ? "and" : null;
    }

    public static string? NormalizePropertyMatchMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return "and";
        return mode.Equals("or", StringComparison.OrdinalIgnoreCase) ? "or" : mode.Equals("and", StringComparison.OrdinalIgnoreCase) ? "and" : null;
    }

    public static string? NormalizeAncestorScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope)) return "anyAncestor";
        return scope.Equals("anyAncestor", StringComparison.OrdinalIgnoreCase) ? "anyAncestor" : scope.Equals("directParent", StringComparison.OrdinalIgnoreCase) ? "directParent" : scope.Equals("nearestOfType", StringComparison.OrdinalIgnoreCase) ? "nearestOfType" : null;
    }

    public static List<List<int>> NormalizeOrGroups(IReadOnlyList<IReadOnlyList<int>>? resourceOrGroups)
    {
        if (resourceOrGroups == null || resourceOrGroups.Count == 0) return new List<List<int>>();
        var result = new List<List<int>>();
        for (var i = 0; i < resourceOrGroups.Count; i++) result.Add(resourceOrGroups[i].Distinct().ToList());
        return result;
    }

    public static bool HasNonPositive(IReadOnlyList<int> values)
    {
        for (var i = 0; i < values.Count; i++) if (values[i] <= 0) return true;
        return false;
    }

    public static List<int> DistinctPropertyIds(IReadOnlyList<int> propertyIds)
    {
        var distinct = new List<int>(propertyIds.Count);
        var seen = new HashSet<int>();
        for (var i = 0; i < propertyIds.Count; i++) if (seen.Add(propertyIds[i])) distinct.Add(propertyIds[i]);
        distinct.Sort();
        return distinct;
    }

    public static string BuildPropertySetCacheKey(IReadOnlyList<int> propertyIds)
        => propertyIds.Count == 0 ? string.Empty : string.Join(',', propertyIds);

    public static IReadOnlyList<string>? NormalizeRelationTypes(IReadOnlyList<string>? relationTypes)
    {
        if (relationTypes == null || relationTypes.Count == 0) return null;
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < relationTypes.Count; i++)
        {
            var value = relationTypes[i]?.Trim();
            if (!string.IsNullOrWhiteSpace(value)) set.Add(value);
        }

        return set.Count == 0 ? null : set.ToList();
    }
}
