namespace Masterly.NonEmptyList.Tests;

public class NonEmptyZipperTests
{
    [Fact]
    public void FromStart_ShouldFocusHead()
    {
        NonEmptyList<string> steps = ["one", "two", "three"];

        NonEmptyZipper<string> zipper = steps.ToZipper();

        Assert.Equal("one", zipper.Focus);
        Assert.Equal(0, zipper.Index);
        Assert.Equal(3, zipper.Count);
        Assert.True(zipper.IsAtStart);
        Assert.False(zipper.IsAtEnd);
        Assert.Empty(zipper.Before);
        Assert.Equal(new[] { "two", "three" }, zipper.After);
    }

    [Fact]
    public void MoveRightAndLeft_ShouldReturnNullAtEnds()
    {
        NonEmptyList<int> list = [1, 2, 3];
        NonEmptyZipper<int> z = list.ToZipper();

        Assert.Null(z.MoveLeft());
        NonEmptyZipper<int> second = z.MoveRight()!;
        NonEmptyZipper<int> third = second.MoveRight()!;

        Assert.Equal(2, second.Focus);
        Assert.Equal(3, third.Focus);
        Assert.Null(third.MoveRight());
        Assert.True(third.IsAtEnd);
        Assert.Equal(2, third.MoveLeft()!.Focus);
        Assert.Equal(new[] { 1, 2 }, third.Before);
        Assert.Equal(1, z.Focus);
    }

    [Fact]
    public void OrStayAndCyclic_ShouldHandleEnds()
    {
        NonEmptyZipper<int> z = NonEmptyZipper<int>.FromEnd([1, 2, 3]);

        Assert.Equal(3, z.Focus);
        Assert.Same(z, z.MoveRightOrStay());
        Assert.Equal(1, z.MoveRightCyclic().Focus);
        Assert.Equal(3, z.ToStart().MoveLeftCyclic().Focus);
        Assert.Equal(1, z.ToStart().MoveLeftOrStay().Focus);
        Assert.Same(z, z.ToEnd());
    }

    [Fact]
    public void FromIndex_MoveTo_Find_ShouldPositionCursor()
    {
        NonEmptyList<char> list = ['a', 'b', 'c', 'd'];

        NonEmptyZipper<char> z = list.ToZipper(2);
        Assert.Equal('c', z.Focus);
        Assert.Equal(new[] { 'a', 'b' }, z.Before);
        Assert.Equal(new[] { 'd' }, z.After);
        Assert.Equal('b', z.MoveTo(1).Focus);
        Assert.Same(z, z.MoveTo(2));
        Assert.Equal('d', z.Find(c => c == 'd')!.Focus);
        Assert.Null(z.Find(c => c == 'z'));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.ToZipper(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.ToZipper(-1));
    }

    [Fact]
    public void Edits_ShouldReturnNewZippersWithoutMutatingSource()
    {
        NonEmptyZipper<int> z = NonEmptyList.Of(1, 2, 3).ToZipper().MoveRight()!;

        Assert.Equal(new[] { 1, 20, 3 }, z.Replace(20).ToNonEmptyList().ToArray());
        Assert.Equal(new[] { 1, 4, 3 }, z.Update(x => x * 2).ToNonEmptyList().ToArray());
        Assert.Equal(new[] { 1, 2, 9, 3 }, z.InsertAfter(9).ToNonEmptyList().ToArray());
        Assert.Equal(new[] { 1, 9, 2, 3 }, z.InsertBefore(9).ToNonEmptyList().ToArray());
        Assert.Equal(2, z.InsertBefore(9).Focus);
        Assert.Equal(2, z.InsertBefore(9).Index);
        Assert.Equal(new[] { 1, 2, 3 }, z.ToNonEmptyList().ToArray());
    }

    [Fact]
    public void Delete_ShouldMoveToNextOrPreviousAndReturnNullForSingleton()
    {
        NonEmptyZipper<int> middle = NonEmptyList.Of(1, 2, 3).ToZipper(1);
        NonEmptyZipper<int> last = NonEmptyList.Of(1, 2, 3).ToZipper(2);

        NonEmptyZipper<int> afterMiddle = middle.Delete()!;
        NonEmptyZipper<int> afterLast = last.Delete()!;

        Assert.Equal(3, afterMiddle.Focus);
        Assert.Equal(new[] { 1, 3 }, afterMiddle.ToNonEmptyList().ToArray());
        Assert.Equal(2, afterLast.Focus);
        Assert.True(afterLast.IsAtEnd);
        Assert.Null(NonEmptyZipper<int>.Singleton(5).Delete());
    }

    [Fact]
    public void Map_Deconstruct_Equality_ToString_ShouldWork()
    {
        NonEmptyZipper<int> z = NonEmptyList.Of(1, 2, 3).ToZipper(1);

        NonEmptyZipper<string> mapped = z.Map(x => x.ToString());
        var (before, focus, after) = z;

        Assert.Equal("2", mapped.Focus);
        Assert.Equal(1, mapped.Index);
        Assert.Equal(new[] { 1 }, before);
        Assert.Equal(2, focus);
        Assert.Equal(new[] { 3 }, after);
        Assert.Equal(z, NonEmptyList.Of(1, 2, 3).ToZipper().MoveRight());
        Assert.NotEqual(z, NonEmptyList.Of(1, 2, 3).ToZipper());
        Assert.Equal(z.GetHashCode(), NonEmptyList.Of(1, 2, 3).ToZipper(1).GetHashCode());
        Assert.Equal("NonEmptyZipper: [1] >2< [3]", z.ToString());
        Assert.False(z.Equals(null));
        Assert.True(z.Equals((object)NonEmptyList.Of(1, 2, 3).ToZipper(1)));
    }

    [Fact]
    public void ImmutableList_ToZipper_ShouldWork()
    {
        ImmutableNonEmptyList<int> list = [1, 2];

        Assert.Equal(1, list.ToZipper().Focus);
        Assert.Throws<ArgumentNullException>(() => NonEmptyZipper<string>.Singleton(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyZipper<int>.FromStart(null!));
    }

    [Fact]
    public void History_ShouldUndoRedoAndDiscardRedoBranchOnPush()
    {
        History<string> h = History.Start("v1");

        Assert.Same(h, h.Undo());
        Assert.Same(h, h.Redo());
        Assert.False(h.CanUndo);
        Assert.False(h.CanRedo);

        History<string> h3 = h.Push("v2").Push("v3");
        Assert.Equal("v3", h3.Current);
        Assert.Equal(2, h3.UndoCount);
        Assert.Equal(0, h3.RedoCount);

        History<string> back = h3.Undo();
        Assert.Equal("v2", back.Current);
        Assert.True(back.CanRedo);
        Assert.Equal("v3", back.Redo().Current);

        History<string> branched = back.Push("v2b");
        Assert.Equal("v2b", branched.Current);
        Assert.False(branched.CanRedo);
        Assert.Equal(new[] { "v1", "v2", "v2b" }, branched.States.ToArray());
        Assert.Equal(new[] { "v1", "v2", "v3" }, h3.States.ToArray());
    }

    [Fact]
    public void History_PushWithFunction_Equality_ToString_ShouldWork()
    {
        History<int> h = History<int>.Start(1).Push(x => x + 1).Push(x => x * 10);

        Assert.Equal(20, h.Current);
        Assert.Equal(History.Start(1).Push(2).Push(20), h);
        Assert.Equal(History.Start(1).Push(2).Push(20).GetHashCode(), h.GetHashCode());
        Assert.NotEqual(h, h.Undo());
        Assert.Equal("History: 2 back, current = 20, 0 forward", h.ToString());
        Assert.True(h.Equals((object)History.Start(1).Push(2).Push(20)));
        Assert.False(h.Equals(null));
        Assert.Throws<ArgumentNullException>(() => History.Start("a").Push((string)null!));
    }
}
