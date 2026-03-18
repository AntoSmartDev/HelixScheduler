using HelixScheduler.Application.Availability;

namespace HelixScheduler.Application.PropertySchema;

internal sealed class PropertySchemaSnapshot
{
    private readonly IReadOnlyDictionary<int, PropertySchemaNode> _nodeMap;
    private readonly IReadOnlyDictionary<int, HashSet<int>> _typeDefinitionMap;

    private PropertySchemaSnapshot(
        IReadOnlyDictionary<int, PropertySchemaNode> nodeMap,
        IReadOnlyDictionary<int, HashSet<int>> typeDefinitionMap,
        IReadOnlyList<PropertyDefinitionDto> definitions,
        IReadOnlyList<PropertyNodeDto> nodes,
        IReadOnlyList<ResourceTypePropertyDto> typeMappings)
    {
        _nodeMap = nodeMap;
        _typeDefinitionMap = typeDefinitionMap;
        Definitions = definitions;
        Nodes = nodes;
        TypeMappings = typeMappings;
    }

    public IReadOnlyList<PropertyDefinitionDto> Definitions { get; }
    public IReadOnlyList<PropertyNodeDto> Nodes { get; }
    public IReadOnlyList<ResourceTypePropertyDto> TypeMappings { get; }

    public static PropertySchemaSnapshot Create(
        IReadOnlyList<PropertySchemaNode> nodes,
        IReadOnlyList<ResourceTypePropertyLink> typeLinks)
    {
        var nodeMap = nodes.ToDictionary(node => node.Id, node => node);
        var nodeDtos = new List<PropertyNodeDto>(nodes.Count);
        var definitions = new List<PropertyDefinitionDto>();

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var definitionId = ResolveDefinitionId(node.Id, nodeMap);

            nodeDtos.Add(new PropertyNodeDto(
                node.Id,
                definitionId,
                node.ParentId,
                node.Key,
                node.Label,
                node.SortOrder));

            if (node.ParentId == null)
            {
                definitions.Add(new PropertyDefinitionDto(
                    node.Id,
                    node.Key,
                    node.Label,
                    node.SortOrder));
            }
        }

        var typeDefinitionMap = new Dictionary<int, HashSet<int>>();
        var typeMappings = new List<ResourceTypePropertyDto>(typeLinks.Count);
        for (var i = 0; i < typeLinks.Count; i++)
        {
            var link = typeLinks[i];
            if (!typeDefinitionMap.TryGetValue(link.ResourceTypeId, out var definitionsForType))
            {
                definitionsForType = new HashSet<int>();
                typeDefinitionMap[link.ResourceTypeId] = definitionsForType;
            }

            definitionsForType.Add(link.PropertyDefinitionId);
            typeMappings.Add(new ResourceTypePropertyDto(link.ResourceTypeId, link.PropertyDefinitionId));
        }

        return new PropertySchemaSnapshot(nodeMap, typeDefinitionMap, definitions, nodeDtos, typeMappings);
    }

    public HashSet<int> ResolveDefinitionIds(
        IReadOnlyList<int> propertyIds,
        string unknownPropertyMessagePrefix)
    {
        var definitionIds = new HashSet<int>();
        for (var i = 0; i < propertyIds.Count; i++)
        {
            var propertyId = propertyIds[i];
            if (!_nodeMap.ContainsKey(propertyId))
            {
                throw new AvailabilityRequestException($"{unknownPropertyMessagePrefix} contains unknown id {propertyId}.");
            }

            definitionIds.Add(ResolveDefinitionId(propertyId, _nodeMap));
        }

        return definitionIds;
    }

    public void ValidateTypeCompatibility(int resourceTypeId, IReadOnlySet<int> definitionIds)
    {
        if (!_typeDefinitionMap.TryGetValue(resourceTypeId, out var allowed))
        {
            throw new AvailabilityRequestException(
                $"Resource type {resourceTypeId} does not allow requested properties.");
        }

        foreach (var definitionId in definitionIds)
        {
            if (!allowed.Contains(definitionId))
            {
                throw new AvailabilityRequestException(
                    $"propertyIds are not compatible with resource type {resourceTypeId}.");
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
