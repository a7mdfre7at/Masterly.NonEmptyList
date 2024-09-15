namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    [Fact]
    public void Add_ShouldAddItemToNonEmptyList()
    {
        NonEmptyList<int> list = new(1);
        list.Add(2);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(2, list.Last);
    }

    [Fact]
    public void Add_ShouldThrowException_WhenAddingNullItem()
    {
        NonEmptyList<string> list = new("first");
        Assert.Throws<ArgumentNullException>(() => list.Add(null));
    }

    [Fact]
    public void AddRange_ShouldAddRangeToNonEmptyList()
    {
        NonEmptyList<int> list = new(1);
        list.AddRange(new[] { 2, 3 });
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }
}