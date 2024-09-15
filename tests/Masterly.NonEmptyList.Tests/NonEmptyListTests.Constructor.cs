namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    [Fact]
    public void Constructor_ShouldCreateNonEmptyList_WithSingleItem()
    {
        NonEmptyList<int> list = new(1);
        Assert.Single(list);
        Assert.Equal(1, list.Head);
        Assert.Equal(1, list.Last);
    }

    [Fact]
    public void Constructor_ShouldCreateNonEmptyList_WithMultipleItems()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenOtherItemsContainsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<string>("1", null, "2", "3"));
    }
}