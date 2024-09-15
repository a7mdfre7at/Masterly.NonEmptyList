namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    [Fact]
    public void Clear_ShouldThrowException_WhenListHasOneItem()
    {
        NonEmptyList<int> list = new(1);
        Assert.Throws<NotSupportedException>(() => list.Clear());
    }

    [Fact]
    public void Remove_ShouldRemoveItem()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        list.Remove(2);
        Assert.DoesNotContain(2, list);
    }

    [Fact]
    public void Remove_ShouldThrowException_WhenListHasOneItem()
    {
        NonEmptyList<int> list = new(1);
        Assert.Throws<InvalidOperationException>(() => list.Remove(1));
    }

    [Fact]
    public void Remove_ShouldThrowException_WhenListHasNullItem()
    {
        NonEmptyList<string> list = new("first", null);
        Assert.Throws<InvalidOperationException>(() => list.Remove(null));
    }

    [Fact]
    public void RemoveAt_ShouldRemoveItem()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        list.RemoveAt(1);
        Assert.DoesNotContain(2, list);
    }

    [Fact]
    public void RemoveAt_ShouldThrowException_WhenListHasOneItem()
    {
        NonEmptyList<int> list = new(1);
        Assert.Throws<InvalidOperationException>(() => list.RemoveAt(0));
    }


    [Fact]
    public void RemoveRange_ShouldRemoveItems()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        list.RemoveRange(1, 3);
        Assert.DoesNotContain(2, list);
        Assert.DoesNotContain(3, list);
        Assert.DoesNotContain(4, list);
    }

    [Fact]
    public void RemoveRange_ShouldThrowException_WhenListHasOneItem()
    {
        NonEmptyList<int> list = new(1);
        Assert.Throws<InvalidOperationException>(() => list.RemoveRange(0, 1));
    }
}