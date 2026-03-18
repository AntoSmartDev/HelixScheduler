namespace HelixScheduler.Application.Availability;

public sealed record ResourceRelationLink(
    int ParentResourceId,
    int ChildResourceId,
    string RelationType);
