namespace HelixScheduler.Core;

public enum SchedulingRuleKind : byte
{
    Weekly = 1,
    Monthly = 2,
    SingleDate = 3,
    Range = 4,
    Repeating = 5
}
