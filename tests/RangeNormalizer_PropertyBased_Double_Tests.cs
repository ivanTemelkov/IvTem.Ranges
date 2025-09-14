namespace IvTem.Ranges.Tests;

using FsCheck;
using FsCheck.Xunit;

public sealed class RangeNormalizer_PropertyBased_Double_Tests
{
    private static IEnumerable<IRange<double>> CreateRanges(double[] starts, double[] widths)
    {
        var list = new List<IRange<double>>();
        var count = Math.Min(starts.Length, widths.Length);

        for (int index = 0; index < count; index++)
        {
            var from = starts[index];

            // Keep sizes controlled and non-negative
            var magnitude = Math.Abs(widths[index]);
            var boundedMagnitude = double.IsFinite(magnitude) ? Math.Min(magnitude, 1000.0) : 0.0;

            var to = from + boundedMagnitude;
            if (double.IsFinite(from) && double.IsFinite(to) && to > from)
            {
                list.Add(TestHelpers.CreateGenericRange(from, to));
            }
        }

        return list;
    }

    [Property(MaxTest = 250)]
    public void Normalization_IsIdempotent_AndProducesNonOverlappingOutput(double[] starts, double[] widths)
    {
        var input = CreateRanges(starts, widths).ToArray();
        var normalizer = new RangeNormalizer<double>();

        var firstPass = normalizer.Normalize(input);
        var secondPass = normalizer.Normalize(firstPass);

        // Idempotence
        secondPass.Should().BeEquivalentTo(firstPass, options => options.WithStrictOrdering());

        // Non-overlapping (half-open): previous.To <= next.From
        for (int index = 1; index < firstPass.Length; index++)
        {
            firstPass[index - 1].To.Should().BeLessThanOrEqualTo(firstPass[index].From);
        }

        // All outputs must be original references
        firstPass.All(r => input.Contains(r)).Should().BeTrue();
    }

    [Property(MaxTest = 200)]
    public void AllOutputsHaveFromStrictlyLessThanTo(double[] starts, double[] widths)
    {
        var input = CreateRanges(starts, widths).ToArray();
        var normalizer = new RangeNormalizer<double>();
        var result = normalizer.Normalize(input);

        result.Should().OnlyContain(range => Comparer<double>.Default.Compare(range.From, range.To) < 0);
    }
}
