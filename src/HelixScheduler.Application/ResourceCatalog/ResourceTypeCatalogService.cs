namespace HelixScheduler.Application.ResourceCatalog;

public sealed class ResourceTypeCatalogService : IResourceTypeCatalogService
{
    private readonly IResourceTypeCatalogDataSource _dataSource;

    public ResourceTypeCatalogService(IResourceTypeCatalogDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public Task<IReadOnlyList<ResourceTypeDto>> GetResourceTypesAsync(CancellationToken ct)
    {
        return _dataSource.GetResourceTypesAsync(ct);
    }
}
