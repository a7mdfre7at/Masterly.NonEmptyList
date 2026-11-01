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

    [Fact]
    public void Tail_ShouldReturnAFreshCopy_SoChangingItNeverLeaksIntoLaterReads()
    {
        NonEmptyList<int> list = [1, 2, 3];

        list.Tail!.Add(99);

        Assert.Equal([1, 2, 3], list);
        Assert.Equal([2, 3], list.Tail!);
        Assert.NotSame(list.Tail, list.Tail);
    }

    [Fact]
    public void Init_ShouldReturnAFreshCopy_SoChangingItNeverLeaksIntoLaterReads()
    {
        NonEmptyList<int> list = [1, 2, 3];

        list.Init!.Add(99);

        Assert.Equal([1, 2], list.Init!);
        Assert.NotSame(list.Init, list.Init);
    }

    [Fact]
    public void Tail_And_Init_ShouldReflectMutationsOfTheList()
    {
        NonEmptyList<int> list = [3, 1, 2];
        Assert.Equal([1, 2], list.Tail!);

        list.Sort();
        list.Add(4);

        Assert.Equal([2, 3, 4], list.Tail!);
        Assert.Equal([1, 2, 3], list.Init!);
    }
}
