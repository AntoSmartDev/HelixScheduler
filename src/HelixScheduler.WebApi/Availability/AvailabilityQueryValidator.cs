using HelixScheduler.Application.Availability;

namespace HelixScheduler.WebApi.Availability;

public sealed class AvailabilityQueryValidator
{
    public bool TryValidate(
        AvailabilitySlotsInput input,
        out AvailabilityComputeRequest request,
        out string error)
    {
        request = null!;
        error = string.Empty;

        if (input.ResourceIds.Count == 0)
        {
            error = "resourceIds is required and must be a comma-separated list of integers.";
            return false;
        }

        var resourceIds = input.ResourceIds.Distinct().ToList();
        if (HasNonPositive(resourceIds))
        {
            error = "resourceIds must contain only positive integers.";
            return false;
        }

        if (input.PropertyIds.Count > 0 && HasNonPositive(input.PropertyIds))
        {
            error = "propertyIds must contain only positive integers.";
            return false;
        }

        if (input.OrGroups.Count > 5)
        {
            error = "orGroups must contain at most 5 groups.";
            return false;
        }

        var usedIds = new HashSet<int>(resourceIds);
        var normalizedGroups = new List<List<int>>();
        for (var groupIndex = 0; groupIndex < input.OrGroups.Count; groupIndex++)
        {
            var group = input.OrGroups[groupIndex];
            if (group.Count == 0)
            {
                error = "orGroups contains an empty group.";
                return false;
            }

            if (group.Count > 10)
            {
                error = "orGroups groups must contain at most 10 items.";
                return false;
            }

            var normalizedGroup = new List<int>();
            var groupSet = new HashSet<int>();
            for (var i = 0; i < group.Count; i++)
            {
                var parsed = group[i];
                if (parsed <= 0)
                {
                    error = "orGroups must contain only positive integers.";
                    return false;
                }

                if (!groupSet.Add(parsed))
                {
                    continue;
                }

                if (usedIds.Add(parsed))
                {
                    normalizedGroup.Add(parsed);
                }
            }

            if (normalizedGroup.Count == 0)
            {
                error = "orGroups group must contain at least one unique resourceId.";
                return false;
            }

            normalizedGroups.Add(normalizedGroup);
        }

        if (usedIds.Count > 20)
        {
            error = "Total resources must be 20 or less.";
            return false;
        }

        request = new AvailabilityComputeRequest(
            input.FromDate,
            input.ToDate,
            resourceIds,
            input.PropertyIds,
            normalizedGroups,
            input.IncludePropertyDescendants,
            input.Explain,
            input.IncludeResourceAncestors,
            input.AncestorRelationTypes,
            input.AncestorMode);
        return true;
    }

    private static bool HasNonPositive(IReadOnlyList<int> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] <= 0)
            {
                return true;
            }
        }

        return false;
    }
}

