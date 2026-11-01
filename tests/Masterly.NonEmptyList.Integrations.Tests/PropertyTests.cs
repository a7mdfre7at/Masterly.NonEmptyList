using CsCheck;
using Masterly.NonEmptyList.Testing;

namespace Masterly.NonEmptyList.Integrations.Tests;

/// <summary>Property-based checks of the published laws using the generators from Masterly.NonEmptyList.Testing.</summary>
public class PropertyTests
{
    private static readonly Gen<NonEmptyList<int>> Ints = NonEmptyGen.List(Gen.Int[-100, 100]);
    private static readonly Gen<NonEmptyList<string>> Strings = NonEmptyGen.List(Gen.String[0, 5]);

    [Fact]
    public void Generators_ShouldNeverProduceEmptyCollections()
    {
        Ints.Sample(xs => xs.Count >= 1 && xs.Count <= 20);
        NonEmptyGen.List(Gen.Int, 3).Sample(xs => xs.Count is >= 1 and <= 3);
        NonEmptyGen.Immutable(Gen.Int).Sample(xs => xs.Count >= 1);
        NonEmptyGen.Set(Gen.Int[0, 3]).Sample(set => set.Count is >= 1 and <= 4);
        NonEmptyGen.Dictionary(Gen.Int[0, 5], Gen.String).Sample(d => d.Count is >= 1 and <= 6);
        NonEmptyGen.Sorted(Gen.Int).Sample(xs => ConstrainedList<int, Sorted>.Satisfies(xs));
        NonEmptyGen.Distinct(Gen.Int[0, 5]).Sample(xs => ConstrainedList<int, Distinct>.Satisfies(xs));
        NonEmptyGen.Constrained<int, AtLeast<Two>>(Gen.Int, minCount: 2).Sample(xs => xs.Count >= 2);
        NonEmptyGen.String().Sample(s => s.HasValue && !string.IsNullOrWhiteSpace(s.Value));
        NonEmptyGen.String("abc").Sample(s => s.Value.All(c => c is 'a' or 'b' or 'c'));
        NonEmptyGen.EventStream(Gen.Int[-1000, 1000]).Sample(stream => stream.Version >= 1);
        NonEmptyGen.Zipper(Gen.Int).Sample(z => z.Index >= 0 && z.Index < z.Count);
        NonEmptyGen.Tree(Gen.Int, maxDepth: 3, maxChildren: 2).Sample(tree => tree.Count >= 1 && tree.Depth <= 3 && tree.Flatten().Count == tree.Count);
        NonEmptyGen.Validation(Gen.Int, Gen.String).Sample(v => v.IsValid || v.Errors.Count >= 1);
    }

    [Fact]
    public void Generators_ShouldRejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.List<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Immutable<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Set<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Dictionary<int, int>(null!, Gen.Int));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Dictionary<int, int>(Gen.Int, null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Constrained<int, Sorted>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Sorted<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Distinct<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.String(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.EventStream<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Zipper<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Tree<int>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Validation<int, int>(null!, Gen.Int));
        Assert.Throws<ArgumentNullException>(() => NonEmptyGen.Validation<int, int>(Gen.Int, null!));
    }

    [Fact]
    public void FunctorLaws_ShouldHold()
    {
        Ints.Sample(xs => NonEmptyListLaws.MapIdentity(xs));
        Ints.Sample(xs => NonEmptyListLaws.MapComposition(xs, x => x + 1, x => x * 2));
        Ints.Sample(xs => NonEmptyListLaws.SelectMatchesMap(xs, x => x.ToString()));
    }

    [Fact]
    public void MonadLaws_ShouldHold()
    {
        Gen.Int.Sample(x => NonEmptyListLaws.FlatMapLeftIdentity(x, v => NonEmptyList.Of(v, v)));
        Ints.Sample(xs => NonEmptyListLaws.FlatMapRightIdentity(xs));
        Ints.Sample(xs => NonEmptyListLaws.FlatMapAssociativity(xs, x => NonEmptyList.Of(x, -x), x => NonEmptyList.Of(x.ToString())));
    }

    [Fact]
    public void FoldLaws_ShouldHold()
    {
        Ints.Sample(xs => NonEmptyListLaws.ReduceMatchesAggregate(xs, (a, b) => a + b));
        Ints.Sample(xs => NonEmptyListLaws.FoldMatchesAggregate(xs, "", (acc, x) => acc + x));
        Ints.Sample(xs => NonEmptyListLaws.ScanLastEqualsFold(xs, 0L, (acc, x) => acc + x));
    }

    [Fact]
    public void StructuralLaws_ShouldHold()
    {
        Ints.Sample(xs => NonEmptyListLaws.HeadTailRoundTrip(xs));
        Ints.Sample(xs => NonEmptyListLaws.InitLastRoundTrip(xs));
        Ints.Sample(xs => NonEmptyListLaws.ReverseTwiceIsIdentity(xs));
        Gen.Select(Ints, Ints).Sample(t => NonEmptyListLaws.ConcatPreservesElements(t.Item1, t.Item2));
        Gen.Select(Ints, Strings).Sample(t => NonEmptyListLaws.ZipUnzipRoundTrip(t.Item1, t.Item2));
        Ints.Sample(xs => NonEmptyListLaws.TailsShape(xs));
        Ints.Sample(xs => NonEmptyListLaws.InitsShape(xs));
        Ints.Sample(xs => NonEmptyListLaws.OrderByIsSortedPermutation(xs));
        Ints.Sample(xs => NonEmptyListLaws.DistinctKeepsFirstOccurrences(xs));
        Ints.Sample(xs => NonEmptyListLaws.EqualityIsStructural(xs));
        Strings.Sample(xs => NonEmptyListLaws.JsonRoundTrip(xs));
    }

    [Fact]
    public void CheckAll_ShouldReportNoFailuresForWellFormedLists()
    {
        Ints.Sample(xs => NonEmptyListLaws.CheckAll(xs).Count == 0);
        Strings.Sample(xs => NonEmptyListLaws.CheckAll(xs).Count == 0);
        Assert.Throws<ArgumentNullException>(() => NonEmptyListLaws.CheckAll<int>(null!));
    }

    [Fact]
    public void Laws_ShouldFailForAnElementTypeWithBrokenEquality()
    {
        NonEmptyList<Shaky> lawful = NonEmptyList.Of(new Shaky(1), new Shaky(2));
        NonEmptyList<Shaky> broken = NonEmptyList.Of(new Shaky(1), new Shaky(-1));

        Assert.Empty(NonEmptyListLaws.CheckAll(lawful));
        Assert.True(NonEmptyListLaws.OrderByIsSortedPermutation(lawful));

        Assert.False(NonEmptyListLaws.ConcatPreservesElements(broken, lawful));
        Assert.False(NonEmptyListLaws.ConcatPreservesElements(lawful, broken));
        Assert.False(NonEmptyListLaws.ZipUnzipRoundTrip(broken, lawful));
        Assert.False(NonEmptyListLaws.ZipUnzipRoundTrip(lawful, broken));
        Assert.False(NonEmptyListLaws.TailsShape(broken));
        Assert.False(NonEmptyListLaws.InitsShape(broken));
        Assert.False(NonEmptyListLaws.OrderByIsSortedPermutation(broken));
        Assert.False(NonEmptyListLaws.OrderByIsSortedPermutation(NonEmptyList.Of(new Contrary(1), new Contrary(2))));
        Assert.False(NonEmptyListLaws.EqualityIsStructural(broken));
        Assert.False(NonEmptyListLaws.JsonRoundTrip(broken));

        IReadOnlyList<string> failures = NonEmptyListLaws.CheckAll(broken);
        Assert.Contains(nameof(NonEmptyListLaws.MapIdentity), failures);
        Assert.Contains(nameof(NonEmptyListLaws.HeadTailRoundTrip), failures);
        Assert.Contains(nameof(NonEmptyListLaws.EqualityIsStructural), failures);
        Assert.Contains(nameof(NonEmptyListLaws.JsonRoundTrip), failures);
        // The fold laws compare only what the folder produces, which never involves the broken Equals here.
        Assert.DoesNotContain(nameof(NonEmptyListLaws.FoldMatchesAggregate), failures);
        Assert.DoesNotContain(nameof(NonEmptyListLaws.ScanLastEqualsFold), failures);
    }

    [Fact]
    public void SoundnessProperties_ShouldHoldUnderRandomMutations()
    {
        // Any sequence of guarded mutations through the IList<T> interface leaves at least one element.
        Gen<int> op = Gen.Int[0, 5];
        Gen.Select(Ints, op.List[0, 30]).Sample(t =>
        {
            NonEmptyList<int> list = t.Item1;
            IList<int> view = list;
            foreach (int o in t.Item2)
            {
                try
                {
                    switch (o)
                    {
                        case 0: view.Add(1); break;
                        case 1: view.RemoveAt(0); break;
                        case 2: view.Remove(view[0]); break;
                        case 3: view.Insert(0, 7); break;
                        case 4: view.Clear(); break;
                        case 5: list.RemoveAll(x => x > 0); break;
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (NotSupportedException)
                {
                }
            }

            return list.Count >= 1 && list.Head.Equals(list[0]) && list.Tail is null == (list.Count == 1);
        });
    }

    [Fact]
    public void ConstrainedListProperties_ShouldHold()
    {
        NonEmptyGen.Sorted(Gen.Int[-50, 50]).Sample(xs => xs.Head <= xs.Last && xs.Relax().Count == xs.Count);
        NonEmptyGen.Distinct(Gen.Int[0, 10]).Sample(xs => xs.Count == Enumerable.Distinct(xs).Count());
        NonEmptyGen.Constrained<int, AtLeast<Two>>(Gen.Int, minCount: 2).Sample(xs => xs.Second() == xs[1] && xs.RestAfterSecond().Count == xs.Count - 2);
    }

    [Fact]
    public void ZipperProperties_ShouldHold()
    {
        NonEmptyGen.Zipper(Gen.Int).Sample(z =>
        {
            NonEmptyList<int> list = z.ToNonEmptyList();
            bool rightThenLeft = z.MoveRight() is null || z.MoveRight()!.MoveLeft()!.Equals(z);
            bool toStart = z.ToStart().Index == 0 && z.ToStart().ToNonEmptyList().Equals(list);
            bool deleteShrinks = z.Delete() is null ? list.Count == 1 : z.Delete()!.Count == list.Count - 1;
            return rightThenLeft && toStart && deleteShrinks && z.Focus == list[z.Index];
        });
    }

    [Fact]
    public void TreeProperties_ShouldHold()
    {
        NonEmptyGen.Tree(Gen.Int, maxDepth: 4, maxChildren: 3).Sample(tree =>
        {
            NonEmptyList<int> flat = tree.Flatten();
            return flat.Count == tree.Count
                && tree.Leaves().Count == tree.PathsToLeaves().Count
                && tree.Levels().Count == tree.Depth
                && tree.Fold<int>((_, children) => 1 + children.Sum()) == tree.Count
                && tree.Map(x => x).Equals(tree)
                && tree.FlattenBreadthFirst().Count == tree.Count
                && tree.FlattenPostOrder().Last == tree.Value;
        });
    }

    [Fact]
    public void EventStreamProperties_ShouldHold()
    {
        NonEmptyGen.EventStream(Gen.Int[-1000, 1000]).Sample(stream =>
            stream.Replay(0, (acc, e) => acc + e) == stream.Sum()
            && stream.ReplayHistory(e => e, (acc, e) => acc + e).Count == stream.Version
            && stream.UpTo(stream.Version).Equals(stream)
            && stream.Since(stream.Version).Count == 0
            && stream.Append(1).Version == stream.Version + 1);
    }
}
