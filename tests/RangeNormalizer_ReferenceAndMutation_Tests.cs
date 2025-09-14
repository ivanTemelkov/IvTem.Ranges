namespace IvTem.Ranges.Tests;

public sealed class RangeNormalizer_ReferenceAndMutation_Tests
{
    [Fact]
    public void OutputContainsOnlyInputReferences()
    {
        var normalizer = new RangeNormalizer<double>();
        var inputRanges = new[]
        {
            TestHelpers.CreateGenericRange(1.0, 4.0),
            TestHelpers.CreateGenericRange(2.0, 6.0),
            TestHelpers.CreateGenericRange(7.0, 9.0),
        };

        var result = normalizer.Normalize(inputRanges);

        result.All(r => inputRanges.Contains(r)).Should().BeTrue("normalizer must not create new range instances");
    }

    [Fact]
    public void KeptRangesAreMutatedInPlace()
    {
        var normalizer = new RangeNormalizer<double>();
        var first = TestHelpers.CreateGenericRange(1.0, 3.0);
        var second = TestHelpers.CreateGenericRange(5.0, 7.0);

        var result = normalizer.Normalize(new[] { second, first });

        result.Should().HaveCount(2);
        result[0].Should().BeSameAs(first);
        result[1].Should().BeSameAs(second);

        var secondPass = normalizer.Normalize(result);
        secondPass[0].Should().BeSameAs(first);
        secondPass[1].Should().BeSameAs(second);
    }
}
