namespace IvTem.Ranges;

public class RangeNormalizationOptions<T> where T : IComparable<T>
{
    public IComparer<T> Comparer { get; init; } = Comparer<T>.Default;
    public GapMode GapMode { get; init; } = GapMode.None;
    public OverlapResolution OverlapResolution { get; init; } = OverlapResolution.KeepLeftChunk;   
}

public class RangeNormalizationOptionsWithClamp<T> : RangeNormalizationOptions<T> where T : IComparable<T>
{
    public bool PreFilterOutsideWindow { get; init; } = true;
    public required T ClampMin { get; init; }
    public required T ClampMax { get; init; }   
}
