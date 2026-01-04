namespace HelixScheduler.Application.ResourceCatalog;

public interface IResourceTypeCatalogService
{
    Task<IReadOnlyList<ResourceTypeDto>> GetResourceTypesAsync(CancellationToken ct);
}
