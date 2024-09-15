namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    [Fact]
    public void Tail_ShouldReturnTailOfList_WithMultipleItems()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int>? tail = list.Tail;

        Assert.Equal(2, tail?.Count);
        Assert.Equal(2, tail?.Head);
        Assert.Equal(3, tail?.Last);
    }

    [Fact]
    public void Tail_ShouldThrowException_WhenListHasOneItem()
    {
        NonEmptyList<int> list = new(1);
        Assert.Null(list.Tail);
    }
}