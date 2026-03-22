namespace HelixScheduler.Application.Management.Properties;

public sealed record CreatePropertyCommand(
    string Key,
    string Label,
    int? SortOrder);

public sealed record UpdatePropertyCommand(
    int PropertyId,
    string Key,
    string Label,
    int? SortOrder);

public sealed record PropertyManagementDto(
    int Id,
    string Key,
    string Label,
    int? ParentId,
    int? SortOrder,
    bool IsActive);

public sealed record PropertyHierarchyRelationDto(
    int ParentPropertyId,
    int ChildPropertyId);

public sealed record AddPropertyParentRelationCommand(
    int ParentPropertyId,
    int ChildPropertyId);

public sealed record RemovePropertyParentRelationCommand(
    int ParentPropertyId,
    int ChildPropertyId);

public sealed record AssignPropertiesToResourceCommand(
    int ResourceId,
    IReadOnlyList<int> PropertyIds);

public sealed record RemovePropertiesFromResourceCommand(
    int ResourceId,
    IReadOnlyList<int> PropertyIds);

public sealed record ResourcePropertyAssignmentsDto(
    int ResourceId,
    IReadOnlyList<PropertyManagementDto> Properties);
