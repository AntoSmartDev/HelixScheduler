using HelixScheduler.Application.ResourceCatalog;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.Controllers;

[ApiController]
[Route("api/resources")]
public sealed class ResourcesController : ControllerBase
{
    private readonly IResourceCatalogService _resourceCatalogService;

    public ResourcesController(IResourceCatalogService resourceCatalogService)
    {
        _resourceCatalogService = resourceCatalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResourceSummary>>> GetResourcesAsync(
        [FromQuery] bool onlySchedulable = true,
        CancellationToken ct = default)
    {
        var resources = await _resourceCatalogService.GetResourcesAsync(onlySchedulable, ct);
        var result = resources
            .Select(resource => new ResourceSummary(
                resource.Id,
                resource.Code,
                resource.Name,
                resource.IsSchedulable))
            .ToList();

        return result;
    }

    public sealed record ResourceSummary(
        int Id,
        string? CodeName,
        string Name,
        bool IsSchedulable);
}
