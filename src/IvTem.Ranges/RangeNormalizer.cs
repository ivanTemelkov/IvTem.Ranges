using System.Collections.Immutable;

namespace IvTem.Ranges;

public sealed class RangeNormalizer<T>
    where T : IComparable<T>
{
    private RangeNormalizationOptions<T> Options { get; }
    private IComparer<T> ValueComparer => Options.Comparer;

    public RangeNormalizer(RangeNormalizationOptions<T>? options = null)
    {
        Options = options ?? new RangeNormalizationOptions<T>();
    }

    /// <summary>
    /// Normalizes ranges IN-PLACE by mutating kept instances (From/To) and returning
    /// an ordered immutable array of the same references. No new ranges are constructed.
    /// Half-open semantics [From, To).
    /// </summary>
    public ImmutableArray<IRange<T>> Normalize(IEnumerable<IRange<T>> source)
        => source.CollectAndPrefilter(ValueComparer)
            .PreFilterByClampWindow(Options)
            .SortByFromThenToDescending(ValueComparer)
            .DropExactDuplicates(ValueComparer)
            .RemoveContainedRanges(ValueComparer)
            .SweepResolveOverlapsAndGaps(Options)
            .ClampResults(Options)
            .ToImmutableArray();
    
}
