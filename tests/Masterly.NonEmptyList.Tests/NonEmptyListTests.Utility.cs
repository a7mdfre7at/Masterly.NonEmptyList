namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region Random Tests

    [Fact]
    public void Random_ShouldReturnElementFromList()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        int result = list.Random();

        Assert.Contains(result, list);
    }

    [Fact]
    public void Random_WithSeed_ShouldBeReproducible()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        Random random1 = new(42);
        Random random2 = new(42);

        int result1 = list.Random(random1);
        int result2 = list.Random(random2);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Random_WithSingleElement_ShouldReturnThatElement()
    {
        NonEmptyList<int> list = new(42);
        int result = list.Random();

        Assert.Equal(42, result);
    }

    #endregion

    #region MinBy/MaxBy Tests

    [Fact]
    public void MinBy_ShouldReturnElementWithMinKey()
    {
        NonEmptyList<string> list = new("apple", "hi", "banana", "ok");
        string result = list.MinBy(s => s.Length);

        Assert.Equal("hi", result);
    }

    [Fact]
    public void MaxBy_ShouldReturnElementWithMaxKey()
    {
        NonEmptyList<string> list = new("apple", "hi", "banana", "ok");
        string result = list.MaxBy(s => s.Length);

        Assert.Equal("banana", result);
    }

    [Fact]
    public void MinBy_WithSingleElement_ShouldReturnThatElement()
    {
        NonEmptyList<int> list = new(42);
        int result = list.MinBy(x => x);

        Assert.Equal(42, result);
    }

    #endregion

    #region ForEachWithIndex Tests

    [Fact]
    public void ForEachWithIndex_ShouldProvideCorrectIndices()
    {
        NonEmptyList<string> list = new("a", "b", "c");
        List<(string Item, int Index)> results = new();

        list.ForEachWithIndex((item, index) => results.Add((item, index)));

        Assert.Equal(3, results.Count);
        Assert.Equal(("a", 0), results[0]);
        Assert.Equal(("b", 1), results[1]);
        Assert.Equal(("c", 2), results[2]);
    }

    #endregion
}
