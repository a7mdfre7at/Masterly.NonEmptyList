using System.Diagnostics;

namespace Masterly.NonEmptyList;

/// <summary>
/// A lazily evaluated sequence whose first element is already known. The rest is produced on demand and may be
/// infinite, so <c>Iterate</c> and <c>Cycle</c> can be total: <see cref="Take"/> with a count of at least one and
/// <see cref="TakeWhile"/> both return a <see cref="NonEmptyList{T}"/> because the head is always included.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
[DebuggerDisplay("Head = {Head}")]
public sealed class NonEmptyEnumerable<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> _rest;

    /// <summary>Creates a sequence from a known head and a lazily evaluated rest.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the head or the rest is null.</exception>
    public NonEmptyEnumerable(T head, IEnumerable<T> rest)
    {
        ArgumentNullException.ThrowIfNull(head);
        ArgumentNullException.ThrowIfNull(rest);
        Head = head;
        _rest = rest;
    }

    /// <summary>The first element. Already evaluated.</summary>
    public T Head { get; }

    /// <summary>Every element after the head. Lazily evaluated and possibly infinite.</summary>
    public IEnumerable<T> Rest => _rest;

    /// <summary>Takes the first <paramref name="count"/> elements. The result is non-empty because the head is always included.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is less than 1.</exception>
    public NonEmptyList<T> Take(int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

        List<T> items = new(count) { Head };
        if (count > 1)
            items.AddRange(_rest.Take(count - 1));

        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>
    /// Takes elements while the predicate holds. The head is always included, even if it fails the predicate,
    /// so the result is non-empty.
    /// </summary>
    public NonEmptyList<T> TakeWhile(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        List<T> items = new() { Head };
        items.AddRange(_rest.TakeWhile(predicate));
        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>Takes elements up to and including the first one that satisfies the predicate.</summary>
    public NonEmptyList<T> TakeUntil(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        List<T> items = new() { Head };
        if (predicate(Head))
            return NonEmptyList<T>.FromTrustedList(items);

        foreach (T item in _rest)
        {
            items.Add(item);
            if (predicate(item))
                break;
        }

        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>Lazily transforms every element. The head is transformed eagerly.</summary>
    public NonEmptyEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new NonEmptyEnumerable<TResult>(selector(Head), _rest.Select(selector));
    }

    /// <summary>Lazily pairs the elements with their index.</summary>
    public NonEmptyEnumerable<(T Item, int Index)> Index()
        => new((Head, 0), _rest.Select(static (item, i) => (item, i + 1)));

    /// <summary>Lazily pairs this sequence with another non-empty sequence, stopping at the shorter one.</summary>
    public NonEmptyEnumerable<(T First, TOther Second)> Zip<TOther>(NonEmptyEnumerable<TOther> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new NonEmptyEnumerable<(T, TOther)>((Head, other.Head), _rest.Zip(other._rest));
    }

    /// <summary>Lazily prepends an element.</summary>
    public NonEmptyEnumerable<T> Prepend(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new NonEmptyEnumerable<T>(item, this);
    }

    /// <summary>Lazily running fold. The first element of the result is the seed combined with the head.</summary>
    public NonEmptyEnumerable<TAccumulate> Scan<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        TAccumulate first = folder(seed, Head);
        return new NonEmptyEnumerable<TAccumulate>(first, ScanRest(first, folder));
    }

    /// <summary>Returns the element at <paramref name="index"/>, evaluating the sequence up to that point.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is negative or beyond a finite sequence.</exception>
    public T ElementAt(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        return index == 0 ? Head : _rest.ElementAt(index - 1);
    }

    /// <summary>Materializes a finite sequence. Never returns for an infinite one.</summary>
    public NonEmptyList<T> ToNonEmptyList()
    {
        List<T> items = new() { Head };
        items.AddRange(_rest);
        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        yield return Head;
        foreach (T item in _rest)
            yield return item;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private IEnumerable<TAccumulate> ScanRest<TAccumulate>(TAccumulate state, Func<TAccumulate, T, TAccumulate> folder)
    {
        foreach (T item in _rest)
        {
            state = folder(state, item);
            yield return state;
        }
    }
}

/// <summary>Factories for <see cref="NonEmptyEnumerable{T}"/>.</summary>
public static class NonEmptyEnumerable
{
    /// <summary>Creates a lazy sequence from a known head and a rest.</summary>
    public static NonEmptyEnumerable<T> Create<T>(T head, IEnumerable<T> rest) => new(head, rest);

    /// <summary>Creates a lazy sequence from a known head and a rest.</summary>
    public static NonEmptyEnumerable<T> Create<T>(T head, params T[] rest) => new(head, rest);

    /// <summary>
    /// Attempts to pull the first element of a sequence. Returns <see langword="null"/> if it is empty; otherwise the
    /// head is in memory and the rest is still lazy. The source is enumerated once.
    /// </summary>
    public static NonEmptyEnumerable<T>? FromEnumerable<T>(IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        IEnumerator<T> enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            enumerator.Dispose();
            return null;
        }

        T head = enumerator.Current;
        if (head is null)
        {
            enumerator.Dispose();
            throw new ArgumentException("Sequence cannot contain null values", nameof(source));
        }

        return new NonEmptyEnumerable<T>(head, Drain(enumerator));
    }

    /// <summary>An infinite sequence: <c>seed, next(seed), next(next(seed)), ...</c>.</summary>
    public static NonEmptyEnumerable<T> Iterate<T>(T seed, Func<T, T> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return new NonEmptyEnumerable<T>(seed, IterateRest(seed, next));
    }

    /// <summary>An infinite sequence that repeats one value.</summary>
    public static NonEmptyEnumerable<T> Repeat<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new NonEmptyEnumerable<T>(value, RepeatForever(value));
    }

    /// <summary>An infinite sequence of integers counting up from <paramref name="start"/>.</summary>
    public static NonEmptyEnumerable<int> From(int start) => Iterate(start, static n => n + 1);

    /// <summary>An infinite sequence that keeps generating values from a function of the index.</summary>
    public static NonEmptyEnumerable<T> Generate<T>(Func<int, T> generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        return new NonEmptyEnumerable<T>(generator(0), GenerateRest(generator));
    }

    private static IEnumerable<T> Drain<T>(IEnumerator<T> enumerator)
    {
        using (enumerator)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }

    private static IEnumerable<T> IterateRest<T>(T current, Func<T, T> next)
    {
        while (true)
        {
            current = next(current);
            yield return current;
        }
    }

    private static IEnumerable<T> RepeatForever<T>(T value)
    {
        while (true)
            yield return value;
    }

    private static IEnumerable<T> GenerateRest<T>(Func<int, T> generator)
    {
        for (int i = 1; ; i++)
            yield return generator(i);
    }
}
