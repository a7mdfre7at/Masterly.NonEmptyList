namespace Masterly.NonEmptyList.Tests;

public class NonEmptyTreeTests
{
    private static NonEmptyTree<string> Org() =>
        NonEmptyTree.Create("ceo",
            NonEmptyTree.Create("cto",
                NonEmptyTree.Leaf("dev1"),
                NonEmptyTree.Leaf("dev2")),
            NonEmptyTree.Create("cfo",
                NonEmptyTree.Leaf("acct")));

    [Fact]
    public void Leaf_ShouldHaveNoChildrenAndCountOne()
    {
        NonEmptyTree<int> leaf = new(7);

        Assert.True(leaf.IsLeaf);
        Assert.Equal(1, leaf.Count);
        Assert.Equal(1, leaf.Depth);
        Assert.Equal(new[] { 7 }, leaf.Flatten().ToArray());
        Assert.Equal(new[] { 7 }, leaf.Leaves().ToArray());
        Assert.Single(leaf.PathsToLeaves());
        Assert.Equal("7", leaf.ToString());
        Assert.Throws<ArgumentNullException>(() => new NonEmptyTree<string>(null!));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyTree<string>("a", new NonEmptyTree<string>[] { null! }));
    }

    [Fact]
    public void Traversals_ShouldVisitInExpectedOrder()
    {
        NonEmptyTree<string> org = Org();

        Assert.Equal(6, org.Count);
        Assert.Equal(3, org.Depth);
        Assert.Equal(new[] { "ceo", "cto", "dev1", "dev2", "cfo", "acct" }, org.Flatten().ToArray());
        Assert.Equal(new[] { "dev1", "dev2", "cto", "acct", "cfo", "ceo" }, org.FlattenPostOrder().ToArray());
        Assert.Equal(new[] { "ceo", "cto", "cfo", "dev1", "dev2", "acct" }, org.FlattenBreadthFirst().ToArray());
        Assert.Equal(new[] { "dev1", "dev2", "acct" }, org.Leaves().ToArray());
    }

    [Fact]
    public void PathsToLeaves_Levels_ShouldBeNonEmpty()
    {
        NonEmptyTree<string> org = Org();

        NonEmptyList<NonEmptyList<string>> paths = org.PathsToLeaves();
        NonEmptyList<NonEmptyList<string>> levels = org.Levels();

        Assert.Equal(3, paths.Count);
        Assert.Equal(new[] { "ceo", "cto", "dev1" }, paths[0].ToArray());
        Assert.Equal(new[] { "ceo", "cfo", "acct" }, paths[2].ToArray());
        Assert.Equal(3, levels.Count);
        Assert.Equal(new[] { "ceo" }, levels[0].ToArray());
        Assert.Equal(new[] { "cto", "cfo" }, levels[1].ToArray());
        Assert.Equal(new[] { "dev1", "dev2", "acct" }, levels[2].ToArray());
    }

    [Fact]
    public void Fold_ShouldComputeDepthWithSafeMax()
    {
        NonEmptyTree<string> org = Org();

        int depth = org.Fold<int>((_, childDepths) => 1 + (childDepths.Count == 0 ? 0 : childDepths.Max()));
        int size = org.Fold<int>((_, sizes) => 1 + sizes.Sum());

        Assert.Equal(3, depth);
        Assert.Equal(6, size);
    }

    [Fact]
    public void Map_MapWithDepth_ShouldPreserveShape()
    {
        NonEmptyTree<string> org = Org();

        NonEmptyTree<int> lengths = org.Map(s => s.Length);
        NonEmptyTree<string> indented = org.MapWithDepth((s, d) => $"{d}:{s}");

        Assert.Equal(3, lengths.Value);
        Assert.Equal(2, lengths.Children.Count);
        Assert.Equal(4, lengths.Children[0].Children[0].Value);
        Assert.Equal("0:ceo", indented.Value);
        Assert.Equal("2:dev1", indented.Children[0].Children[0].Value);
    }

    [Fact]
    public void Find_PathTo_Any_All_ShouldSearch()
    {
        NonEmptyTree<string> org = Org();

        Assert.Equal("cto", org.Find(s => s.StartsWith("ct"))!.Value);
        Assert.Null(org.Find(s => s == "nobody"));
        Assert.Equal(new[] { "ceo", "cto", "dev2" }, org.PathTo(s => s == "dev2")!.ToArray());
        Assert.Null(org.PathTo(s => s == "nobody"));
        Assert.True(org.Any(s => s == "acct"));
        Assert.False(org.All(s => s.Length == 3));
        Assert.True(org.All(s => s.Length >= 3));
    }

    [Fact]
    public void Edits_ShouldReturnNewTrees()
    {
        NonEmptyTree<string> org = Org();

        NonEmptyTree<string> pruned = org.PruneChildren(s => s != "cfo");
        NonEmptyTree<string> grown = org.AddChild("coo").AddChild(NonEmptyTree.Leaf("cmo"));
        NonEmptyTree<string> renamed = org.WithValue("chair");

        Assert.Equal(4, pruned.Count);
        Assert.Equal(8, grown.Count);
        Assert.Equal("cmo", grown.Children[^1].Value);
        Assert.Equal("chair", renamed.Value);
        Assert.Equal(6, renamed.Count);
        Assert.Equal(6, org.Count);
    }

    [Fact]
    public void Equality_Deconstruct_Outline_ShouldWork()
    {
        NonEmptyTree<string> a = Org();
        NonEmptyTree<string> b = Org();

        var (value, children) = a;

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, a.AddChild("x"));
        Assert.NotEqual(a, a.WithValue("y"));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.Equal("ceo", value);
        Assert.Equal(2, children.Count);
        Assert.Equal("ceo [cto [dev1, dev2], cfo [acct]]", a.ToString());
        Assert.Equal("ceo\n  cto\n    dev1\n    dev2\n  cfo\n    acct", a.ToOutline().Replace("\r\n", "\n"));
    }

    [Fact]
    public void Factories_Unfold_FromParentKeys_ShouldBuildTrees()
    {
        NonEmptyTree<int> halves = NonEmptyTree.Unfold(8, n => n > 1 ? new[] { n / 2 } : Array.Empty<int>());
        NonEmptyTree<string> flat = NonEmptyTree.Create("root", new[] { "a", "b" });

        Assert.Equal(new[] { 8, 4, 2, 1 }, halves.Flatten().ToArray());
        Assert.Equal(2, flat.Children.Count);
        Assert.True(flat.Children.All(c => c.IsLeaf));

        var rows = new[]
        {
            (Id: 1, Parent: (int?)null, Name: "root"),
            (Id: 2, Parent: (int?)1, Name: "child"),
            (Id: 3, Parent: (int?)2, Name: "grandchild"),
            (Id: 4, Parent: (int?)null, Name: "root2"),
        };

        NonEmptyList<NonEmptyTree<(int Id, int? Parent, string Name)>>? forest = NonEmptyTree.FromParentKeys(rows, r => r.Id, r => r.Parent);

        Assert.NotNull(forest);
        Assert.Equal(2, forest!.Count);
        Assert.Equal(3, forest[0].Count);
        Assert.Equal("grandchild", forest[0].Leaves().Head.Name);
        Assert.Null(NonEmptyTree.FromParentKeys(new[] { (Id: 1, Parent: (int?)1) }, r => r.Id, r => r.Parent));
    }
}
