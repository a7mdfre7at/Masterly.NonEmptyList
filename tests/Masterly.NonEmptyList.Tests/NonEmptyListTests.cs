namespace Masterly.NonEmptyList.Tests;

public class NonEmptyListTests
{
    [Fact]
    public void Constructor_ShouldCreateNonEmptyList_WithSingleItem()
    {
        var list = new NonEmptyList<int>(1);
        Assert.Single(list);
        Assert.Equal(1, list.Head);
        Assert.Equal(1, list.Last);
    }

    [Fact]
    public void Constructor_ShouldCreateNonEmptyList_WithMultipleItems()
    {
        var list = new NonEmptyList<int>(1, 2, 3);
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }

    [Fact]
    public void Tail_ShouldReturnTailOfList_WithMultipleItems()
    {
        var list = new NonEmptyList<int>(1, 2, 3);
        var tail = list.Tail;

        Assert.Equal(2, tail.Count);
        Assert.Equal(2, tail.Head);
        Assert.Equal(3, tail.Last);
    }

    [Fact]
    public void Tail_ShouldThrowException_WhenListHasOneItem()
    {
        var list = new NonEmptyList<int>(1);
        Assert.Null(list.Tail);
    }

    [Fact]
    public void Add_ShouldAddItemToNonEmptyList()
    {
        var list = new NonEmptyList<int>(1);
        list.Add(2);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(2, list.Last);
    }

    [Fact]
    public void Add_ShouldThrowException_WhenAddingNullItem()
    {
        var list = new NonEmptyList<string>("first");
        Assert.Throws<ArgumentNullException>(() => list.Add(null));
    }

    [Fact]
    public void Indexer_ShouldReturnCorrectElement()
    {
        var list = new NonEmptyList<int>(1, 2, 3);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenItemExists()
    {
        var list = new NonEmptyList<int>(1, 2, 3);
        Assert.Contains(2, list);
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenItemDoesNotExist()
    {
        var list = new NonEmptyList<int>(1, 2, 3);
        Assert.DoesNotContain(4, list);
    }

    [Fact]
    public void ToString_ShouldReturnCorrectRepresentation()
    {
        var list = new NonEmptyList<int>(1, 2, 3);
        Assert.Equal("NonEmptyList: [1, 2, 3]", list.ToString());
    }

    [Fact]
    public void From_ShouldCreateNonEmptyList_FromEnumerable()
    {
        var enumerable = new List<int> { 1, 2, 3 };
        var list = NonEmptyList<int>.From(enumerable);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }

    [Fact]
    public void From_ShouldThrowException_WhenEnumerableIsEmpty()
    {
        var enumerable = new List<int>();
        Assert.Throws<ArgumentException>(() => NonEmptyList<int>.From(enumerable));
    }

    [Fact]
    public void From_ShouldThrowException_WhenEnumerableIsNull()
    {
        Assert.Throws<ArgumentException>(() => NonEmptyList<int>.From(null));
    }

    [Fact]
    public void AddRange_ShouldAddRangeToNonEmptyList()
    {
        var list = new NonEmptyList<int>(1);
        list.AddRange(new[] { 2, 3 });
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }
}