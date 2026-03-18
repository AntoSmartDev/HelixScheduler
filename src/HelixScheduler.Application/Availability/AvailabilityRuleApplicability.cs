using HelixScheduler.Core;

namespace HelixScheduler.Application.Availability;

internal static class AvailabilityRuleApplicability
{
    public static bool RuleAppliesToPeriod(AvailabilityRule rule, DatePeriod period)
    {
        return rule.Kind switch
        {
            RuleKind.RecurringWeekly => WeeklyRuleApplies(rule, period),
            RuleKind.SingleDate => rule.SingleDate != null && rule.SingleDate.Value >= period.From && rule.SingleDate.Value <= period.To,
            RuleKind.Range => RangeRuleApplies(rule, period),
            RuleKind.Monthly => MonthlyRuleApplies(rule, period),
            RuleKind.Repeating => RepeatingRuleApplies(rule, period),
            _ => false
        };
    }

    private static bool WeeklyRuleApplies(AvailabilityRule rule, DatePeriod period)
    {
        if (rule.DaysOfWeekMask == null) return false;
        var start = rule.FromDate ?? period.From;
        var end = rule.ToDate ?? period.To;
        if (end < period.From || start > period.To) return false;
        var from = start < period.From ? period.From : start;
        var to = end > period.To ? period.To : end;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var bit = 1 << (int)day.DayOfWeek;
            if ((rule.DaysOfWeekMask.Value & bit) == bit) return true;
        }

        return false;
    }

    private static bool RangeRuleApplies(AvailabilityRule rule, DatePeriod period)
    {
        var start = rule.FromDate ?? period.From;
        var end = rule.ToDate ?? period.To;
        return !(end < period.From || start > period.To);
    }

    private static bool MonthlyRuleApplies(AvailabilityRule rule, DatePeriod period)
    {
        if (rule.DayOfMonth == null || rule.DayOfMonth <= 0 || rule.DayOfMonth > 31) return false;
        for (var day = period.From; day <= period.To; day = day.AddDays(1)) if (day.Day == rule.DayOfMonth.Value) return true;
        return false;
    }

    private static bool RepeatingRuleApplies(AvailabilityRule rule, DatePeriod period)
    {
        if (rule.IntervalDays == null || rule.IntervalDays <= 0) return false;
        var start = rule.FromDate ?? period.From;
        var end = rule.ToDate ?? period.To;
        if (end < period.From || start > period.To) return false;
        if (start < period.From)
        {
            var delta = period.From.DayNumber - start.DayNumber;
            start = start.AddDays((delta / rule.IntervalDays.Value) * rule.IntervalDays.Value);
        }

        return start <= end && start <= period.To;
    }
}
