namespace HelixScheduler.Application.ResourceCatalog;

public sealed class ResourceTypeCatalogService : IResourceTypeCatalogService
{
    private readonly IResourceTypeCatalogQueryService _queryService;

    public ResourceTypeCatalogService(IResourceTypeCatalogQueryService queryService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    }

    public Task<IReadOnlyList<ResourceTypeDto>> GetResourceTypesAsync(CancellationToken ct)
    {
        return _queryService.GetResourceTypesAsync(ct);
    }
}
