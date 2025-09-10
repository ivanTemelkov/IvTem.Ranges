namespace IvTem.Ranges;

public sealed class MyRange : IRange<int>
{
    public int From { get; set; }
    public int To   { get; set; }
    public override string ToString() => $"[{From},{To})";
}