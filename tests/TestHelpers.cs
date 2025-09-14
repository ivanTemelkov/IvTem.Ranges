namespace IvTem.Ranges.Tests;

// Helpers kept verbose and without abbreviations.

public static class TestHelpers
{
    public static (T From, T To) ToEndpoints<T>(IRange<T> range)
        where T : IComparable<T>
        => (range.From, range.To);

    public static IEnumerable<(T From, T To)> Endpoints<T>(this IEnumerable<IRange<T>> ranges)
        where T : IComparable<T>
        => ranges.Select(ToEndpoints);

    // Local generic range type for testing generics when needed (e.g., DateTime)
    private sealed class TestRange<T> : IRange<T> where T : IComparable<T>
    {
        public T From { get; set; }
        public T To { get; set; }

        public string Id { get;  } = Guid.NewGuid().ToString();

        public TestRange(T from, T to)
        {
            From = from;
            To = to;
        }

        public override string ToString() => $"[{From},{To})";
    }

    public static IRange<T> CreateGenericRange<T>(T from, T to) where T : IComparable<T>
        => new TestRange<T>(from, to);
}