namespace HelixScheduler.Application.PropertySchema;

public sealed record PropertyDefinitionDto(
    int Id,
    string Key,
    string Label,
    int? SortOrder);

public sealed record PropertyNodeDto(
    int Id,
    int DefinitionId,
    int? ParentId,
    string Key,
    string Label,
    int? SortOrder);

public sealed record ResourceTypePropertyDto(
    int ResourceTypeId,
    int PropertyDefinitionId);

public sealed record PropertySchemaResponse(
    IReadOnlyList<PropertyDefinitionDto> Definitions,
    IReadOnlyList<PropertyNodeDto> Nodes,
    IReadOnlyList<ResourceTypePropertyDto> TypeMappings);
