namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class NonEmptyListValueComparerTests
{
    [Fact]
    public void Equals_WithSameLists_ShouldReturnTrue()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list1 = new NonEmptyList<int>(1, 2, 3);
        var list2 = new NonEmptyList<int>(1, 2, 3);

        Assert.True(comparer.Equals(list1, list2));
    }

    [Fact]
    public void Equals_WithDifferentLists_ShouldReturnFalse()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list1 = new NonEmptyList<int>(1, 2, 3);
        var list2 = new NonEmptyList<int>(1, 2, 4);

        Assert.False(comparer.Equals(list1, list2));
    }

    [Fact]
    public void Equals_WithDifferentCounts_ShouldReturnFalse()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list1 = new NonEmptyList<int>(1, 2, 3);
        var list2 = new NonEmptyList<int>(1, 2);

        Assert.False(comparer.Equals(list1, list2));
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list = new NonEmptyList<int>(1, 2, 3);

        Assert.True(comparer.Equals(list, list));
    }

    [Fact]
    public void Equals_WithNullFirst_ShouldReturnFalse()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list = new NonEmptyList<int>(1, 2, 3);

        Assert.False(comparer.Equals(null, list));
    }

    [Fact]
    public void Equals_WithNullSecond_ShouldReturnFalse()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list = new NonEmptyList<int>(1, 2, 3);

        Assert.False(comparer.Equals(list, null));
    }

    [Fact]
    public void Equals_WithBothNull_ShouldReturnTrue()
    {
        var comparer = new NonEmptyListValueComparer<int>();

        Assert.True(comparer.Equals(null, null));
    }

    [Fact]
    public void GetHashCode_WithSameLists_ShouldReturnSameHash()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list1 = new NonEmptyList<int>(1, 2, 3);
        var list2 = new NonEmptyList<int>(1, 2, 3);

        Assert.Equal(comparer.GetHashCode(list1), comparer.GetHashCode(list2));
    }

    [Fact]
    public void GetHashCode_WithDifferentLists_ShouldReturnDifferentHash()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var list1 = new NonEmptyList<int>(1, 2, 3);
        var list2 = new NonEmptyList<int>(4, 5, 6);

        // Note: This test may occasionally fail due to hash collisions, but it's statistically unlikely
        Assert.NotEqual(comparer.GetHashCode(list1), comparer.GetHashCode(list2));
    }

    [Fact]
    public void Snapshot_ShouldCreateIndependentCopy()
    {
        var comparer = new NonEmptyListValueComparer<int>();
        var original = new NonEmptyList<int>(1, 2, 3);

        var snapshot = comparer.Snapshot(original);

        // Should be equal
        Assert.True(comparer.Equals(original, snapshot));

        // Modify original
        original.Add(4);

        // Snapshot should not be affected
        Assert.Equal(3, snapshot.Count);
        Assert.Equal(4, original.Count);
    }

    [Fact]
    public void WithCustomComparer_ShouldUseProvidedComparer()
    {
        var stringComparer = StringComparer.OrdinalIgnoreCase;
        var comparer = new NonEmptyListValueComparer<string>(stringComparer);

        var list1 = new NonEmptyList<string>("Hello", "World");
        var list2 = new NonEmptyList<string>("HELLO", "WORLD");

        Assert.True(comparer.Equals(list1, list2));
    }

    [Fact]
    public void WithCustomComparer_DifferentValues_ShouldReturnFalse()
    {
        var stringComparer = StringComparer.OrdinalIgnoreCase;
        var comparer = new NonEmptyListValueComparer<string>(stringComparer);

        var list1 = new NonEmptyList<string>("Hello", "World");
        var list2 = new NonEmptyList<string>("Hello", "Universe");

        Assert.False(comparer.Equals(list1, list2));
    }
}
