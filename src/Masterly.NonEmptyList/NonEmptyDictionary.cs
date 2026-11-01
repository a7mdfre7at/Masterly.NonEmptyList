using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// A dictionary that always contains at least one entry. Wraps a <see cref="Dictionary{TKey, TValue}"/>; removing the
/// last entry or clearing throws. Useful for lookups such as "rates per currency" where an empty table is a configuration bug.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
[JsonConverter(typeof(NonEmptyDictionaryJsonConverterFactory))]
[DebuggerDisplay("Count = {Count}")]
public sealed class NonEmptyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IEquatable<NonEmptyDictionary<TKey, TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _items;

    private NonEmptyDictionary(Dictionary<TKey, TValue> trustedItems) => _items = trustedItems;

    /// <summary>Creates a dictionary with one entry.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the key or value is null.</exception>
    public NonEmptyDictionary(TKey key, TValue value, IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        _items = new Dictionary<TKey, TValue>(comparer) { [key] = value };
    }

    /// <summary>Creates a dictionary from one entry and optional others.</summary>
    /// <exception cref="ArgumentException">Thrown if a key is duplicated.</exception>
    public NonEmptyDictionary(KeyValuePair<TKey, TValue> first, params KeyValuePair<TKey, TValue>[] rest)
        : this(first.Key, first.Value)
    {
        ArgumentNullException.ThrowIfNull(rest);
        foreach (KeyValuePair<TKey, TValue> pair in rest)
            AddCore(pair.Key, pair.Value, nameof(rest));
    }

    /// <summary>Creates a dictionary from a sequence of pairs.</summary>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty or a key is duplicated.</exception>
    public static NonEmptyDictionary<TKey, TValue> From(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        Dictionary<TKey, TValue> items = new(comparer);
        foreach (KeyValuePair<TKey, TValue> pair in source)
        {
            if (pair.Value is null)
                throw new ArgumentNullException(nameof(source), "Values cannot be null");
            items.Add(pair.Key, pair.Value);
        }

        if (items.Count == 0)
            throw new ArgumentException("Sequence cannot be empty", nameof(source));

        return new NonEmptyDictionary<TKey, TValue>(items);
    }

    /// <summary>Creates a dictionary from a sequence by projecting keys and values.</summary>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty or a key is duplicated.</exception>
    public static NonEmptyDictionary<TKey, TValue> From<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(valueSelector);
        return From(source.Select(x => new KeyValuePair<TKey, TValue>(keySelector(x), valueSelector(x))), comparer);
    }

    /// <summary>Attempts to create a dictionary, returning <see langword="false"/> for a null or empty sequence or duplicate keys.</summary>
    public static bool TryFrom(IEnumerable<KeyValuePair<TKey, TValue>>? source, [NotNullWhen(true)] out NonEmptyDictionary<TKey, TValue>? result, IEqualityComparer<TKey>? comparer = null)
    {
        result = null;
        if (source is null)
            return false;

        Dictionary<TKey, TValue> items = new(comparer);
        foreach (KeyValuePair<TKey, TValue> pair in source)
        {
            if (pair.Value is null || !items.TryAdd(pair.Key, pair.Value))
                return false;
        }

        if (items.Count == 0)
            return false;

        result = new NonEmptyDictionary<TKey, TValue>(items);
        return true;
    }

    /// <summary>Creates a dictionary with a single entry.</summary>
    public static NonEmptyDictionary<TKey, TValue> Singleton(TKey key, TValue value) => new(key, value);

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <summary>The comparer used for keys.</summary>
    public IEqualityComparer<TKey> Comparer => _items.Comparer;

    /// <summary>Gets or sets the value for a key. Setting adds or replaces; it can never empty the dictionary.</summary>
    /// <exception cref="KeyNotFoundException">Thrown when getting a missing key.</exception>
    public TValue this[TKey key]
    {
        get => _items[key];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _items[key] = value;
        }
    }

    /// <summary>The keys. Never empty.</summary>
    public Dictionary<TKey, TValue>.KeyCollection Keys => _items.Keys;

    /// <summary>The values. Never empty.</summary>
    public Dictionary<TKey, TValue>.ValueCollection Values => _items.Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => _items.Keys;
    ICollection<TValue> IDictionary<TKey, TValue>.Values => _items.Values;
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _items.Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _items.Values;
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    /// <summary>Returns an arbitrary entry. Never throws because the dictionary is never empty.</summary>
    public KeyValuePair<TKey, TValue> Any()
    {
        foreach (KeyValuePair<TKey, TValue> pair in _items)
            return pair;
        throw new UnreachableException();
    }

    /// <summary>The keys as a non-empty list.</summary>
    public NonEmptyList<TKey> KeyList() => NonEmptyList<TKey>.FromTrustedList(new List<TKey>(_items.Keys));

    /// <summary>The values as a non-empty list.</summary>
    public NonEmptyList<TValue> ValueList() => NonEmptyList<TValue>.FromTrustedList(new List<TValue>(_items.Values));

    /// <summary>The entries as a non-empty list.</summary>
    public NonEmptyList<KeyValuePair<TKey, TValue>> ToNonEmptyList() => NonEmptyList<KeyValuePair<TKey, TValue>>.FromTrustedList(new List<KeyValuePair<TKey, TValue>>(_items));

    /// <summary>Adds an entry.</summary>
    /// <exception cref="ArgumentException">Thrown if the key already exists.</exception>
    public void Add(TKey key, TValue value) => AddCore(key, value, nameof(key));

    /// <summary>Adds an entry if the key is absent.</summary>
    public bool TryAdd(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return _items.TryAdd(key, value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _items.ContainsKey(key);

    /// <summary>Returns <see langword="true"/> if any entry has the given value.</summary>
    public bool ContainsValue(TValue value) => _items.ContainsValue(value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_items).Contains(item);

    /// <inheritdoc />
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _items.TryGetValue(key, out value);

    /// <summary>Returns the value for the key, or <paramref name="fallback"/> if absent.</summary>
    public TValue GetValueOrDefault(TKey key, TValue fallback) => _items.TryGetValue(key, out TValue? value) ? value : fallback;

    /// <summary>Removes an entry. Returns <see langword="false"/> if the key was absent.</summary>
    /// <exception cref="InvalidOperationException">Thrown if removing it would empty the dictionary.</exception>
    public bool Remove(TKey key)
    {
        if (!_items.ContainsKey(key))
            return false;

        if (_items.Count == 1)
            throw new InvalidOperationException("Cannot remove the last entry of a NonEmptyDictionary");

        return RemoveChecked(key, out _);
    }

    /// <summary>Removes an entry and returns its value.</summary>
    /// <exception cref="InvalidOperationException">Thrown if removing it would empty the dictionary.</exception>
    public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (!_items.ContainsKey(key))
        {
            value = default;
            return false;
        }

        if (_items.Count == 1)
            throw new InvalidOperationException("Cannot remove the last entry of a NonEmptyDictionary");

        return RemoveChecked(key, out value);
    }

    private bool RemoveChecked(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        bool removed = _items.Remove(key, out value);

        // A comparer that re-enters this dictionary can remove the other entries while this removal is in flight.
        if (_items.Count == 0)
        {
            _items.Add(key, value!);
            throw new InvalidOperationException("Cannot remove the last entry of a NonEmptyDictionary");
        }

        return removed;
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_items).Contains(item) && Remove(item.Key);

    /// <summary>Clearing is not supported because a NonEmptyDictionary cannot be empty.</summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Clear() => throw new NotSupportedException("Cannot clear a NonEmptyDictionary");

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_items).CopyTo(array, arrayIndex);

    /// <summary>Applies a function to every value, keeping the keys.</summary>
    public NonEmptyDictionary<TKey, TResult> MapValues<TResult>(Func<TValue, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        Dictionary<TKey, TResult> result = new(_items.Count, _items.Comparer);
        foreach (KeyValuePair<TKey, TValue> pair in _items)
        {
            TResult mapped = selector(pair.Value);
            if (mapped is null)
                throw new InvalidOperationException("Selector returned null");
            result[pair.Key] = mapped;
        }

        return new NonEmptyDictionary<TKey, TResult>(result);
    }

    /// <summary>Applies a function to every key and value, keeping the keys.</summary>
    public NonEmptyDictionary<TKey, TResult> Map<TResult>(Func<TKey, TValue, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        Dictionary<TKey, TResult> result = new(_items.Count, _items.Comparer);
        foreach (KeyValuePair<TKey, TValue> pair in _items)
        {
            TResult mapped = selector(pair.Key, pair.Value);
            if (mapped is null)
                throw new InvalidOperationException("Selector returned null");
            result[pair.Key] = mapped;
        }

        return new NonEmptyDictionary<TKey, TResult>(result);
    }

    /// <summary>Returns the entries matching the predicate, or <see langword="null"/> if none match.</summary>
    public NonEmptyDictionary<TKey, TValue>? Where(Func<TKey, TValue, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        Dictionary<TKey, TValue> result = new(_items.Comparer);
        foreach (KeyValuePair<TKey, TValue> pair in _items)
        {
            if (predicate(pair.Key, pair.Value))
                result[pair.Key] = pair.Value;
        }

        return result.Count == 0 ? null : new NonEmptyDictionary<TKey, TValue>(result);
    }

    /// <summary>Copies the entries to a plain <see cref="Dictionary{TKey, TValue}"/>.</summary>
    public Dictionary<TKey, TValue> ToDictionary() => new(_items, _items.Comparer);

    /// <summary>Returns an allocation-free enumerator.</summary>
    public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>Two dictionaries are equal when they hold the same keys mapped to equal values.</summary>
    public bool Equals(NonEmptyDictionary<TKey, TValue>? other)
    {
        if (other is null || other.Count != Count)
            return false;

        foreach (KeyValuePair<TKey, TValue> pair in _items)
        {
            if (!other._items.TryGetValue(pair.Key, out TValue? value) || !EqualityComparer<TValue>.Default.Equals(pair.Value, value))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NonEmptyDictionary<TKey, TValue> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 0;
        foreach (KeyValuePair<TKey, TValue> pair in _items)
            hash ^= HashCode.Combine(_items.Comparer.GetHashCode(pair.Key), pair.Value);
        return HashCode.Combine(hash, Count);
    }

    /// <inheritdoc />
    public override string ToString() => $"NonEmptyDictionary: {{{string.Join(", ", _items.Select(static p => $"{p.Key}: {p.Value}"))}}}";

    private void AddCore(TKey key, TValue value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(key, paramName);
        ArgumentNullException.ThrowIfNull(value, paramName);
        _items.Add(key, value);
    }
}

/// <summary>Factories for <see cref="NonEmptyDictionary{TKey, TValue}"/>.</summary>
public static class NonEmptyDictionary
{
    /// <summary>Creates a dictionary with a single entry.</summary>
    public static NonEmptyDictionary<TKey, TValue> Of<TKey, TValue>(TKey key, TValue value) where TKey : notnull => new(key, value);

    /// <summary>Creates a dictionary from one entry and optional others.</summary>
    public static NonEmptyDictionary<TKey, TValue> OfPairs<TKey, TValue>((TKey Key, TValue Value) first, params (TKey Key, TValue Value)[] rest) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(rest);
        NonEmptyDictionary<TKey, TValue> result = new(first.Key, first.Value);
        foreach ((TKey key, TValue value) in rest)
            result.Add(key, value);
        return result;
    }

    /// <summary>Creates a dictionary from a sequence of pairs.</summary>
    public static NonEmptyDictionary<TKey, TValue> From<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        => NonEmptyDictionary<TKey, TValue>.From(source, comparer);

    /// <summary>Creates a dictionary from a sequence of pairs.</summary>
    public static NonEmptyDictionary<TKey, TValue> ToNonEmptyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        => NonEmptyDictionary<TKey, TValue>.From(source, comparer);

    /// <summary>Creates a dictionary by projecting keys and values from a sequence.</summary>
    public static NonEmptyDictionary<TKey, TValue> ToNonEmptyDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        => NonEmptyDictionary<TKey, TValue>.From(source, keySelector, valueSelector, comparer);

    /// <summary>Creates a dictionary by projecting keys from a non-empty list. Keys must be unique.</summary>
    public static NonEmptyDictionary<TKey, T> ToNonEmptyDictionary<T, TKey>(this NonEmptyList<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        => NonEmptyDictionary<TKey, T>.From(source, keySelector, static x => x, comparer);

    /// <summary>Attempts to create a dictionary from a sequence of pairs.</summary>
    public static bool TryToNonEmptyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>? source, [NotNullWhen(true)] out NonEmptyDictionary<TKey, TValue>? result) where TKey : notnull
        => NonEmptyDictionary<TKey, TValue>.TryFrom(source, out result);
}

/// <summary>JSON converter factory for <see cref="NonEmptyDictionary{TKey, TValue}"/>.</summary>
public sealed class NonEmptyDictionaryJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(NonEmptyDictionary<,>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type[] args = typeToConvert.GetGenericArguments();
        Type converterType = typeof(NonEmptyDictionaryJsonConverter<,>).MakeGenericType(args[0], args[1]);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// JSON converter for <see cref="NonEmptyDictionary{TKey, TValue}"/>. Delegates to the built-in dictionary converter so key
/// handling (strings, numbers, enums, custom key converters) matches <see cref="Dictionary{TKey, TValue}"/>, then checks non-emptiness.
/// </summary>
public sealed class NonEmptyDictionaryJsonConverter<TKey, TValue> : JsonConverter<NonEmptyDictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <inheritdoc />
    public override NonEmptyDictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        Dictionary<TKey, TValue>? items = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(ref reader, options);
        if (items is null || items.Count == 0)
            throw new JsonException("NonEmptyDictionary cannot be empty");

        if (items.Values.Any(static v => v is null))
            throw new JsonException("NonEmptyDictionary cannot contain null values");

        return NonEmptyDictionary<TKey, TValue>.From(items);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, NonEmptyDictionary<TKey, TValue> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.ToDictionary(), options);
}
