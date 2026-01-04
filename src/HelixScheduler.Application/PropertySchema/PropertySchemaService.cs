using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.PropertySchema;

public sealed class PropertySchemaService : IPropertySchemaService
{
    private readonly IPropertySchemaDataSource _dataSource;

    public PropertySchemaService(IPropertySchemaDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<PropertySchemaResponse> GetSchemaAsync(CancellationToken ct)
    {
        var nodes = await _dataSource.GetPropertyNodesAsync(ct).ConfigureAwait(false);
        if (nodes.Count == 0)
        {
            return new PropertySchemaResponse(
                Array.Empty<PropertyDefinitionDto>(),
                Array.Empty<PropertyNodeDto>(),
                Array.Empty<ResourceTypePropertyDto>());
        }

        var typeLinks = await _dataSource.GetResourceTypePropertiesAsync(ct).ConfigureAwait(false);

        var nodeMap = nodes.ToDictionary(node => node.Id, node => node);
        var definitionIds = new HashSet<int>();
        var nodeDtos = new List<PropertyNodeDto>(nodes.Count);

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var definitionId = ResolveDefinitionId(node.Id, nodeMap);
            if (node.ParentId == null)
            {
                definitionIds.Add(node.Id);
            }
            else
            {
                definitionIds.Add(definitionId);
            }

            nodeDtos.Add(new PropertyNodeDto(
                node.Id,
                definitionId,
                node.ParentId,
                node.Key,
                node.Label,
                node.SortOrder));
        }

        var definitions = nodeDtos
            .Where(node => node.ParentId == null)
            .Select(node => new PropertyDefinitionDto(
                node.Id,
                node.Key,
                node.Label,
                node.SortOrder))
            .ToList();

        var typeMappings = typeLinks
            .Select(link => new ResourceTypePropertyDto(link.ResourceTypeId, link.PropertyDefinitionId))
            .ToList();

        return new PropertySchemaResponse(definitions, nodeDtos, typeMappings);
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

        var nodes = await _dataSource.GetPropertyNodesAsync(ct).ConfigureAwait(false);
        var nodeMap = nodes.ToDictionary(node => node.Id, node => node);
        var definitionIds = new HashSet<int>();

        for (var i = 0; i < propertyIds.Count; i++)
        {
            var propertyId = propertyIds[i];
            if (!nodeMap.ContainsKey(propertyId))
            {
                throw new AvailabilityRequestException($"propertyIds contains unknown id {propertyId}.");
            }

            definitionIds.Add(ResolveDefinitionId(propertyId, nodeMap));
        }

        var typeLinks = await _dataSource.GetResourceTypePropertiesAsync(ct).ConfigureAwait(false);
        var typeDefinitionMap = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < typeLinks.Count; i++)
        {
            var link = typeLinks[i];
            if (!typeDefinitionMap.TryGetValue(link.ResourceTypeId, out var defs))
            {
                defs = new HashSet<int>();
                typeDefinitionMap[link.ResourceTypeId] = defs;
            }
            defs.Add(link.PropertyDefinitionId);
        }

        var assignments = await _dataSource.GetResourceTypeAssignmentsAsync(resourceIds, ct)
            .ConfigureAwait(false);

        for (var i = 0; i < assignments.Count; i++)
        {
            var assignment = assignments[i];
            if (!typeDefinitionMap.TryGetValue(assignment.ResourceTypeId, out var allowed))
            {
                throw new AvailabilityRequestException(
                    $"Resource type {assignment.ResourceTypeId} does not allow requested properties.");
            }

            foreach (var definitionId in definitionIds)
            {
                if (!allowed.Contains(definitionId))
                {
                    throw new AvailabilityRequestException(
                        $"propertyIds are not compatible with resource type {assignment.ResourceTypeId}.");
                }
            }
        }
    }

    private static int ResolveDefinitionId(
        int nodeId,
        IReadOnlyDictionary<int, PropertySchemaNode> nodeMap)
    {
        var current = nodeMap[nodeId];
        while (current.ParentId != null && nodeMap.TryGetValue(current.ParentId.Value, out var parent))
        {
            current = parent;
        }

        return current.Id;
    }
}
