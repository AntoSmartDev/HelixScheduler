using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Availability.QueryServices;
using HelixScheduler.Application.Management.Hierarchy;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.Management.Validation;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Application.Management.ResourceCatalog;

namespace HelixScheduler.Application.Management.CatalogRead;

public sealed class ManagementCatalogReadService : IManagementCatalogReadService
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantStore _tenantStore;
    private readonly IResourceCatalogService _resourceCatalogService;
    private readonly IResourceTypeCatalogService _resourceTypeCatalogService;
    private readonly IResourceCatalogQueryService _resourceCatalogQueryService;
    private readonly IResourceManagementService _resourceManagementService;
    private readonly IResourceTypeManagementService _resourceTypeManagementService;
    private readonly IHierarchyManagementService _hierarchyManagementService;
    private readonly IPropertySchemaService _propertySchemaService;
    private readonly IAvailabilitySummaryQueryService _availabilitySummaryQueryService;
    private readonly IManagementValidationService _managementValidationService;

    public ManagementCatalogReadService(
        ITenantContext tenantContext,
        ITenantStore tenantStore,
        IResourceCatalogService resourceCatalogService,
        IResourceTypeCatalogService resourceTypeCatalogService,
        IResourceCatalogQueryService resourceCatalogQueryService,
        IResourceManagementService resourceManagementService,
        IResourceTypeManagementService resourceTypeManagementService,
        IHierarchyManagementService hierarchyManagementService,
        IPropertySchemaService propertySchemaService,
        IAvailabilitySummaryQueryService availabilitySummaryQueryService,
        IManagementValidationService managementValidationService)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
        _resourceCatalogService = resourceCatalogService ?? throw new ArgumentNullException(nameof(resourceCatalogService));
        _resourceTypeCatalogService = resourceTypeCatalogService ?? throw new ArgumentNullException(nameof(resourceTypeCatalogService));
        _resourceCatalogQueryService = resourceCatalogQueryService ?? throw new ArgumentNullException(nameof(resourceCatalogQueryService));
        _resourceManagementService = resourceManagementService ?? throw new ArgumentNullException(nameof(resourceManagementService));
        _resourceTypeManagementService = resourceTypeManagementService ?? throw new ArgumentNullException(nameof(resourceTypeManagementService));
        _hierarchyManagementService = hierarchyManagementService ?? throw new ArgumentNullException(nameof(hierarchyManagementService));
        _propertySchemaService = propertySchemaService ?? throw new ArgumentNullException(nameof(propertySchemaService));
        _availabilitySummaryQueryService = availabilitySummaryQueryService ?? throw new ArgumentNullException(nameof(availabilitySummaryQueryService));
        _managementValidationService = managementValidationService ?? throw new ArgumentNullException(nameof(managementValidationService));
    }

    public async Task<ManagementResult<SchedulerCatalogSnapshot>> GetSchedulerCatalogSnapshotAsync(CancellationToken ct)
    {
        var tenant = await ResolveTenantAsync(ct).ConfigureAwait(false);
        if (!tenant.Succeeded)
        {
            return ManagementResult<SchedulerCatalogSnapshot>.Failure(tenant.Errors);
        }

        var resourceTypesTask = _resourceTypeCatalogService.GetResourceTypesAsync(ct);
        var resourcesTask = _resourceCatalogService.GetResourcesAsync(onlySchedulable: false, ct);
        var hierarchyTask = _hierarchyManagementService.GetHierarchyRelationsAsync(ct);
        var propertySchemaTask = _propertySchemaService.GetSchemaAsync(ct);
        var validationTask = _managementValidationService.ValidateTenantModelAsync(ct);

        await Task.WhenAll(resourceTypesTask, resourcesTask, hierarchyTask, propertySchemaTask, validationTask)
            .ConfigureAwait(false);

        var resources = await resourcesTask.ConfigureAwait(false);
        var visibleResourceIds = resources.Select(resource => resource.Id).ToHashSet();
        var hierarchy = (await hierarchyTask.ConfigureAwait(false))
            .Where(relation =>
                visibleResourceIds.Contains(relation.ParentResourceId) &&
                visibleResourceIds.Contains(relation.ChildResourceId))
            .ToList();

        return ManagementResult<SchedulerCatalogSnapshot>.Success(
            new SchedulerCatalogSnapshot(
                tenant.Value!,
                await resourceTypesTask.ConfigureAwait(false),
                resources,
                hierarchy,
                await propertySchemaTask.ConfigureAwait(false),
                await validationTask.ConfigureAwait(false)));
    }

    public async Task<ManagementResult<ResourceConfigurationSnapshot>> GetResourceConfigurationSnapshotAsync(
        ResourceConfigurationSnapshotRequest request,
        CancellationToken ct)
    {
        var requestErrors = ValidateResourceSnapshotRequest(request);
        if (requestErrors.Count > 0)
        {
            return ManagementResult<ResourceConfigurationSnapshot>.Failure(requestErrors);
        }

        var tenant = await ResolveTenantAsync(ct).ConfigureAwait(false);
        if (!tenant.Succeeded)
        {
            return ManagementResult<ResourceConfigurationSnapshot>.Failure(tenant.Errors);
        }

        var resource = await _resourceManagementService.GetResourceAsync(request.ResourceId, ct).ConfigureAwait(false);
        if (!resource.Succeeded)
        {
            return ManagementResult<ResourceConfigurationSnapshot>.Failure(resource.Errors);
        }

        var resourceTypeTask = _resourceTypeManagementService.GetResourceTypeAsync(resource.Value!.TypeId, ct);
        var assignedPropertiesTask = LoadAssignedPropertiesAsync(request.ResourceId, ct);
        var hierarchyTask = _hierarchyManagementService.GetHierarchyRelationsAsync(ct);
        var propertySchemaTask = _propertySchemaService.GetSchemaAsync(ct);
        var validationTask = _managementValidationService.ValidateResourceConfigurationAsync(request.ResourceId, ct);
        var ruleSummariesTask = _availabilitySummaryQueryService.GetRuleSummariesAsync(
            request.FromDateUtc,
            request.ToDateUtc,
            new[] { request.ResourceId },
            ct);

        var fromUtc = DateTime.SpecifyKind(request.FromDateUtc.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtcExclusive = DateTime.SpecifyKind(request.ToDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var busySummariesTask = _availabilitySummaryQueryService.GetBusyEventSummariesAsync(
            fromUtc,
            toUtcExclusive,
            new[] { request.ResourceId },
            ct);

        await Task.WhenAll(
                resourceTypeTask,
                assignedPropertiesTask,
                hierarchyTask,
                propertySchemaTask,
                validationTask,
                ruleSummariesTask,
                busySummariesTask)
            .ConfigureAwait(false);

        var resourceTypeResult = await resourceTypeTask.ConfigureAwait(false);
        var hierarchy = (await hierarchyTask.ConfigureAwait(false))
            .Where(relation => relation.ParentResourceId == request.ResourceId || relation.ChildResourceId == request.ResourceId)
            .ToList();

        return ManagementResult<ResourceConfigurationSnapshot>.Success(
            new ResourceConfigurationSnapshot(
                resource.Value,
                resourceTypeResult.Succeeded ? resourceTypeResult.Value : null,
                await assignedPropertiesTask.ConfigureAwait(false),
                hierarchy,
                await propertySchemaTask.ConfigureAwait(false),
                await ruleSummariesTask.ConfigureAwait(false),
                await busySummariesTask.ConfigureAwait(false),
                await validationTask.ConfigureAwait(false)));
    }

    private async Task<IReadOnlyList<ResourcePropertyDto>> LoadAssignedPropertiesAsync(int resourceId, CancellationToken ct)
    {
        var propertiesTask = _resourceCatalogQueryService.GetPropertiesAsync(ct);
        var linksTask = _resourceCatalogQueryService.GetPropertyLinksAsync(new[] { resourceId }, ct);

        await Task.WhenAll(propertiesTask, linksTask).ConfigureAwait(false);

        var properties = await propertiesTask.ConfigureAwait(false);
        var propertyMap = properties.ToDictionary(property => property.Id, property => property);
        var links = await linksTask.ConfigureAwait(false);

        return links
            .Select(link => propertyMap.TryGetValue(link.PropertyId, out var property) ? property : null)
            .Where(property => property != null)
            .Select(property => new ResourcePropertyDto(
                property!.Id,
                property.Key,
                property.Label,
                property.ParentId,
                property.SortOrder))
            .ToList();
    }

    private async Task<ManagementResult<TenantInfo>> ResolveTenantAsync(CancellationToken ct)
    {
        if (_tenantContext.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(_tenantContext.TenantKey))
        {
            return ManagementResult<TenantInfo>.Failure(TenantContextUnresolved());
        }

        var tenant = await _tenantStore.FindByKeyAsync(_tenantContext.TenantKey, ct).ConfigureAwait(false);
        if (tenant == null || tenant.Id != _tenantContext.TenantId)
        {
            return ManagementResult<TenantInfo>.Failure(
                CreateError(
                    "tenant.not-found",
                    ManagementErrorCategory.NotFound,
                    $"Tenant '{_tenantContext.TenantKey}' was not found.",
                    "tenant"));
        }

        return ManagementResult<TenantInfo>.Success(tenant);
    }

    private static List<ManagementError> ValidateResourceSnapshotRequest(ResourceConfigurationSnapshotRequest request)
    {
        var errors = new List<ManagementError>();
        if (request.ResourceId <= 0)
        {
            errors.Add(CreateError(
                "resource.id.required",
                ManagementErrorCategory.Validation,
                "ResourceId must be greater than zero.",
                "resourceId"));
        }

        if (request.FromDateUtc > request.ToDateUtc)
        {
            errors.Add(CreateError(
                "catalog-read.range.invalid",
                ManagementErrorCategory.Validation,
                "FromDateUtc must be less than or equal to ToDateUtc.",
                "range"));
        }

        return errors;
    }

    private static ManagementError TenantContextUnresolved()
    {
        return CreateError(
            "tenant.context.unresolved",
            ManagementErrorCategory.InvalidOperation,
            "Tenant context is not resolved.",
            "tenant");
    }

    private static ManagementError CreateError(
        string code,
        ManagementErrorCategory category,
        string message,
        string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
