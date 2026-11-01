namespace Masterly.NonEmptyList.Tests;

public class NonEmptyEnumerableTests
{
    [Fact]
    public void Iterate_ShouldBeInfiniteAndTakeShouldBeNonEmpty()
    {
        NonEmptyEnumerable<int> powers = NonEmptyList.Iterate(1, x => x * 2);

        NonEmptyList<int> ten = powers.Take(10);

        Assert.Equal(1, powers.Head);
        Assert.Equal(new[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 }, ten.ToArray());
        Assert.Single(powers.Take(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => powers.Take(0));
        Assert.Equal(1024, powers.ElementAt(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => powers.ElementAt(-1));
    }

    [Fact]
    public void TakeWhile_ShouldAlwaysIncludeHead()
    {
        NonEmptyEnumerable<int> from5 = NonEmptyEnumerable.From(5);

        Assert.Equal(new[] { 5, 6, 7 }, from5.TakeWhile(x => x < 8).ToArray());
        Assert.Equal(new[] { 5 }, from5.TakeWhile(_ => false).ToArray());
        Assert.Equal(new[] { 5, 6, 7, 8 }, from5.TakeUntil(x => x == 8).ToArray());
        Assert.Equal(new[] { 5 }, from5.TakeUntil(x => x == 5).ToArray());
    }

    [Fact]
    public void Cycle_ShouldRepeatForever()
    {
        NonEmptyList<string> colors = ["red", "green"];

        NonEmptyEnumerable<string> cycle = colors.Cycle();

        Assert.Equal("red", cycle.Head);
        Assert.Equal(new[] { "red", "green", "red", "green", "red" }, cycle.Take(5).ToArray());
        Assert.Equal(new[] { "red", "green", "red" }, cycle.Take(3).ToArray());
        Assert.Equal(new[] { "red", "green" }, NonEmptyList.Of("red", "green").Cycle().Take(2).ToArray());
    }

    [Fact]
    public void Select_Index_Zip_Prepend_Scan_ShouldStayLazyAndNonEmpty()
    {
        NonEmptyEnumerable<int> naturals = NonEmptyEnumerable.From(1);

        Assert.Equal(new[] { 2, 4, 6 }, naturals.Select(x => x * 2).Take(3).ToArray());
        Assert.Equal((1, 0), naturals.Index().Head);
        Assert.Equal((3, 2), naturals.Index().ElementAt(2));
        Assert.Equal(new[] { (1, 'a'), (2, 'b') }, naturals.Zip(NonEmptyEnumerable.Create('a', 'b')).ToNonEmptyList().ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, naturals.Prepend(0).Take(3).ToArray());
        Assert.Equal(new[] { 1, 3, 6, 10 }, naturals.Scan(0, (acc, x) => acc + x).Take(4).ToArray());
    }

    [Fact]
    public void Repeat_Generate_Create_ShouldWork()
    {
        Assert.Equal(new[] { "x", "x", "x" }, NonEmptyEnumerable.Repeat("x").Take(3).ToArray());
        Assert.Equal(new[] { 0, 1, 4, 9 }, NonEmptyEnumerable.Generate(i => i * i).Take(4).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, NonEmptyEnumerable.Create(1, 2, 3).ToNonEmptyList().ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, NonEmptyEnumerable.Create(1, new List<int> { 2, 3 }).ToArray());
        Assert.Throws<ArgumentNullException>(() => NonEmptyEnumerable.Create<string>(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => NonEmptyEnumerable.Repeat<string>(null!));
    }

    [Fact]
    public void FromEnumerable_ShouldReturnNullForEmptyAndEnumerateOnce()
    {
        int pulls = 0;
        IEnumerable<int> Source()
        {
            for (int i = 1; i <= 3; i++)
            {
                pulls++;
                yield return i;
            }
        }

        NonEmptyEnumerable<int>? ne = NonEmptyEnumerable.FromEnumerable(Source());

        Assert.NotNull(ne);
        Assert.Equal(1, pulls);
        Assert.Equal(1, ne!.Head);
        Assert.Equal(new[] { 2, 3 }, ne.Rest.ToArray());
        Assert.Equal(3, pulls);
        Assert.Null(NonEmptyEnumerable.FromEnumerable(Enumerable.Empty<int>()));
        Assert.Throws<ArgumentException>(() => NonEmptyEnumerable.FromEnumerable(new string[] { null! }));
    }

    [Fact]
    public void Enumeration_ShouldYieldHeadThenRest()
    {
        NonEmptyEnumerable<int> seq = NonEmptyEnumerable.Create(1, 2, 3);

        Assert.Equal(new[] { 1, 2, 3 }, seq.ToArray());
        Assert.Equal(3, ((System.Collections.IEnumerable)seq).Cast<int>().Count());
        Assert.Equal(6, seq.Sum());
    }
}
