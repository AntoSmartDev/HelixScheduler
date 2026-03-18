namespace HelixScheduler.Core;

public static class SlotComposition
{
    public static List<UtcSlot> IntersectByTime(
        IReadOnlyList<UtcSlot> first,
        IReadOnlyList<UtcSlot> second,
        IReadOnlyCollection<int>? resultResourceIds = null)
    {
        var resourceIds = resultResourceIds ?? Array.Empty<int>();
        var result = new List<UtcSlot>();
        var i = 0;
        var j = 0;

        while (i < first.Count && j < second.Count)
        {
            var start = first[i].StartUtc > second[j].StartUtc ? first[i].StartUtc : second[j].StartUtc;
            var end = first[i].EndUtc < second[j].EndUtc ? first[i].EndUtc : second[j].EndUtc;

            if (end > start)
            {
                result.Add(new UtcSlot(start, end, resourceIds));
            }

            if (first[i].EndUtc <= second[j].EndUtc)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return result;
    }

    public static List<UtcSlot> Normalize(IReadOnlyList<UtcSlot> slots, bool mergeByResources)
    {
        if (slots.Count == 0)
        {
            return new List<UtcSlot>();
        }

        var ordered = new List<UtcSlot>(slots);
        ordered.Sort(SlotTimeComparer.Instance);

        var normalized = new List<UtcSlot> { ordered[0] };
        for (var index = 1; index < ordered.Count; index++)
        {
            var last = normalized[^1];
            var current = ordered[index];

            var canMerge = current.StartUtc <= last.EndUtc;
            if (mergeByResources)
            {
                canMerge = canMerge && SameResources(last.ResourceIds, current.ResourceIds);
            }

            if (canMerge)
            {
                var end = current.EndUtc > last.EndUtc ? current.EndUtc : last.EndUtc;
                normalized[^1] = new UtcSlot(last.StartUtc, end, last.ResourceIds);
            }
            else
            {
                normalized.Add(current);
            }
        }

        return normalized;
    }

    public static List<UtcSlot> Project(IReadOnlyList<UtcSlot> slots, IReadOnlyCollection<int> resourceIds)
    {
        if (slots.Count == 0)
        {
            return new List<UtcSlot>();
        }

        var projected = new List<UtcSlot>(slots.Count);
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            projected.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, resourceIds));
        }

        return projected;
    }

    public static List<UtcSlot> UnionByTime(
        IReadOnlyList<int> resourceIds,
        IReadOnlyDictionary<int, List<UtcSlot>> perResource,
        IReadOnlyCollection<int>? resultResourceIds = null)
    {
        var projectedIds = resultResourceIds ?? Array.Empty<int>();
        var slots = new List<UtcSlot>();

        for (var i = 0; i < resourceIds.Count; i++)
        {
            if (!perResource.TryGetValue(resourceIds[i], out var resourceSlots))
            {
                continue;
            }

            var needed = slots.Count + resourceSlots.Count;
            if (slots.Capacity < needed)
            {
                slots.Capacity = needed;
            }

            for (var s = 0; s < resourceSlots.Count; s++)
            {
                var slot = resourceSlots[s];
                slots.Add(new UtcSlot(slot.StartUtc, slot.EndUtc, projectedIds));
            }
        }

        return Normalize(slots, mergeByResources: false);
    }

    private static bool SameResources(IReadOnlyCollection<int> first, IReadOnlyCollection<int> second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        using var firstEnumerator = first.GetEnumerator();
        using var secondEnumerator = second.GetEnumerator();
        while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext())
        {
            if (firstEnumerator.Current != secondEnumerator.Current)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class SlotTimeComparer : IComparer<UtcSlot>
    {
        public static SlotTimeComparer Instance { get; } = new();

        public int Compare(UtcSlot? x, UtcSlot? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var startCompare = x.StartUtc.CompareTo(y.StartUtc);
            if (startCompare != 0)
            {
                return startCompare;
            }

            return x.EndUtc.CompareTo(y.EndUtc);
        }
    }
}
