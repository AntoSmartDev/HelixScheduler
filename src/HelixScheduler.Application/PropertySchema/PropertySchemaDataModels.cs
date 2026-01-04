namespace HelixScheduler.Application.PropertySchema;

public sealed record PropertySchemaNode(
    int Id,
    int? ParentId,
    string Key,
    string Label,
    int? SortOrder);

public sealed record ResourceTypePropertyLink(
    int ResourceTypeId,
    int PropertyDefinitionId);

public sealed record ResourceTypeAssignment(
    int ResourceId,
    int ResourceTypeId);
