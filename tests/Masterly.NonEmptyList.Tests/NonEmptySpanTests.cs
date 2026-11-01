namespace Masterly.NonEmptyList.Tests;

public class NonEmptySpanTests
{
    [Fact]
    public void From_ShouldRejectEmptyAndExposeEnds()
    {
        NonEmptySpan<int> span = NonEmptySpan.From(stackalloc int[] { 3, 1, 2 });

        Assert.Equal(3, span.Head);
        Assert.Equal(2, span.Last);
        Assert.Equal(3, span.Length);
        Assert.Equal(1, span[1]);
        Assert.Equal(new[] { 1, 2 }, span.Tail.ToArray());
        Assert.Equal(new[] { 3, 1 }, span.Init.ToArray());
        Assert.Equal(3, span.Span.Length);
        Assert.Throws<ArgumentException>(() => { NonEmptySpan.From(ReadOnlySpan<int>.Empty); });
        Assert.Throws<ArgumentException>(() => { NonEmptySpan.From(Span<int>.Empty); });
        Assert.Throws<ArgumentException>(() => { NonEmptySpan.From(Array.Empty<int>()); });
        Assert.Throws<ArgumentNullException>(() => { NonEmptySpan.From<int>(null!); });
        Assert.Equal(1, NonEmptySpan.From(new[] { 1 }).Head);
        Assert.Equal(1, NonEmptySpan.From(new int[] { 1 }.AsSpan()).Head);
    }

    [Fact]
    public void TryFrom_ShouldReportEmptiness()
    {
        Assert.True(NonEmptySpan.TryFrom(stackalloc int[] { 1 }, out NonEmptySpan<int> ok));
        Assert.Equal(1, ok.Head);
        Assert.False(NonEmptySpan.TryFrom(ReadOnlySpan<int>.Empty, out _));
    }

    [Fact]
    public void Aggregations_ShouldNotNeedSeedOrEmptyCheck()
    {
        NonEmptySpan<int> span = NonEmptySpan.From(stackalloc int[] { 3, 1, 2 });

        Assert.Equal(6, span.Reduce((a, b) => a + b));
        Assert.Equal("312", span.Fold("", (acc, x) => acc + x));
        Assert.Equal(3, span.Max());
        Assert.Equal(1, span.Min());
        Assert.Equal(6, span.Sum());
        Assert.Equal(2, span.Average());
        Assert.Equal(2.0, NonEmptySpan.From(stackalloc double[] { 1, 3 }).Average());
        Assert.Throws<OverflowException>(() => { NonEmptySpan.From(stackalloc int[] { int.MaxValue, 1 }).Sum(); });
    }

    [Fact]
    public void Conversions_ShouldCopy()
    {
        NonEmptySpan<int> span = NonEmptySpan.From(stackalloc int[] { 1, 2 });

        NonEmptyList<int> list = span.ToNonEmptyList();
        int[] array = span.ToArray();
        ReadOnlySpan<int> plain = span;

        Assert.Equal(new[] { 1, 2 }, list.ToArray());
        Assert.Equal(new[] { 1, 2 }, array);
        Assert.Equal(2, plain.Length);
        Assert.Equal("NonEmptySpan<Int32>[2]", span.ToString());

        int sum = 0;
        foreach (int x in span)
            sum += x;
        Assert.Equal(3, sum);
    }

    [Fact]
    public void AsNonEmptySpan_ShouldViewListStorage()
    {
        NonEmptyList<int> list = [5, 6];
        ImmutableNonEmptyList<int> immutable = [7, 8];

        NonEmptySpan<int> view = list.AsNonEmptySpan();
        NonEmptySpan<int> immutableView = immutable.AsNonEmptySpan();

        Assert.Equal(5, view.Head);
        Assert.Equal(11, view.Sum());
        Assert.Equal(8, immutableView.Last);
        Assert.Equal(2, immutable.AsSpan().Length);
    }
}
