namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    [Fact]
    public void From_ShouldCreateNonEmptyList_FromEnumerable()
    {
        List<int> enumerable = new() { 1, 2, 3 };
        NonEmptyList<int> list = NonEmptyList<int>.From(enumerable);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }

    [Fact]
    public void From_ShouldThrowException_WhenEnumerableIsEmpty()
    {
        List<int> enumerable = new();
        Assert.Throws<ArgumentException>(() => NonEmptyList<int>.From(enumerable));
    }

    [Fact]
    public void From_ShouldThrowException_WhenEnumerableIsNull()
        => Assert.Throws<ArgumentException>(() => NonEmptyList<int>.From(null));
}