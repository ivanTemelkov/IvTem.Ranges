namespace IvTem.Ranges.Tests;

public sealed class RangeNormalizer_Double_ClampTests
{
    private static RangeNormalizer<double> CreateClampingNormalizer(double windowFrom, double windowTo)
    {
        var options = new RangeNormalizationOptionsWithClamp<double>
        {
            PreFilterOutsideWindow = false,
            ClampMin = windowFrom,
            ClampMax = windowTo
        };
        
        
        return new RangeNormalizer<double>(options);
    }

    [Fact]
    public void RangesEntirelyOutsideWindow_AreDropped()
    {
        var normalizer = CreateClampingNormalizer(2.0, 8.0);

        var outsideOnLeftTouching = TestHelpers.CreateGenericRange(0.0, 2.0); // becomes empty after trim -> dropped
        var outsideOnRightTouching = TestHelpers.CreateGenericRange(8.0, 10.0); // empty -> dropped
        var farLeft = TestHelpers.CreateGenericRange(double.NegativeInfinity, 1.0); // dropped
        var farRight = TestHelpers.CreateGenericRange(9.0, double.PositiveInfinity); // dropped
        var inside = TestHelpers.CreateGenericRange(3.0, 4.0); // kept

        var result = normalizer.Normalize(new[] { farRight, inside, outsideOnLeftTouching, outsideOnRightTouching, farLeft });

        result.Should().ContainSingle().Which.Should().BeSameAs(inside);
        TestHelpers.ToEndpoints(result[0]).Should().Be((3.0, 4.0));
    }

    [Fact]
    public void PartiallyOverlappingRanges_AreTrimmedToWindow()
    {
        var normalizer = CreateClampingNormalizer(2.0, 8.0);

        var leftPartial = TestHelpers.CreateGenericRange(0.0, 3.0);   // -> [2,3)
        var inside = TestHelpers.CreateGenericRange(4.0, 5.0);        // unchanged
        var rightPartial = TestHelpers.CreateGenericRange(7.0, 10.0); // -> [7,8)

        var result = normalizer.Normalize(new[] { rightPartial, inside, leftPartial });

        result.Endpoints().Should().BeEquivalentTo(new[] { (2.0, 3.0), (4.0, 5.0), (7.0, 8.0) }, o => o.WithoutStrictOrdering());

        leftPartial.From.Should().Be(2.0);
        leftPartial.To.Should().Be(3.0);
        inside.From.Should().Be(4.0);
        inside.To.Should().Be(5.0);
        rightPartial.From.Should().Be(7.0);
        rightPartial.To.Should().Be(8.0);
    }

    [Fact]
    public void ClampIsAppliedAfterMerge()
    {
        var normalizer = CreateClampingNormalizer(10.0, 20.0);

        // These first merge to [5,25), then clamp -> [10,20)
        var left = TestHelpers.CreateGenericRange(5.0, 12.0);
        var middle = TestHelpers.CreateGenericRange(10.0, 18.0);
        var right = TestHelpers.CreateGenericRange(17.0, 25.0);

        var result = normalizer.Normalize(new[] { left, right, middle });

        result.Should().HaveCount(3);
        TestHelpers.ToEndpoints(result[0]).Should().Be((10.0, 20.0));
        result[0].Should().BeOneOf(left, middle, right);
    }

    [Fact]
    public void DegenerateAfterTrim_IsDropped()
    {
        var normalizer = CreateClampingNormalizer(10.0, 20.0);

        var atLeftEdge = TestHelpers.CreateGenericRange(5.0, 10.0);
        var atRightEdge = TestHelpers.CreateGenericRange(20.0, 25.0);

        var result = normalizer.Normalize(new[] { atLeftEdge, atRightEdge });

        result.Should().BeEmpty();
    }
}
