namespace HelixScheduler.Application.ResourceCatalog;

public interface IResourceTypeCatalogQueryService
{
    Task<IReadOnlyList<ResourceTypeDto>> GetResourceTypesAsync(CancellationToken ct);
}
