using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// A hash set that always contains at least one element. Wraps a <see cref="HashSet{T}"/>; every operation that could
/// empty the set (<c>Clear</c>, removing the last element, <c>IntersectWith</c> a disjoint set, and so on) throws instead.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
[JsonConverter(typeof(NonEmptySetJsonConverterFactory))]
[CollectionBuilder(typeof(NonEmptySet), nameof(NonEmptySet.Create))]
[DebuggerDisplay("Count = {Count}")]
public sealed class NonEmptySet<T> : ISet<T>, IReadOnlySet<T>, IReadOnlyCollection<T>, IEquatable<NonEmptySet<T>>
{
    private readonly HashSet<T> _items;

    private NonEmptySet(HashSet<T> trustedItems) => _items = trustedItems;

    /// <summary>Creates a set with one element and optional others. Duplicates collapse.</summary>
    /// <exception cref="ArgumentNullException">Thrown if any element is null.</exception>
    public NonEmptySet(T first, params T[] rest) : this(first, rest, null)
    {
    }

    /// <summary>Creates a set with a custom comparer, one element and optional others.</summary>
    /// <exception cref="ArgumentNullException">Thrown if any element is null.</exception>
    public NonEmptySet(T first, IEnumerable<T> rest, IEqualityComparer<T>? comparer)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(rest);

        _items = new HashSet<T>(comparer) { first };
        foreach (T item in rest)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(rest), "Set cannot contain null values");
            _items.Add(item);
        }
    }

    /// <summary>Creates a set from a sequence.</summary>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the sequence is null or contains null.</exception>
    public static NonEmptySet<T> From(IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        HashSet<T> set = new(comparer);
        foreach (T item in source)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(source), "Set cannot contain null values");
            set.Add(item);
        }

        if (set.Count == 0)
            throw new ArgumentException("Sequence cannot be empty", nameof(source));

        return new NonEmptySet<T>(set);
    }

    /// <summary>Creates a set from a span.</summary>
    /// <exception cref="ArgumentException">Thrown if the span is empty.</exception>
    public static NonEmptySet<T> FromSpan(ReadOnlySpan<T> items)
    {
        if (items.Length == 0)
            throw new ArgumentException("Span cannot be empty", nameof(items));

        HashSet<T> set = new(items.Length);
        foreach (T item in items)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(items), "Set cannot contain null values");
            set.Add(item);
        }

        return new NonEmptySet<T>(set);
    }

    /// <summary>Attempts to create a set, returning <see langword="false"/> for a null or empty sequence.</summary>
    public static bool TryFrom(IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptySet<T>? result, IEqualityComparer<T>? comparer = null)
    {
        result = null;
        if (source is null)
            return false;

        HashSet<T> set = new(comparer);
        foreach (T item in source)
        {
            if (item is null)
                return false;
            set.Add(item);
        }

        if (set.Count == 0)
            return false;

        result = new NonEmptySet<T>(set);
        return true;
    }

    /// <summary>Creates a set with a single element.</summary>
    public static NonEmptySet<T> Singleton(T item) => new(item);

    /// <summary>
    /// Parses a comma-separated string such as <c>"a,b,c"</c>. Elements are parsed like <see cref="NonEmptyList{T}.Parse(string, IFormatProvider?)"/>;
    /// duplicates collapse. Lets minimal APIs and MVC bind a set from a query-string value.
    /// </summary>
    /// <exception cref="FormatException">Thrown if the string is empty or an element cannot be parsed.</exception>
    public static NonEmptySet<T> Parse(string s, IFormatProvider? provider = null)
        => From(NonEmptyList<T>.Parse(s, provider).Items);

    /// <summary>Attempts to parse a comma-separated string. Returns <see langword="false"/> for empty or invalid input.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonEmptySet<T> result)
    {
        if (NonEmptyList<T>.TryParse(s, provider, out NonEmptyList<T>? list))
        {
            result = From(list.Items);
            return true;
        }

        result = null;
        return false;
    }

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <summary>The comparer used to determine equality.</summary>
    public IEqualityComparer<T> Comparer => _items.Comparer;

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>Returns an arbitrary element. Never throws because the set is never empty.</summary>
    public T Any()
    {
        foreach (T item in _items)
            return item;
        throw new UnreachableException();
    }

    /// <summary>Adds an element. Returns <see langword="false"/> if it was already present.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the element is null.</exception>
    public bool Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _items.Add(item);
    }

    void ICollection<T>.Add(T item) => Add(item);

    /// <summary>Removes an element. Returns <see langword="false"/> if it was not present.</summary>
    /// <exception cref="InvalidOperationException">Thrown if removing it would empty the set.</exception>
    public bool Remove(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.Contains(item))
            return false;

        if (_items.Count == 1)
            throw new InvalidOperationException("Cannot remove the last element of a NonEmptySet");

        return _items.Remove(item);
    }

    /// <summary>Removes every element matching the predicate, as long as at least one element remains.</summary>
    /// <exception cref="InvalidOperationException">Thrown if every element matches.</exception>
    public int RemoveWhere(Predicate<T> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        int matching = _items.Count(x => match(x));
        if (matching >= _items.Count)
            throw new InvalidOperationException("Cannot remove every element of a NonEmptySet");

        return _items.RemoveWhere(match);
    }

    /// <summary>Clearing is not supported because a NonEmptySet cannot be empty.</summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Clear() => throw new NotSupportedException("Cannot clear a NonEmptySet");

    /// <inheritdoc />
    public bool Contains(T item) => _items.Contains(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>Copies the elements to an array.</summary>
    public void CopyTo(T[] array) => _items.CopyTo(array);

    /// <summary>Adds every element of <paramref name="other"/>.</summary>
    public void UnionWith(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other)
            Add(item);
    }

    /// <summary>Keeps only the elements also in <paramref name="other"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the sets are disjoint.</exception>
    public void IntersectWith(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> otherSet = other as HashSet<T> ?? new HashSet<T>(other, _items.Comparer);
        if (!_items.Overlaps(otherSet))
            throw new InvalidOperationException("Intersection would empty the NonEmptySet");

        _items.IntersectWith(otherSet);
    }

    /// <summary>Removes every element in <paramref name="other"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the result would be empty.</exception>
    public void ExceptWith(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> otherSet = other as HashSet<T> ?? new HashSet<T>(other, _items.Comparer);
        if (_items.IsSubsetOf(otherSet))
            throw new InvalidOperationException("Difference would empty the NonEmptySet");

        _items.ExceptWith(otherSet);
    }

    /// <summary>Keeps the elements in exactly one of the two sets.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the sets are equal, which would empty the result.</exception>
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> otherSet = other as HashSet<T> ?? new HashSet<T>(other, _items.Comparer);
        if (_items.SetEquals(otherSet))
            throw new InvalidOperationException("Symmetric difference would empty the NonEmptySet");

        _items.SymmetricExceptWith(otherSet);
    }

    /// <inheritdoc />
    public bool IsSubsetOf(IEnumerable<T> other) => _items.IsSubsetOf(other);

    /// <inheritdoc />
    public bool IsSupersetOf(IEnumerable<T> other) => _items.IsSupersetOf(other);

    /// <inheritdoc />
    public bool IsProperSubsetOf(IEnumerable<T> other) => _items.IsProperSubsetOf(other);

    /// <inheritdoc />
    public bool IsProperSupersetOf(IEnumerable<T> other) => _items.IsProperSupersetOf(other);

    /// <inheritdoc />
    public bool Overlaps(IEnumerable<T> other) => _items.Overlaps(other);

    /// <inheritdoc />
    public bool SetEquals(IEnumerable<T> other) => _items.SetEquals(other);

    /// <summary>Returns a new set containing the elements of both sets.</summary>
    public NonEmptySet<T> Union(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> result = new(_items, _items.Comparer);
        foreach (T item in other)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(other), "Set cannot contain null values");
            result.Add(item);
        }

        return new NonEmptySet<T>(result);
    }

    /// <summary>Returns the elements also in <paramref name="other"/>, or <see langword="null"/> if the sets are disjoint.</summary>
    public NonEmptySet<T>? Intersect(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> result = new(_items, _items.Comparer);
        result.IntersectWith(other);
        return result.Count == 0 ? null : new NonEmptySet<T>(result);
    }

    /// <summary>Returns the elements not in <paramref name="other"/>, or <see langword="null"/> if none remain.</summary>
    public NonEmptySet<T>? Except(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> result = new(_items, _items.Comparer);
        result.ExceptWith(other);
        return result.Count == 0 ? null : new NonEmptySet<T>(result);
    }

    /// <summary>Applies a function to every element. The result is non-empty; duplicates collapse.</summary>
    public NonEmptySet<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        HashSet<TResult> result = new();
        foreach (T item in _items)
        {
            TResult mapped = selector(item);
            if (mapped is null)
                throw new InvalidOperationException("Selector returned null");
            result.Add(mapped);
        }

        return new NonEmptySet<TResult>(result);
    }

    /// <summary>Returns the elements as a non-empty list in enumeration order.</summary>
    public NonEmptyList<T> ToNonEmptyList() => NonEmptyList<T>.FromTrustedList(new List<T>(_items));

    /// <summary>Returns the elements as a non-empty list, sorted.</summary>
    public NonEmptyList<T> ToSortedNonEmptyList(IComparer<T>? comparer = null)
    {
        List<T> items = new(_items);
        items.Sort(comparer);
        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>Copies the elements to a plain <see cref="HashSet{T}"/>.</summary>
    public HashSet<T> ToHashSet() => new(_items, _items.Comparer);

    /// <summary>Returns an allocation-free enumerator.</summary>
    public HashSet<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>Two sets are equal when they contain the same elements.</summary>
    public bool Equals(NonEmptySet<T>? other) => other is not null && _items.SetEquals(other._items);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NonEmptySet<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 0;
        foreach (T item in _items)
            hash ^= _items.Comparer.GetHashCode(item!);
        return HashCode.Combine(hash, _items.Count);
    }

    /// <inheritdoc />
    public override string ToString() => $"NonEmptySet: {{{string.Join(", ", _items)}}}";
}

/// <summary>Factories for <see cref="NonEmptySet{T}"/>.</summary>
public static class NonEmptySet
{
    /// <summary>Creates a set from a span. Used by the compiler for collection expressions.</summary>
    public static NonEmptySet<T> Create<T>(ReadOnlySpan<T> items) => NonEmptySet<T>.FromSpan(items);

    /// <summary>Creates a set with one element and optional others.</summary>
    public static NonEmptySet<T> Of<T>(T first, params T[] rest) => new(first, rest);

    /// <summary>Creates a set from a sequence.</summary>
    public static NonEmptySet<T> From<T>(IEnumerable<T> source, IEqualityComparer<T>? comparer = null) => NonEmptySet<T>.From(source, comparer);

    /// <summary>Creates a set from a sequence.</summary>
    public static NonEmptySet<T> ToNonEmptySet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) => NonEmptySet<T>.From(source, comparer);

    /// <summary>Creates a set from a non-empty list. Duplicates collapse.</summary>
    public static NonEmptySet<T> ToNonEmptySet<T>(this NonEmptyList<T> source, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return NonEmptySet<T>.From(source.Items, comparer);
    }

    /// <summary>Attempts to create a set from a sequence.</summary>
    public static bool TryToNonEmptySet<T>(this IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptySet<T>? result)
        => NonEmptySet<T>.TryFrom(source, out result);
}

/// <summary>JSON converter factory for <see cref="NonEmptySet{T}"/>.</summary>
public sealed class NonEmptySetJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(NonEmptySet<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(NonEmptySetJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>JSON converter for <see cref="NonEmptySet{T}"/>. Reads and writes a JSON array.</summary>
public sealed class NonEmptySetJsonConverter<T> : JsonConverter<NonEmptySet<T>>
{
    /// <inheritdoc />
    public override NonEmptySet<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array");

        HashSet<T> items = new();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            T? item = JsonSerializer.Deserialize<T>(ref reader, options);
            if (item is null)
                throw new JsonException("NonEmptySet cannot contain null elements");
            items.Add(item);
        }

        if (items.Count == 0)
            throw new JsonException("NonEmptySet cannot be empty");

        return NonEmptySet<T>.From(items);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, NonEmptySet<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (T item in value)
            JsonSerializer.Serialize(writer, item, options);
        writer.WriteEndArray();
    }
}
