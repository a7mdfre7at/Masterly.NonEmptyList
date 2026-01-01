namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region Equals Tests

    [Fact]
    public void Equals_WithSameElements_ShouldReturnTrue()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 3);

        Assert.True(list1.Equals(list2));
    }

    [Fact]
    public void Equals_WithDifferentElements_ShouldReturnFalse()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 4);

        Assert.False(list1.Equals(list2));
    }

    [Fact]
    public void Equals_WithDifferentLengths_ShouldReturnFalse()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2);

        Assert.False(list1.Equals(list2));
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        NonEmptyList<int> list = new(1, 2, 3);

        Assert.False(list.Equals(null));
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        NonEmptyList<int> list = new(1, 2, 3);

        Assert.True(list.Equals(list));
    }

    [Fact]
    public void Equals_Object_WithSameElements_ShouldReturnTrue()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        object list2 = new NonEmptyList<int>(1, 2, 3);

        Assert.True(list1.Equals(list2));
    }

    [Fact]
    public void Equals_Object_WithDifferentType_ShouldReturnFalse()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        object other = new List<int> { 1, 2, 3 };

        Assert.False(list.Equals(other));
    }

    #endregion

    #region SequenceEqual with Comparer Tests

    [Fact]
    public void SequenceEqual_WithComparer_ShouldUseComparer()
    {
        NonEmptyList<string> list1 = new("A", "B", "C");
        NonEmptyList<string> list2 = new("a", "b", "c");

        Assert.True(list1.SequenceEqual(list2, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void SequenceEqual_WithComparer_DifferentElements_ShouldReturnFalse()
    {
        NonEmptyList<string> list1 = new("A", "B", "C");
        NonEmptyList<string> list2 = new("a", "b", "d");

        Assert.False(list1.SequenceEqual(list2, StringComparer.OrdinalIgnoreCase));
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameElements_ShouldReturnSameHash()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 3);

        Assert.Equal(list1.GetHashCode(), list2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentElements_ShouldReturnDifferentHash()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 4);

        // Note: Hash collisions are possible, so this isn't guaranteed
        // but is very likely for different small lists
        Assert.NotEqual(list1.GetHashCode(), list2.GetHashCode());
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void EqualityOperator_WithSameElements_ShouldReturnTrue()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 3);

        Assert.True(list1 == list2);
    }

    [Fact]
    public void EqualityOperator_WithDifferentElements_ShouldReturnFalse()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 4);

        Assert.False(list1 == list2);
    }

    [Fact]
    public void EqualityOperator_WithNullLeft_ShouldReturnFalse()
    {
        NonEmptyList<int>? list1 = null;
        NonEmptyList<int> list2 = new(1, 2, 3);

        Assert.False(list1 == list2);
    }

    [Fact]
    public void EqualityOperator_WithNullRight_ShouldReturnFalse()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int>? list2 = null;

        Assert.False(list1 == list2);
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ShouldReturnTrue()
    {
        NonEmptyList<int>? list1 = null;
        NonEmptyList<int>? list2 = null;

        Assert.True(list1 == list2);
    }

    [Fact]
    public void InequalityOperator_WithDifferentElements_ShouldReturnTrue()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 4);

        Assert.True(list1 != list2);
    }

    [Fact]
    public void InequalityOperator_WithSameElements_ShouldReturnFalse()
    {
        NonEmptyList<int> list1 = new(1, 2, 3);
        NonEmptyList<int> list2 = new(1, 2, 3);

        Assert.False(list1 != list2);
    }

    #endregion
}
