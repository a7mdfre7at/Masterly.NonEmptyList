using System.Collections;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// The non-empty guarantee must hold no matter which interface the list is accessed through.
/// In v2 these operations emptied the list because method hiding does not apply to interface calls.
/// </summary>
public partial class NonEmptyListTests
{
    [Fact]
    public void IListOfT_Clear_ShouldThrowAndLeaveListIntact()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        IList<int> asInterface = list;

        Assert.Throws<NotSupportedException>(() => asInterface.Clear());
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void ICollectionOfT_Remove_ShouldNotEmptyTheList()
    {
        NonEmptyList<int> list = new(1, 2);
        ICollection<int> asCollection = list;

        Assert.True(asCollection.Remove(1));
        Assert.Throws<InvalidOperationException>(() => asCollection.Remove(2));
        Assert.Single(list);
        Assert.Equal(2, list.Head);
    }

    [Fact]
    public void IListOfT_RemoveAt_OnSingleElement_ShouldThrow()
    {
        NonEmptyList<int> list = new(42);
        IList<int> asInterface = list;

        Assert.Throws<InvalidOperationException>(() => asInterface.RemoveAt(0));
        Assert.Equal(42, list.Head);
    }

    [Fact]
    public void NonGenericIList_Clear_ShouldThrow()
    {
        NonEmptyList<string> list = new("a", "b");
        IList asInterface = list;

        Assert.Throws<NotSupportedException>(() => asInterface.Clear());
        Assert.Throws<InvalidOperationException>(() =>
        {
            asInterface.Remove("a");
            asInterface.Remove("b");
        });
        Assert.Single(list);
    }

    [Fact]
    public void NonGenericIList_AddAndIndexer_ShouldWork()
    {
        NonEmptyList<string> list = new("a");
        IList asInterface = list;

        int index = asInterface.Add("b");
        asInterface[0] = "z";

        Assert.Equal(1, index);
        Assert.Equal(new[] { "z", "b" }, list.ToArray());
        Assert.True(asInterface.Contains("b"));
        Assert.Equal(1, asInterface.IndexOf("b"));
        Assert.Throws<ArgumentException>(() => asInterface.Add(5));
        Assert.Throws<ArgumentNullException>(() => asInterface.Add(null));
    }

    [Fact]
    public void Remove_ShouldReturnFalse_WhenItemIsAbsent()
    {
        NonEmptyList<int> list = new(1);

        Assert.False(list.Remove(99));
        Assert.Single(list);
    }

    [Fact]
    public void MutationThroughInterface_ShouldInvalidateTailCache()
    {
        NonEmptyList<int> list = new(1, 2);
        NonEmptyList<int>? tailBefore = list.Tail;

        IList<int> asInterface = list;
        asInterface.Add(3);

        Assert.Equal(new[] { 2 }, tailBefore!.ToArray());
        Assert.Equal(new[] { 2, 3 }, list.Tail!.ToArray());
        Assert.Equal(new[] { 1, 2 }, list.Init!.ToArray());
    }

    [Fact]
    public void IsNotAList_AnyMore()
    {
        object list = new NonEmptyList<int>(1);

        Assert.False(list is List<int>);
        Assert.True(list is IList<int>);
        Assert.True(list is IReadOnlyList<int>);
        Assert.True(list is IList);
    }

    [Fact]
    public void ListCompatibilityMembers_ShouldForward()
    {
        NonEmptyList<int> list = new(3, 1, 2);

        list.Sort();
        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());

        list.Reverse();
        Assert.Equal(new[] { 3, 2, 1 }, list.ToArray());

        Assert.True(list.Exists(x => x == 2));
        Assert.Equal(2, list.Find(x => x == 2));
        Assert.Equal(1, list.FindIndex(x => x == 2));
        Assert.True(list.TrueForAll(x => x > 0));
        Assert.Equal(new[] { 2, 1 }, list.GetRange(1, 2));
        Assert.Equal(2, list.IndexOf(1));
        Assert.True(list.Capacity >= 3);

        int sum = 0;
        list.ForEach(x => sum += x);
        Assert.Equal(6, sum);

        NonEmptyList<string> converted = list.ConvertAll(x => x.ToString());
        Assert.Equal(new[] { "3", "2", "1" }, converted.ToArray());
    }

    [Fact]
    public void AsReadOnly_ShouldReflectLaterChanges()
    {
        NonEmptyList<int> list = new(1);
        IReadOnlyList<int> view = list.AsReadOnly();

        list.Add(2);

        Assert.Equal(2, view.Count);
    }
}
