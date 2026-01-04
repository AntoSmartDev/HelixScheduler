namespace HelixScheduler.Application.ResourceCatalog;

public sealed record ResourceCatalogResource(
    int Id,
    string? Code,
    string Name,
    bool IsSchedulable,
    int TypeId,
    string TypeKey,
    string TypeLabel);

public sealed record ResourceCatalogProperty(
    int Id,
    string Key,
    string Label,
    int? ParentId,
    int? SortOrder);

public sealed record ResourcePropertyLink(
    int ResourceId,
    int PropertyId);
