using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.PropertySchema;

public sealed class PropertySchemaService : IPropertySchemaService
{
    private readonly IPropertySchemaQueryService _queryService;

    public PropertySchemaService(IPropertySchemaQueryService queryService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    }

    public async Task<PropertySchemaResponse> GetSchemaAsync(CancellationToken ct)
    {
        var nodes = await _queryService.GetPropertyNodesAsync(ct).ConfigureAwait(false);
        if (nodes.Count == 0)
        {
            return new PropertySchemaResponse(
                Array.Empty<PropertyDefinitionDto>(),
                Array.Empty<PropertyNodeDto>(),
                Array.Empty<ResourceTypePropertyDto>());
        }

        var typeLinks = await _queryService.GetResourceTypePropertiesAsync(ct).ConfigureAwait(false);
        var snapshot = PropertySchemaSnapshot.Create(nodes, typeLinks);
        return new PropertySchemaResponse(snapshot.Definitions, snapshot.Nodes, snapshot.TypeMappings);
    }

    public async Task ValidatePropertyFiltersAsync(
        IReadOnlyList<int> resourceIds,
        IReadOnlyList<int> propertyIds,
        CancellationToken ct)
    {
        if (resourceIds.Count == 0 || propertyIds.Count == 0)
        {
            return;
        }

        var snapshot = await LoadSnapshotAsync(ct).ConfigureAwait(false);
        var definitionIds = snapshot.ResolveDefinitionIds(propertyIds, "propertyIds");

        var assignments = await _queryService.GetResourceTypeAssignmentsAsync(resourceIds, ct)
            .ConfigureAwait(false);

        for (var i = 0; i < assignments.Count; i++)
        {
            var assignment = assignments[i];
            snapshot.ValidateTypeCompatibility(assignment.ResourceTypeId, definitionIds);
        }
    }

    public async Task ValidatePropertyFiltersForTypeAsync(
        int resourceTypeId,
        IReadOnlyList<int> propertyIds,
        CancellationToken ct)
    {
        if (propertyIds.Count == 0)
        {
            return;
        }

        var snapshot = await LoadSnapshotAsync(ct).ConfigureAwait(false);
        var definitionIds = snapshot.ResolveDefinitionIds(propertyIds, "propertyIds");
        snapshot.ValidateTypeCompatibility(resourceTypeId, definitionIds);
    }

    public Task<IReadOnlyList<ResourceTypeAssignment>> GetResourceTypeAssignmentsAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        return _queryService.GetResourceTypeAssignmentsAsync(resourceIds, ct);
    }

    private async Task<PropertySchemaSnapshot> LoadSnapshotAsync(CancellationToken ct)
    {
        var nodes = await _queryService.GetPropertyNodesAsync(ct).ConfigureAwait(false);
        if (nodes.Count == 0)
        {
            return PropertySchemaSnapshot.Create(
                Array.Empty<PropertySchemaNode>(),
                Array.Empty<ResourceTypePropertyLink>());
        }

        var typeLinks = await _queryService.GetResourceTypePropertiesAsync(ct).ConfigureAwait(false);
        return PropertySchemaSnapshot.Create(nodes, typeLinks);
    }
}
