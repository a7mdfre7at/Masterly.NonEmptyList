// Ignore Spelling: Deconstruct

namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    [Fact]
    public void Contains_ShouldReturnTrue_WhenItemExists()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        Assert.Contains(2, list);
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenItemDoesNotExist()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        Assert.DoesNotContain(4, list);
    }

    [Fact]
    public void ToString_ShouldReturnCorrectRepresentation()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        Assert.Equal("NonEmptyList: [1, 2, 3]", list.ToString());
    }

    // write test for deconstruction
    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        (int head, NonEmptyList<int>? tail) = list;

        Assert.Equal(1, head);
        Assert.Equal(2, tail?.Head);
        Assert.Equal(3, tail?.Last);
    }
}