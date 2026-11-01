using System.Numerics;
using System.Runtime.InteropServices;

namespace Masterly.NonEmptyList;

/// <summary>
/// A stack-only, allocation-free view over at least one element. Obtain one from <see cref="NonEmptySpan.AsNonEmptySpan{T}(NonEmptyList{T})"/>
/// or <see cref="NonEmptySpan.From{T}(ReadOnlySpan{T})"/>, then call <c>Head</c>, <c>Max</c> or <c>Sum</c> without an empty check
/// and without touching the heap.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public readonly ref struct NonEmptySpan<T>
{
    private readonly ReadOnlySpan<T> _span;

    internal NonEmptySpan(ReadOnlySpan<T> trustedSpan) => _span = trustedSpan;

    /// <summary>The first element.</summary>
    public T Head => _span[0];

    /// <summary>The last element.</summary>
    public T Last => _span[^1];

    /// <summary>The elements after the first. May be empty.</summary>
    public ReadOnlySpan<T> Tail => _span[1..];

    /// <summary>The elements before the last. May be empty.</summary>
    public ReadOnlySpan<T> Init => _span[..^1];

    /// <summary>The number of elements. Always at least 1.</summary>
    public int Length => _span.Length;

    /// <summary>Gets the element at <paramref name="index"/>.</summary>
    public T this[int index] => _span[index];

    /// <summary>The underlying span.</summary>
    public ReadOnlySpan<T> Span => _span;

    /// <summary>Converts to the underlying span.</summary>
    public static implicit operator ReadOnlySpan<T>(NonEmptySpan<T> span) => span._span;

    /// <summary>Combines the elements left to right with no seed.</summary>
    public T Reduce(Func<T, T, T> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);
        T acc = _span[0];
        for (int i = 1; i < _span.Length; i++)
            acc = reducer(acc, _span[i]);
        return acc;
    }

    /// <summary>Folds the elements into an accumulator.</summary>
    public TResult Fold<TResult>(TResult seed, Func<TResult, T, TResult> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        TResult acc = seed;
        foreach (T item in _span)
            acc = folder(acc, item);
        return acc;
    }

    /// <summary>The largest element using the default comparer.</summary>
    public T Max()
    {
        Comparer<T> comparer = Comparer<T>.Default;
        T best = _span[0];
        for (int i = 1; i < _span.Length; i++)
        {
            if (comparer.Compare(_span[i], best) > 0)
                best = _span[i];
        }

        return best;
    }

    /// <summary>The smallest element using the default comparer.</summary>
    public T Min()
    {
        Comparer<T> comparer = Comparer<T>.Default;
        T best = _span[0];
        for (int i = 1; i < _span.Length; i++)
        {
            if (comparer.Compare(_span[i], best) < 0)
                best = _span[i];
        }

        return best;
    }

    /// <summary>Copies the elements into a new <see cref="NonEmptyList{T}"/>.</summary>
    public NonEmptyList<T> ToNonEmptyList() => NonEmptyList<T>.FromSpan(_span);

    /// <summary>Copies the elements into a new array. Never empty.</summary>
    public T[] ToArray() => _span.ToArray();

    /// <summary>Returns an enumerator over the elements.</summary>
    public ReadOnlySpan<T>.Enumerator GetEnumerator() => _span.GetEnumerator();

    /// <inheritdoc />
    public override string ToString() => $"NonEmptySpan<{typeof(T).Name}>[{_span.Length}]";
}

/// <summary>Factories and numeric helpers for <see cref="NonEmptySpan{T}"/>.</summary>
public static class NonEmptySpan
{
    /// <summary>Wraps a span, checking that it has at least one element.</summary>
    /// <exception cref="ArgumentException">Thrown if the span is empty.</exception>
    public static NonEmptySpan<T> From<T>(ReadOnlySpan<T> span)
        => span.Length == 0 ? throw new ArgumentException("Span cannot be empty", nameof(span)) : new NonEmptySpan<T>(span);

    /// <summary>Wraps a span, checking that it has at least one element.</summary>
    /// <exception cref="ArgumentException">Thrown if the span is empty.</exception>
    public static NonEmptySpan<T> From<T>(Span<T> span) => From((ReadOnlySpan<T>)span);

    /// <summary>Wraps an array, checking that it has at least one element.</summary>
    /// <exception cref="ArgumentException">Thrown if the array is null or empty.</exception>
    public static NonEmptySpan<T> From<T>(T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        return From(array.AsSpan());
    }

    /// <summary>Attempts to wrap a span, returning <see langword="false"/> if it is empty.</summary>
    public static bool TryFrom<T>(ReadOnlySpan<T> span, out NonEmptySpan<T> result)
    {
        result = span.Length == 0 ? default : new NonEmptySpan<T>(span);
        return span.Length != 0;
    }

    /// <summary>Views the list's current storage as a non-empty span. Do not mutate the list while the span is alive.</summary>
    public static NonEmptySpan<T> AsNonEmptySpan<T>(this NonEmptyList<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return new NonEmptySpan<T>(CollectionsMarshal.AsSpan(list.Items));
    }

    /// <summary>Views the immutable list as a non-empty span.</summary>
    public static NonEmptySpan<T> AsNonEmptySpan<T>(this ImmutableNonEmptyList<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return new NonEmptySpan<T>(list.AsSpan());
    }

    /// <summary>Checked sum. No seed is needed because the span has a first element.</summary>
    public static T Sum<T>(this NonEmptySpan<T> span) where T : IAdditionOperators<T, T, T>
    {
        ReadOnlySpan<T> items = span.Span;
        T total = items[0];
        for (int i = 1; i < items.Length; i++)
            total = checked(total + items[i]);
        return total;
    }

    /// <summary>Arithmetic mean.</summary>
    public static T Average<T>(this NonEmptySpan<T> span) where T : INumberBase<T>
    {
        ReadOnlySpan<T> items = span.Span;
        T total = items[0];
        for (int i = 1; i < items.Length; i++)
            total += items[i];
        return total / T.CreateChecked(items.Length);
    }
}
