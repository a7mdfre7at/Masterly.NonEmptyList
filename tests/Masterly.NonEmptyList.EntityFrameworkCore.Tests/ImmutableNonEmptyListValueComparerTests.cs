namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class ImmutableNonEmptyListValueComparerTests
{
    [Fact]
    public void Equals_WithSameLists_ShouldReturnTrue()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var list1 = new ImmutableNonEmptyList<int>(1, 2, 3);
        var list2 = new ImmutableNonEmptyList<int>(1, 2, 3);

        Assert.True(comparer.Equals(list1, list2));
    }

    [Fact]
    public void Equals_WithDifferentLists_ShouldReturnFalse()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var list1 = new ImmutableNonEmptyList<int>(1, 2, 3);
        var list2 = new ImmutableNonEmptyList<int>(1, 2, 4);

        Assert.False(comparer.Equals(list1, list2));
    }

    [Fact]
    public void Equals_WithDifferentCounts_ShouldReturnFalse()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var list1 = new ImmutableNonEmptyList<int>(1, 2, 3);
        var list2 = new ImmutableNonEmptyList<int>(1, 2);

        Assert.False(comparer.Equals(list1, list2));
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var list = new ImmutableNonEmptyList<int>(1, 2, 3);

        Assert.True(comparer.Equals(list, list));
    }

    [Fact]
    public void Equals_WithNullFirst_ShouldReturnFalse()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var list = new ImmutableNonEmptyList<int>(1, 2, 3);

        Assert.False(comparer.Equals(null, list));
    }

    [Fact]
    public void Equals_WithNullSecond_ShouldReturnFalse()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var list = new ImmutableNonEmptyList<int>(1, 2, 3);

        Assert.False(comparer.Equals(list, null));
    }

    [Fact]
    public void Equals_WithBothNull_ShouldReturnTrue()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();

        Assert.True(comparer.Equals(null, null));
    }

    [Fact]
    public void GetHashCode_WithSameLists_ShouldReturnSameHash()
    {
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var list1 = new ImmutableNonEmptyList<int>(1, 2, 3);
        var list2 = new ImmutableNonEmptyList<int>(1, 2, 3);

        Assert.Equal(comparer.GetHashCode(list1), comparer.GetHashCode(list2));
    }

    [Fact]
    public void Snapshot_ShouldReturnSameInstance()
    {
        // ImmutableNonEmptyList is immutable, so snapshot should return same instance
        var comparer = new ImmutableNonEmptyListValueComparer<int>();
        var original = new ImmutableNonEmptyList<int>(1, 2, 3);

        var snapshot = comparer.Snapshot(original);

        Assert.Same(original, snapshot);
    }

    [Fact]
    public void WithCustomComparer_ShouldUseProvidedComparer()
    {
        var stringComparer = StringComparer.OrdinalIgnoreCase;
        var comparer = new ImmutableNonEmptyListValueComparer<string>(stringComparer);

        var list1 = new ImmutableNonEmptyList<string>("Hello", "World");
        var list2 = new ImmutableNonEmptyList<string>("HELLO", "WORLD");

        Assert.True(comparer.Equals(list1, list2));
    }
}
