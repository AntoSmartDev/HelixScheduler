namespace HelixScheduler.Application.ResourceCatalog;

public interface IResourceCatalogDataSource
{
    Task<IReadOnlyList<ResourceCatalogResource>> GetResourcesAsync(
        bool onlySchedulable,
        CancellationToken ct);

    Task<IReadOnlyList<ResourceCatalogProperty>> GetPropertiesAsync(CancellationToken ct);

    Task<IReadOnlyList<ResourcePropertyLink>> GetPropertyLinksAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct);
}
