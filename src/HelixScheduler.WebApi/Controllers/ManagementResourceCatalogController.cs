using HelixScheduler.Application.Hierarchy;
using HelixScheduler.Application.PropertyManagement;
using HelixScheduler.Application.ResourceCatalog.Management;
using HelixScheduler.WebApi.Management;
using Microsoft.AspNetCore.Mvc;

namespace HelixScheduler.WebApi.Controllers;

[ApiController]
[Route("api/management")]
public sealed class ManagementResourceCatalogController : ManagementControllerBase
{
    private readonly IResourceTypeManagementService _resourceTypeService;
    private readonly IResourceManagementService _resourceService;
    private readonly IHierarchyManagementService _hierarchyService;
    private readonly IPropertyManagementService _propertyService;
    private readonly IResourcePropertyAssignmentManagementService _resourcePropertyAssignmentService;

    public ManagementResourceCatalogController(
        IResourceTypeManagementService resourceTypeService,
        IResourceManagementService resourceService,
        IHierarchyManagementService hierarchyService,
        IPropertyManagementService propertyService,
        IResourcePropertyAssignmentManagementService resourcePropertyAssignmentService)
    {
        _resourceTypeService = resourceTypeService;
        _resourceService = resourceService;
        _hierarchyService = hierarchyService;
        _propertyService = propertyService;
        _resourcePropertyAssignmentService = resourcePropertyAssignmentService;
    }

    [HttpPost("resource-types")]
    public async Task<ActionResult<ResourceTypeManagementDto>> CreateResourceTypeAsync(
        [FromBody] CreateResourceTypeCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceTypeService.CreateResourceTypeAsync(command, ct));
    }

    [HttpPut("resource-types")]
    public async Task<ActionResult<ResourceTypeManagementDto>> UpdateResourceTypeAsync(
        [FromBody] UpdateResourceTypeCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceTypeService.UpdateResourceTypeAsync(command, ct));
    }

    [HttpGet("resource-types")]
    public Task<IReadOnlyList<ResourceTypeManagementDto>> ListResourceTypesAsync(CancellationToken ct = default)
    {
        return _resourceTypeService.ListResourceTypesAsync(ct);
    }

    [HttpGet("resource-types/{resourceTypeId:int}")]
    public async Task<ActionResult<ResourceTypeManagementDto>> GetResourceTypeAsync(int resourceTypeId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceTypeService.GetResourceTypeAsync(resourceTypeId, ct));
    }

    [HttpPost("resource-types/{resourceTypeId:int}/activate")]
    public async Task<ActionResult<ResourceTypeManagementDto>> ActivateResourceTypeAsync(int resourceTypeId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceTypeService.ActivateResourceTypeAsync(resourceTypeId, ct));
    }

    [HttpPost("resource-types/{resourceTypeId:int}/deactivate")]
    public async Task<ActionResult<ResourceTypeManagementDto>> DeactivateResourceTypeAsync(int resourceTypeId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceTypeService.DeactivateResourceTypeAsync(resourceTypeId, ct));
    }

    [HttpPost("resources")]
    public async Task<ActionResult<ResourceManagementDto>> CreateResourceAsync(
        [FromBody] CreateResourceCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceService.CreateResourceAsync(command, ct));
    }

    [HttpPut("resources")]
    public async Task<ActionResult<ResourceManagementDto>> UpdateResourceAsync(
        [FromBody] UpdateResourceCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceService.UpdateResourceAsync(command, ct));
    }

    [HttpGet("resources")]
    public Task<IReadOnlyList<ResourceManagementDto>> ListResourcesAsync(CancellationToken ct = default)
    {
        return _resourceService.ListResourcesAsync(ct);
    }

    [HttpGet("resources/{resourceId:int}")]
    public async Task<ActionResult<ResourceManagementDto>> GetResourceAsync(int resourceId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceService.GetResourceAsync(resourceId, ct));
    }

    [HttpPost("resources/{resourceId:int}/activate")]
    public async Task<ActionResult<ResourceManagementDto>> ActivateResourceAsync(int resourceId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceService.ActivateResourceAsync(resourceId, ct));
    }

    [HttpPost("resources/{resourceId:int}/deactivate")]
    public async Task<ActionResult<ResourceManagementDto>> DeactivateResourceAsync(int resourceId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceService.DeactivateResourceAsync(resourceId, ct));
    }

    [HttpPost("resources/{resourceId:int}/archive")]
    public async Task<ActionResult<ResourceManagementDto>> ArchiveResourceAsync(int resourceId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourceService.ArchiveResourceAsync(resourceId, ct));
    }

    [HttpPost("hierarchy/relations")]
    public async Task<ActionResult<HierarchyRelationDto>> AddHierarchyRelationAsync(
        [FromBody] AddParentRelationCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _hierarchyService.AddParentRelationAsync(command, ct));
    }

    [HttpDelete("hierarchy/relations")]
    public async Task<ActionResult<HierarchyRelationDto>> RemoveHierarchyRelationAsync(
        [FromBody] RemoveParentRelationCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _hierarchyService.RemoveParentRelationAsync(command, ct));
    }

    [HttpGet("hierarchy/relations")]
    public Task<IReadOnlyList<HierarchyRelationDto>> GetHierarchyRelationsAsync(CancellationToken ct = default)
    {
        return _hierarchyService.GetHierarchyRelationsAsync(ct);
    }

    [HttpPost("properties")]
    public async Task<ActionResult<PropertyManagementDto>> CreatePropertyAsync(
        [FromBody] CreatePropertyCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _propertyService.CreatePropertyAsync(command, ct));
    }

    [HttpPut("properties")]
    public async Task<ActionResult<PropertyManagementDto>> UpdatePropertyAsync(
        [FromBody] UpdatePropertyCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _propertyService.UpdatePropertyAsync(command, ct));
    }

    [HttpGet("properties")]
    public Task<IReadOnlyList<PropertyManagementDto>> ListPropertiesAsync(CancellationToken ct = default)
    {
        return _propertyService.ListPropertiesAsync(ct);
    }

    [HttpGet("properties/{propertyId:int}")]
    public async Task<ActionResult<PropertyManagementDto>> GetPropertyAsync(int propertyId, CancellationToken ct = default)
    {
        return FromManagementResult(await _propertyService.GetPropertyAsync(propertyId, ct));
    }

    [HttpPost("properties/{propertyId:int}/activate")]
    public async Task<ActionResult<PropertyManagementDto>> ActivatePropertyAsync(int propertyId, CancellationToken ct = default)
    {
        return FromManagementResult(await _propertyService.ActivatePropertyAsync(propertyId, ct));
    }

    [HttpPost("properties/{propertyId:int}/deactivate")]
    public async Task<ActionResult<PropertyManagementDto>> DeactivatePropertyAsync(int propertyId, CancellationToken ct = default)
    {
        return FromManagementResult(await _propertyService.DeactivatePropertyAsync(propertyId, ct));
    }

    [HttpPost("properties/relations")]
    public async Task<ActionResult<PropertyHierarchyRelationDto>> AddPropertyRelationAsync(
        [FromBody] AddPropertyParentRelationCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _propertyService.AddPropertyParentRelationAsync(command, ct));
    }

    [HttpDelete("properties/relations")]
    public async Task<ActionResult<PropertyHierarchyRelationDto>> RemovePropertyRelationAsync(
        [FromBody] RemovePropertyParentRelationCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _propertyService.RemovePropertyParentRelationAsync(command, ct));
    }

    [HttpGet("properties/relations")]
    public Task<IReadOnlyList<PropertyHierarchyRelationDto>> GetPropertyRelationsAsync(CancellationToken ct = default)
    {
        return _propertyService.GetPropertyRelationsAsync(ct);
    }

    [HttpPost("resource-property-assignments/assign")]
    public async Task<ActionResult<ResourcePropertyAssignmentsDto>> AssignPropertiesToResourceAsync(
        [FromBody] AssignPropertiesToResourceCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _resourcePropertyAssignmentService.AssignPropertiesToResourceAsync(command, ct));
    }

    [HttpPost("resource-property-assignments/remove")]
    public async Task<ActionResult<ResourcePropertyAssignmentsDto>> RemovePropertiesFromResourceAsync(
        [FromBody] RemovePropertiesFromResourceCommand command,
        CancellationToken ct = default)
    {
        return FromManagementResult(await _resourcePropertyAssignmentService.RemovePropertiesFromResourceAsync(command, ct));
    }

    [HttpGet("resource-property-assignments/{resourceId:int}")]
    public async Task<ActionResult<ResourcePropertyAssignmentsDto>> GetResourcePropertiesAsync(int resourceId, CancellationToken ct = default)
    {
        return FromManagementResult(await _resourcePropertyAssignmentService.GetResourcePropertiesAsync(resourceId, ct));
    }
}
