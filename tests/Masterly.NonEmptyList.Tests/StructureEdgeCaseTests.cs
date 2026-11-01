using System.ComponentModel;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Edge cases of <see cref="NonEmptyString"/>, <see cref="NonEmptyTree{T}"/>, <see cref="NonEmptyZipper{T}"/>,
/// <see cref="History{TState}"/> and <see cref="NonEmptyGrouping{TKey, TElement}"/>.
/// </summary>
public class StructureEdgeCaseTests
{
    #region NonEmptyString

    [Fact]
    public void Split_WhenEveryPartIsEmpty_ShouldFallBackToTheWholeString()
    {
        NonEmptyString text = new(",,");

        NonEmptyList<NonEmptyString> parts = text.Split(',');

        Assert.Equal(new[] { new NonEmptyString(",,") }, parts);

        // The fallback is the trimmed string, not the original padded one.
        NonEmptyString padded = new(" ,, ");
        Assert.Equal(new[] { new NonEmptyString(",,") }, padded.Split(','));
    }

    [Fact]
    public void Equals_Object_ShouldRejectUnrelatedTypes()
    {
        NonEmptyString text = new("42");

        Assert.False(text.Equals((object)42));
        Assert.False(text.Equals((object?)null));
        Assert.True(text.Equals((object)new NonEmptyString("42")));
        Assert.True(text.Equals((object)"42"));
        Assert.False(text.Equals((object)"43"));
    }

    [Fact]
    public void TypeConverter_ShouldOnlyHandleStrings()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(NonEmptyString));

        Assert.IsType<NonEmptyStringTypeConverter>(converter);
        Assert.False(converter.CanConvertFrom(typeof(int)));
        Assert.False(converter.CanConvertTo(typeof(int)));
    }

    #endregion

    #region NonEmptyTree

    [Fact]
    public void TreeEquals_ShouldShortCircuitOnSameInstanceAndDetectChildDifferences()
    {
        NonEmptyTree<int> tree = NonEmptyTree.Create(1, NonEmptyTree.Leaf(2), NonEmptyTree.Leaf(3));
        NonEmptyTree<int> differentChild = NonEmptyTree.Create(1, NonEmptyTree.Leaf(2), NonEmptyTree.Leaf(4));

        Assert.True(tree.Equals(tree));
        Assert.False(tree.Equals(differentChild));
        Assert.False(tree.Equals((object)"not a tree"));

        // NeverEqual is not equal even to itself, so only the reference short-circuit can return true.
        NonEmptyTree<NeverEqual> opaque = NonEmptyTree.Leaf(new NeverEqual());
        Assert.True(opaque.Equals(opaque));
        Assert.False(opaque.Equals(NonEmptyTree.Leaf(new NeverEqual())));
    }

    private sealed class NeverEqual
    {
        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => 0;
    }

    [Fact]
    public void Create_WithEnumerableOfChildren_ShouldAttachThem()
    {
        IEnumerable<NonEmptyTree<string>> children = new List<NonEmptyTree<string>> { NonEmptyTree.Leaf("a"), NonEmptyTree.Leaf("b") };

        NonEmptyTree<string> tree = NonEmptyTree.Create("root", children);

        Assert.Equal(new[] { "a", "b" }, tree.Children.Select(c => c.Value));
    }

    private sealed record Folder(string Id, string? ParentId);

    [Fact]
    public void FromParentKeys_WithReferenceTypeKeys_ShouldBuildTheForest()
    {
        Folder[] rows = { new("root", null), new("docs", "root"), new("img", "root"), new("other", null) };

        NonEmptyList<NonEmptyTree<Folder>>? forest = NonEmptyTree.FromParentKeys(rows, f => f.Id, f => f.ParentId);

        Assert.NotNull(forest);
        Assert.Equal(new[] { "root", "other" }, forest!.Select(t => t.Value.Id));
        Assert.Equal(new[] { "docs", "img" }, forest[0].Children.Select(c => c.Value.Id));
        Assert.Throws<ArgumentNullException>(() => NonEmptyTree.FromParentKeys<Folder, string>(rows, f => f.Id, null!));
    }

    #endregion

    #region NonEmptyZipper and History

    [Fact]
    public void Zipper_OnSingleElementList_ShouldBeAtBothEnds()
    {
        NonEmptyList<int> single = new(7);

        NonEmptyZipper<int> fromStart = NonEmptyZipper<int>.FromStart(single);
        NonEmptyZipper<int> fromEnd = NonEmptyZipper<int>.FromEnd(single);

        Assert.True(fromStart.IsAtStart && fromStart.IsAtEnd);
        Assert.Equal(fromStart, fromEnd);
        Assert.Empty(fromEnd.After);
    }

    [Fact]
    public void Zipper_MoveLeftFromTheEnd_ShouldStartTheRightSide()
    {
        NonEmptyZipper<int> zipper = NonEmptyZipper<int>.FromEnd(new NonEmptyList<int>(1, 2, 3));

        NonEmptyZipper<int>? moved = zipper.MoveLeft();

        Assert.Equal(2, moved!.Focus);
        Assert.Equal(new[] { 3 }, moved.After);

        NonEmptyZipper<int>? again = moved.MoveLeft();
        Assert.Equal(1, again!.Focus);
        Assert.Equal(new[] { 2, 3 }, again.After);
    }

    [Fact]
    public void Zipper_InsertAfterAtEndAndInsertBeforeInMiddle_ShouldKeepFocus()
    {
        NonEmptyZipper<int> atEnd = NonEmptyZipper<int>.FromEnd(new NonEmptyList<int>(1, 2));
        NonEmptyZipper<int> middle = NonEmptyZipper<int>.FromIndex(new NonEmptyList<int>(1, 2, 3), 1);

        NonEmptyZipper<int> appended = atEnd.InsertAfter(9);
        NonEmptyZipper<int> prepended = middle.InsertBefore(9);

        Assert.Equal(2, appended.Focus);
        Assert.Equal(new[] { 1, 2, 9 }, appended.ToNonEmptyList());
        Assert.Equal(2, prepended.Focus);
        Assert.Equal(new[] { 1, 9, 2, 3 }, prepended.ToNonEmptyList());
    }

    [Fact]
    public void Zipper_AtTheStart_ShouldNotMoveLeftAndShouldGrowLeftOnInsertBefore()
    {
        NonEmptyZipper<int> start = NonEmptyZipper<int>.FromIndex(new NonEmptyList<int>(1, 2), 0);

        Assert.Null(start.MoveLeft());
        Assert.Same(start, start.ToStart());

        NonEmptyZipper<int> inserted = start.InsertBefore(0);
        Assert.Equal(1, inserted.Focus);
        Assert.Equal(1, inserted.Index);
        Assert.Equal(new[] { 0, 1, 2 }, inserted.ToNonEmptyList());
    }

    [Fact]
    public void Zipper_MapOnSingleton_ShouldMapOnlyTheFocus()
    {
        NonEmptyZipper<string> mapped = NonEmptyZipper<int>.Singleton(4).Map(x => $"#{x}");

        Assert.Equal("#4", mapped.Focus);
        Assert.Equal(1, mapped.Count);
    }

    [Fact]
    public void ZipperAndHistory_EqualsObject_ShouldRejectOtherTypes()
    {
        NonEmptyZipper<int> zipper = NonEmptyZipper<int>.Singleton(1);
        History<int> history = History<int>.Start(1);

        Assert.True(zipper.Equals((object)NonEmptyZipper<int>.Singleton(1)));
        Assert.False(zipper.Equals((object)1));
        Assert.True(history.Equals((object)History<int>.Start(1)));
        Assert.False(history.Equals((object)zipper));
    }

    #endregion

    [Fact]
    public void Grouping_ToString_ShouldShowKeyAndItems()
    {
        NonEmptyGrouping<bool, int> group = new NonEmptyList<int>(1, 2, 3).GroupBy(x => x % 2 == 1)[0];

        Assert.Equal("NonEmptyGrouping: Key = True, [1, 3]", group.ToString());
    }
}
