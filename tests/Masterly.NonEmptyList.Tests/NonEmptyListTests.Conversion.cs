namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region ToHashSet Tests

    [Fact]
    public void ToHashSet_ShouldCreateHashSet()
    {
        NonEmptyList<int> list = new(1, 2, 2, 3, 3, 3);
        HashSet<int> set = list.ToHashSet();

        Assert.Equal(3, set.Count);
        Assert.Contains(1, set);
        Assert.Contains(2, set);
        Assert.Contains(3, set);
    }

    [Fact]
    public void ToHashSet_WithComparer_ShouldUseComparer()
    {
        NonEmptyList<string> list = new("A", "a", "B", "b");
        HashSet<string> set = list.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(2, set.Count);
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_WithKeySelector_ShouldCreateDictionary()
    {
        NonEmptyList<string> list = new("apple", "banana", "cherry");
        Dictionary<char, string> dict = list.ToDictionary(s => s[0]);

        Assert.Equal(3, dict.Count);
        Assert.Equal("apple", dict['a']);
        Assert.Equal("banana", dict['b']);
        Assert.Equal("cherry", dict['c']);
    }

    [Fact]
    public void ToDictionary_WithKeyAndValueSelector_ShouldCreateDictionary()
    {
        NonEmptyList<string> list = new("apple", "banana", "cherry");
        Dictionary<char, int> dict = list.ToDictionary(s => s[0], s => s.Length);

        Assert.Equal(3, dict.Count);
        Assert.Equal(5, dict['a']);
        Assert.Equal(6, dict['b']);
        Assert.Equal(6, dict['c']);
    }

    #endregion

    #region ToQueue Tests

    [Fact]
    public void ToQueue_ShouldCreateQueueInOrder()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        Queue<int> queue = list.ToQueue();

        Assert.Equal(3, queue.Count);
        Assert.Equal(1, queue.Dequeue());
        Assert.Equal(2, queue.Dequeue());
        Assert.Equal(3, queue.Dequeue());
    }

    #endregion

    #region ToStack Tests

    [Fact]
    public void ToStack_ShouldCreateStack()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        Stack<int> stack = list.ToStack();

        Assert.Equal(3, stack.Count);
        Assert.Equal(3, stack.Pop()); // Last in, first out
        Assert.Equal(2, stack.Pop());
        Assert.Equal(1, stack.Pop());
    }

    #endregion

    #region ToLinkedList Tests

    [Fact]
    public void ToLinkedList_ShouldCreateLinkedList()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        LinkedList<int> linked = list.ToLinkedList();

        Assert.Equal(3, linked.Count);
        Assert.Equal(1, linked.First!.Value);
        Assert.Equal(3, linked.Last!.Value);
    }

    #endregion

    #region AsReadOnly Tests

    [Fact]
    public void AsReadOnly_ShouldReturnReadOnlyList()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        IReadOnlyList<int> readOnly = list.AsReadOnly();

        Assert.Equal(3, readOnly.Count);
        Assert.Equal(1, readOnly[0]);
    }

    #endregion

    #region ToArrayNonEmpty Tests

    [Fact]
    public void ToArrayNonEmpty_ShouldCreateArray()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        int[] array = list.ToArrayNonEmpty();

        Assert.Equal(new[] { 1, 2, 3 }, array);
    }

    #endregion
}
