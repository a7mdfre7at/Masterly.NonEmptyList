#pragma warning disable CA2260 // deriving from NonEmptyList<T> is the feature under test, not a CRTP mistake
using System.Collections;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// The protected hooks on <see cref="NonEmptyList{T}"/> let derived types add invariants without reimplementing the list.
/// This mirrors what the source generator emits for [NonEmptyCollection].
/// </summary>
public class InvariantHooksTests
{
    private sealed class DistinctBounded : NonEmptyList<string>
    {
        public DistinctBounded(IEnumerable<string> items) : base(NonEmptyList<string>.From(items))
        {
            if (Count > 3)
                throw new ArgumentException("too many");
            if (Enumerable.Distinct(this).Count() != Count)
                throw new ArgumentException("duplicates");
        }

        public List<string> Log { get; } = new();

        protected override void ValidateItem(string item)
        {
            Log.Add($"item:{item}");
            if (Contains(item))
                throw new ArgumentException($"Duplicate: {item}");
        }

        protected override void ValidateRange(IReadOnlyList<string> items)
        {
            base.ValidateRange(items);
            Log.Add($"range:{items.Count}");
            if (items.Distinct().Count() != items.Count)
                throw new ArgumentException("Duplicates within batch");
        }

        protected override void ValidateReplacement(int index, string item)
        {
            Log.Add($"replace:{index}:{item}");
            int existing = IndexOf(item);
            if (existing >= 0 && existing != index)
                throw new ArgumentException($"Duplicate: {item}");
        }

        protected override void ValidateCount(int proposedCount)
        {
            Log.Add($"count:{proposedCount}");
            if (proposedCount > 3)
                throw new ArgumentException("Too many");
            if (proposedCount < 2)
                throw new ArgumentException("Too few");
        }
    }

    private sealed class Frozen : NonEmptyList<int>
    {
        public Frozen(params int[] items) : base(new NonEmptyList<int>(items[0], items.Skip(1)))
        {
        }

        protected override bool IsReadOnly => true;

        protected override void EnsureMutable() => throw new NotSupportedException("frozen");
    }

    [Fact]
    public void ProtectedCopyConstructor_ShouldCopyAndRejectNull()
    {
        DistinctBounded list = new(new[] { "a", "b" });

        Assert.Equal(new[] { "a", "b" }, list.ToArray());
        Assert.Throws<ArgumentException>(() => new DistinctBounded(new[] { "a", "a" }));
        Assert.Throws<ArgumentException>(() => new DistinctBounded(Array.Empty<string>()));
    }

    [Fact]
    public void ValidateItem_ShouldRunForEveryAddition()
    {
        DistinctBounded list = new(new[] { "a", "b" });

        list.Add("c");
        Assert.Throws<ArgumentException>(() => list.Add("a"));
        Assert.Throws<ArgumentException>(() => list.Insert(0, "b"));
        Assert.Throws<ArgumentException>(() => list.AddFirst("c"));
        Assert.Throws<ArgumentException>(() => list.AddRange(new[] { "a" }));
        Assert.Throws<ArgumentException>(() => list.InsertRange(0, new[] { "z", "b" }));
        Assert.Throws<ArgumentException>(() => list.InsertRange(0, new[] { "z", "z" }));
        Assert.Throws<ArgumentException>(() => list.AddRange(new List<string> { "z", "z" }));
        Assert.Contains("range:2", list.Log);
        Assert.Throws<ArgumentException>(() => ((IList)list).Add("a"));
        Assert.Throws<ArgumentException>(() => ((IList)list).Insert(0, "a"));
        Assert.Equal(new[] { "a", "b", "c" }, list.ToArray());
        Assert.Contains("item:a", list.Log);
    }

    [Fact]
    public void ValidateCount_ShouldBoundAdditionsAndRemovals()
    {
        DistinctBounded list = new(new[] { "a", "b", "c" });

        Assert.Throws<ArgumentException>(() => list.Add("d"));
        Assert.Throws<ArgumentException>(() => list.AddRange(new[] { "d", "e" }));
        Assert.Throws<ArgumentException>(() => list.InsertRange(0, new[] { "d" }));
        Assert.Throws<ArgumentException>(() => list.RemoveRange(0, 2));
        Assert.Throws<ArgumentException>(() => list.RemoveAll(s => s != "a"));
        list.Remove("c");
        Assert.Throws<ArgumentException>(() => list.Remove("b"));
        Assert.Throws<ArgumentException>(() => list.RemoveAt(0));
        Assert.Throws<ArgumentException>(() => ((IList)list).Remove("a"));
        Assert.Equal(new[] { "a", "b" }, list.ToArray());
        Assert.Contains("count:1", list.Log);
        Assert.Contains("count:4", list.Log);
    }

    [Fact]
    public void ValidateCount_ShouldPassForLegalRemovals()
    {
        DistinctBounded list = new(new[] { "a", "b", "c" });

        Assert.Equal(1, list.RemoveAll(s => s == "c"));
        Assert.False(list.Remove("zzz"));
        list.Add("c");
        list.RemoveAt(2);
        list.Add("c");
        list.RemoveRange(2, 1);
        Assert.Equal(new[] { "a", "b" }, list.ToArray());
    }

    [Fact]
    public void ValidateReplacement_ShouldAllowSameSlotAndRejectOtherDuplicates()
    {
        DistinctBounded list = new(new[] { "a", "b" });

        list[0] = "a";
        list[0] = "z";
        Assert.Throws<ArgumentException>(() => list[0] = "b");
        Assert.Throws<ArgumentException>(() => list.SetRange(0, new[] { "b" }));
        Assert.Throws<ArgumentException>(() => ((IList)list)[1] = "z");
        list.SetRange(0, new[] { "x", "y" });
        Assert.Equal(new[] { "x", "y" }, list.ToArray());
        Assert.Contains("replace:0:z", list.Log);
    }

    [Fact]
    public void ReadOnlyDerivedType_ShouldRejectEveryMutationAndReportIsReadOnly()
    {
        Frozen frozen = new(1, 2, 3);

        Assert.True(((ICollection<int>)frozen).IsReadOnly);
        Assert.True(((IList)frozen).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => frozen.Add(4));
        Assert.Throws<NotSupportedException>(() => frozen.Sort());
        Assert.Throws<NotSupportedException>(() => frozen.Reverse());
        Assert.Throws<NotSupportedException>(() => frozen.Remove(1));
        Assert.Throws<NotSupportedException>(() => frozen.Clear());
        Assert.Throws<NotSupportedException>(() => frozen[0] = 5);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)frozen).RemoveAt(0));
        Assert.Equal(6, frozen.Sum());
        Assert.Equal(new[] { 1, 2, 3 }, frozen.ToArray());
    }

    [Fact]
    public void PlainNonEmptyList_ShouldNotBeReadOnly()
    {
        NonEmptyList<int> list = [1];

        Assert.False(((ICollection<int>)list).IsReadOnly);
        Assert.False(((IList)list).IsReadOnly);
        Assert.False(((IList)list).IsFixedSize);
    }
}
