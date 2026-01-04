namespace HelixScheduler.Application.ResourceCatalog;

public interface IResourceCatalogService
{
    Task<IReadOnlyList<ResourceDto>> GetResourcesAsync(
        bool onlySchedulable,
        CancellationToken ct);

    Task<IReadOnlyList<ResourcePropertyDto>> GetPropertiesAsync(CancellationToken ct);
}
