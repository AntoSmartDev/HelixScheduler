namespace HelixScheduler.Core;

/// <summary>
/// Resource identity with its required type identifier.
/// </summary>
public sealed record ResourceDefinition(int ResourceId, ResourceTypeId TypeId);
