using System.Diagnostics.CodeAnalysis;

namespace IvTem.Ranges;

internal static class Extensions
{
    internal static List<IRange<T>> SortByFromThenToDescending<T>([NotNull] this List<IRange<T>> ranges,
        [NotNull] IComparer<T> valueComparer) where T : IComparable<T>
    {
        ranges.Sort((left, right) =>
        {
            var compareFrom = valueComparer.Compare(left.From, right.From);
            
            if (compareFrom != 0)
                return compareFrom;
            
            // To descending -> longer first at same From
            return -valueComparer.Compare(left.To, right.To);
        });

        return ranges;
    }
    
    internal static List<IRange<T>> DropExactDuplicates<T>([NotNull] this List<IRange<T>> sorted,
        [NotNull] IComparer<T> valueComparer) where T : IComparable<T>
    {
        var unique = new List<IRange<T>>(sorted.Count);
        IRange<T>? previous = null;

        foreach (var current in sorted)
        {
            if (previous is not null &&
                valueComparer.Compare(current.From, previous.From) == 0 &&
                valueComparer.Compare(current.To,   previous.To)   == 0)
            {
                continue; // duplicate
            }
            unique.Add(current);
            previous = current;
        }
        return unique;
    }
    
    internal static List<IRange<T>> CollectAndPrefilter<T>([NotNull] this IEnumerable<IRange<T>> source,
        [NotNull] IComparer<T> valueComparer) where T  : IComparable<T>
    {
        var ranges = new List<IRange<T>>();
        foreach (var range in source)
        {
            if (valueComparer.Compare(range.From, range.To) > 0)
                continue; // invalid
            
            if (valueComparer.Compare(range.From, range.To) == 0)
                continue; // empty
            
            ranges.Add(range);
        }
        return ranges;
    }
    
    /// <summary>
    /// With sort (From asc, To desc), any later range with To <= maxKeptTo is fully contained.
    /// Keep only the outer (bigger) ranges.
    /// </summary>
    internal static List<IRange<T>> RemoveContainedRanges<T>([NotNull] this List<IRange<T>> sorted,
        [NotNull] IComparer<T> valueComparer) where T  : IComparable<T>
    {
        var output = new List<IRange<T>>(sorted.Count);
        var first = sorted[0];
        output.Add(first);
        var maxKeptTo = first.To;

        for (int i = 1; i < sorted.Count; i++)
        {
            var candidate = sorted[i];
            if (valueComparer.Compare(candidate.To, maxKeptTo) <= 0)
            {
                // fully contained -> ignore
                continue;
            }
            output.Add(candidate);
            maxKeptTo = candidate.To;
        }
        return output;
    }
    
    
    /// <summary>
    /// Linear sweep: resolve partial overlaps per OverlapResolution and handle gaps per GapMode.
    /// Mutates only the kept instances.
    /// </summary>
    internal static List<IRange<T>> SweepResolveOverlapsAndGaps<T>([NotNull] this List<IRange<T>> ranges,
        [NotNull] RangeNormalizationOptions<T> options) where T  : IComparable<T>
    {
        var output = new List<IRange<T>>(ranges.Count);
        var current = ranges[0];

        for (int i = 1; i < ranges.Count; i++)
        {
            var nextRange = ranges[i];
            int relation = options.Comparer.Compare(nextRange.From, current.To);

            if (relation < 0)
            {
                // Partial overlap (containments already removed)
                ResolvePartialOverlap(output, ref current, nextRange, options.Comparer, options.OverlapResolution);
            }
            else if (relation == 0)
            {
                // Adjacency: do NOT merge (by your requirement). Just emit current and move on.
                AddIfNotEmpty(output, current, options.Comparer);
                current = nextRange;
            }
            else // relation > 0 -> gap
            {
                HandleGap(output, ref current, nextRange, options.Comparer, options.GapMode);
            }
        }

        AddIfNotEmpty(output, current, options.Comparer);
        return output;
    }
    
    internal static List<IRange<T>> PreFilterByClampWindow<T>([NotNull] this List<IRange<T>> ranges,
        RangeNormalizationOptions<T> options) where T  : IComparable<T>
    {
        if (options is not RangeNormalizationOptionsWithClamp<T> withClamp)
            return ranges;

        if (withClamp.PreFilterOutsideWindow == false)
            return ranges;
            
        var kept = new List<IRange<T>>(ranges.Count);
        foreach (var range in ranges)
        {
            if (IsEntirelyOutsideClamp(range, options.Comparer, withClamp.ClampMax, withClamp.ClampMin) == false)
                kept.Add(range);
            // else: drop early—no effect on correctness since it can never intersect the window
        }
        return kept;
    }
    
    private static bool IsEntirelyOutsideClamp<T>(IRange<T> range,
        IComparer<T> valueComparer,
        T clamMax,
        T clampMin)
        where T : IComparable<T>
    {
        bool rightOfWindow = valueComparer.Compare(range.From, clamMax) >= 0;
        bool leftOfWindow  = valueComparer.Compare(range.To, clampMin) <= 0;

        return rightOfWindow || leftOfWindow;
    }
    
    private static void HandleGap<T>(List<IRange<T>> output, ref IRange<T> current, IRange<T> nextRange,
        IComparer<T> comparer,
        GapMode gapMode) where T  : IComparable<T>
    {
        switch (gapMode)
        {
            case GapMode.None:
                AddIfNotEmpty(output, current, comparer);
                current = nextRange;
                break;

            case GapMode.ExpandRight:
                // Bridge by setting left.To = right.From
                current.To = nextRange.From;
                AddIfNotEmpty(output, current, comparer);
                current = nextRange;
                break;

            case GapMode.ExpandLeft:
                // Shift right to start at left.To
                nextRange.From = current.To;
                AddIfNotEmpty(output, current, comparer);
                current = nextRange;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(gapMode));
        }
    }

    private static void AddIfNotEmpty<T>(List<IRange<T>> output, IRange<T> range, IComparer<T> comparer) where T  : IComparable<T>
    {
        if (comparer.Compare(range.From, range.To) == 0)
            return;
        
        output.Add(range);
    }
    
    private static void ResolvePartialOverlap<T>(List<IRange<T>> output, ref IRange<T> current, IRange<T> nextRange,
        IComparer<T> comparer, OverlapResolution overlapResolution) where T  : IComparable<T>
    {
        switch (overlapResolution)
        {
            case OverlapResolution.KeepLeftChunk:
                // Keep current; trim head of nextRange to start at current.To
                nextRange.From = current.To;
                AddIfNotEmpty(output, current, comparer);
                current = nextRange; // may become empty; next loop will move past it if so
                break;

            case OverlapResolution.KeepRightChunk:
                // Trim tail of current to end at nextRange.From; keep nextRange
                current.To = nextRange.From;
                AddIfNotEmpty(output, current, comparer);
                current = nextRange;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(overlapResolution));
        }
    }
    
    internal static List<IRange<T>> ClampResults<T>(this List<IRange<T>> ranges, RangeNormalizationOptions<T>  options) where T : IComparable<T>
    {
        if (options is not RangeNormalizationOptionsWithClamp<T> withClamp)
            return ranges;

        var clamped = new List<IRange<T>>(ranges.Count);
        foreach (var range in ranges)
        {
            if (withClamp.ClampMin is not null && options.Comparer.Compare(range.From, withClamp.ClampMin) < 0)
                range.From = withClamp.ClampMin;
            
            if (withClamp.ClampMax is not null && options.Comparer.Compare(range.To, withClamp.ClampMax) > 0)
                range.To = withClamp.ClampMax;

            var fromToRelation = options.Comparer.Compare(range.From, range.To);
            
            if (fromToRelation < 0)
                clamped.Add(range);
        }
        return clamped;
    }
}