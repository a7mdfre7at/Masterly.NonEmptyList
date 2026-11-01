namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Overloads and edge cases of the non-empty LINQ and math extensions that the main suites do not reach.
/// </summary>
public partial class NonEmptyListTests
{
    #region LINQ overloads

    [Fact]
    public void SelectMany_WithNonEmptyListSelector_ShouldFlatten()
    {
        NonEmptyList<int> source = new(1, 2);

        NonEmptyList<int> result = NonEmptyListLinqExtensions.SelectMany(source, x => new NonEmptyList<int>(x, x * 10));

        Assert.Equal(new[] { 1, 10, 2, 20 }, result);
        Assert.Throws<ArgumentNullException>(() => NonEmptyListLinqExtensions.SelectMany((NonEmptyList<int>)null!, x => new NonEmptyList<int>(x)));
    }

    [Fact]
    public void SelectMany_WithImmutableSelector_ShouldFlatten()
    {
        NonEmptyList<int> source = new(1, 2);

        NonEmptyList<string> result = source.SelectMany(x => ImmutableNonEmptyList.Of($"{x}a", $"{x}b"));

        Assert.Equal(new[] { "1a", "1b", "2a", "2b" }, result);
        Assert.Throws<ArgumentNullException>(() => ((NonEmptyList<int>)null!).SelectMany(x => ImmutableNonEmptyList.Of(x)));
        Assert.Throws<ArgumentNullException>(() => source.SelectMany((Func<int, ImmutableNonEmptyList<int>>)null!));
    }

    [Fact]
    public void Distinct_WithComparer_ShouldUseIt()
    {
        NonEmptyList<string> source = new("a", "A", "b");

        NonEmptyList<string> result = source.Distinct(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(new[] { "a", "b" }, result);
        Assert.Throws<ArgumentNullException>(() => ((NonEmptyList<string>)null!).Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public void GroupBy_WithElementSelector_ShouldProjectElements()
    {
        NonEmptyList<string> source = new("apple", "avocado", "banana");

        var groups = source.GroupBy(s => s[0], s => s.Length);

        Assert.Equal(2, groups.Count);
        Assert.Equal('a', groups[0].Key);
        Assert.Equal(new[] { 5, 7 }, groups[0]);
        Assert.Equal(new[] { 6 }, groups[1]);
    }

    [Fact]
    public void GroupBy_WithElementSelectorReturningNull_ShouldThrow()
    {
        NonEmptyList<string> source = new("a", "b");

        Assert.Throws<ArgumentNullException>(() => source.GroupBy(s => s, s => s == "b" ? null : s));
    }

    [Fact]
    public void Interleave_WhenFirstIsLonger_ShouldAppendItsRemainder()
    {
        NonEmptyList<int> result = new NonEmptyList<int>(1, 3, 5, 7).Interleave(new NonEmptyList<int>(2));

        Assert.Equal(new[] { 1, 2, 3, 5, 7 }, result);
    }

    [Fact]
    public void Cycle_WithTimesBelowOne_ShouldThrow()
    {
        NonEmptyList<int> source = new(1);

        ArgumentOutOfRangeException zero = Assert.Throws<ArgumentOutOfRangeException>(() => source.Cycle(0));
        Assert.Equal("times", zero.ParamName);

        // ParamName matters here: without the guard, the negative list capacity throws with "capacity".
        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => source.Cycle(-1));
        Assert.Equal("times", negative.ParamName);

        Assert.Equal(new[] { 1 }, source.Cycle(1));
    }

    [Fact]
    public void Max_WithComparer_ShouldFindALaterLargerElement()
    {
        NonEmptyList<int> source = new(3, 5, 1, 4);
        IComparer<int> descending = Comparer<int>.Create((a, b) => b.CompareTo(a));

        // The supplied comparer and the default comparer disagree, and the winner sits at a later index.
        Assert.Equal(1, source.Max(descending));
        Assert.Equal(5, source.Max());
    }

    [Fact]
    public void Min_WithSelector_ShouldReturnSmallestProjection()
    {
        NonEmptyList<string> source = new("ccc", "a", "bb");

        Assert.Equal(1, source.Min(s => s.Length));
        Assert.Equal(2, new NonEmptyList<string>("bb", "ccc").Min(s => s.Length));
        Assert.Throws<ArgumentNullException>(() => ((NonEmptyList<string>)null!).Min(s => s.Length));
        Assert.Throws<ArgumentNullException>(() => source.Min((Func<string, int>)null!));
    }

    [Fact]
    public void MinMax_OnAscendingList_ShouldTrackMaxAcrossTheList()
    {
        (int min, int max) = new NonEmptyList<int>(1, 2, 3, 4).MinMax();

        Assert.Equal(1, min);
        Assert.Equal(4, max);
    }

    [Fact]
    public void ToNonEmptyListOrNull_ShouldReturnNullForNullOrEmptyAndListOtherwise()
    {
        Assert.Null(((IEnumerable<int>?)null).ToNonEmptyListOrNull());
        Assert.Null(Array.Empty<int>().ToNonEmptyListOrNull());
        Assert.Equal(new[] { 1, 2 }, new[] { 1, 2 }.ToNonEmptyListOrNull());
    }

    [Fact]
    public void ToNonEmptyList_FromReadOnlySpan_ShouldCopyOrThrowWhenEmpty()
    {
        ReadOnlySpan<int> span = new[] { 1, 2, 3 };

        Assert.Equal(new[] { 1, 2, 3 }, span.ToNonEmptyList());
        Assert.Throws<ArgumentException>(() => ReadOnlySpan<int>.Empty.ToNonEmptyList());
    }

    #endregion

    #region Math overloads

    [Fact]
    public void Average_WithLongSelector_ShouldNotOverflow()
    {
        NonEmptyList<int> source = new(1, 2);

        double average = source.Average(x => long.MaxValue - x);

        Assert.Equal((double)(((decimal)long.MaxValue * 2 - 3) / 2), average);
        Assert.Throws<ArgumentNullException>(() => ((NonEmptyList<int>)null!).Average(x => (long)x));
        Assert.Throws<ArgumentNullException>(() => source.Average((Func<int, long>)null!));
    }

    [Fact]
    public void Median_OfLongs_ShouldHandleOddAndEvenCounts()
    {
        Assert.Equal(2d, new NonEmptyList<long>(3, 1, 2).Median());
        Assert.Equal(2.5d, new NonEmptyList<long>(4, 1, 3, 2).Median());
        Assert.Equal((double)long.MaxValue, new NonEmptyList<long>(long.MaxValue, long.MaxValue).Median());
        Assert.Throws<ArgumentNullException>(() => ((NonEmptyList<long>)null!).Median());
    }

    [Fact]
    public void Median_Generic_ShouldHandleOddAndEvenCounts()
    {
        Assert.Equal(2m, new NonEmptyList<decimal>(3m, 1m, 2m).Median());
        Assert.Equal(2.5m, new NonEmptyList<decimal>(4m, 1m, 3m, 2m).Median());
    }

    #endregion
}
