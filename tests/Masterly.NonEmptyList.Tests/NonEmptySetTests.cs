using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

public class NonEmptySetTests
{
    [Fact]
    public void Construction_ShouldCollapseDuplicatesAndRejectEmpty()
    {
        NonEmptySet<int> set = new(1, 1, 2);
        NonEmptySet<string> ci = new("a", new[] { "A" }, StringComparer.OrdinalIgnoreCase);

        Assert.Equal(2, set.Count);
        Assert.Single(ci);
        Assert.Throws<ArgumentException>(() => NonEmptySet<int>.From(Array.Empty<int>()));
        Assert.Throws<ArgumentNullException>(() => NonEmptySet<string>.From(new string[] { "a", null! }));
        Assert.Throws<ArgumentNullException>(() => NonEmptySet<string>.From(null!));
        Assert.Throws<ArgumentNullException>(() => new NonEmptySet<string>(null!));
        Assert.Throws<ArgumentNullException>(() => new NonEmptySet<string>("a", new string[] { null! }, null));
        Assert.Throws<ArgumentException>(() => NonEmptySet<int>.FromSpan(ReadOnlySpan<int>.Empty));
        Assert.Throws<ArgumentNullException>(() => NonEmptySet<string>.FromSpan(new string[] { null! }));
    }

    [Fact]
    public void CollectionExpression_Factories_ShouldWork()
    {
        NonEmptySet<int> set = [3, 3, 1];
        var of = NonEmptySet.Of("a", "b");
        var from = new[] { 1, 2 }.ToNonEmptySet();
        var fromList = NonEmptyList.Of(1, 1).ToNonEmptySet();

        Assert.Equal(2, set.Count);
        Assert.Equal(2, of.Count);
        Assert.Equal(2, from.Count);
        Assert.Single(fromList);
        Assert.Single(NonEmptySet<int>.Singleton(5));
        Assert.True(NonEmptySet<int>.TryFrom(new[] { 1 }, out _));
        Assert.False(NonEmptySet<int>.TryFrom(Array.Empty<int>(), out _));
        Assert.False(NonEmptySet<int>.TryFrom(null, out _));
        Assert.False(NonEmptySet<string>.TryFrom(new string[] { null! }, out _));
        Assert.True(new[] { 1 }.TryToNonEmptySet(out _));
        Assert.Equal(2, NonEmptySet.From(new[] { 1, 2 }).Count);
    }

    [Fact]
    public void Parse_ShouldReadCommaSeparatedValuesAndCollapseDuplicates()
    {
        NonEmptySet<int> set = NonEmptySet<int>.Parse("1, 2,2 ,3");

        Assert.Equal(3, set.Count);
        Assert.True(NonEmptySet<string>.TryParse("a,a", null, out NonEmptySet<string>? strings));
        Assert.Single(strings!);
        Assert.False(NonEmptySet<int>.TryParse("", null, out _));
        Assert.False(NonEmptySet<int>.TryParse(null, null, out _));
        Assert.False(NonEmptySet<int>.TryParse("1,x", null, out _));
        Assert.ThrowsAny<Exception>(() => NonEmptySet<int>.Parse(""));
    }

    [Fact]
    public void Remove_ShouldNeverEmptyTheSet()
    {
        NonEmptySet<int> set = [1, 2];

        Assert.False(set.Remove(99));
        Assert.True(set.Remove(1));
        Assert.Throws<InvalidOperationException>(() => set.Remove(2));
        Assert.Throws<NotSupportedException>(() => set.Clear());
        Assert.Throws<InvalidOperationException>(() => set.RemoveWhere(_ => true));
        Assert.Equal(2, set.Any());
        Assert.Throws<ArgumentNullException>(() => NonEmptySet.Of("a").Remove(null!));
    }

    [Fact]
    public void RemoveWhere_ShouldRemoveWhenSomethingRemains()
    {
        NonEmptySet<int> set = [1, 2, 3];

        Assert.Equal(2, set.RemoveWhere(x => x > 1));
        Assert.Equal(new[] { 1 }, set.ToArray());
    }

    [Fact]
    public void SetOperationsInPlace_ShouldGuardEmptiness()
    {
        NonEmptySet<int> set = [1, 2, 3];

        set.UnionWith(new[] { 4 });
        Assert.Equal(4, set.Count);
        set.IntersectWith(new[] { 1, 2, 9 });
        Assert.Equal(2, set.Count);
        Assert.Throws<InvalidOperationException>(() => set.IntersectWith(new[] { 9 }));
        set.ExceptWith(new[] { 1 });
        Assert.Equal(new[] { 2 }, set.ToArray());
        Assert.Throws<InvalidOperationException>(() => set.ExceptWith(new[] { 2 }));
        Assert.Throws<InvalidOperationException>(() => set.SymmetricExceptWith(new[] { 2 }));
        set.SymmetricExceptWith(new[] { 2, 5 });
        Assert.Equal(new[] { 5 }, set.ToArray());
        ((ICollection<int>)set).Add(6);
        Assert.Equal(2, set.Count);
        Assert.False(((ICollection<int>)set).IsReadOnly);
        Assert.Throws<ArgumentNullException>(() => NonEmptySet.Of("a").Add(null!));
    }

    [Fact]
    public void SetQueries_ShouldForward()
    {
        NonEmptySet<int> set = [1, 2];

        Assert.Contains(1, (ISet<int>)set);
        Assert.True(set.IsSubsetOf(new[] { 1, 2, 3 }));
        Assert.True(set.IsSupersetOf(new[] { 1 }));
        Assert.True(set.IsProperSubsetOf(new[] { 1, 2, 3 }));
        Assert.True(set.IsProperSupersetOf(new[] { 1 }));
        Assert.True(set.Overlaps(new[] { 2 }));
        Assert.True(set.SetEquals(new[] { 2, 1 }));
        Assert.Same(EqualityComparer<int>.Default, set.Comparer);

        int[] target = new int[3];
        set.CopyTo(target, 1);
        Assert.Equal(0, target[0]);
        set.CopyTo(target);
        Assert.Contains(1, target);
    }

    [Fact]
    public void NonMutatingSetOperations_ShouldReturnNullWhenEmpty()
    {
        NonEmptySet<int> set = [1, 2];

        Assert.Equal(3, set.Union(new[] { 3 }).Count);
        Assert.Equal(new[] { 2 }, set.Intersect(new[] { 2, 3 })!.ToArray());
        Assert.Null(set.Intersect(new[] { 7 }));
        Assert.Equal(new[] { 1 }, set.Except(new[] { 2 })!.ToArray());
        Assert.Null(set.Except(new[] { 1, 2 }));
        Assert.Equal(2, set.Count);
        Assert.Throws<ArgumentNullException>(() => NonEmptySet.Of("a").Union(new string[] { null! }));
    }

    [Fact]
    public void Map_ToList_ToHashSet_ShouldWork()
    {
        NonEmptySet<int> set = [1, 2, 3];

        NonEmptySet<int> parity = set.Map(x => x % 2);
        NonEmptyList<int> sorted = set.ToSortedNonEmptyList();
        HashSet<int> copy = set.ToHashSet();
        copy.Clear();

        Assert.Equal(2, parity.Count);
        Assert.Equal(new[] { 1, 2, 3 }, sorted.ToArray());
        Assert.Equal(3, set.ToNonEmptyList().Count);
        Assert.Equal(3, set.Count);
        Assert.Throws<InvalidOperationException>(() => set.Map<string>(_ => null!));
    }

    [Fact]
    public void Equality_ToString_ShouldUseSetSemantics()
    {
        NonEmptySet<int> a = [1, 2];
        NonEmptySet<int> b = [2, 1];

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, NonEmptySet.Of(1));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.StartsWith("NonEmptySet: {", a.ToString());
        Assert.Equal(2, ((System.Collections.IEnumerable)a).Cast<int>().Count());
        Assert.Equal(2, ((IEnumerable<int>)a).Count());
    }

    [Fact]
    public void Json_ShouldRoundTripAndRejectEmpty()
    {
        NonEmptySet<string> set = ["a", "b"];

        string json = JsonSerializer.Serialize(set);
        NonEmptySet<string>? back = JsonSerializer.Deserialize<NonEmptySet<string>>(json);

        Assert.Equal(set, back);
        Assert.Null(JsonSerializer.Deserialize<NonEmptySet<string>>("null"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptySet<string>>("[]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptySet<string>>("[null]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptySet<string>>("{}"));
        Assert.Single(JsonSerializer.Deserialize<NonEmptySet<int>>("[1,1,1]")!);
    }
}
