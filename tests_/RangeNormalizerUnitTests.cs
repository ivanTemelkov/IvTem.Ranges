using IvTem.Ranges;

namespace UnitTests;

internal sealed class MyRange : IRange<double>
{
    public double From { get; set; }
    public double To { get; set; }

    public string Id { get; } = Guid.NewGuid().ToString();

    public MyRange(double from, double to)
    {
        From = from;
        To = to;
    }
}

public class RangeNormalizerUnitTests
{
    private static readonly IRange<double>[] testRange1 =
    [
        new MyRange(10, 15),
        new MyRange(20, 30),
    ];
    
    [Fact]
    public void It_Normmalizes_Simple_Range()
    {
        var options = new RangeNormalizationOptions<double>
        {
            GapMode = GapMode.ExpandRight,
            OverlapResolution = OverlapResolution.KeepLeftChunk
        };

        var sut = new RangeNormalizer<double>(options);

        var result = sut.Normalize(testRange1);
    }
}