namespace IvTem.Ranges;

public interface IRange<T> where T : IComparable<T>
{
    T From { get; set; }
    T To   { get; set; }
}