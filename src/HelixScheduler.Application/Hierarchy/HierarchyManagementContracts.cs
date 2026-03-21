namespace HelixScheduler.Application.Hierarchy;

public sealed record AddParentRelationCommand(
    int ParentResourceId,
    int ChildResourceId,
    string RelationType);

public sealed record RemoveParentRelationCommand(
    int ParentResourceId,
    int ChildResourceId,
    string RelationType);

public sealed record HierarchyRelationDto(
    int ParentResourceId,
    int ChildResourceId,
    string RelationType);

public sealed record HierarchyResourceState(
    int ResourceId,
    bool IsActive,
    bool IsArchived);
