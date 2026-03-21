namespace HelixScheduler.Application.ResourceCatalog.Management;

public sealed record CreateResourceTypeCommand(
    string Key,
    string Label,
    int? SortOrder);

public sealed record UpdateResourceTypeCommand(
    int ResourceTypeId,
    string Key,
    string Label,
    int? SortOrder);

public sealed record ResourceTypeManagementDto(
    int Id,
    string Key,
    string Label,
    int? SortOrder,
    bool IsActive);

public sealed record CreateResourceCommand(
    string? Code,
    string Name,
    bool IsSchedulable,
    int Capacity,
    int TypeId);

public sealed record UpdateResourceCommand(
    int ResourceId,
    string? Code,
    string Name,
    bool IsSchedulable,
    int Capacity,
    int TypeId);

public sealed record ResourceManagementDto(
    int Id,
    string? Code,
    string Name,
    bool IsSchedulable,
    int Capacity,
    int TypeId,
    bool IsActive,
    bool IsArchived,
    DateTime CreatedAtUtc);
