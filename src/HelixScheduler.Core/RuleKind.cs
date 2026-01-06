namespace HelixScheduler.Core;

public enum RuleKind : byte
{
    RecurringWeekly = 1,
    SingleDate = 2,
    Range = 3,
    Monthly = 4,
    Repeating = 5
}
