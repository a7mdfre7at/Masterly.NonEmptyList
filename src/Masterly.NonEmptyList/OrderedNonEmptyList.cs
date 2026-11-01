// CA2260 is a heuristic for the curiously recurring template pattern. It fires here only because
// NonEmptyList<T> implements ISpanParsable<NonEmptyList<T>>; the derived types below are correct.
#pragma warning disable CA2260

namespace Masterly.NonEmptyList;

/// <summary>
/// A <see cref="NonEmptyList{T}"/> produced by <c>OrderBy</c> that remembers its ordering so <c>ThenBy</c> can refine it.
/// Enables query syntax such as <c>orderby c.LastName, c.FirstName</c> without leaving the non-empty type.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class OrderedNonEmptyList<T> : NonEmptyList<T>, IOrderedEnumerable<T>
{
    private readonly IComparer<T> _comparer;

    private OrderedNonEmptyList(List<T> sortedItems, IComparer<T> comparer) : base(sortedItems)
    {
        _comparer = comparer;
    }

    /// <summary>
    /// Sorts the source with a stable sort and wraps the result.
    /// </summary>
    internal static OrderedNonEmptyList<T> Create(NonEmptyList<T> source, IComparer<T> comparer)
    {
        List<T> sorted = source.Items.OrderBy(x => x, comparer).ToList();
        return new OrderedNonEmptyList<T>(sorted, comparer);
    }

    /// <summary>
    /// Creates a new ordered list that keeps the current ordering and breaks ties with the given key.
    /// </summary>
    internal OrderedNonEmptyList<T> ThenByCore<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        IComparer<T> tieBreaker = new KeyComparer<T, TKey>(keySelector, comparer ?? Comparer<TKey>.Default, descending);
        return Create(this, new CompositeComparer<T>(_comparer, tieBreaker));
    }

    IOrderedEnumerable<T> IOrderedEnumerable<T>.CreateOrderedEnumerable<TKey>(
        Func<T, TKey> keySelector,
        IComparer<TKey>? comparer,
        bool descending)
        => ThenByCore(keySelector, comparer, descending);
}

/// <summary>
/// Compares elements by a projected key, optionally in descending order.
/// </summary>
internal sealed class KeyComparer<T, TKey> : IComparer<T>
{
    private readonly Func<T, TKey> _keySelector;
    private readonly IComparer<TKey> _comparer;
    private readonly int _sign;

    public KeyComparer(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending)
    {
        _keySelector = keySelector;
        _comparer = comparer;
        _sign = descending ? -1 : 1;
    }

    public int Compare(T? x, T? y) => _sign * _comparer.Compare(_keySelector(x!), _keySelector(y!));
}

/// <summary>
/// Applies a primary comparer and falls back to a secondary one on ties.
/// </summary>
internal sealed class CompositeComparer<T> : IComparer<T>
{
    private readonly IComparer<T> _primary;
    private readonly IComparer<T> _secondary;

    public CompositeComparer(IComparer<T> primary, IComparer<T> secondary)
    {
        _primary = primary;
        _secondary = secondary;
    }

    public int Compare(T? x, T? y)
    {
        int result = _primary.Compare(x, y);
        return result != 0 ? result : _secondary.Compare(x, y);
    }
}

/// <summary>
/// A group produced by <c>GroupBy</c> on a <see cref="NonEmptyList{T}"/>. Every group has at least one element,
/// so the group is itself a <see cref="NonEmptyList{T}"/>.
/// </summary>
/// <typeparam name="TKey">The type of the grouping key.</typeparam>
/// <typeparam name="TElement">The type of the grouped elements.</typeparam>
public sealed class NonEmptyGrouping<TKey, TElement> : NonEmptyList<TElement>, IGrouping<TKey, TElement>
{
    internal NonEmptyGrouping(TKey key, List<TElement> items) : base(items)
    {
        Key = key;
    }

    /// <summary>
    /// Gets the key shared by every element in the group.
    /// </summary>
    public TKey Key { get; }

    /// <inheritdoc />
    public override string ToString() => $"NonEmptyGrouping: Key = {Key}, [{string.Join(", ", Items)}]";
}
