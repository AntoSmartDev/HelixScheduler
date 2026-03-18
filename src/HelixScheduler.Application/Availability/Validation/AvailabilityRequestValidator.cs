using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.Filtering;

namespace HelixScheduler.Application.Availability.Validation;

internal sealed class AvailabilityRequestValidator
{
    private const int MaxRangeDays = 31;
    private const int MaxRequiredResources = 10;
    private const int MaxOrGroups = 5;
    private const int MaxOrGroupItems = 10;
    private const int MaxTotalResources = 20;
    private const int MaxAncestorFilters = 5;
    private const int MaxPropertyFilterGroups = 5;
    private const int MaxPropertyFilterIdsPerGroup = 10;

    private readonly PropertySchema.IPropertySchemaService _propertySchemaService;

    public AvailabilityRequestValidator(PropertySchema.IPropertySchemaService propertySchemaService) => _propertySchemaService = propertySchemaService;

    public async Task ValidateAsync(AvailabilityComputeRequest request, CancellationToken ct)
    {
        ValidateRequest(request);
        await ValidatePropertySchemaAsync(request, ct).ConfigureAwait(false);
        await ValidateAncestorFiltersAsync(request, ct).ConfigureAwait(false);
    }

    private async Task ValidatePropertySchemaAsync(AvailabilityComputeRequest request, CancellationToken ct)
    {
        var propertyIds = AvailabilityPropertyFilterEvaluator.CollectPropertyFilterIds(request);
        if (propertyIds.Count == 0) return;

        var resourceIds = new HashSet<int>(request.RequiredResourceIds);
        var orGroups = request.ResourceOrGroups ?? Array.Empty<IReadOnlyList<int>>();
        for (var gi = 0; gi < orGroups.Count; gi++) for (var i = 0; i < orGroups[gi].Count; i++) resourceIds.Add(orGroups[gi][i]);

        await _propertySchemaService.ValidatePropertyFiltersAsync(resourceIds.ToList(), propertyIds, ct).ConfigureAwait(false);
    }

    private async Task ValidateAncestorFiltersAsync(AvailabilityComputeRequest request, CancellationToken ct)
    {
        if (request.AncestorFilters == null || request.AncestorFilters.Count == 0) return;
        for (var i = 0; i < request.AncestorFilters.Count; i++)
        {
            var filter = request.AncestorFilters[i];
            if (filter.PropertyIds == null || filter.PropertyIds.Count == 0) continue;
            await _propertySchemaService.ValidatePropertyFiltersForTypeAsync(filter.ResourceTypeId, filter.PropertyIds.Distinct().ToList(), ct).ConfigureAwait(false);
        }
    }

    private static void ValidateRequest(AvailabilityComputeRequest request)
    {
        if (request.RequiredResourceIds == null) throw new AvailabilityRequestException("resourceIds is required and must contain at least one item.");
        if (request.FromDate > request.ToDate) throw new AvailabilityRequestException("fromDate must be less than or equal to toDate.");
        if ((request.ToDate.DayNumber - request.FromDate.DayNumber) + 1 > MaxRangeDays) throw new AvailabilityRequestException("Date range must be 31 days or less.");
        if (AvailabilityRequestNormalization.HasNonPositive(request.RequiredResourceIds)) throw new AvailabilityRequestException("resourceIds must contain only positive integers.");

        var distinctRequired = request.RequiredResourceIds.Distinct().ToList();
        if (distinctRequired.Count > MaxRequiredResources) throw new AvailabilityRequestException("resourceIds must contain at most 10 items.");

        if (request.PropertyFilterGroups != null && request.PropertyFilterGroups.Count > 0)
        {
            if (request.PropertyFilterGroups.Count > MaxPropertyFilterGroups) throw new AvailabilityRequestException("propertyFilterGroups must contain at most 5 groups.");
            for (var i = 0; i < request.PropertyFilterGroups.Count; i++)
            {
                var group = request.PropertyFilterGroups[i];
                if (group == null) throw new AvailabilityRequestException("propertyFilterGroups cannot contain null entries.");
                if (group.PropertyIds == null || group.PropertyIds.Count == 0) throw new AvailabilityRequestException("propertyFilterGroups requires propertyIds.");
                if (AvailabilityRequestNormalization.HasNonPositive(group.PropertyIds)) throw new AvailabilityRequestException("propertyFilterGroups propertyIds must be positive integers.");
                if (group.PropertyIds.Count > MaxPropertyFilterIdsPerGroup) throw new AvailabilityRequestException("propertyFilterGroups propertyIds must contain at most 10 items.");
                if (AvailabilityRequestNormalization.NormalizePropertyMatchMode(group.MatchMode) == null) throw new AvailabilityRequestException("propertyFilterGroups matchMode must be 'or' or 'and'.");
            }
        }

        var orGroups = request.ResourceOrGroups ?? Array.Empty<IReadOnlyList<int>>();
        if (orGroups.Count > MaxOrGroups) throw new AvailabilityRequestException("orGroups must contain at most 5 groups.");

        var usedIds = new HashSet<int>(distinctRequired);
        for (var gi = 0; gi < orGroups.Count; gi++)
        {
            var group = orGroups[gi];
            if (group == null || group.Count == 0) throw new AvailabilityRequestException("orGroups contains an empty group.");
            if (group.Count > MaxOrGroupItems) throw new AvailabilityRequestException("orGroups groups must contain at most 10 items.");

            var groupSet = new HashSet<int>();
            for (var i = 0; i < group.Count; i++)
            {
                var value = group[i];
                if (value <= 0) throw new AvailabilityRequestException("orGroups must contain only positive integers.");
                if (!groupSet.Add(value)) continue;
                usedIds.Add(value);
            }

            if (groupSet.Count == 0) throw new AvailabilityRequestException("orGroups group must contain at least one unique resourceId.");
        }

        if (usedIds.Count == 0) throw new AvailabilityRequestException("resourceIds is required and must contain at least one item.");
        if (usedIds.Count > MaxTotalResources) throw new AvailabilityRequestException("Total resources must be 20 or less.");

        var includeAncestors = request.IncludeResourceAncestors || (request.AncestorFilters?.Count > 0);
        if (includeAncestors && AvailabilityRequestNormalization.NormalizeAncestorMode(request.AncestorMode) == null) throw new AvailabilityRequestException("ancestorMode must be 'perGroup' or 'global'.");

        if (request.AncestorFilters != null && request.AncestorFilters.Count > 0)
        {
            if (request.AncestorFilters.Count > MaxAncestorFilters) throw new AvailabilityRequestException("ancestorFilters must contain at most 5 entries.");
            for (var i = 0; i < request.AncestorFilters.Count; i++)
            {
                var filter = request.AncestorFilters[i];
                if (filter.ResourceTypeId <= 0) throw new AvailabilityRequestException("ancestorFilters requires positive resourceTypeId.");
                if (filter.PropertyIds == null || filter.PropertyIds.Count == 0) throw new AvailabilityRequestException("ancestorFilters requires propertyIds.");
                if (AvailabilityRequestNormalization.HasNonPositive(filter.PropertyIds)) throw new AvailabilityRequestException("ancestorFilters propertyIds must be positive integers.");
                if (AvailabilityRequestNormalization.NormalizeMatchMode(filter.MatchMode) == null) throw new AvailabilityRequestException("ancestorFilters matchMode must be 'or' or 'and'.");
                if (AvailabilityRequestNormalization.NormalizeAncestorScope(filter.Scope) == null) throw new AvailabilityRequestException("ancestorFilters scope must be 'anyAncestor', 'directParent', or 'nearestOfType'.");
            }
        }

        if (request.SlotDurationMinutes.HasValue && (request.SlotDurationMinutes.Value <= 0 || request.SlotDurationMinutes.Value > 1440))
        {
            throw new AvailabilityRequestException("slotDurationMinutes must be between 1 and 1440.");
        }
    }
}
