using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController : ControllerBase
{
    private readonly IResourceCatalogService _resourceCatalogService;
    private readonly IResourceTypeCatalogService _resourceTypeCatalogService;
    private readonly IPropertySchemaService _propertySchemaService;

    public CatalogController(
        IResourceCatalogService resourceCatalogService,
        IResourceTypeCatalogService resourceTypeCatalogService,
        IPropertySchemaService propertySchemaService)
    {
        _resourceCatalogService = resourceCatalogService;
        _resourceTypeCatalogService = resourceTypeCatalogService;
        _propertySchemaService = propertySchemaService;
    }

    [HttpGet("resources")]
    public async Task<ActionResult<IReadOnlyList<ResourceDto>>> GetResourcesAsync(
        [FromQuery] bool onlySchedulable = true,
        CancellationToken ct = default)
    {
        var resources = await _resourceCatalogService.GetResourcesAsync(onlySchedulable, ct);
        return Ok(resources);
    }

    [HttpGet("resource-types")]
    public async Task<ActionResult<IReadOnlyList<ResourceTypeDto>>> GetResourceTypesAsync(
        CancellationToken ct = default)
    {
        var types = await _resourceTypeCatalogService.GetResourceTypesAsync(ct);
        return Ok(types);
    }

    [HttpGet("properties")]
    public async Task<ActionResult<PropertySchemaResponse>> GetPropertySchemaAsync(
        CancellationToken ct = default)
    {
        var schema = await _propertySchemaService.GetSchemaAsync(ct);
        return Ok(schema);
    }
}
