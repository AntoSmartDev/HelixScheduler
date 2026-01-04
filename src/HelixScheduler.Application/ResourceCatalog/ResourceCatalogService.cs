namespace HelixScheduler.Application.ResourceCatalog;

public sealed class ResourceCatalogService : IResourceCatalogService
{
    private readonly IResourceCatalogDataSource _dataSource;

    public ResourceCatalogService(IResourceCatalogDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<IReadOnlyList<ResourceDto>> GetResourcesAsync(
        bool onlySchedulable,
        CancellationToken ct)
    {
        var resources = await _dataSource.GetResourcesAsync(onlySchedulable, ct).ConfigureAwait(false);
        if (resources.Count == 0)
        {
            return Array.Empty<ResourceDto>();
        }

        var resourceIds = resources.Select(resource => resource.Id).ToList();
        var links = await _dataSource.GetPropertyLinksAsync(resourceIds, ct).ConfigureAwait(false);
        var properties = await _dataSource.GetPropertiesAsync(ct).ConfigureAwait(false);

        var propertyMap = properties.ToDictionary(property => property.Id, property => property);
        var resourceProperties = new Dictionary<int, List<ResourcePropertyDto>>();
        var resourcePropertyIds = new Dictionary<int, HashSet<int>>();

        for (var i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (!propertyMap.TryGetValue(link.PropertyId, out var property))
            {
                continue;
            }

            if (!resourceProperties.TryGetValue(link.ResourceId, out var list))
            {
                list = new List<ResourcePropertyDto>();
                resourceProperties[link.ResourceId] = list;
            }

            if (!resourcePropertyIds.TryGetValue(link.ResourceId, out var seen))
            {
                seen = new HashSet<int>();
                resourcePropertyIds[link.ResourceId] = seen;
            }

            if (!seen.Add(property.Id))
            {
                continue;
            }

            list.Add(new ResourcePropertyDto(
                property.Id,
                property.Key,
                property.Label,
                property.ParentId,
                property.SortOrder));
        }

        var result = new List<ResourceDto>(resources.Count);
        for (var i = 0; i < resources.Count; i++)
        {
            var resource = resources[i];
            resourceProperties.TryGetValue(resource.Id, out var props);
            props ??= new List<ResourcePropertyDto>();
            result.Add(new ResourceDto(
                resource.Id,
                resource.Code,
                resource.Name,
                resource.IsSchedulable,
                resource.TypeId,
                resource.TypeKey,
                resource.TypeLabel,
                props));
        }

        return result;
    }

    public async Task<IReadOnlyList<ResourcePropertyDto>> GetPropertiesAsync(CancellationToken ct)
    {
        var properties = await _dataSource.GetPropertiesAsync(ct).ConfigureAwait(false);
        if (properties.Count == 0)
        {
            return Array.Empty<ResourcePropertyDto>();
        }

        return properties
            .Select(property => new ResourcePropertyDto(
                property.Id,
                property.Key,
                property.Label,
                property.ParentId,
                property.SortOrder))
            .ToList();
    }
}
