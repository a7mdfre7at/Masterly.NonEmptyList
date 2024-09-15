namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    [Fact]
    public void Indexer_ShouldReturnCorrectElement()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }
}