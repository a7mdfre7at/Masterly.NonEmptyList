using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

public class ImmutableNonEmptyListTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithSingleElement_ShouldCreateList()
    {
        ImmutableNonEmptyList<int> list = new(42);

        Assert.Single(list);
        Assert.Equal(42, list.Head);
    }

    [Fact]
    public void Constructor_WithMultipleElements_ShouldCreateList()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list.Head);
        Assert.Equal(3, list.Last);
    }

    [Fact]
    public void Constructor_WithNullFirstItem_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ImmutableNonEmptyList<string>(null!));
    }

    [Fact]
    public void Constructor_WithNullInOtherItems_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ImmutableNonEmptyList<string>("a", null!, "c"));
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void From_WithValidEnumerable_ShouldCreateList()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList<int>.From(new[] { 1, 2, 3 });

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void From_WithEmptyEnumerable_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            ImmutableNonEmptyList<int>.From(Array.Empty<int>()));
    }

    [Fact]
    public void TryFrom_WithValidEnumerable_ShouldReturnTrue()
    {
        bool success = ImmutableNonEmptyList<int>.TryFrom(new[] { 1, 2, 3 }, out ImmutableNonEmptyList<int>? result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
    }

    [Fact]
    public void TryFrom_WithEmptyEnumerable_ShouldReturnFalse()
    {
        bool success = ImmutableNonEmptyList<int>.TryFrom(Array.Empty<int>(), out ImmutableNonEmptyList<int>? result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Singleton_ShouldCreateSingleElementList()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList<int>.Singleton(42);

        Assert.Single(list);
        Assert.Equal(42, list.Head);
    }

    [Fact]
    public void Repeat_ShouldCreateRepeatedElements()
    {
        ImmutableNonEmptyList<string> list = ImmutableNonEmptyList<string>.Repeat("x", 5);

        Assert.Equal(5, list.Count);
        Assert.All(list, item => Assert.Equal("x", item));
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void Tail_WithMultipleElements_ShouldReturnTail()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        ImmutableNonEmptyList<int>? tail = list.Tail;

        Assert.NotNull(tail);
        Assert.Equal(2, tail!.Count);
        Assert.Equal(2, tail.Head);
    }

    [Fact]
    public void Tail_WithSingleElement_ShouldReturnNull()
    {
        ImmutableNonEmptyList<int> list = new(42);

        Assert.Null(list.Tail);
    }

    [Fact]
    public void Init_WithMultipleElements_ShouldReturnInit()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        ImmutableNonEmptyList<int>? init = list.Init;

        Assert.NotNull(init);
        Assert.Equal(2, init!.Count);
        Assert.Equal(1, init.Head);
        Assert.Equal(2, init.Last);
    }

    [Fact]
    public void Init_WithSingleElement_ShouldReturnNull()
    {
        ImmutableNonEmptyList<int> list = new(42);

        Assert.Null(list.Init);
    }

    #endregion

    #region Immutable Operation Tests

    [Fact]
    public void Append_ShouldReturnNewListWithItem()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3);
        ImmutableNonEmptyList<int> result = original.Append(4);

        Assert.Equal(3, original.Count); // Original unchanged
        Assert.Equal(4, result.Count);
        Assert.Equal(4, result.Last);
    }

    [Fact]
    public void AppendRange_ShouldReturnNewListWithItems()
    {
        ImmutableNonEmptyList<int> original = new(1, 2);
        ImmutableNonEmptyList<int> result = original.AppendRange(new[] { 3, 4, 5 });

        Assert.Equal(2, original.Count);
        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result.ToArray());
    }

    [Fact]
    public void Prepend_ShouldReturnNewListWithItemAtStart()
    {
        ImmutableNonEmptyList<int> original = new(2, 3, 4);
        ImmutableNonEmptyList<int> result = original.Prepend(1);

        Assert.Equal(3, original.Count);
        Assert.Equal(4, result.Count);
        Assert.Equal(1, result.Head);
    }

    [Fact]
    public void RemoveFirst_ShouldReturnTail()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3);
        ImmutableNonEmptyList<int>? result = original.RemoveFirst();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal(2, result.Head);
    }

    [Fact]
    public void RemoveFirst_WithSingleElement_ShouldReturnNull()
    {
        ImmutableNonEmptyList<int> original = new(42);
        ImmutableNonEmptyList<int>? result = original.RemoveFirst();

        Assert.Null(result);
    }

    [Fact]
    public void RemoveLast_ShouldReturnInit()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3);
        ImmutableNonEmptyList<int>? result = original.RemoveLast();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal(2, result.Last);
    }

    [Fact]
    public void RemoveAt_ShouldRemoveElement()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3, 4);
        ImmutableNonEmptyList<int>? result = original.RemoveAt(1);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal(new[] { 1, 3, 4 }, result.ToArray());
    }

    [Fact]
    public void Remove_ShouldRemoveFirstOccurrence()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3, 2);
        ImmutableNonEmptyList<int>? result = original.Remove(2);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal(new[] { 1, 3, 2 }, result.ToArray());
    }

    [Fact]
    public void Remove_WhenNotFound_ShouldReturnSame()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3);
        ImmutableNonEmptyList<int>? result = original.Remove(99);

        Assert.Same(original, result);
    }

    [Fact]
    public void SetItem_ShouldReturnNewListWithReplacedElement()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3);
        ImmutableNonEmptyList<int> result = original.SetItem(1, 20);

        Assert.Equal(2, original[1]); // Original unchanged
        Assert.Equal(20, result[1]);
    }

    [Fact]
    public void Insert_ShouldReturnNewListWithInsertedElement()
    {
        ImmutableNonEmptyList<int> original = new(1, 3, 4);
        ImmutableNonEmptyList<int> result = original.Insert(1, 2);

        Assert.Equal(3, original.Count);
        Assert.Equal(4, result.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, result.ToArray());
    }

    #endregion

    #region Functional Operation Tests

    [Fact]
    public void Map_ShouldTransformAllElements()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        ImmutableNonEmptyList<int> result = list.Map(x => x * 2);

        Assert.Equal(new[] { 2, 4, 6 }, result.ToArray());
    }

    [Fact]
    public void FlatMap_ShouldFlattenNestedLists()
    {
        ImmutableNonEmptyList<int> list = new(1, 2);
        ImmutableNonEmptyList<int> result = list.FlatMap(x => new ImmutableNonEmptyList<int>(x, x * 10));

        Assert.Equal(new[] { 1, 10, 2, 20 }, result.ToArray());
    }

    [Fact]
    public void Reduce_ShouldCombineAllElements()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3, 4);
        int result = list.Reduce((a, b) => a + b);

        Assert.Equal(10, result);
    }

    [Fact]
    public void Fold_ShouldAccumulateWithSeed()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        int result = list.Fold(10, (acc, x) => acc + x);

        Assert.Equal(16, result);
    }

    [Fact]
    public void Zip_ShouldPairElements()
    {
        ImmutableNonEmptyList<int> list1 = new(1, 2, 3);
        ImmutableNonEmptyList<string> list2 = new("a", "b", "c");
        ImmutableNonEmptyList<(int, string)> result = list1.Zip(list2);

        Assert.Equal((1, "a"), result[0]);
        Assert.Equal((2, "b"), result[1]);
        Assert.Equal((3, "c"), result[2]);
    }

    [Fact]
    public void Match_WithSingleElement_ShouldCallSingleFunction()
    {
        ImmutableNonEmptyList<int> list = new(42);
        string result = list.Match(
            single: x => $"Single: {x}",
            multiple: (head, tail) => $"Multiple: {head}"
        );

        Assert.Equal("Single: 42", result);
    }

    #endregion

    #region Collection Operation Tests

    [Fact]
    public void Concat_ShouldCombineLists()
    {
        ImmutableNonEmptyList<int> list1 = new(1, 2);
        ImmutableNonEmptyList<int> list2 = new(3, 4);
        ImmutableNonEmptyList<int> result = list1.Concat(list2);

        Assert.Equal(new[] { 1, 2, 3, 4 }, result.ToArray());
    }

    [Fact]
    public void Reverse_ShouldReverseElements()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        ImmutableNonEmptyList<int> result = list.Reverse();

        Assert.Equal(new[] { 3, 2, 1 }, result.ToArray());
    }

    [Fact]
    public void Distinct_ShouldRemoveDuplicates()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 2, 3, 3, 3);
        ImmutableNonEmptyList<int> result = list.Distinct();

        Assert.Equal(new[] { 1, 2, 3 }, result.ToArray());
    }

    [Fact]
    public void Take_ShouldReturnFirstNElements()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3, 4, 5);
        ImmutableNonEmptyList<int>? result = list.Take(3);

        Assert.NotNull(result);
        Assert.Equal(new[] { 1, 2, 3 }, result!.ToArray());
    }

    [Fact]
    public void Skip_ShouldSkipFirstNElements()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3, 4, 5);
        ImmutableNonEmptyList<int>? result = list.Skip(2);

        Assert.NotNull(result);
        Assert.Equal(new[] { 3, 4, 5 }, result!.ToArray());
    }

    [Fact]
    public void Intersperse_ShouldInsertSeparators()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        ImmutableNonEmptyList<int> result = list.Intersperse(0);

        Assert.Equal(new[] { 1, 0, 2, 0, 3 }, result.ToArray());
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void ToMutable_ShouldCreateMutableList()
    {
        ImmutableNonEmptyList<int> immutable = new(1, 2, 3);
        NonEmptyList<int> mutable = immutable.ToMutable();

        Assert.Equal(immutable.ToArray(), mutable.ToArray());

        // Verify it's mutable
        mutable.Add(4);
        Assert.Equal(4, mutable.Count);
        Assert.Equal(3, immutable.Count); // Immutable unchanged
    }

    [Fact]
    public void ToImmutable_FromMutable_ShouldCreateImmutableList()
    {
        NonEmptyList<int> mutable = new(1, 2, 3);
        ImmutableNonEmptyList<int> immutable = mutable.ToImmutable();

        Assert.Equal(mutable.ToArray(), immutable.ToArray());
    }

    [Fact]
    public void ToArray_ShouldCreateArray()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        int[] array = list.ToArray();

        Assert.Equal(new[] { 1, 2, 3 }, array);
    }

    [Fact]
    public void ToList_ShouldCreateMutableList()
    {
        ImmutableNonEmptyList<int> immutable = new(1, 2, 3);
        List<int> list = immutable.ToList();

        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameElements_ShouldReturnTrue()
    {
        ImmutableNonEmptyList<int> list1 = new(1, 2, 3);
        ImmutableNonEmptyList<int> list2 = new(1, 2, 3);

        Assert.True(list1.Equals(list2));
        Assert.True(list1 == list2);
    }

    [Fact]
    public void Equals_WithDifferentElements_ShouldReturnFalse()
    {
        ImmutableNonEmptyList<int> list1 = new(1, 2, 3);
        ImmutableNonEmptyList<int> list2 = new(1, 2, 4);

        Assert.False(list1.Equals(list2));
        Assert.True(list1 != list2);
    }

    [Fact]
    public void GetHashCode_SameElements_ShouldReturnSameHash()
    {
        ImmutableNonEmptyList<int> list1 = new(1, 2, 3);
        ImmutableNonEmptyList<int> list2 = new(1, 2, 3);

        Assert.Equal(list1.GetHashCode(), list2.GetHashCode());
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void JsonSerialize_ShouldSerializeAsArray()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        string json = JsonSerializer.Serialize(list);

        Assert.Equal("[1,2,3]", json);
    }

    [Fact]
    public void JsonDeserialize_ShouldDeserializeFromArray()
    {
        string json = "[1,2,3]";
        ImmutableNonEmptyList<int>? list = JsonSerializer.Deserialize<ImmutableNonEmptyList<int>>(json);

        Assert.NotNull(list);
        Assert.Equal(new[] { 1, 2, 3 }, list!.ToArray());
    }

    [Fact]
    public void JsonRoundTrip_ShouldPreserveData()
    {
        ImmutableNonEmptyList<int> original = new(1, 2, 3);
        string json = JsonSerializer.Serialize(original);
        ImmutableNonEmptyList<int>? restored = JsonSerializer.Deserialize<ImmutableNonEmptyList<int>>(json);

        Assert.NotNull(restored);
        Assert.Equal(original, restored);
    }

    #endregion

    #region Deconstruction Tests

    [Fact]
    public void Deconstruct_ShouldDeconstructCorrectly()
    {
        ImmutableNonEmptyList<int> list = new(1, 2, 3);
        (int head, ImmutableNonEmptyList<int>? tail) = list;

        Assert.Equal(1, head);
        Assert.NotNull(tail);
        Assert.Equal(2, tail!.Head);
    }

    #endregion
}
