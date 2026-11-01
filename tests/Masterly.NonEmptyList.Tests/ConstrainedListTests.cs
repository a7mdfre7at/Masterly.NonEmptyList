using System.Collections;
using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

public class ConstrainedListTests
{
    [Fact]
    public void AtLeastTwo_ShouldAcceptTwoAndRejectOne()
    {
        ConstrainedList<int, AtLeast<Two>> pair = ConstrainedList<int, AtLeast<Two>>.From(new[] { 1, 2 });

        Assert.Equal(2, pair.Count);
        Assert.Equal(2, pair.Second());
        Assert.Throws<ArgumentException>(() => ConstrainedList<int, AtLeast<Two>>.From(new[] { 1 }));
        Assert.Throws<ArgumentException>(() => ConstrainedList<int, AtLeast<Two>>.From(Array.Empty<int>()));
    }

    [Fact]
    public void CollectionExpression_ShouldBuildConstrainedList()
    {
        ConstrainedList<string, AtLeast<Three>> podium = ["gold", "silver", "bronze"];

        Assert.Equal("silver", podium.Second());
        Assert.Equal("bronze", podium.Third());
        Assert.Throws<ArgumentException>(() =>
        {
            ConstrainedList<string, AtLeast<Three>> tooFew = ["only", "two"];
            return tooFew;
        });
    }

    [Fact]
    public void Distinct_ShouldRejectDuplicates()
    {
        Assert.True(ConstrainedList<string, Distinct>.TryFrom(new[] { "a", "b" }, out _));
        Assert.False(ConstrainedList<string, Distinct>.TryFrom(new[] { "a", "a" }, out _));
        Assert.False(ConstrainedList<string, Distinct>.TryFrom(null, out _));
        Assert.False(ConstrainedList<string, Distinct>.TryFrom(new string[] { "a", null! }, out _));
    }

    [Fact]
    public void Sorted_ShouldRequireAscendingOrder()
    {
        Assert.True(ConstrainedList<int, Sorted>.Satisfies(new[] { 1, 1, 2 }));
        Assert.False(ConstrainedList<int, Sorted>.Satisfies(new[] { 2, 1 }));
        Assert.True(ConstrainedList<int, SortedDescending>.Satisfies(new[] { 3, 2, 2 }));
        Assert.False(ConstrainedList<int, SortedDescending>.Satisfies(new[] { 1, 2 }));
        Assert.False(ConstrainedList<int, Sorted>.Satisfies(null));
        Assert.False(ConstrainedList<int, Sorted>.Satisfies(Array.Empty<int>()));
    }

    [Fact]
    public void And_ShouldComposeConstraints()
    {
        var ok = ConstrainedList<int, And<AtLeast<Two>, Distinct>>.From(new[] { 1, 2, 3 });

        Assert.Equal(2, ok.Second());
        Assert.Equal("at least 2 elements and distinct elements", ConstrainedList<int, And<AtLeast<Two>, Distinct>>.Description);
        Assert.Throws<ArgumentException>(() => ConstrainedList<int, And<AtLeast<Two>, Distinct>>.From(new[] { 1, 1 }));
        Assert.Throws<ArgumentException>(() => ConstrainedList<int, And<AtLeast<Two>, Distinct>>.From(new[] { 1 }));
    }

    [Fact]
    public void Or_AtMost_Exactly_ShouldWork()
    {
        Assert.True(ConstrainedList<int, Or<Sorted, SortedDescending>>.Satisfies(new[] { 3, 2, 1 }));
        Assert.False(ConstrainedList<int, Or<Sorted, SortedDescending>>.Satisfies(new[] { 1, 3, 2 }));
        Assert.True(ConstrainedList<int, AtMost<Two>>.Satisfies(new[] { 1, 2 }));
        Assert.False(ConstrainedList<int, AtMost<Two>>.Satisfies(new[] { 1, 2, 3 }));
        Assert.True(ConstrainedList<int, Exactly<Three>>.Satisfies(new[] { 1, 2, 3 }));
        Assert.False(ConstrainedList<int, Exactly<Three>>.Satisfies(new[] { 1, 2 }));
        Assert.Equal("exactly 1 element", ConstrainedList<int, Exactly<One>>.Description);
        Assert.Equal("at most 10 elements", ConstrainedList<int, AtMost<Ten>>.Description);
        Assert.Equal(3, ConstrainedList<int, Exactly<Three>>.From(new[] { 1, 2, 3 }).Third());
    }

    [Fact]
    public void Cardinalities_ShouldExposeValues()
    {
        Assert.Equal(1, One.Value);
        Assert.Equal(2, Two.Value);
        Assert.Equal(3, Three.Value);
        Assert.Equal(4, Four.Value);
        Assert.Equal(5, Five.Value);
        Assert.Equal(6, Six.Value);
        Assert.Equal(7, Seven.Value);
        Assert.Equal(8, Eight.Value);
        Assert.Equal(9, Nine.Value);
        Assert.Equal(10, Ten.Value);
        Assert.Equal(16, Sixteen.Value);
        Assert.Equal(32, ThirtyTwo.Value);
        Assert.Equal(64, SixtyFour.Value);
        Assert.Equal(100, Hundred.Value);
        Assert.Equal(1000, Thousand.Value);
    }

    [Fact]
    public void NonEmptyConstraint_ShouldBehaveLikeImmutableNonEmptyList()
    {
        ConstrainedList<int, NonEmpty> list = ConstrainedList<int, NonEmpty>.FromSpan(stackalloc int[] { 1, 2 });

        Assert.Equal("at least 1 element", ConstrainedList<int, NonEmpty>.Description);
        Assert.Equal(1, list.Head);
        Assert.True(((ICollection<int>)list).IsReadOnly);
        Assert.True(((IList)list).IsReadOnly);
    }

    [Fact]
    public void Mutations_ShouldThrowNotSupported()
    {
        ConstrainedList<int, AtLeast<Two>> list = ConstrainedList<int, AtLeast<Two>>.From(new[] { 1, 2, 3 });

        Assert.Throws<NotSupportedException>(() => list.Add(4));
        Assert.Throws<NotSupportedException>(() => list.AddRange(new[] { 4 }));
        Assert.Throws<NotSupportedException>(() => list.Insert(0, 4));
        Assert.Throws<NotSupportedException>(() => list.InsertRange(0, new[] { 4 }));
        Assert.Throws<NotSupportedException>(() => list.Remove(1));
        Assert.Throws<NotSupportedException>(() => list.Remove(99));
        Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => list.RemoveRange(0, 1));
        Assert.Throws<NotSupportedException>(() => list.RemoveAll(x => x == 1));
        Assert.Throws<NotSupportedException>(() => list.Clear());
        Assert.Throws<NotSupportedException>(() => list[0] = 9);
        Assert.Throws<NotSupportedException>(() => list.Sort());
        Assert.Throws<NotSupportedException>(() => list.Sort((a, b) => a - b));
        Assert.Throws<NotSupportedException>(() => list.Sort(Comparer<int>.Default));
        Assert.Throws<NotSupportedException>(() => list.Sort(0, 2, null));
        Assert.Throws<NotSupportedException>(() => list.Reverse());
        Assert.Throws<NotSupportedException>(() => list.Reverse(0, 2));
        Assert.Throws<NotSupportedException>(() => list.AddFirst(0));
        Assert.Throws<NotSupportedException>(() => list.SetRange(0, new[] { 7 }));
        Assert.Throws<NotSupportedException>(() => ((IList)list).Add(5));
        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());
    }

    [Fact]
    public void WithOperations_ShouldReturnNewCheckedLists()
    {
        ConstrainedList<int, And<AtLeast<Two>, AtMost<Three>>> list = [1, 2];

        var appended = list.WithAppended(3);
        var prepended = list.WithPrepended(0);
        var replaced = list.WithItem(0, 9);
        var shrunk = appended.WithoutAt(0);

        Assert.Equal(new[] { 1, 2, 3 }, appended.ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, prepended.ToArray());
        Assert.Equal(new[] { 9, 2 }, replaced.ToArray());
        Assert.Equal(new[] { 2, 3 }, shrunk.ToArray());
        Assert.Equal(new[] { 1, 2 }, list.ToArray());
        Assert.Throws<ArgumentException>(() => appended.WithAppended(4));
        Assert.Throws<ArgumentException>(() => list.WithoutAt(0));
        Assert.Throws<ArgumentNullException>(() => ConstrainedList<string, NonEmpty>.From(new[] { "a" }).WithAppended(null!));
    }

    [Fact]
    public void Relax_As_TryAs_ShouldConvertBetweenConstraints()
    {
        ConstrainedList<int, Sorted> sorted = [1, 2, 2];

        NonEmptyList<int> mutable = sorted.Relax();
        mutable.Add(0);

        Assert.Equal(new[] { 1, 2, 2, 0 }, mutable.ToArray());
        Assert.Equal(3, sorted.Count);
        Assert.Equal(3, sorted.As<AtLeast<Two>>().Count);
        Assert.Throws<ArgumentException>(() => sorted.As<Distinct>());
        Assert.False(sorted.TryAs<Distinct>(out _));
        Assert.True(sorted.TryAs<AtMost<Three>>(out var bounded));
        Assert.Equal(3, bounded!.Count);
    }

    [Fact]
    public void Validate_ShouldProduceValidationErrors()
    {
        var missing = ConstrainedList<int, AtLeast<Two>>.Validate(null, "ids");
        var empty = ConstrainedList<int, AtLeast<Two>>.Validate(Array.Empty<int>(), "ids");
        var nulls = ConstrainedList<string, AtLeast<Two>>.Validate(new string[] { "a", null! }, "ids");
        var tooFew = ConstrainedList<int, AtLeast<Two>>.Validate(new[] { 1 }, "ids");
        var ok = ConstrainedList<int, AtLeast<Two>>.Validate(new[] { 1, 2 });

        Assert.Equal("required", missing.Errors.Head.Code);
        Assert.Equal("ids", missing.Errors.Head.Path);
        Assert.Equal("nonEmpty", empty.Errors.Head.Code);
        Assert.Equal("noNulls", nulls.Errors.Head.Code);
        Assert.Equal("constraint", tooFew.Errors.Head.Code);
        Assert.Contains("at least 2", tooFew.Errors.Head.Message);
        Assert.True(ok.IsValid);
        Assert.Equal(2, ok.Value.Second());
    }

    [Fact]
    public void LinqAndMath_ShouldStillWorkOnConstrainedLists()
    {
        ConstrainedList<int, AtLeast<Two>> list = [3, 1, 2];

        NonEmptyList<int> doubled = list.Select(x => x * 2);
        Assert.Equal(new[] { 6, 2, 4 }, doubled.ToArray());
        Assert.Equal(6, list.Sum());
        Assert.Equal(3, list.Max());
        Assert.Equal(new[] { 1, 2, 3 }, list.OrderBy(x => x).ToArray());
        Assert.True(list is [3, .. var rest] && rest.Count == 2);
        Assert.Equal(new[] { 2 }, list.RestAfterSecond());
    }

    [Fact]
    public void Json_ShouldRoundTripAndEnforceConstraint()
    {
        ConstrainedList<int, And<AtLeast<Two>, Distinct>> list = [1, 2];

        string json = JsonSerializer.Serialize(list);
        var back = JsonSerializer.Deserialize<ConstrainedList<int, And<AtLeast<Two>, Distinct>>>(json);

        Assert.Equal("[1,2]", json);
        Assert.Equal(list, back);
        Assert.Null(JsonSerializer.Deserialize<ConstrainedList<int, And<AtLeast<Two>, Distinct>>>("null"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConstrainedList<int, And<AtLeast<Two>, Distinct>>>("[1]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConstrainedList<int, And<AtLeast<Two>, Distinct>>>("[1,1]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConstrainedList<int, And<AtLeast<Two>, Distinct>>>("[]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConstrainedList<int, And<AtLeast<Two>, Distinct>>>("1"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConstrainedList<string, NonEmpty>>("[\"a\", null]"));
    }

    [Fact]
    public void Json_InsideAnObject_ShouldWork()
    {
        var dto = new RouteDto([new(0, 0), new(1, 1)]);

        string json = JsonSerializer.Serialize(dto);
        RouteDto back = JsonSerializer.Deserialize<RouteDto>(json)!;

        Assert.Equal(2, back.Waypoints.Count);
        Assert.Equal(new Waypoint(1, 1), back.Waypoints.Second());
    }

    [Fact]
    public void Factories_ShouldInferTypes()
    {
        var a = ConstrainedList.From<int, AtLeast<Two>>(new[] { 1, 2 });
        var b = ConstrainedList.Create<int, Distinct>(stackalloc int[] { 1, 2 });
        bool ok = ConstrainedList.TryFrom<int, Sorted>(new[] { 2, 1 }, out var c);
        var d = new[] { 1, 2 }.ToConstrainedList<int, AtLeast<Two>>();
        var e = new NonEmptyList<int>(1, 2).Constrain<int, AtLeast<Two>>();

        Assert.Equal(2, a.Count);
        Assert.Equal(2, b.Count);
        Assert.False(ok);
        Assert.Null(c);
        Assert.Equal(2, d.Second());
        Assert.Equal(2, e.Second());
        Assert.Contains("at least 2 elements", a.ToString());
    }

    [Fact]
    public void Equality_ShouldFollowNonEmptyListSemantics()
    {
        ConstrainedList<int, NonEmpty> a = [1, 2];
        ConstrainedList<int, NonEmpty> b = [1, 2];
        NonEmptyList<int> c = [1, 2];

        Assert.Equal(a, b);
        Assert.True(a.Equals(c));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    private sealed record Waypoint(double Lat, double Lon);

    private sealed record RouteDto(ConstrainedList<Waypoint, AtLeast<Two>> Waypoints);
}
