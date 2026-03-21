using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Availability;
using HelixScheduler.Application.Hierarchy;
using HelixScheduler.Application.ManagementValidation;
using HelixScheduler.Application.PropertySchema;
using HelixScheduler.Application.ResourceCatalog;
using HelixScheduler.Application.ResourceCatalog.Management;

namespace HelixScheduler.Application.CatalogRead;

public sealed record SchedulerCatalogSnapshot(
    TenantInfo Tenant,
    IReadOnlyList<ResourceTypeDto> ResourceTypes,
    IReadOnlyList<ResourceDto> Resources,
    IReadOnlyList<HierarchyRelationDto> HierarchyRelations,
    PropertySchemaResponse PropertySchema,
    ManagementValidationResult Validation);

public sealed record ResourceConfigurationSnapshotRequest(
    int ResourceId,
    DateOnly FromDateUtc,
    DateOnly ToDateUtc);

public sealed record ResourceConfigurationSnapshot(
    ResourceManagementDto Resource,
    ResourceTypeManagementDto? ResourceType,
    IReadOnlyList<ResourcePropertyDto> AssignedProperties,
    IReadOnlyList<HierarchyRelationDto> HierarchyRelations,
    PropertySchemaResponse PropertySchema,
    IReadOnlyList<RuleSummary> Rules,
    IReadOnlyList<BusyEventSummary> BusyEvents,
    ManagementValidationResult Validation);
