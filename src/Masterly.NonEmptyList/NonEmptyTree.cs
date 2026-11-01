using System.Diagnostics;

namespace Masterly.NonEmptyList;

/// <summary>
/// An immutable rose tree with a guaranteed root. Menus, org charts, file systems, bills of materials and
/// comment threads all have one, so <see cref="Flatten"/>, <see cref="Leaves"/> and <see cref="PathsToLeaves"/>
/// can never be empty and <see cref="Fold{TResult}"/> can call <c>Max</c> on child results safely.
/// </summary>
/// <typeparam name="T">The node value type.</typeparam>
[DebuggerDisplay("Value = {Value}, Children = {Children.Count}, Count = {Count}")]
public sealed class NonEmptyTree<T> : IEquatable<NonEmptyTree<T>>
{
    private int _count;

    /// <summary>Creates a leaf.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the value is null.</exception>
    public NonEmptyTree(T value) : this(value, Array.Empty<NonEmptyTree<T>>())
    {
    }

    /// <summary>Creates a node with children.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the value or any child is null.</exception>
    public NonEmptyTree(T value, IEnumerable<NonEmptyTree<T>> children)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(children);

        NonEmptyTree<T>[] array = children.ToArray();
        if (Array.IndexOf(array, null) >= 0)
            throw new ArgumentNullException(nameof(children), "Children cannot contain null");

        Value = value;
        Children = array;
    }

    /// <summary>Creates a node with children.</summary>
    public NonEmptyTree(T value, params NonEmptyTree<T>[] children) : this(value, (IEnumerable<NonEmptyTree<T>>)children)
    {
    }

    /// <summary>The value stored at this node. Never null.</summary>
    public T Value { get; }

    /// <summary>The child subtrees, in order. May be empty.</summary>
    public IReadOnlyList<NonEmptyTree<T>> Children { get; }

    /// <summary><see langword="true"/> if this node has no children.</summary>
    public bool IsLeaf => Children.Count == 0;

    /// <summary>The total number of nodes in the tree, including this one. Always at least 1.</summary>
    public int Count
    {
        get
        {
            if (_count == 0)
            {
                int total = 1;
                foreach (NonEmptyTree<T> child in Children)
                    total += child.Count;
                _count = total;
            }

            return _count;
        }
    }

    /// <summary>The number of levels. A leaf has depth 1.</summary>
    public int Depth => 1 + (Children.Count == 0 ? 0 : Children.Max(static c => c.Depth));

    /// <summary>All values in pre-order: the root first, then each subtree. Never empty.</summary>
    public NonEmptyList<T> Flatten()
    {
        List<T> items = new(Count);
        CollectPreOrder(this, items);
        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>All values in post-order: each subtree first, then the root. Never empty.</summary>
    public NonEmptyList<T> FlattenPostOrder()
    {
        List<T> items = new(Count);
        CollectPostOrder(this, items);
        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>All values in breadth-first order. Never empty.</summary>
    public NonEmptyList<T> FlattenBreadthFirst()
    {
        List<T> items = new(Count);
        Queue<NonEmptyTree<T>> queue = new();
        queue.Enqueue(this);
        while (queue.Count > 0)
        {
            NonEmptyTree<T> node = queue.Dequeue();
            items.Add(node.Value);
            foreach (NonEmptyTree<T> child in node.Children)
                queue.Enqueue(child);
        }

        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>The values of all leaves, left to right. Never empty: a tree without children is its own leaf.</summary>
    public NonEmptyList<T> Leaves()
    {
        List<T> items = new();
        CollectLeaves(this, items);
        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>Every path from the root to a leaf. Each path is non-empty because it contains the root.</summary>
    public NonEmptyList<NonEmptyList<T>> PathsToLeaves()
    {
        List<NonEmptyList<T>> paths = new();
        CollectPaths(this, new List<T>(), paths);
        return NonEmptyList<NonEmptyList<T>>.FromTrustedList(paths);
    }

    /// <summary>The nodes at each depth, root level first. Each level is non-empty.</summary>
    public NonEmptyList<NonEmptyList<T>> Levels()
    {
        List<NonEmptyList<T>> levels = new();
        List<NonEmptyTree<T>> current = new() { this };
        while (current.Count > 0)
        {
            levels.Add(NonEmptyList<T>.FromTrustedList(current.Select(static n => n.Value).ToList()));
            current = current.SelectMany(static n => n.Children).ToList();
        }

        return NonEmptyList<NonEmptyList<T>>.FromTrustedList(levels);
    }

    /// <summary>Applies a function to every value, preserving the shape.</summary>
    public NonEmptyTree<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new NonEmptyTree<TResult>(selector(Value), Children.Select(c => c.Map(selector)));
    }

    /// <summary>Applies a function that also receives the node's depth, with the root at depth 0.</summary>
    public NonEmptyTree<TResult> MapWithDepth<TResult>(Func<T, int, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return MapWithDepth(selector, 0);
    }

    /// <summary>
    /// Bottom-up fold. <paramref name="folder"/> receives each node's value and the already-folded results of its
    /// children, which may be empty for leaves.
    /// </summary>
    public TResult Fold<TResult>(Func<T, IReadOnlyList<TResult>, TResult> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        TResult[] childResults = new TResult[Children.Count];
        for (int i = 0; i < childResults.Length; i++)
            childResults[i] = Children[i].Fold(folder);

        return folder(Value, childResults);
    }

    /// <summary>Returns the first node in pre-order whose value matches, or <see langword="null"/>.</summary>
    public NonEmptyTree<T>? Find(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (predicate(Value))
            return this;

        foreach (NonEmptyTree<T> child in Children)
        {
            if (child.Find(predicate) is { } found)
                return found;
        }

        return null;
    }

    /// <summary>Returns the path from the root to the first node whose value matches, or <see langword="null"/>.</summary>
    public NonEmptyList<T>? PathTo(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        List<T> path = new();
        return TryBuildPath(this, predicate, path) ? NonEmptyList<T>.FromTrustedList(path) : null;
    }

    /// <summary><see langword="true"/> if any node satisfies the predicate.</summary>
    public bool Any(Func<T, bool> predicate) => Find(predicate) is not null;

    /// <summary><see langword="true"/> if every node satisfies the predicate.</summary>
    public bool All(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return predicate(Value) && Children.All(c => c.All(predicate));
    }

    /// <summary>Removes every subtree whose root fails the predicate. The root itself is always kept.</summary>
    public NonEmptyTree<T> PruneChildren(Func<T, bool> keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        return new NonEmptyTree<T>(Value, Children.Where(c => keep(c.Value)).Select(c => c.PruneChildren(keep)));
    }

    /// <summary>Returns a tree with an additional child appended.</summary>
    public NonEmptyTree<T> AddChild(NonEmptyTree<T> child)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new NonEmptyTree<T>(Value, Children.Append(child));
    }

    /// <summary>Returns a tree with an additional leaf child appended.</summary>
    public NonEmptyTree<T> AddChild(T value) => AddChild(new NonEmptyTree<T>(value));

    /// <summary>Returns a tree with the root value replaced.</summary>
    public NonEmptyTree<T> WithValue(T value) => new(value, Children);

    /// <summary>Deconstructs into the value and its children.</summary>
    public void Deconstruct(out T value, out IReadOnlyList<NonEmptyTree<T>> children)
    {
        value = Value;
        children = Children;
    }

    /// <inheritdoc />
    public bool Equals(NonEmptyTree<T>? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (!EqualityComparer<T>.Default.Equals(Value, other.Value) || Children.Count != other.Children.Count)
            return false;

        for (int i = 0; i < Children.Count; i++)
        {
            if (!Children[i].Equals(other.Children[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NonEmptyTree<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Value);
        hash.Add(Children.Count);
        foreach (NonEmptyTree<T> child in Children)
            hash.Add(child.GetHashCode());
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
        => IsLeaf ? $"{Value}" : $"{Value} [{string.Join(", ", Children)}]";

    /// <summary>Renders the tree as an indented outline, one node per line.</summary>
    public string ToOutline(string indent = "  ")
    {
        System.Text.StringBuilder sb = new();
        WriteOutline(this, sb, indent, 0);
        return sb.ToString().TrimEnd('\r', '\n');
    }

    private NonEmptyTree<TResult> MapWithDepth<TResult>(Func<T, int, TResult> selector, int depth)
        => new(selector(Value, depth), Children.Select(c => c.MapWithDepth(selector, depth + 1)));

    private static void CollectPreOrder(NonEmptyTree<T> node, List<T> items)
    {
        items.Add(node.Value);
        foreach (NonEmptyTree<T> child in node.Children)
            CollectPreOrder(child, items);
    }

    private static void CollectPostOrder(NonEmptyTree<T> node, List<T> items)
    {
        foreach (NonEmptyTree<T> child in node.Children)
            CollectPostOrder(child, items);
        items.Add(node.Value);
    }

    private static void CollectLeaves(NonEmptyTree<T> node, List<T> items)
    {
        if (node.IsLeaf)
        {
            items.Add(node.Value);
            return;
        }

        foreach (NonEmptyTree<T> child in node.Children)
            CollectLeaves(child, items);
    }

    private static void CollectPaths(NonEmptyTree<T> node, List<T> prefix, List<NonEmptyList<T>> paths)
    {
        prefix.Add(node.Value);
        if (node.IsLeaf)
        {
            paths.Add(NonEmptyList<T>.FromTrustedList(new List<T>(prefix)));
        }
        else
        {
            foreach (NonEmptyTree<T> child in node.Children)
                CollectPaths(child, prefix, paths);
        }

        prefix.RemoveAt(prefix.Count - 1);
    }

    private static bool TryBuildPath(NonEmptyTree<T> node, Func<T, bool> predicate, List<T> path)
    {
        path.Add(node.Value);
        if (predicate(node.Value))
            return true;

        foreach (NonEmptyTree<T> child in node.Children)
        {
            if (TryBuildPath(child, predicate, path))
                return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    private static void WriteOutline(NonEmptyTree<T> node, System.Text.StringBuilder sb, string indent, int depth)
    {
        for (int i = 0; i < depth; i++)
            sb.Append(indent);
        sb.AppendLine(node.Value?.ToString());
        foreach (NonEmptyTree<T> child in node.Children)
            WriteOutline(child, sb, indent, depth + 1);
    }
}

/// <summary>Factories for <see cref="NonEmptyTree{T}"/>.</summary>
public static class NonEmptyTree
{
    /// <summary>Creates a leaf.</summary>
    public static NonEmptyTree<T> Leaf<T>(T value) => new(value);

    /// <summary>Creates a node with children.</summary>
    public static NonEmptyTree<T> Create<T>(T value, params NonEmptyTree<T>[] children) => new(value, children);

    /// <summary>Creates a node with children.</summary>
    public static NonEmptyTree<T> Create<T>(T value, IEnumerable<NonEmptyTree<T>> children) => new(value, children);

    /// <summary>Creates a node whose children are all leaves.</summary>
    public static NonEmptyTree<T> Create<T>(T value, IEnumerable<T> leafChildren)
    {
        ArgumentNullException.ThrowIfNull(leafChildren);
        return new NonEmptyTree<T>(value, leafChildren.Select(static v => new NonEmptyTree<T>(v)));
    }

    /// <summary>
    /// Builds a tree from a root and a function that returns each node's children. The function is called once per node.
    /// </summary>
    public static NonEmptyTree<T> Unfold<T>(T root, Func<T, IEnumerable<T>> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        return new NonEmptyTree<T>(root, children(root).Select(c => Unfold(c, children)));
    }

    /// <summary>
    /// Builds trees from flat parent-keyed rows, such as a self-referencing table. Rows whose parent key is null
    /// become roots. Returns <see langword="null"/> if there are no roots.
    /// </summary>
    public static NonEmptyList<NonEmptyTree<T>>? FromParentKeys<T, TKey>(
        IEnumerable<T> rows,
        Func<T, TKey> keySelector,
        Func<T, TKey?> parentKeySelector)
        where TKey : class
    {
        ArgumentNullException.ThrowIfNull(parentKeySelector);
        return FromParentKeysCore(rows, keySelector, r => parentKeySelector(r) is { } p ? (true, p) : (false, default!));
    }

    /// <summary>
    /// Builds trees from flat parent-keyed rows with value-type keys, such as a self-referencing table with an
    /// <c>int? ParentId</c> column. Rows whose parent key is null become roots. Returns <see langword="null"/> if there are no roots.
    /// </summary>
    public static NonEmptyList<NonEmptyTree<T>>? FromParentKeys<T, TKey>(
        IEnumerable<T> rows,
        Func<T, TKey> keySelector,
        Func<T, TKey?> parentKeySelector)
        where TKey : struct
    {
        ArgumentNullException.ThrowIfNull(parentKeySelector);
        return FromParentKeysCore(rows, keySelector, r => parentKeySelector(r) is { } p ? (true, p) : (false, default));
    }

    private static NonEmptyList<NonEmptyTree<T>>? FromParentKeysCore<T, TKey>(
        IEnumerable<T> rows,
        Func<T, TKey> keySelector,
        Func<T, (bool HasParent, TKey Parent)> parentSelector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(keySelector);

        List<T> all = rows.ToList();
        ILookup<TKey, T> byParent = all.Where(r => parentSelector(r).HasParent).ToLookup(r => parentSelector(r).Parent);

        NonEmptyTree<T> Build(T row) => new(row, byParent[keySelector(row)].Select(Build));

        List<NonEmptyTree<T>> roots = all.Where(r => !parentSelector(r).HasParent).Select(Build).ToList();
        return roots.Count == 0 ? null : NonEmptyList<NonEmptyTree<T>>.FromTrustedList(roots);
    }
}
