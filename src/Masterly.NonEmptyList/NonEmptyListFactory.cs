using System.Diagnostics.CodeAnalysis;

namespace Masterly.NonEmptyList;

/// <summary>
/// Non-generic entry points for creating <see cref="NonEmptyList{T}"/> instances with type inference,
/// and the target of collection expressions (<c>NonEmptyList&lt;int&gt; xs = [1, 2, 3];</c>).
/// </summary>
public static class NonEmptyList
{
    /// <summary>
    /// Creates a NonEmptyList from a span of items. This is the method the compiler calls for collection expressions.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The items to copy. Must contain at least one element.</param>
    /// <exception cref="ArgumentException">Thrown if the span is empty, for example for the collection expression <c>[]</c>.</exception>
    public static NonEmptyList<T> Create<T>(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty)
            throw new ArgumentException("A NonEmptyList must contain at least one element; the collection expression was empty.", nameof(items));

        return new NonEmptyList<T>(items[0], items[1..]);
    }

    /// <summary>
    /// Creates a NonEmptyList from a first element and any number of additional elements, inferring the element type.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="first">The first element.</param>
    /// <param name="rest">Additional elements.</param>
    public static NonEmptyList<T> Of<T>(T first, params T[] rest) => new(first, rest);

    /// <summary>
    /// Creates a NonEmptyList from a first element and any number of additional elements without allocating an array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="first">The first element.</param>
    /// <param name="rest">Additional elements.</param>
    public static NonEmptyList<T> Of<T>(T first, params ReadOnlySpan<T> rest) => new(first, rest);

    /// <summary>
    /// Creates a NonEmptyList from a sequence, inferring the element type.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence. Must contain at least one element.</param>
    /// <exception cref="ArgumentException">Thrown if the sequence is null or empty.</exception>
    public static NonEmptyList<T> From<T>(IEnumerable<T> source) => NonEmptyList<T>.From(source);

    /// <summary>
    /// Attempts to create a NonEmptyList from a sequence, inferring the element type.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="result">The created list when the sequence had at least one element.</param>
    public static bool TryFrom<T>(IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptyList<T>? result)
        => NonEmptyList<T>.TryFrom(source, out result);

    /// <summary>
    /// Creates a NonEmptyList containing a single element.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="item">The single element.</param>
    public static NonEmptyList<T> Singleton<T>(T item) => new(item);

    /// <summary>
    /// Creates a NonEmptyList by repeating an element.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="item">The element to repeat.</param>
    /// <param name="count">The number of repetitions. Must be at least 1.</param>
    public static NonEmptyList<T> Repeat<T>(T item, int count) => NonEmptyList<T>.Repeat(item, count);

    /// <summary>
    /// Creates a NonEmptyList of consecutive integers.
    /// </summary>
    /// <param name="start">The first value.</param>
    /// <param name="count">The number of values. Must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is less than 1.</exception>
    public static NonEmptyList<int> Range(int start, int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

        List<int> items = new(count);
        for (int i = 0; i < count; i++)
            items.Add(start + i);

        return NonEmptyList<int>.FromTrustedList(items);
    }

    /// <summary>
    /// Creates a NonEmptyList by repeatedly applying a function to a seed:
    /// <c>seed, next(seed), next(next(seed)), ...</c>. The seed is always included, so the result is never empty.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="seed">The first element.</param>
    /// <param name="next">The function that produces each element from the previous one.</param>
    /// <param name="count">The total number of elements. Must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is less than 1.</exception>
    public static NonEmptyList<T> Iterate<T>(T seed, Func<T, T> next, int count)
    {
        return IterateCore(seed, next, count);
    }

    /// <summary>
    /// Creates an infinite lazy sequence: <c>seed, next(seed), next(next(seed)), ...</c>. Because the seed is always
    /// present, <c>Take(n)</c> and <c>TakeWhile</c> on the result return a <see cref="NonEmptyList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="seed">The first element.</param>
    /// <param name="next">The function that produces each element from the previous one.</param>
    public static NonEmptyEnumerable<T> Iterate<T>(T seed, Func<T, T> next) => NonEmptyEnumerable.Iterate(seed, next);

    private static NonEmptyList<T> IterateCore<T>(T seed, Func<T, T> next, int count)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

        List<T> items = new(count) { seed };
        T current = seed;
        for (int i = 1; i < count; i++)
        {
            current = next(current);
            items.Add(current);
        }

        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>
    /// Creates a NonEmptyList by applying a function to a seed while it produces values. The seed is always included,
    /// so the result is never empty even if <paramref name="next"/> immediately returns null.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="seed">The first element.</param>
    /// <param name="next">A function returning the next element, or null to stop.</param>
    public static NonEmptyList<T> Unfold<T>(T seed, Func<T, T?> next) where T : class
    {
        ArgumentNullException.ThrowIfNull(next);

        List<T> items = new() { seed };
        T? current = next(seed);
        while (current is not null)
        {
            items.Add(current);
            current = next(current);
        }

        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>
    /// Creates a NonEmptyList by applying a function to a seed while it produces values. The seed is always included.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="seed">The first element.</param>
    /// <param name="next">A function returning the next element, or null to stop.</param>
    public static NonEmptyList<T> UnfoldValue<T>(T seed, Func<T, T?> next) where T : struct
    {
        ArgumentNullException.ThrowIfNull(next);

        List<T> items = new() { seed };
        T? current = next(seed);
        while (current.HasValue)
        {
            items.Add(current.Value);
            current = next(current.Value);
        }

        return NonEmptyList<T>.FromTrustedList(items);
    }
}
