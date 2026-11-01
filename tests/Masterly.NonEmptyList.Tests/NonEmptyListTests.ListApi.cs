using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// The List&lt;T&gt;-compatible surface (search, copy, sort, capacity) and the non-generic interfaces.
/// </summary>
public partial class NonEmptyListTests
{
    #region Construction guards

    [Fact]
    public void Constructor_Span_WithNullFirstItem_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<string>(null!, "b"));
    }

    [Fact]
    public void Constructor_Enumerable_WithNullFirstItem_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<string>(null!, new List<string> { "b" }));
    }

    [Fact]
    public void Constructor_Enumerable_WithNullOtherItems_ShouldCreateSingleElementList()
    {
        NonEmptyList<string> list = new("a", (IEnumerable<string>)null!);

        Assert.Equal(new[] { "a" }, list);
    }

    [Fact]
    public void Constructor_Enumerable_WithLazySequence_ShouldMaterializeIt()
    {
        NonEmptyList<int> list = new(1, Enumerable.Range(2, 3).Where(_ => true));

        Assert.Equal(new[] { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void Constructor_Enumerable_WithNullInOtherItems_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<string>("a", new List<string> { "b", null! }));
    }

    [Fact]
    public void Of_WithExplicitArray_ShouldUseTheArrayOverload()
    {
        NonEmptyList<int> list = NonEmptyList.Of(1, new[] { 2, 3 });

        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    #endregion

    #region Range mutation guards

    [Fact]
    public void AddRange_WithEmptyOrNullContainingCollection_ShouldThrowAndLeaveListUnchanged()
    {
        NonEmptyList<string> list = new("a");

        Assert.Throws<ArgumentException>(() => list.AddRange(new List<string>()));
        Assert.Throws<ArgumentNullException>(() => list.AddRange(new List<string> { "b", null! }));
        Assert.Equal(new[] { "a" }, list);
    }

    [Fact]
    public void InsertRange_ShouldInsertAtIndex()
    {
        NonEmptyList<int> list = new(1, 4);

        list.InsertRange(1, new[] { 2, 3 });

        Assert.Equal(new[] { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void InsertRange_WithEmptyOrNullContainingCollection_ShouldThrowAndLeaveListUnchanged()
    {
        NonEmptyList<string> list = new("a");

        Assert.Throws<ArgumentException>(() => list.InsertRange(0, new List<string>()));
        Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, new List<string> { null! }));
        Assert.Equal(new[] { "a" }, list);
    }

    [Fact]
    public void SetRange_WithLazySequence_ShouldReplaceElements()
    {
        NonEmptyList<int> list = new(1, 2, 3);

        list.SetRange(1, Enumerable.Range(20, 2).Where(_ => true));

        Assert.Equal(new[] { 1, 20, 21 }, list);
    }

    #endregion

    #region Search and copy

    [Fact]
    public void IndexOfAndLastIndexOf_Overloads_ShouldMatchListSemantics()
    {
        NonEmptyList<int> list = new(1, 2, 1, 2, 1);

        Assert.Equal(2, list.IndexOf(1, 1));
        Assert.Equal(-1, list.IndexOf(1, 1, 1));
        Assert.Equal(4, list.LastIndexOf(1));
        Assert.Equal(2, list.LastIndexOf(1, 3));
        Assert.Equal(-1, list.LastIndexOf(1, 3, 1));
    }

    [Fact]
    public void CopyTo_Overloads_ShouldCopyRequestedElements()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        int[] whole = new int[3];
        int[] part = new int[4];

        list.CopyTo(whole);
        list.CopyTo(1, part, 2, 2);

        Assert.Equal(new[] { 1, 2, 3 }, whole);
        Assert.Equal(new[] { 0, 0, 2, 3 }, part);
    }

    [Fact]
    public void FindFamily_ShouldMatchListSemantics()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5, 6);
        Predicate<int> even = x => x % 2 == 0;

        Assert.Equal(new[] { 2, 4, 6 }, list.FindAll(even));
        Assert.Empty(list.FindAll(x => x > 10));
        Assert.Equal(3, list.FindIndex(2, even));
        Assert.Equal(-1, list.FindIndex(4, 1, even));
        Assert.Equal(6, list.FindLast(even));
        Assert.Equal(0, list.FindLast(x => x > 10));
        Assert.Equal(5, list.FindLastIndex(even));
        Assert.Equal(3, list.FindLastIndex(4, even));
        Assert.Equal(-1, list.FindLastIndex(4, 1, even));
    }

    [Fact]
    public void BinarySearch_Overloads_ShouldFindElementsInSortedList()
    {
        NonEmptyList<int> list = new(1, 3, 5, 7);
        IComparer<int> descending = Comparer<int>.Create((a, b) => b.CompareTo(a));
        NonEmptyList<int> reversed = new(7, 5, 3, 1);

        Assert.Equal(2, list.BinarySearch(5));
        Assert.Equal(~2, list.BinarySearch(4));
        Assert.Equal(1, reversed.BinarySearch(5, descending));
        Assert.Equal(3, reversed.BinarySearch(2, 2, 1, descending));

        // The default comparer would give ~4 and ~0 here, so these pin that the comparer is used.
        Assert.Equal(0, reversed.BinarySearch(7, descending));
        Assert.Equal(2, reversed.BinarySearch(3, descending));

        // A whole-list search would find 7 at index 0, so this pins that index/count are honored.
        Assert.Equal(~2, reversed.BinarySearch(2, 2, 7, descending));
    }

    #endregion

    #region Ordering and capacity

    [Fact]
    public void ReverseRange_ShouldReverseOnlyThatRange()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4);

        list.Reverse(1, 2);

        Assert.Equal(new[] { 1, 3, 2, 4 }, list);
    }

    [Fact]
    public void Sort_Overloads_ShouldSortInPlace()
    {
        IComparer<int> descending = Comparer<int>.Create((a, b) => b.CompareTo(a));
        NonEmptyList<int> byComparison = new(3, 1, 2);
        NonEmptyList<int> byComparer = new(3, 1, 2);
        NonEmptyList<int> byRange = new(0, 3, 1, 2, 9);
        NonEmptyList<int> byDefault = new(3, 1, 2);

        byComparison.Sort((a, b) => b.CompareTo(a));
        byComparer.Sort(descending);
        byRange.Sort(1, 3, descending);
        byDefault.Sort();

        Assert.Equal(new[] { 3, 2, 1 }, byComparison);
        Assert.Equal(new[] { 3, 2, 1 }, byComparer);
        // Ignoring the range gives {9,3,2,1,0}; ignoring the comparer gives {0,1,2,3,9}.
        Assert.Equal(new[] { 0, 3, 2, 1, 9 }, byRange);
        Assert.Equal(new[] { 1, 2, 3 }, byDefault);
    }

    [Fact]
    public void CapacityMembers_ShouldManageInternalStorage()
    {
        NonEmptyList<int> list = new(1, 2);

        list.Capacity = 50;
        Assert.Equal(50, list.Capacity);

        list.TrimExcess();
        Assert.Equal(2, list.Capacity);

        Assert.True(list.EnsureCapacity(10) >= 10);
        Assert.True(list.Capacity >= 10);
        Assert.Equal(new[] { 1, 2 }, list);
    }

    [Fact]
    public void First_ShouldBeTheHead()
    {
        Assert.Equal(7, new NonEmptyList<int>(7, 8).First);
    }

    #endregion

    #region Non-generic interfaces

    [Fact]
    public void ICollection_NonGeneric_ShouldExposeSyncMembersAndCopyTo()
    {
        ICollection collection = new NonEmptyList<int>(1, 2);
        int[] target = new int[3];

        collection.CopyTo(target, 1);

        Assert.False(collection.IsSynchronized);
        Assert.NotNull(collection.SyncRoot);
        Assert.Equal(new[] { 0, 1, 2 }, target);
    }

    [Fact]
    public void IList_NonGeneric_ShouldReadAndIgnoreForeignTypes()
    {
        IList list = new NonEmptyList<int>(1, 2);

        Assert.Equal(2, list[1]);
        Assert.True(list.Contains(2));
        Assert.False(list.Contains("2"));
        Assert.Equal(1, list.IndexOf(2));
        Assert.Equal(-1, list.IndexOf("2"));
    }

    #endregion

    #region Functional helpers

    [Fact]
    public void MatchAction_ShouldInvokeSingleOrMultiple()
    {
        List<string> calls = new();

        new NonEmptyList<int>(1).Match(h => calls.Add($"single {h}"), (h, t) => calls.Add($"multi {h}+{t.Count}"));
        new NonEmptyList<int>(1, 2, 3).Match(h => calls.Add($"single {h}"), (h, t) => calls.Add($"multi {h}+{t.Count}"));

        Assert.Equal(new[] { "single 1", "multi 1+2" }, calls);
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<int>(1).Match(null!, (_, _) => { }));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyList<int>(1).Match(_ => { }, null!));
    }

    [Fact]
    public void DistinctList_WithComparer_ShouldUseIt()
    {
        NonEmptyList<string> list = new("x", "X", "y");

        Assert.Equal(new[] { "x", "y" }, list.DistinctList(StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SkipNonEmpty_WithZeroOrNegative_ShouldReturnACopy(int count)
    {
        NonEmptyList<int> list = new(1, 2);

        NonEmptyList<int> copy = list.SkipNonEmpty(count)!;

        Assert.NotSame(list, copy);
        Assert.Equal(list, copy);

        // The copy must not share storage with the original.
        copy.Add(3);
        Assert.Equal(new[] { 1, 2 }, list);
    }

    [Fact]
    public void SlidingAndChunkNonEmpty_WithInvalidArguments_ShouldThrow()
    {
        NonEmptyList<int> list = new(1, 2);

        // .First() drives a single MoveNext, so a missing guard fails fast instead of looping forever.
        Assert.Throws<ArgumentOutOfRangeException>("size", () => list.Sliding(0).First());
        Assert.Throws<ArgumentOutOfRangeException>("step", () => list.Sliding(1, 0).First());
        Assert.Throws<ArgumentOutOfRangeException>("size", () => list.ChunkNonEmpty(0).First());
    }

    [Fact]
    public void AsSpanAndAsMemory_ShouldExposeTheElements()
    {
        NonEmptyList<int> list = new(1, 2, 3);

        Assert.Equal(new[] { 1, 2, 3 }, list.AsSpan().ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, list.AsMemory().ToArray());
    }

    [Fact]
    public void SequenceEqual_WithComparer_ShouldHandleNullSameAndDifferentCounts()
    {
        NonEmptyList<string> list = new("a", "b");

        Assert.False(list.SequenceEqual(null!, StringComparer.Ordinal));
        Assert.True(list.SequenceEqual(list, StringComparer.Ordinal));
        Assert.False(list.SequenceEqual(new NonEmptyList<string>("a"), StringComparer.Ordinal));
        Assert.True(list.SequenceEqual(new NonEmptyList<string>("A", "B"), StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void DebuggerTypeProxy_ShouldShowTheElements()
    {
        NonEmptyList<int> list = new(1, 2);
        DebuggerTypeProxyAttribute proxy = typeof(NonEmptyList<int>).GetCustomAttribute<DebuggerTypeProxyAttribute>()!;
        Type viewType = Type.GetType(proxy.ProxyTypeName)!.MakeGenericType(typeof(int));

        object view = Activator.CreateInstance(viewType, list)!;
        int[] items = (int[])viewType.GetProperty("Items")!.GetValue(view)!;

        Assert.Equal(new[] { 1, 2 }, items);
    }

    #endregion
}
