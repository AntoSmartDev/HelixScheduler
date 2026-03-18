using HelixScheduler.Core;

namespace HelixScheduler.Application.Availability;

internal static class AvailabilitySlotPostProcessor
{
    public static IReadOnlyList<UtcSlot> ApplySlotDuration(IReadOnlyList<UtcSlot> slots, AvailabilityComputeRequest request)
    {
        if (slots.Count == 0 || !request.SlotDurationMinutes.HasValue) return slots;
        var duration = TimeSpan.FromMinutes(request.SlotDurationMinutes.Value);
        if (duration <= TimeSpan.Zero) return slots;

        var result = new List<UtcSlot>();
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var length = slot.EndUtc - slot.StartUtc;
            if (length <= TimeSpan.Zero) continue;

            var fullCount = (int)(length.Ticks / duration.Ticks);
            for (var index = 0; index < fullCount; index++)
            {
                var start = slot.StartUtc.AddTicks(duration.Ticks * index);
                result.Add(new UtcSlot(start, start.Add(duration), slot.ResourceIds));
            }

            var remainderTicks = length.Ticks - (duration.Ticks * fullCount);
            if (request.IncludeRemainderSlot && remainderTicks > 0)
            {
                var remainderStart = slot.StartUtc.AddTicks(duration.Ticks * fullCount);
                if (slot.EndUtc > remainderStart) result.Add(new UtcSlot(remainderStart, slot.EndUtc, slot.ResourceIds));
            }
        }

        return result;
    }

    public static AvailabilityExplanation BuildEmptyExplanation(AvailabilityResult result, bool hasPositiveRules, bool hasNegativeRules, bool hasBusySlots, DateOnly fromDate, DateOnly toDate)
    {
        var fromUtc = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtcExclusive = DateTime.SpecifyKind(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        if (!hasPositiveRules) return new AvailabilityExplanation("NoPositiveRule", null, fromUtc, toUtcExclusive, null, null, "No positive rules apply to the requested range.");
        if (hasBusySlots) return new AvailabilityExplanation("FullyBlockedByBusy", null, fromUtc, toUtcExclusive, null, null, "Busy events block availability in the requested range.");
        if (hasNegativeRules) return new AvailabilityExplanation("FullyBlockedByNegativeRule", null, fromUtc, toUtcExclusive, null, null, "Negative rules block availability in the requested range.");
        return new AvailabilityExplanation("PartiallyBlocked", null, fromUtc, toUtcExclusive, null, null, "Availability is blocked by rules or busy events.");
    }
}
