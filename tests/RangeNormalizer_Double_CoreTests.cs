namespace IvTem.Ranges.Tests;

public sealed class RangeNormalizer_Double_CoreTests
{
    private static RangeNormalizer<double> CreateNormalizer(
        IComparer<double>? comparer = null,
        Action<RangeNormalizationOptions<double>>? configureOptions = null)
    {
        
        RangeNormalizationOptions<double> options;

        options = comparer is null
            ? new RangeNormalizationOptions<double>()
            : new RangeNormalizationOptions<double>()
            {
                Comparer = comparer,
            };

        configureOptions?.Invoke(options);
        return new RangeNormalizer<double>(options);
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        var normalizer = CreateNormalizer();
        var result = normalizer.Normalize([]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void SingleRange_IsUnchanged_AndReferenceIsPreserved()
    {
        var normalizer = CreateNormalizer();
        var singleRange = TestHelpers.CreateGenericRange(1.0, 5.0);

        var result = normalizer.Normalize([singleRange]);

        result.Should().HaveCount(1);
        result[0].Should().BeSameAs(singleRange);
        TestHelpers.ToEndpoints(result[0]).Should().Be((1.0, 5.0));
    }

    [Fact]
    public void UnsortedInput_IsSortedByFrom()
    {
        var normalizer = CreateNormalizer();
        var rangeTenToTwelve = TestHelpers.CreateGenericRange(10.0, 12.0);
        var rangeMinusFiveToZero = TestHelpers.CreateGenericRange(-5.0, 0.0);
        var rangeZeroToOne = TestHelpers.CreateGenericRange(0.0, 1.0);

        var result = normalizer.Normalize(new IRange<double>[] { rangeTenToTwelve, rangeZeroToOne, rangeMinusFiveToZero });

        result.Endpoints().Should().Equal(new[] { (-5.0, 0.0), (0.0, 1.0), (10.0, 12.0) });
        result.All(r => new[] { rangeTenToTwelve, rangeMinusFiveToZero, rangeZeroToOne }.Contains(r))
              .Should().BeTrue("no new instances should be constructed");
    }


    [Fact]
    public void TouchingRanges_AreNotMerged_BecauseHalfOpen()
    {
        var normalizer = CreateNormalizer();
        var left = TestHelpers.CreateGenericRange(1.0, 3.0);
        var right = TestHelpers.CreateGenericRange(3.0, 5.0);

        var result = normalizer.Normalize(new[] { right, left });

        result.Endpoints().Should().Equal(new[] { (1.0, 3.0), (3.0, 5.0) });
    }

    [Fact]
    public void InvalidOrEmptyRanges_AreDropped()
    {
        var normalizer = CreateNormalizer();
        var emptyRange = TestHelpers.CreateGenericRange(5.0, 5.0);
        var invertedRange = TestHelpers.CreateGenericRange(7.0, 2.0);
        var validRange = TestHelpers.CreateGenericRange(0.0, 1.0);

        var result = normalizer.Normalize(new[] { emptyRange, validRange, invertedRange });

        result.Should().ContainSingle().Which.Should().BeSameAs(validRange);
        TestHelpers.ToEndpoints(result[0]).Should().Be((0.0, 1.0));
    }

    [Fact]
    public void Idempotence_NormalizingTwice_YieldsSameResultAndReferences()
    {
        var normalizer = CreateNormalizer();
        var firstRange = TestHelpers.CreateGenericRange(1.0, 3.0);
        var secondRange = TestHelpers.CreateGenericRange(2.0, 5.0);

        var firstResult = normalizer.Normalize(new[] { firstRange, secondRange });
        var secondResult = normalizer.Normalize(firstResult);

        secondResult.Should().BeEquivalentTo(firstResult, options => options.WithStrictOrdering());
        secondResult[0].Should().BeSameAs(firstResult[0]);
        if (secondResult.Length > 1) secondResult[1].Should().BeSameAs(firstResult[1]);
    }

    [Fact]
    public void NoOverlapsExist_AfterNormalization()
    {
        var normalizer = CreateNormalizer();
        var a = TestHelpers.CreateGenericRange(-10.0, -1.0);
        var b = TestHelpers.CreateGenericRange(-1.0, 0.0); // touching a
        var c = TestHelpers.CreateGenericRange(0.0, 3.0);
        var d = TestHelpers.CreateGenericRange(1.0, 2.0);  // inside c

        var result = normalizer.Normalize(new[] { d, c, b, a });

        for (int index = 1; index < result.Length; index++)
        {
            result[index - 1].To.Should().BeLessThanOrEqualTo(result[index].From);
        }
    }

    [Fact]
    public void CustomComparer_IsUsedForSorting()
    {
        var descendingComparer = Comparer<double>.Create((left, right) => right.CompareTo(left));
        var normalizer = CreateNormalizer(descendingComparer);

        var rangeNegative = TestHelpers.CreateGenericRange(-1.0, 0.0);
        var rangeZero = TestHelpers.CreateGenericRange(0.0, 1.0);
        var rangePositive = TestHelpers.CreateGenericRange(1.0, 2.0);

        var result = normalizer.Normalize(new[] { rangeZero, rangeNegative, rangePositive });

        result.Select(r => r.From).Should().Equal(1.0, 0.0, -1.0);
    }

    [Fact]
    public void ExtremeValues_AreHandled()
    {
        var normalizer = CreateNormalizer();
        var minToNext = TestHelpers.CreateGenericRange(double.MinValue, BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(double.MinValue) + 1));
        var prevToMax = TestHelpers.CreateGenericRange(BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(double.MaxValue) - 1), double.MaxValue);

        var result = normalizer.Normalize(new[] { prevToMax, minToNext });

        result.Endpoints().Should().HaveCount(2);
        result.Should().Contain(minToNext);
        result.Should().Contain(prevToMax);
    }
}
