namespace HelixScheduler.Application.ResourceCatalog;

public interface IResourceTypeCatalogDataSource
{
    Task<IReadOnlyList<ResourceTypeDto>> GetResourceTypesAsync(CancellationToken ct);
}
