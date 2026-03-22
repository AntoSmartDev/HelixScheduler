using HelixScheduler.Application.Management;

namespace HelixScheduler.Application.Management.Validation;

public sealed record ManagementValidationResult(
    bool IsValid,
    IReadOnlyList<ManagementError> Findings);

public sealed record ResourceValidationSnapshot(
    int Id,
    int TypeId,
    bool IsActive,
    bool IsArchived);

public sealed record ResourceTypeValidationSnapshot(
    int Id,
    bool IsActive);

public sealed record ResourceRelationValidationSnapshot(
    int ParentResourceId,
    int ChildResourceId,
    string RelationType);

public sealed record PropertyValidationSnapshot(
    int Id,
    int? ParentId,
    bool IsActive);

public sealed record ResourcePropertyAssignmentSnapshot(
    int ResourceId,
    int PropertyId);

public sealed record ResourceTypePropertyMappingSnapshot(
    int ResourceTypeId,
    int PropertyId);

public sealed record RuleResourceBindingSnapshot(
    long RuleId,
    int ResourceId,
    bool RuleIsActive);

public sealed record BusyEventResourceBindingSnapshot(
    long BusyEventId,
    int ResourceId,
    bool BusyEventIsActive);

public sealed record InactiveResourcePropertyAssignmentReference(
    int ResourceId,
    int PropertyId);

public sealed record InactiveResourceTypePropertyMappingReference(
    int ResourceTypeId,
    int PropertyId);

public sealed record LegacyPropertyReferenceSnapshot(
    IReadOnlyList<InactiveResourcePropertyAssignmentReference> InactiveResourcePropertyAssignments,
    IReadOnlyList<InactiveResourceTypePropertyMappingReference> InactiveResourceTypePropertyMappings);

public sealed record LegacyPropertyReferenceCleanupResult(
    int RemovedResourcePropertyAssignments,
    int RemovedResourceTypePropertyMappings);

public sealed record LegacyConsistencyRepairPreview(
    IReadOnlyList<InactiveResourcePropertyAssignmentReference> InactiveResourcePropertyAssignments,
    IReadOnlyList<InactiveResourceTypePropertyMappingReference> InactiveResourceTypePropertyMappings)
{
    public int TotalRepairableItems =>
        InactiveResourcePropertyAssignments.Count + InactiveResourceTypePropertyMappings.Count;
}

public sealed record LegacyConsistencyReport(
    ManagementValidationResult Validation,
    LegacyConsistencyRepairPreview RepairPreview);

public sealed record LegacyConsistencyCleanupResult(
    int RemovedResourcePropertyAssignments,
    int RemovedResourceTypePropertyMappings,
    LegacyConsistencyReport ReportAfter);

public sealed record TenantValidationSnapshot(
    IReadOnlyList<ResourceValidationSnapshot> Resources,
    IReadOnlyList<ResourceTypeValidationSnapshot> ResourceTypes,
    IReadOnlyList<ResourceRelationValidationSnapshot> ResourceRelations,
    IReadOnlyList<PropertyValidationSnapshot> Properties,
    IReadOnlyList<ResourcePropertyAssignmentSnapshot> ResourcePropertyAssignments,
    IReadOnlyList<ResourceTypePropertyMappingSnapshot> ResourceTypePropertyMappings,
    IReadOnlyList<RuleResourceBindingSnapshot> RuleResourceBindings,
    IReadOnlyList<BusyEventResourceBindingSnapshot> BusyEventResourceBindings);
