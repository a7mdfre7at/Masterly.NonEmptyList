namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region Map Tests

    [Fact]
    public void Map_ShouldTransformAllElements()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int> result = list.Map(x => x * 2);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0]);
        Assert.Equal(4, result[1]);
        Assert.Equal(6, result[2]);
    }

    [Fact]
    public void Map_ShouldChangeType()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<string> result = list.Map(x => x.ToString());

        Assert.Equal("1", result[0]);
        Assert.Equal("2", result[1]);
        Assert.Equal("3", result[2]);
    }

    [Fact]
    public void MapWithIndex_ShouldIncludeIndex()
    {
        NonEmptyList<string> list = new("a", "b", "c");
        NonEmptyList<string> result = list.MapWithIndex((item, index) => $"{index}:{item}");

        Assert.Equal("0:a", result[0]);
        Assert.Equal("1:b", result[1]);
        Assert.Equal("2:c", result[2]);
    }

    #endregion

    #region FlatMap Tests

    [Fact]
    public void FlatMap_ShouldFlattenNestedLists()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int> result = list.FlatMap(x => new NonEmptyList<int>(x, x * 10));

        Assert.Equal(6, result.Count);
        Assert.Equal(new[] { 1, 10, 2, 20, 3, 30 }, result.ToArray());
    }

    #endregion

    #region Reduce Tests

    [Fact]
    public void Reduce_ShouldCombineAllElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4);
        int result = list.Reduce((a, b) => a + b);

        Assert.Equal(10, result);
    }

    [Fact]
    public void Reduce_WithSingleElement_ShouldReturnElement()
    {
        NonEmptyList<int> list = new(42);
        int result = list.Reduce((a, b) => a + b);

        Assert.Equal(42, result);
    }

    [Fact]
    public void ReduceRight_ShouldReduceFromRight()
    {
        NonEmptyList<string> list = new("a", "b", "c");
        string result = list.ReduceRight((a, b) => $"({a}+{b})");

        Assert.Equal("(a+(b+c))", result);
    }

    #endregion

    #region Fold Tests

    [Fact]
    public void Fold_ShouldAccumulateWithSeed()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        int result = list.Fold(10, (acc, x) => acc + x);

        Assert.Equal(16, result);
    }

    [Fact]
    public void Fold_ShouldChangeType()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        string result = list.Fold("", (acc, x) => acc + x.ToString());

        Assert.Equal("123", result);
    }

    [Fact]
    public void FoldRight_ShouldFoldFromRight()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        string result = list.FoldRight("", (x, acc) => acc + x.ToString());

        Assert.Equal("321", result);
    }

    #endregion

    #region Zip Tests

    [Fact]
    public void Zip_ShouldPairElements()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<string> list2 = new("a", "b", "c");
        NonEmptyList<(int, string)> result = list1.Zip(list2);

        Assert.Equal(3, result.Count);
        Assert.Equal((1, "a"), result[0]);
        Assert.Equal((2, "b"), result[1]);
        Assert.Equal((3, "c"), result[2]);
    }

    [Fact]
    public void Zip_WithDifferentLengths_ShouldTakeShorter()
    {
        NonEmptyList<int> list1 = new(1, 2, 3, 4, 5);
        NonEmptyList<string> list2 = new("a", "b");
        NonEmptyList<(int, string)> result = list1.Zip(list2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ZipWith_ShouldApplySelector()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(10, 20, 30);
        NonEmptyList<int> result = list1.ZipWith(list2, (a, b) => a + b);

        Assert.Equal(new[] { 11, 22, 33 }, result.ToArray());
    }

    [Fact]
    public void ZipWithIndex_ShouldIncludeIndices()
    {
        NonEmptyList<string> list = new("a", "b", "c");
        NonEmptyList<(string, int)> result = list.ZipWithIndex();

        Assert.Equal(("a", 0), result[0]);
        Assert.Equal(("b", 1), result[1]);
        Assert.Equal(("c", 2), result[2]);
    }

    #endregion

    #region Partition Tests

    [Fact]
    public void Partition_ShouldSplitByPredicate()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5, 6);
        (IReadOnlyList<int> even, IReadOnlyList<int> odd) = list.Partition(x => x % 2 == 0);

        Assert.Equal(new[] { 2, 4, 6 }, even);
        Assert.Equal(new[] { 1, 3, 5 }, odd);
    }

    [Fact]
    public void Partition_WithAllMatching_ShouldReturnEmptyNotMatching()
    {
        NonEmptyList<int> list = new(2, 4, 6);
        (IReadOnlyList<int> even, IReadOnlyList<int> odd) = list.Partition(x => x % 2 == 0);

        Assert.Equal(3, even.Count);
        Assert.Empty(odd);
    }

    #endregion

    #region GroupByNonEmpty Tests

    [Fact]
    public void GroupByNonEmpty_ShouldGroupByKey()
    {
        NonEmptyList<string> list = new("apple", "banana", "apricot", "blueberry");
        Dictionary<char, NonEmptyList<string>> result = list.GroupByNonEmpty(s => s[0]);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result['a'].Count);
        Assert.Equal(2, result['b'].Count);
        Assert.Contains("apple", result['a']);
        Assert.Contains("apricot", result['a']);
    }

    [Fact]
    public void GroupByNonEmpty_WithElementSelector_ShouldProjectElements()
    {
        NonEmptyList<string> list = new("apple", "banana", "apricot");
        Dictionary<char, NonEmptyList<int>> result = list.GroupByNonEmpty(s => s[0], s => s.Length);

        Assert.Equal(5, result['a'].Head); // "apple".Length
        Assert.Equal(6, result['b'].Head); // "banana".Length
    }

    #endregion
}
