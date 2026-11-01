// Ignore Spelling: Unzip

using System.Diagnostics.CodeAnalysis;

namespace Masterly.NonEmptyList;

/// <summary>
/// LINQ operators that provably preserve the non-empty guarantee. Because these overloads take a
/// <see cref="NonEmptyList{T}"/> receiver they win overload resolution over the <see cref="Enumerable"/> versions,
/// so ordinary LINQ, including query syntax, stays inside the non-empty type:
/// <code>
/// NonEmptyList&lt;string&gt; names =
///     from c in customers
///     orderby c.LastName, c.FirstName
///     select c.FullName;
/// </code>
/// Operators that can legitimately produce an empty result (<c>Where</c>, <c>Skip</c>, <c>Take</c>, <c>OfType</c>, ...)
/// are deliberately not overridden and fall back to <see cref="Enumerable"/>, returning <see cref="IEnumerable{T}"/>.
/// </summary>
public static class NonEmptyListLinqExtensions
{
    #region Projection

    /// <summary>Projects each element into a new form. The result has the same number of elements, so it is non-empty.</summary>
    public static NonEmptyList<TResult> Select<T, TResult>(this NonEmptyList<T> source, Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Map(selector);
    }

    /// <summary>Projects each element and its index into a new form. The result is non-empty.</summary>
    public static NonEmptyList<TResult> Select<T, TResult>(this NonEmptyList<T> source, Func<T, int, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.MapWithIndex(selector);
    }

    /// <summary>Projects each element to a non-empty list and flattens. The result is non-empty.</summary>
    public static NonEmptyList<TResult> SelectMany<T, TResult>(this NonEmptyList<T> source, Func<T, NonEmptyList<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.FlatMap(selector);
    }

    /// <summary>Projects each element to a non-empty list and flattens. The result is non-empty.</summary>
    public static NonEmptyList<TResult> SelectMany<T, TResult>(this NonEmptyList<T> source, Func<T, ImmutableNonEmptyList<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult> results = new();
        foreach (T item in source.Items)
            results.AddRange(selector(item));

        return NonEmptyList<TResult>.FromTrustedList(results);
    }

    /// <summary>
    /// Projects each element to a non-empty list, then combines each source element with each of its results.
    /// Enables <c>from x in xs from y in f(x) select g(x, y)</c> when <c>f</c> returns a non-empty list.
    /// </summary>
    public static NonEmptyList<TResult> SelectMany<T, TCollection, TResult>(
        this NonEmptyList<T> source,
        Func<T, NonEmptyList<TCollection>> collectionSelector,
        Func<T, TCollection, TResult> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(collectionSelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        List<TResult> results = new();
        foreach (T item in source.Items)
        {
            NonEmptyList<TCollection> inner = collectionSelector(item);
            ArgumentNullException.ThrowIfNull(inner, nameof(collectionSelector));

            foreach (TCollection element in inner.Items)
                results.Add(resultSelector(item, element));
        }

        return NonEmptyList<TResult>.FromTrustedList(results);
    }

    /// <summary>Flattens a non-empty list of non-empty lists.</summary>
    public static NonEmptyList<T> Flatten<T>(this NonEmptyList<NonEmptyList<T>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<T> results = new();
        foreach (NonEmptyList<T> inner in source.Items)
            results.AddRange(inner.Items);

        return NonEmptyList<T>.FromTrustedList(results);
    }

    #endregion

    #region Ordering

    /// <summary>Sorts the elements in ascending order by key. The result is non-empty and supports <c>ThenBy</c>.</summary>
    public static OrderedNonEmptyList<T> OrderBy<T, TKey>(this NonEmptyList<T> source, Func<T, TKey> keySelector)
        => OrderBy(source, keySelector, null);

    /// <summary>Sorts the elements in ascending order by key using a comparer. The result is non-empty and supports <c>ThenBy</c>.</summary>
    public static OrderedNonEmptyList<T> OrderBy<T, TKey>(this NonEmptyList<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return OrderedNonEmptyList<T>.Create(source, new KeyComparer<T, TKey>(keySelector, comparer ?? Comparer<TKey>.Default, descending: false));
    }

    /// <summary>Sorts the elements in descending order by key. The result is non-empty and supports <c>ThenBy</c>.</summary>
    public static OrderedNonEmptyList<T> OrderByDescending<T, TKey>(this NonEmptyList<T> source, Func<T, TKey> keySelector)
        => OrderByDescending(source, keySelector, null);

    /// <summary>Sorts the elements in descending order by key using a comparer. The result is non-empty and supports <c>ThenBy</c>.</summary>
    public static OrderedNonEmptyList<T> OrderByDescending<T, TKey>(this NonEmptyList<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return OrderedNonEmptyList<T>.Create(source, new KeyComparer<T, TKey>(keySelector, comparer ?? Comparer<TKey>.Default, descending: true));
    }

    /// <summary>Sorts the elements in ascending order. The result is non-empty.</summary>
    public static OrderedNonEmptyList<T> Order<T>(this NonEmptyList<T> source) => Order(source, null);

    /// <summary>Sorts the elements in ascending order using a comparer. The result is non-empty.</summary>
    public static OrderedNonEmptyList<T> Order<T>(this NonEmptyList<T> source, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        return OrderedNonEmptyList<T>.Create(source, comparer ?? Comparer<T>.Default);
    }

    /// <summary>Sorts the elements in descending order. The result is non-empty.</summary>
    public static OrderedNonEmptyList<T> OrderDescending<T>(this NonEmptyList<T> source) => OrderDescending(source, null);

    /// <summary>Sorts the elements in descending order using a comparer. The result is non-empty.</summary>
    public static OrderedNonEmptyList<T> OrderDescending<T>(this NonEmptyList<T> source, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        return OrderedNonEmptyList<T>.Create(source, new KeyComparer<T, T>(x => x, comparer ?? Comparer<T>.Default, descending: true));
    }

    /// <summary>Performs a subsequent ascending ordering by key.</summary>
    public static OrderedNonEmptyList<T> ThenBy<T, TKey>(this OrderedNonEmptyList<T> source, Func<T, TKey> keySelector)
        => ThenBy(source, keySelector, null);

    /// <summary>Performs a subsequent ascending ordering by key using a comparer.</summary>
    public static OrderedNonEmptyList<T> ThenBy<T, TKey>(this OrderedNonEmptyList<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.ThenByCore(keySelector, comparer, descending: false);
    }

    /// <summary>Performs a subsequent descending ordering by key.</summary>
    public static OrderedNonEmptyList<T> ThenByDescending<T, TKey>(this OrderedNonEmptyList<T> source, Func<T, TKey> keySelector)
        => ThenByDescending(source, keySelector, null);

    /// <summary>Performs a subsequent descending ordering by key using a comparer.</summary>
    public static OrderedNonEmptyList<T> ThenByDescending<T, TKey>(this OrderedNonEmptyList<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.ThenByCore(keySelector, comparer, descending: true);
    }

    #endregion

    #region Set operations

    /// <summary>Returns the distinct elements. At least the first element survives, so the result is non-empty.</summary>
    public static NonEmptyList<T> Distinct<T>(this NonEmptyList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.DistinctList();
    }

    /// <summary>Returns the distinct elements using a comparer. The result is non-empty.</summary>
    public static NonEmptyList<T> Distinct<T>(this NonEmptyList<T> source, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.DistinctList(comparer);
    }

    /// <summary>Concatenates a possibly empty sequence with a non-empty list. The result is non-empty.</summary>
    public static NonEmptyList<T> Concat<T>(this IEnumerable<T> first, NonEmptyList<T> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        List<T> results = new(first);
        results.AddRange(second.Items);
        return NonEmptyList<T>.FromTrustedList(results);
    }

    /// <summary>Returns the set union of a non-empty list and a sequence. The result is non-empty.</summary>
    public static NonEmptyList<T> Union<T>(this NonEmptyList<T> first, IEnumerable<T> second)
        => Union(first, second, null);

    /// <summary>Returns the set union of a non-empty list and a sequence using a comparer. The result is non-empty.</summary>
    public static NonEmptyList<T> Union<T>(this NonEmptyList<T> first, IEnumerable<T> second, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NonEmptyList<T>.FromTrustedList(Enumerable.Union(first.Items, second, comparer).ToList());
    }

    /// <summary>Returns the set union of a sequence and a non-empty list. The result is non-empty.</summary>
    public static NonEmptyList<T> Union<T>(this IEnumerable<T> first, NonEmptyList<T> second)
        => Union(first, second, null);

    /// <summary>Returns the set union of a sequence and a non-empty list using a comparer. The result is non-empty.</summary>
    public static NonEmptyList<T> Union<T>(this IEnumerable<T> first, NonEmptyList<T> second, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return NonEmptyList<T>.FromTrustedList(Enumerable.Union(first, second.Items, comparer).ToList());
    }

    #endregion

    #region Grouping and partitioning

    /// <summary>
    /// Groups the elements by key. A non-empty source produces at least one group, and every group has at least one element,
    /// so the result is a non-empty list of non-empty groups. Enables <c>group x by k into g</c> in query syntax.
    /// </summary>
    public static NonEmptyList<NonEmptyGrouping<TKey, T>> GroupBy<T, TKey>(this NonEmptyList<T> source, Func<T, TKey> keySelector)
        => GroupBy(source, keySelector, null);

    /// <summary>Groups the elements by key using a comparer. The result is a non-empty list of non-empty groups.</summary>
    public static NonEmptyList<NonEmptyGrouping<TKey, T>> GroupBy<T, TKey>(
        this NonEmptyList<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer)
        => GroupBy(source, keySelector, x => x, comparer);

    /// <summary>Groups the elements by key and projects each element. The result is a non-empty list of non-empty groups.</summary>
    public static NonEmptyList<NonEmptyGrouping<TKey, TElement>> GroupBy<T, TKey, TElement>(
        this NonEmptyList<T> source,
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector)
        => GroupBy(source, keySelector, elementSelector, null);

    /// <summary>Groups the elements by key using a comparer and projects each element. The result is a non-empty list of non-empty groups.</summary>
    public static NonEmptyList<NonEmptyGrouping<TKey, TElement>> GroupBy<T, TKey, TElement>(
        this NonEmptyList<T> source,
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector,
        IEqualityComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(elementSelector);

        List<NonEmptyGrouping<TKey, TElement>> groups = new();
        foreach (IGrouping<TKey, TElement> group in Enumerable.GroupBy(source.Items, keySelector, elementSelector, comparer))
        {
            List<TElement> elements = group.ToList();
            foreach (TElement element in elements)
            {
                if (element is null)
                    throw new ArgumentNullException(nameof(elementSelector), "NonEmptyList cannot contain null values");
            }

            groups.Add(new NonEmptyGrouping<TKey, TElement>(group.Key, elements));
        }

        return NonEmptyList<NonEmptyGrouping<TKey, TElement>>.FromTrustedList(groups);
    }

    /// <summary>
    /// Splits the list into chunks of the given size. Every chunk is non-empty and there is at least one chunk,
    /// so the result is a non-empty list of non-empty lists.
    /// </summary>
    public static NonEmptyList<NonEmptyList<T>> Chunk<T>(this NonEmptyList<T> source, int size)
    {
        ArgumentNullException.ThrowIfNull(source);
        return NonEmptyList<NonEmptyList<T>>.FromTrustedList(source.ChunkNonEmpty(size).ToList());
    }

    #endregion

    #region Combining

    /// <summary>Splits a non-empty list of pairs into two non-empty lists.</summary>
    public static (NonEmptyList<TFirst> Firsts, NonEmptyList<TSecond> Seconds) Unzip<TFirst, TSecond>(
        this NonEmptyList<(TFirst First, TSecond Second)> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<TFirst> firsts = new(source.Count);
        List<TSecond> seconds = new(source.Count);
        foreach ((TFirst first, TSecond second) in source.Items)
        {
            firsts.Add(first);
            seconds.Add(second);
        }

        return (NonEmptyList<TFirst>.FromTrustedList(firsts), NonEmptyList<TSecond>.FromTrustedList(seconds));
    }

    /// <summary>
    /// Alternates elements from two lists: <c>a1, b1, a2, b2, ...</c>. When one list runs out, the remaining
    /// elements of the other are appended. The result is non-empty.
    /// </summary>
    public static NonEmptyList<T> Interleave<T>(this NonEmptyList<T> first, NonEmptyList<T> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        List<T> results = new(first.Count + second.Count);
        int shared = Math.Min(first.Count, second.Count);
        for (int i = 0; i < shared; i++)
        {
            results.Add(first.Items[i]);
            results.Add(second.Items[i]);
        }

        for (int i = shared; i < first.Count; i++)
            results.Add(first.Items[i]);

        for (int i = shared; i < second.Count; i++)
            results.Add(second.Items[i]);

        return NonEmptyList<T>.FromTrustedList(results);
    }

    /// <summary>
    /// Repeats the list forever. Cycling an empty sequence is undefined, so this operator is only available on non-empty lists.
    /// Combine with <c>Take</c> to obtain a finite result.
    /// </summary>
    public static NonEmptyEnumerable<T> Cycle<T>(this NonEmptyList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new NonEmptyEnumerable<T>(source.Head, CycleRest(source));
    }

    private static IEnumerable<T> CycleRest<T>(NonEmptyList<T> source)
    {
        for (int i = 1; i < source.Count; i++)
            yield return source.Items[i];

        while (true)
        {
            for (int i = 0; i < source.Count; i++)
                yield return source.Items[i];
        }
    }

    /// <summary>Repeats the list a fixed number of times. The result is non-empty.</summary>
    public static NonEmptyList<T> Cycle<T>(this NonEmptyList<T> source, int times)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (times < 1)
            throw new ArgumentOutOfRangeException(nameof(times), "Times must be at least 1");

        List<T> results = new(source.Count * times);
        for (int t = 0; t < times; t++)
            results.AddRange(source.Items);

        return NonEmptyList<T>.FromTrustedList(results);
    }

    #endregion

    #region Aggregation without an empty case

    /// <summary>Returns the maximum element. Never throws for lack of elements and never returns null.</summary>
    public static T Max<T>(this NonEmptyList<T> source) => Max(source, (IComparer<T>?)null);

    /// <summary>Returns the maximum element according to a comparer.</summary>
    public static T Max<T>(this NonEmptyList<T> source, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        comparer ??= Comparer<T>.Default;

        T best = source.Items[0];
        for (int i = 1; i < source.Count; i++)
        {
            if (comparer.Compare(source.Items[i], best) > 0)
                best = source.Items[i];
        }

        return best;
    }

    /// <summary>Returns the maximum projected value. Never returns null.</summary>
    public static TResult Max<T, TResult>(this NonEmptyList<T> source, Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        IComparer<TResult> comparer = Comparer<TResult>.Default;
        TResult best = selector(source.Items[0]);
        for (int i = 1; i < source.Count; i++)
        {
            TResult candidate = selector(source.Items[i]);
            if (comparer.Compare(candidate, best) > 0)
                best = candidate;
        }

        return best;
    }

    /// <summary>Returns the minimum element. Never throws for lack of elements and never returns null.</summary>
    public static T Min<T>(this NonEmptyList<T> source) => Min(source, (IComparer<T>?)null);

    /// <summary>Returns the minimum element according to a comparer.</summary>
    public static T Min<T>(this NonEmptyList<T> source, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        comparer ??= Comparer<T>.Default;

        T best = source.Items[0];
        for (int i = 1; i < source.Count; i++)
        {
            if (comparer.Compare(source.Items[i], best) < 0)
                best = source.Items[i];
        }

        return best;
    }

    /// <summary>Returns the minimum projected value. Never returns null.</summary>
    public static TResult Min<T, TResult>(this NonEmptyList<T> source, Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        IComparer<TResult> comparer = Comparer<TResult>.Default;
        TResult best = selector(source.Items[0]);
        for (int i = 1; i < source.Count; i++)
        {
            TResult candidate = selector(source.Items[i]);
            if (comparer.Compare(candidate, best) < 0)
                best = candidate;
        }

        return best;
    }

    /// <summary>Returns the minimum and maximum elements in a single pass.</summary>
    public static (T Min, T Max) MinMax<T>(this NonEmptyList<T> source) => MinMax(source, null);

    /// <summary>Returns the minimum and maximum elements in a single pass using a comparer.</summary>
    public static (T Min, T Max) MinMax<T>(this NonEmptyList<T> source, IComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        comparer ??= Comparer<T>.Default;

        T min = source.Items[0];
        T max = min;
        for (int i = 1; i < source.Count; i++)
        {
            T item = source.Items[i];
            if (comparer.Compare(item, min) < 0) min = item;
            if (comparer.Compare(item, max) > 0) max = item;
        }

        return (min, max);
    }

    #endregion

    #region Entering the type

    /// <summary>
    /// Materializes a sequence into a NonEmptyList.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty.</exception>
    public static NonEmptyList<T> ToNonEmptyList<T>(this IEnumerable<T> source) => NonEmptyList<T>.From(source);

    /// <summary>
    /// Materializes a sequence into a NonEmptyList, or returns null if the sequence is empty.
    /// </summary>
    public static NonEmptyList<T>? ToNonEmptyListOrNull<T>(this IEnumerable<T>? source)
        => NonEmptyList<T>.TryFrom(source, out NonEmptyList<T>? result) ? result : null;

    /// <summary>
    /// Attempts to materialize a sequence into a NonEmptyList.
    /// </summary>
    public static bool TryToNonEmptyList<T>(this IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptyList<T>? result)
        => NonEmptyList<T>.TryFrom(source, out result);

    /// <summary>
    /// Materializes a span into a NonEmptyList.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the span is empty.</exception>
    public static NonEmptyList<T> ToNonEmptyList<T>(this ReadOnlySpan<T> source) => NonEmptyList<T>.FromSpan(source);

    #endregion
}
