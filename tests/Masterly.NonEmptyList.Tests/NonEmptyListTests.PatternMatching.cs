namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region Match Tests

    [Fact]
    public void Match_WithSingleElement_ShouldCallSingleFunction()
    {
        NonEmptyList<int> list = new(42);
        string result = list.Match(
            single: x => $"Single: {x}",
            multiple: (head, tail) => $"Multiple: {head}, {tail.Count}"
        );

        Assert.Equal("Single: 42", result);
    }

    [Fact]
    public void Match_WithMultipleElements_ShouldCallMultipleFunction()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        string result = list.Match(
            single: x => $"Single: {x}",
            multiple: (head, tail) => $"Multiple: {head}, {tail.Count}"
        );

        Assert.Equal("Multiple: 1, 2", result);
    }

    [Fact]
    public void Match_Action_WithSingleElement_ShouldExecuteSingleAction()
    {
        NonEmptyList<int> list = new(42);
        string? result = null;

        list.Match(
            single: x => result = $"Single: {x}",
            multiple: (head, tail) => result = $"Multiple: {head}"
        );

        Assert.Equal("Single: 42", result);
    }

    [Fact]
    public void Match_Action_WithMultipleElements_ShouldExecuteMultipleAction()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        string? result = null;

        list.Match(
            single: x => result = $"Single: {x}",
            multiple: (head, tail) => result = $"Multiple: {head}, Tail: {tail.Count}"
        );

        Assert.Equal("Multiple: 1, Tail: 2", result);
    }

    #endregion

    #region Three-Element Deconstruction Tests

    [Fact]
    public void Deconstruct_ThreeElements_ShouldDeconstructCorrectly()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        (int first, int second, IEnumerable<int> rest) = list;

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(new[] { 3, 4, 5 }, rest.ToArray());
    }

    [Fact]
    public void Deconstruct_ThreeElements_WithTwoElements_ShouldHaveEmptyRest()
    {
        NonEmptyList<int> list = new(1, 2);
        (int first, int second, IEnumerable<int> rest) = list;

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Empty(rest);
    }

    [Fact]
    public void Deconstruct_ThreeElements_WithSingleElement_ShouldHaveDefaultSecond()
    {
        NonEmptyList<int> list = new(42);
        (int first, int second, IEnumerable<int> rest) = list;

        Assert.Equal(42, first);
        Assert.Equal(0, second); // default(int)
        Assert.Empty(rest);
    }

    #endregion
}
