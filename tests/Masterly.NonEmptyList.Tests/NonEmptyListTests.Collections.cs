namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region Init Tests

    [Fact]
    public void Init_WithMultipleElements_ShouldReturnAllButLast()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4);
        NonEmptyList<int>? init = list.Init;

        Assert.NotNull(init);
        Assert.Equal(3, init!.Count);
        Assert.Equal(new[] { 1, 2, 3 }, init.ToArray());
    }

    [Fact]
    public void Init_WithSingleElement_ShouldReturnNull()
    {
        NonEmptyList<int> list = new(1);
        Assert.Null(list.Init);
    }

    #endregion

    #region Concat Tests

    [Fact]
    public void Concat_WithNonEmptyList_ShouldCombineBothLists()
    {
        NonEmptyList<int> list1 = new(1, 2);
        NonEmptyList<int> list2 = new(3, 4);
        NonEmptyList<int> result = list1.Concat(list2);

        Assert.Equal(4, result.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, result.ToArray());
    }

    [Fact]
    public void Concat_WithEnumerable_ShouldAppendElements()
    {
        NonEmptyList<int> list = new(1, 2);
        NonEmptyList<int> result = list.Concat(new[] { 3, 4, 5 });

        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result.ToArray());
    }

    [Fact]
    public void Concat_WithEmptyEnumerable_ShouldReturnSameElements()
    {
        NonEmptyList<int> list = new(1, 2);
        NonEmptyList<int> result = list.Concat(Array.Empty<int>());

        Assert.Equal(2, result.Count);
    }

    #endregion

    #region Prepend Tests

    [Fact]
    public void Prepend_ShouldAddAtBeginning()
    {
        NonEmptyList<int> list = new(2, 3);
        list.Prepend(1);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());
    }

    #endregion

    #region ReverseList Tests

    [Fact]
    public void ReverseList_ShouldReturnReversedList()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4);
        NonEmptyList<int> result = list.ReverseList();

        Assert.Equal(new[] { 4, 3, 2, 1 }, result.ToArray());
    }

    [Fact]
    public void ReverseList_WithSingleElement_ShouldReturnSame()
    {
        NonEmptyList<int> list = new(42);
        NonEmptyList<int> result = list.ReverseList();

        Assert.Equal(42, result.Head);
    }

    #endregion

    #region Distinct Tests

    [Fact]
    public void DistinctList_ShouldRemoveDuplicates()
    {
        NonEmptyList<int> list = new(1, 2, 2, 3, 3, 3);
        NonEmptyList<int> result = list.DistinctList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 1, 2, 3 }, result.ToArray());
    }

    [Fact]
    public void DistinctBy_ShouldRemoveDuplicatesByKey()
    {
        NonEmptyList<string> list = new("apple", "apricot", "banana", "blueberry");
        NonEmptyList<string> result = list.DistinctBy(s => s[0]);

        Assert.Equal(2, result.Count);
        Assert.Equal("apple", result[0]);
        Assert.Equal("banana", result[1]);
    }

    #endregion

    #region Take/Skip Tests

    [Fact]
    public void TakeNonEmpty_ShouldReturnFirstNElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        NonEmptyList<int>? result = list.TakeNonEmpty(3);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal(new[] { 1, 2, 3 }, result.ToArray());
    }

    [Fact]
    public void TakeNonEmpty_WithZeroCount_ShouldReturnNull()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int>? result = list.TakeNonEmpty(0);

        Assert.Null(result);
    }

    [Fact]
    public void SkipNonEmpty_ShouldSkipFirstNElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        NonEmptyList<int>? result = list.SkipNonEmpty(2);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal(new[] { 3, 4, 5 }, result.ToArray());
    }

    [Fact]
    public void SkipNonEmpty_SkippingAll_ShouldReturnNull()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int>? result = list.SkipNonEmpty(3);

        Assert.Null(result);
    }

    [Fact]
    public void TakeAtLeastOne_ShouldEnsureMinimumOne()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int> result = list.TakeAtLeastOne(0);

        Assert.Single(result);
        Assert.Equal(1, result.Head);
    }

    #endregion

    #region Sliding Tests

    [Fact]
    public void Sliding_ShouldReturnWindows()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        List<NonEmptyList<int>> windows = list.Sliding(3).ToList();

        Assert.Equal(3, windows.Count);
        Assert.Equal(new[] { 1, 2, 3 }, windows[0].ToArray());
        Assert.Equal(new[] { 2, 3, 4 }, windows[1].ToArray());
        Assert.Equal(new[] { 3, 4, 5 }, windows[2].ToArray());
    }

    [Fact]
    public void Sliding_WithStep_ShouldSkipElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5, 6);
        List<NonEmptyList<int>> windows = list.Sliding(2, 2).ToList();

        Assert.Equal(3, windows.Count);
        Assert.Equal(new[] { 1, 2 }, windows[0].ToArray());
        Assert.Equal(new[] { 3, 4 }, windows[1].ToArray());
        Assert.Equal(new[] { 5, 6 }, windows[2].ToArray());
    }

    #endregion

    #region Chunk Tests

    [Fact]
    public void ChunkNonEmpty_ShouldSplitIntoChunks()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        List<NonEmptyList<int>> chunks = list.ChunkNonEmpty(2).ToList();

        Assert.Equal(3, chunks.Count);
        Assert.Equal(new[] { 1, 2 }, chunks[0].ToArray());
        Assert.Equal(new[] { 3, 4 }, chunks[1].ToArray());
        Assert.Equal(new[] { 5 }, chunks[2].ToArray());
    }

    #endregion

    #region Intersperse Tests

    [Fact]
    public void Intersperse_ShouldInsertSeparators()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int> result = list.Intersperse(0);

        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { 1, 0, 2, 0, 3 }, result.ToArray());
    }

    [Fact]
    public void Intersperse_WithSingleElement_ShouldReturnSame()
    {
        NonEmptyList<int> list = new(42);
        NonEmptyList<int> result = list.Intersperse(0);

        Assert.Single(result);
        Assert.Equal(42, result.Head);
    }

    #endregion

    #region Rotate Tests

    [Fact]
    public void RotateLeft_ShouldRotateElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        NonEmptyList<int> result = list.RotateLeft(2);

        Assert.Equal(new[] { 3, 4, 5, 1, 2 }, result.ToArray());
    }

    [Fact]
    public void RotateRight_ShouldRotateElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        NonEmptyList<int> result = list.RotateRight(2);

        Assert.Equal(new[] { 4, 5, 1, 2, 3 }, result.ToArray());
    }

    [Fact]
    public void RotateLeft_WithFullRotation_ShouldReturnSame()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int> result = list.RotateLeft(3);

        Assert.Equal(new[] { 1, 2, 3 }, result.ToArray());
    }

    [Fact]
    public void RotateLeft_WithNegative_ShouldRotateRight()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        NonEmptyList<int> result = list.RotateLeft(-2);

        Assert.Equal(new[] { 4, 5, 1, 2, 3 }, result.ToArray());
    }

    #endregion
}
