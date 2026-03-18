namespace HelixScheduler.Core;

/// <summary>
/// Property filter used by callers to select candidate resources.
/// </summary>
public sealed record PropertyFilter(int PropertyId, bool IncludePropertyDescendants);
