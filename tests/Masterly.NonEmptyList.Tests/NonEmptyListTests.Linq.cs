namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Type-preserving LINQ: standard operators keep the NonEmptyList type when non-emptiness is provable.
/// </summary>
public partial class NonEmptyListTests
{
    private sealed record Customer(string LastName, string FirstName, string Country, int Age);

    private static NonEmptyList<Customer> Customers() =>
    [
        new("Smith", "Zoe", "UK", 30),
        new("Jones", "Adam", "US", 25),
        new("Smith", "Adam", "UK", 41),
        new("Brown", "Mia", "US", 35),
    ];

    [Fact]
    public void Select_ShouldReturnNonEmptyList()
    {
        NonEmptyList<int> list = [1, 2, 3];

        NonEmptyList<string> strings = list.Select(x => x.ToString());
        NonEmptyList<int> indexed = list.Select((x, i) => x * i);

        Assert.Equal(new[] { "1", "2", "3" }, strings.ToArray());
        Assert.Equal(new[] { 0, 2, 6 }, indexed.ToArray());
    }

    [Fact]
    public void QuerySyntax_WithOrderByThenBy_ShouldStayNonEmpty()
    {
        NonEmptyList<string> names =
            from c in Customers()
            orderby c.LastName, c.FirstName descending
            select $"{c.LastName}, {c.FirstName}";

        Assert.Equal(new[] { "Brown, Mia", "Jones, Adam", "Smith, Zoe", "Smith, Adam" }, names.ToArray());
    }

    [Fact]
    public void QuerySyntax_WithGroupBy_ShouldProduceNonEmptyGroups()
    {
        NonEmptyList<(string Country, int Count, int MaxAge)> summary =
            from c in Customers()
            group c by c.Country into g
            select (g.Key, g.Count, g.Max(x => x.Age));

        Assert.Equal(2, summary.Count);
        Assert.Contains(("UK", 2, 41), summary);
        Assert.Contains(("US", 2, 35), summary);
    }

    [Fact]
    public void GroupBy_ShouldReturnGroupsThatAreNonEmptyLists()
    {
        NonEmptyList<NonEmptyGrouping<string, Customer>> groups = Customers().GroupBy(c => c.LastName);

        Assert.Equal(3, groups.Count);
        NonEmptyGrouping<string, Customer> smiths = groups.Single(g => g.Key == "Smith");
        Assert.Equal(2, smiths.Count);
        Assert.Equal("Zoe", smiths.Head.FirstName);
        Assert.IsAssignableFrom<IGrouping<string, Customer>>(smiths);
    }

    [Fact]
    public void QuerySyntax_WithSelectMany_ShouldStayNonEmpty()
    {
        NonEmptyList<int> xs = [1, 2];

        NonEmptyList<string> pairs =
            from x in xs
            from y in NonEmptyList.Of("a", "b")
            select $"{x}{y}";

        Assert.Equal(new[] { "1a", "1b", "2a", "2b" }, pairs.ToArray());
    }

    [Fact]
    public void Where_ShouldFallBackToIEnumerable()
    {
        NonEmptyList<int> list = [1, 2, 3];

        IEnumerable<int> filtered = list.Where(x => x > 5);

        Assert.False(filtered is NonEmptyList<int>);
        Assert.Empty(filtered);
    }

    [Fact]
    public void OrderBy_ShouldBeStable_AndThenByShouldRefine()
    {
        NonEmptyList<Customer> ordered = Customers().OrderBy(c => c.LastName);
        NonEmptyList<Customer> refined = Customers().OrderBy(c => c.LastName).ThenBy(c => c.FirstName);
        NonEmptyList<Customer> descending = Customers().OrderByDescending(c => c.Age);

        Assert.Equal(new[] { "Brown", "Jones", "Smith", "Smith" }, ordered.Select(c => c.LastName).ToArray());
        Assert.Equal("Zoe", ordered[2].FirstName);
        Assert.Equal("Adam", refined[2].FirstName);
        Assert.Equal(41, descending.Head.Age);
    }

    [Fact]
    public void OrderedNonEmptyList_ShouldImplementIOrderedEnumerable()
    {
        IOrderedEnumerable<Customer> ordered = Customers().OrderBy(c => c.LastName);

        List<Customer> refined = Enumerable.ThenByDescending(ordered, c => c.Age).ToList();

        Assert.Equal(41, refined[2].Age);
        Assert.Equal(30, refined[3].Age);
    }

    [Fact]
    public void Order_ShouldSortNaturally()
    {
        NonEmptyList<int> list = [3, 1, 2];

        Assert.Equal(new[] { 1, 2, 3 }, list.Order().ToArray());
        Assert.Equal(new[] { 3, 2, 1 }, list.OrderDescending().ToArray());
    }

    [Fact]
    public void Distinct_Union_Concat_ShouldStayNonEmpty()
    {
        NonEmptyList<int> list = [1, 1, 2];
        IEnumerable<int> empty = Enumerable.Empty<int>();

        NonEmptyList<int> distinct = list.Distinct();
        NonEmptyList<int> union = list.Union(new[] { 2, 3 });
        NonEmptyList<int> unionReversed = new[] { 0 }.Union(list);
        NonEmptyList<int> concat = empty.Concat(list);

        Assert.Equal(new[] { 1, 2 }, distinct.ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, union.ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, unionReversed.ToArray());
        Assert.Equal(new[] { 1, 1, 2 }, concat.ToArray());
    }

    [Fact]
    public void Chunk_Flatten_Unzip_Interleave_ShouldStayNonEmpty()
    {
        NonEmptyList<int> list = [1, 2, 3, 4, 5];

        NonEmptyList<NonEmptyList<int>> chunks = list.Chunk(2);
        NonEmptyList<int> flattened = chunks.Flatten();
        (NonEmptyList<int> firsts, NonEmptyList<string> seconds) = list.Select(x => (x, x.ToString())).Unzip();
        NonEmptyList<int> interleaved = NonEmptyList.Of(1, 3).Interleave(NonEmptyList.Of(2, 4, 6, 8));

        Assert.Equal(3, chunks.Count);
        Assert.Equal(new[] { 5 }, chunks.Last.ToArray());
        Assert.Equal(list, flattened);
        Assert.Equal(list, firsts);
        Assert.Equal("5", seconds.Last);
        Assert.Equal(new[] { 1, 2, 3, 4, 6, 8 }, interleaved.ToArray());
    }

    [Fact]
    public void Cycle_ShouldRepeat()
    {
        NonEmptyList<string> colors = ["red", "green"];

        Assert.Equal(new[] { "red", "green", "red", "green", "red" }, colors.Cycle().Take(5).ToArray());
        Assert.Equal(4, colors.Cycle(2).Count);
    }

    [Fact]
    public void MinMax_ShouldReturnNonNullableValues()
    {
        NonEmptyList<string> words = ["pear", "apple", "fig"];

        string max = words.Max();
        string min = words.Min();
        int longest = words.Max(w => w.Length);
        (string lo, string hi) = words.MinMax();

        Assert.Equal("pear", max);
        Assert.Equal("apple", min);
        Assert.Equal(5, longest);
        Assert.Equal(("apple", "pear"), (lo, hi));
        Assert.Equal("fig", words.MinBy(w => w.Length));
        Assert.Equal("apple", words.MaxBy(w => w.Length));
    }

    [Fact]
    public void ToNonEmptyList_ShouldEnterTheType()
    {
        IEnumerable<int> source = Enumerable.Range(1, 3);

        NonEmptyList<int> list = source.ToNonEmptyList();
        NonEmptyList<int>? none = Enumerable.Empty<int>().ToNonEmptyListOrNull();

        Assert.Equal(3, list.Count);
        Assert.Null(none);
        Assert.Throws<ArgumentException>(() => Enumerable.Empty<int>().ToNonEmptyList());
        Assert.True(source.TryToNonEmptyList(out NonEmptyList<int>? result) && result.Head == 1);
    }

    [Fact]
    public void StandardLinq_ThatCannotThrowOnNonEmpty_StillWorks()
    {
        NonEmptyList<int> list = [4, 5, 6];

        Assert.Equal(4, list.First());
        Assert.Equal(6, list.Last());
        Assert.Equal(3, list.Count());
        Assert.Equal(15, list.Aggregate((a, b) => a + b));
    }
}
