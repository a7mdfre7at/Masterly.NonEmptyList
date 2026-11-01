namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Collection expressions, list patterns, range indexing and the type-inferring factories.
/// </summary>
public partial class NonEmptyListTests
{
    [Fact]
    public void CollectionExpression_ShouldCreateNonEmptyList()
    {
        NonEmptyList<int> list = [1, 2, 3];

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }

    [Fact]
    public void CollectionExpression_WithSpread_ShouldFlatten()
    {
        NonEmptyList<int> source = [1, 2];
        NonEmptyList<int> list = [.. source, 3, .. new[] { 4, 5 }];

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, list.ToArray());
    }

    [Fact]
    public void CollectionExpression_Empty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            NonEmptyList<int> list = [];
            return list;
        });
    }

    [Fact]
    public void CollectionExpression_ForImmutableList_ShouldWork()
    {
        ImmutableNonEmptyList<string> list = ["a", "b"];

        Assert.Equal(2, list.Count);
        Assert.Equal("a", list.Head);
        Assert.Throws<ArgumentException>(() =>
        {
            ImmutableNonEmptyList<string> empty = [];
            return empty;
        });
    }

    [Fact]
    public void ListPattern_ShouldMatchHeadAndRest()
    {
        NonEmptyList<int> list = [1, 2, 3];

        string description = list switch
        {
            [var only] => $"only {only}",
            [var head, .. var rest] => $"{head} then {rest.Count} more",
            _ => throw new System.Diagnostics.UnreachableException("a NonEmptyList always has at least one element"),
        };

        Assert.Equal("1 then 2 more", description);
        Assert.True(list is [1, 2, 3]);
        Assert.True(list is [_, _, ..]);
        Assert.False(list is [_]);
    }

    [Fact]
    public void RangeIndexing_ShouldReturnCopies()
    {
        NonEmptyList<int> list = [1, 2, 3, 4];

        List<int> tail = list[1..];
        List<int> middle = list[1..3];
        List<int> empty = list[4..];

        Assert.Equal(new[] { 2, 3, 4 }, tail);
        Assert.Equal(new[] { 2, 3 }, middle);
        Assert.Empty(empty);
        Assert.Equal(4, list[^1]);
        Assert.Equal(new[] { 2, 3 }, list.Slice(1, 2));
    }

    [Fact]
    public void ImmutableList_SliceAndPatterns_ShouldWork()
    {
        ImmutableNonEmptyList<int> list = [1, 2, 3];

        Assert.Equal(new[] { 2, 3 }, list[1..]);
        Assert.Equal(3, list[^1]);
        Assert.True(list is [1, .. var rest] && rest.Length == 2);
    }

    [Fact]
    public void Factory_Of_ShouldInferType()
    {
        var list = NonEmptyList.Of("a", "b", "c");
        var single = NonEmptyList.Of(42);

        Assert.IsType<NonEmptyList<string>>(list);
        Assert.Equal(3, list.Count);
        Assert.Single(single);
    }

    [Fact]
    public void Factory_Range_ShouldProduceConsecutiveValues()
    {
        NonEmptyList<int> range = NonEmptyList.Range(5, 3);

        Assert.Equal(new[] { 5, 6, 7 }, range.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => NonEmptyList.Range(0, 0));
    }

    [Fact]
    public void Factory_Iterate_ShouldIncludeSeed()
    {
        NonEmptyList<int> powers = NonEmptyList.Iterate(1, x => x * 2, 5);

        Assert.Equal(new[] { 1, 2, 4, 8, 16 }, powers.ToArray());
        Assert.Single(NonEmptyList.Iterate(7, x => x + 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NonEmptyList.Iterate(1, x => x, 0));
    }

    [Fact]
    public void Factory_Unfold_ShouldStopAtNull()
    {
        NonEmptyList<string> steps = NonEmptyList.Unfold("a", s => s.Length < 3 ? s + "a" : null);
        NonEmptyList<int> countdown = NonEmptyList.UnfoldValue(3, n => n > 1 ? n - 1 : null);

        Assert.Equal(new[] { "a", "aa", "aaa" }, steps.ToArray());
        Assert.Equal(new[] { 3, 2, 1 }, countdown.ToArray());
        Assert.Single(NonEmptyList.Unfold("x", _ => null));
    }

    [Fact]
    public void Factory_From_And_TryFrom_ShouldInferType()
    {
        IEnumerable<int> source = Enumerable.Range(1, 3);

        NonEmptyList<int> list = NonEmptyList.From(source);
        bool ok = NonEmptyList.TryFrom(Enumerable.Empty<int>(), out NonEmptyList<int>? missing);

        Assert.Equal(3, list.Count);
        Assert.False(ok);
        Assert.Null(missing);

        if (NonEmptyList.TryFrom(source, out NonEmptyList<int>? found))
            Assert.Equal(1, found.Head);
        else
            Assert.Fail("expected success");
    }

    [Fact]
    public void EnumerableConstructor_ShouldCopyAndValidate()
    {
        List<int> source = new() { 1, 2 };

        NonEmptyList<int> list = new(source);
        ImmutableNonEmptyList<string> immutable = new(new[] { "a" });
        source.Add(3);

        Assert.Equal(new[] { 1, 2 }, list.ToArray());
        Assert.Single(immutable);
        Assert.Throws<ArgumentException>(() => new NonEmptyList<int>(new List<int>()));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<int>((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<string>(new string[] { "a", null! }));
        Assert.Throws<ArgumentException>(() => new ImmutableNonEmptyList<int>(Enumerable.Empty<int>()));
        Assert.Throws<ArgumentNullException>(() => new ImmutableNonEmptyList<int>((IEnumerable<int>)null!));
        Assert.Throws<ArgumentNullException>(() => new ImmutableNonEmptyList<string>(new string[] { null! }));
    }

    [Fact]
    public void FromSpan_ShouldCopyElements()
    {
        ReadOnlySpan<int> span = stackalloc int[] { 1, 2, 3 };

        NonEmptyList<int> list = NonEmptyList<int>.FromSpan(span);

        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());
        Assert.Throws<ArgumentException>(() => NonEmptyList<int>.FromSpan(ReadOnlySpan<int>.Empty));
    }

    [Fact]
    public void Scan_ShouldReturnRunningTotals()
    {
        NonEmptyList<int> list = [1, 2, 3];

        Assert.Equal(new[] { 0, 1, 3, 6 }, list.Scan(0, (acc, x) => acc + x).ToArray());
        Assert.Equal(new[] { 1, 3, 6 }, list.Scan((a, b) => a + b).ToArray());
    }

    [Fact]
    public void TailsAndInits_ShouldEnumerateSuffixesAndPrefixes()
    {
        NonEmptyList<int> list = [1, 2, 3];

        NonEmptyList<NonEmptyList<int>> tails = list.Tails();
        NonEmptyList<NonEmptyList<int>> inits = list.Inits();

        Assert.Equal(3, tails.Count);
        Assert.Equal(new[] { 1, 2, 3 }, tails[0].ToArray());
        Assert.Equal(new[] { 3 }, tails[2].ToArray());
        Assert.Equal(new[] { 1 }, inits[0].ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, inits[2].ToArray());
    }

    [Fact]
    public void ImmutableFactory_Of_ShouldInferType()
    {
        var list = ImmutableNonEmptyList.Of(1, 2);

        Assert.IsType<ImmutableNonEmptyList<int>>(list);
        Assert.Equal(2, list.Count);
    }
}
