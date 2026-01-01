namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region TryFrom Tests

    [Fact]
    public void TryFrom_WithValidEnumerable_ShouldReturnTrue()
    {
        bool success = NonEmptyList<int>.TryFrom(new[] { 1, 2, 3 }, out NonEmptyList<int>? result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
    }

    [Fact]
    public void TryFrom_WithNull_ShouldReturnFalse()
    {
        bool success = NonEmptyList<int>.TryFrom(null, out NonEmptyList<int>? result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFrom_WithEmptyEnumerable_ShouldReturnFalse()
    {
        bool success = NonEmptyList<int>.TryFrom(Array.Empty<int>(), out NonEmptyList<int>? result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFrom_WithNullElements_ShouldReturnFalse()
    {
        string?[] arrayWithNull = new string?[] { "a", null, "b" };
        bool success = NonEmptyList<string>.TryFrom(arrayWithNull!, out NonEmptyList<string>? result);

        Assert.False(success);
        Assert.Null(result);
    }

    #endregion

    #region Indexer Null Check Tests

    [Fact]
    public void Indexer_SetWithNull_ShouldThrow()
    {
        NonEmptyList<string> list = new("a", "b", "c");

        Assert.Throws<ArgumentNullException>(() => list[1] = null!);
    }

    [Fact]
    public void Indexer_SetWithValidValue_ShouldWork()
    {
        NonEmptyList<string> list = new("a", "b", "c");
        list[1] = "x";

        Assert.Equal("x", list[1]);
    }

    #endregion

    #region RemoveAll Tests

    [Fact]
    public void RemoveAll_ShouldRemoveMatchingElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5, 6);
        int removed = list.RemoveAll(x => x % 2 == 0);

        Assert.Equal(3, removed);
        Assert.Equal(3, list.Count);
        Assert.Equal(new[] { 1, 3, 5 }, list.ToArray());
    }

    [Fact]
    public void RemoveAll_WhenWouldEmptyList_ShouldThrow()
    {
        NonEmptyList<int> list = new(2, 4, 6);

        Assert.Throws<InvalidOperationException>(() => list.RemoveAll(x => x % 2 == 0));
    }

    [Fact]
    public void RemoveAll_WhenNoMatches_ShouldReturnZero()
    {
        NonEmptyList<int> list = new(1, 3, 5);
        int removed = list.RemoveAll(x => x % 2 == 0);

        Assert.Equal(0, removed);
        Assert.Equal(3, list.Count);
    }

    #endregion

    #region SetRange Tests

    [Fact]
    public void SetRange_ShouldUpdateElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        list.SetRange(1, new[] { 20, 30, 40 });

        Assert.Equal(new[] { 1, 20, 30, 40, 5 }, list.ToArray());
    }

    [Fact]
    public void SetRange_WithNullElements_ShouldThrow()
    {
        NonEmptyList<string> list = new("a", "b", "c");

        Assert.Throws<ArgumentNullException>(() => list.SetRange(0, new string[] { null! }));
    }

    [Fact]
    public void SetRange_OutOfBounds_ShouldThrow()
    {
        NonEmptyList<int> list = new(1, 2, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.SetRange(2, new[] { 10, 20 }));
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithAllValid_ShouldReturnSelf()
    {
        NonEmptyList<int> list = new(2, 4, 6);
        NonEmptyList<int> result = list.Validate(x => x % 2 == 0, "All must be even");

        Assert.Same(list, result);
    }

    [Fact]
    public void Validate_WithInvalidElement_ShouldThrow()
    {
        NonEmptyList<int> list = new(2, 3, 6);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            list.Validate(x => x % 2 == 0, "All must be even"));

        Assert.Equal("All must be even", ex.Message);
    }

    [Fact]
    public void TryValidate_WithAllValid_ShouldReturnTrue()
    {
        NonEmptyList<int> list = new(2, 4, 6);
        bool result = list.TryValidate(x => x % 2 == 0, out int failing);

        Assert.True(result);
        Assert.Equal(0, failing); // default(int)
    }

    [Fact]
    public void TryValidate_WithInvalidElement_ShouldReturnFalse()
    {
        NonEmptyList<int> list = new(2, 3, 6);
        bool result = list.TryValidate(x => x % 2 == 0, out int failing);

        Assert.False(result);
        Assert.Equal(3, failing);
    }

    #endregion

    #region Singleton and Repeat Tests

    [Fact]
    public void Singleton_ShouldCreateSingleElementList()
    {
        NonEmptyList<int> list = NonEmptyList<int>.Singleton(42);

        Assert.Single(list);
        Assert.Equal(42, list.Head);
    }

    [Fact]
    public void Repeat_ShouldCreateRepeatedElements()
    {
        NonEmptyList<string> list = NonEmptyList<string>.Repeat("x", 5);

        Assert.Equal(5, list.Count);
        Assert.All(list, item => Assert.Equal("x", item));
    }

    [Fact]
    public void Repeat_WithCountLessThanOne_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NonEmptyList<int>.Repeat(1, 0));
    }

    #endregion
}
