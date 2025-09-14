namespace IvTem.Ranges.Tests;

public sealed class RangeNormalizer_Generic_DateTime_Tests
{
    private static RangeNormalizer<DateTime> CreateDateTimeNormalizer(RangeNormalizationOptions<DateTime> options)
    {
        return new RangeNormalizer<DateTime>(options);
    }

    [Fact]
    public void WorksWithDateTimeAndEdgeValues()
    {
        var normalizer = CreateDateTimeNormalizer(new  RangeNormalizationOptions<DateTime>());
        var a = TestHelpers.CreateGenericRange(DateTime.MinValue, DateTime.MinValue.AddSeconds(1));
        var b = TestHelpers.CreateGenericRange(DateTime.MaxValue.AddSeconds(-1), DateTime.MaxValue);

        var result = normalizer.Normalize(new[] { b, a });

        result.Endpoints().Should().Equal(new[]
        {
            (DateTime.MinValue, DateTime.MinValue.AddSeconds(1)),
            (DateTime.MaxValue.AddSeconds(-1), DateTime.MaxValue),
        });
    }

    [Fact]
    public void ClampWithDateTime_TrimAndDropDegenerate()
    {
        var windowStart = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var windowEnd = new DateTime(2025, 01, 10, 0, 0, 0, DateTimeKind.Utc);

        var options = new RangeNormalizationOptionsWithClamp<DateTime>()
        {
            ClampMin = windowStart,
            ClampMax = windowEnd
        };
        
        var normalizer = new RangeNormalizer<DateTime>(options);

        var left = TestHelpers.CreateGenericRange(windowStart.AddDays(-2), windowStart.AddDays(1)); // -> [start, +1d)
        var inside = TestHelpers.CreateGenericRange(windowStart.AddDays(2), windowStart.AddDays(3)); // unchanged
        var right = TestHelpers.CreateGenericRange(windowEnd.AddDays(-1), windowEnd.AddDays(2)); // -> [-1d, end)

        var result = normalizer.Normalize(new[] { right, inside, left });

        result.Endpoints().Should().BeEquivalentTo(new[]
        {
            (windowStart, windowStart.AddDays(1)),
            (windowStart.AddDays(2), windowStart.AddDays(3)),
            (windowEnd.AddDays(-1), windowEnd),
        }, o => o.WithoutStrictOrdering());
    }
}
