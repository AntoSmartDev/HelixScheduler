namespace HelixScheduler.Application.ResourceCatalog;

public sealed record ResourcePropertyDto(
    int Id,
    string Key,
    string Label,
    int? ParentId,
    int? SortOrder);

public sealed record ResourceDto(
    int Id,
    string? Code,
    string Name,
    bool IsSchedulable,
    int TypeId,
    string TypeKey,
    string TypeLabel,
    IReadOnlyList<ResourcePropertyDto> Properties);

public sealed record ResourceTypeDto(
    int Id,
    string Key,
    string Label,
    int? SortOrder);
