using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// An immutable generic list that guarantees at least one element.
/// All modification operations return new instances, leaving the original unchanged.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
[JsonConverter(typeof(ImmutableNonEmptyListJsonConverterFactory))]
public sealed class ImmutableNonEmptyList<T> : IReadOnlyList<T>, IEquatable<ImmutableNonEmptyList<T>>
{
    private readonly T[] _items;
    private ImmutableNonEmptyList<T>? _cachedTail;
    private ImmutableNonEmptyList<T>? _cachedInit;

    #region Properties

    /// <summary>
    /// Gets the number of elements in the list.
    /// </summary>
    public int Count => _items.Length;

    /// <summary>
    /// Gets the first element of the list.
    /// </summary>
    public T Head => _items[0];

    /// <summary>
    /// Gets the first element of the list.
    /// </summary>
    public T First => Head;

    /// <summary>
    /// Gets the last element of the list.
    /// </summary>
    public T Last => _items[^1];

    /// <summary>
    /// Gets the list of elements excluding the first (tail of the list).
    /// Returns null if there is only one element.
    /// </summary>
    public ImmutableNonEmptyList<T>? Tail
    {
        get
        {
            if (_cachedTail != null) return _cachedTail;
            if (Count == 1) return null;

            _cachedTail = new ImmutableNonEmptyList<T>(_items[1..]);
            return _cachedTail;
        }
    }

    /// <summary>
    /// Gets the list of elements excluding the last (init of the list).
    /// Returns null if there is only one element.
    /// </summary>
    public ImmutableNonEmptyList<T>? Init
    {
        get
        {
            if (_cachedInit != null) return _cachedInit;
            if (Count == 1) return null;

            _cachedInit = new ImmutableNonEmptyList<T>(_items[..^1]);
            return _cachedInit;
        }
    }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index] => _items[index];

    #endregion

    #region Constructors

    private ImmutableNonEmptyList(T[] items)
    {
        _items = items;
    }

    /// <summary>
    /// Creates a new ImmutableNonEmptyList with the first element and additional optional elements.
    /// </summary>
    /// <param name="firstItem">The first item in the list (required).</param>
    /// <param name="otherItems">Optional additional items to include in the list.</param>
    /// <exception cref="ArgumentNullException">Thrown if any item is null.</exception>
    public ImmutableNonEmptyList(T firstItem, params T[] otherItems)
        : this(firstItem, otherItems.AsEnumerable()) { }

    /// <summary>
    /// Creates a new ImmutableNonEmptyList with the first element and an IEnumerable of other elements.
    /// </summary>
    /// <param name="firstItem">The first item in the list (required).</param>
    /// <param name="otherItems">An IEnumerable containing the other items in the list.</param>
    /// <exception cref="ArgumentNullException">Thrown if any item is null.</exception>
    public ImmutableNonEmptyList(T firstItem, IEnumerable<T> otherItems)
    {
        if (firstItem is null)
            throw new ArgumentNullException(nameof(firstItem), "First item cannot be null");

        IList<T> items = otherItems as IList<T> ?? otherItems?.ToList() ?? new List<T>();

        if (items.Any(item => item is null))
            throw new ArgumentNullException(nameof(otherItems), "Other items cannot contain null values");

        _items = new T[1 + items.Count];
        _items[0] = firstItem;

        for (int i = 0; i < items.Count; i++)
        {
            _items[i + 1] = items[i];
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates an ImmutableNonEmptyList from an existing IEnumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to create the list from.</param>
    /// <returns>An ImmutableNonEmptyList containing the elements.</returns>
    /// <exception cref="ArgumentException">Thrown if the enumerable is null or empty.</exception>
    public static ImmutableNonEmptyList<T> From(IEnumerable<T> enumerable)
    {
        if (enumerable is null)
            throw new ArgumentException("Cannot create an ImmutableNonEmptyList from null or empty", nameof(enumerable));

        IList<T> items = enumerable as IList<T> ?? enumerable.ToList();

        if (items.Count == 0)
            throw new ArgumentException("Cannot create an ImmutableNonEmptyList from null or empty", nameof(enumerable));

        return new ImmutableNonEmptyList<T>(items[0], items.Skip(1));
    }

    /// <summary>
    /// Attempts to create an ImmutableNonEmptyList from an existing IEnumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to create the list from.</param>
    /// <param name="result">When successful, contains the list; otherwise, null.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    public static bool TryFrom(IEnumerable<T>? enumerable, out ImmutableNonEmptyList<T>? result)
    {
        result = null;

        if (enumerable is null)
            return false;

        IList<T> items = enumerable as IList<T> ?? enumerable.ToList();

        if (items.Count == 0)
            return false;

        if (items.Any(item => item is null))
            return false;

        result = new ImmutableNonEmptyList<T>(items[0], items.Skip(1));
        return true;
    }

    /// <summary>
    /// Creates an ImmutableNonEmptyList containing a single element.
    /// </summary>
    /// <param name="item">The single item.</param>
    /// <returns>An ImmutableNonEmptyList with one element.</returns>
    public static ImmutableNonEmptyList<T> Singleton(T item) => new(item);

    /// <summary>
    /// Creates an ImmutableNonEmptyList by repeating an element.
    /// </summary>
    /// <param name="item">The item to repeat.</param>
    /// <param name="count">The number of times to repeat (must be at least 1).</param>
    /// <returns>An ImmutableNonEmptyList with the repeated element.</returns>
    public static ImmutableNonEmptyList<T> Repeat(T item, int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

        ArgumentNullException.ThrowIfNull(item);

        return new ImmutableNonEmptyList<T>(item, Enumerable.Repeat(item, count - 1));
    }

    #endregion

    #region Immutable Operations

    /// <summary>
    /// Returns a new list with the item appended.
    /// </summary>
    /// <param name="item">The item to append.</param>
    /// <returns>A new ImmutableNonEmptyList with the item added at the end.</returns>
    public ImmutableNonEmptyList<T> Append(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        T[] newItems = new T[_items.Length + 1];
        Array.Copy(_items, newItems, _items.Length);
        newItems[^1] = item;

        return new ImmutableNonEmptyList<T>(newItems);
    }

    /// <summary>
    /// Returns a new list with the items appended.
    /// </summary>
    /// <param name="items">The items to append.</param>
    /// <returns>A new ImmutableNonEmptyList with the items added at the end.</returns>
    public ImmutableNonEmptyList<T> AppendRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        IList<T> itemsList = items as IList<T> ?? items.ToList();

        if (itemsList.Any(item => item is null))
            throw new ArgumentNullException(nameof(items), "Items cannot contain null values");

        if (itemsList.Count == 0)
            return this;

        T[] newItems = new T[_items.Length + itemsList.Count];
        Array.Copy(_items, newItems, _items.Length);

        for (int i = 0; i < itemsList.Count; i++)
        {
            newItems[_items.Length + i] = itemsList[i];
        }

        return new ImmutableNonEmptyList<T>(newItems);
    }

    /// <summary>
    /// Returns a new list with the item prepended.
    /// </summary>
    /// <param name="item">The item to prepend.</param>
    /// <returns>A new ImmutableNonEmptyList with the item added at the beginning.</returns>
    public ImmutableNonEmptyList<T> Prepend(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        T[] newItems = new T[_items.Length + 1];
        newItems[0] = item;
        Array.Copy(_items, 0, newItems, 1, _items.Length);

        return new ImmutableNonEmptyList<T>(newItems);
    }

    /// <summary>
    /// Returns a new list with the first element removed.
    /// Returns null if the list has only one element.
    /// </summary>
    /// <returns>A new ImmutableNonEmptyList without the first element, or null.</returns>
    public ImmutableNonEmptyList<T>? RemoveFirst()
    {
        return Tail;
    }

    /// <summary>
    /// Returns a new list with the last element removed.
    /// Returns null if the list has only one element.
    /// </summary>
    /// <returns>A new ImmutableNonEmptyList without the last element, or null.</returns>
    public ImmutableNonEmptyList<T>? RemoveLast()
    {
        return Init;
    }

    /// <summary>
    /// Returns a new list with the element at the specified index removed.
    /// Returns null if removing the element would leave the list empty.
    /// </summary>
    /// <param name="index">The index of the element to remove.</param>
    /// <returns>A new ImmutableNonEmptyList without the element, or null.</returns>
    public ImmutableNonEmptyList<T>? RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_items.Length == 1)
            return null;

        T[] newItems = new T[_items.Length - 1];
        Array.Copy(_items, 0, newItems, 0, index);
        Array.Copy(_items, index + 1, newItems, index, _items.Length - index - 1);

        return new ImmutableNonEmptyList<T>(newItems);
    }

    /// <summary>
    /// Returns a new list with the first occurrence of the item removed.
    /// Returns null if removing the item would leave the list empty.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>A new ImmutableNonEmptyList without the item, or null, or this if item not found.</returns>
    public ImmutableNonEmptyList<T>? Remove(T item)
    {
        int index = Array.IndexOf(_items, item);
        if (index < 0)
            return this;

        return RemoveAt(index);
    }

    /// <summary>
    /// Returns a new list with the element at the specified index replaced.
    /// </summary>
    /// <param name="index">The index of the element to replace.</param>
    /// <param name="item">The new item.</param>
    /// <returns>A new ImmutableNonEmptyList with the element replaced.</returns>
    public ImmutableNonEmptyList<T> SetItem(int index, T item)
    {
        if (index < 0 || index >= _items.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        ArgumentNullException.ThrowIfNull(item);

        T[] newItems = new T[_items.Length];
        Array.Copy(_items, newItems, _items.Length);
        newItems[index] = item;

        return new ImmutableNonEmptyList<T>(newItems);
    }

    /// <summary>
    /// Returns a new list with an item inserted at the specified index.
    /// </summary>
    /// <param name="index">The index at which to insert.</param>
    /// <param name="item">The item to insert.</param>
    /// <returns>A new ImmutableNonEmptyList with the item inserted.</returns>
    public ImmutableNonEmptyList<T> Insert(int index, T item)
    {
        if (index < 0 || index > _items.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        ArgumentNullException.ThrowIfNull(item);

        T[] newItems = new T[_items.Length + 1];
        Array.Copy(_items, 0, newItems, 0, index);
        newItems[index] = item;
        Array.Copy(_items, index, newItems, index + 1, _items.Length - index);

        return new ImmutableNonEmptyList<T>(newItems);
    }

    #endregion

    #region Functional Operations

    /// <summary>
    /// Projects each element into a new form.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="selector">A transform function.</param>
    /// <returns>A new ImmutableNonEmptyList with the transformed elements.</returns>
    public ImmutableNonEmptyList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        TResult[] results = _items.Select(selector).ToArray();
        return new ImmutableNonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Projects each element with its index into a new form.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="selector">A transform function with index.</param>
    /// <returns>A new ImmutableNonEmptyList with the transformed elements.</returns>
    public ImmutableNonEmptyList<TResult> MapWithIndex<TResult>(Func<T, int, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        TResult[] results = _items.Select((item, index) => selector(item, index)).ToArray();
        return new ImmutableNonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Projects each element to an ImmutableNonEmptyList and flattens.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="selector">A transform function returning an ImmutableNonEmptyList.</param>
    /// <returns>A new flattened ImmutableNonEmptyList.</returns>
    public ImmutableNonEmptyList<TResult> FlatMap<TResult>(Func<T, ImmutableNonEmptyList<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        TResult[] results = _items.SelectMany(selector).ToArray();
        return new ImmutableNonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Applies a reducer function to produce a single value.
    /// </summary>
    /// <param name="reducer">A function that combines two elements.</param>
    /// <returns>The reduced value.</returns>
    public T Reduce(Func<T, T, T> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);
        return _items.Aggregate(reducer);
    }

    /// <summary>
    /// Applies a folder function with a seed value.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="folder">A function that combines the accumulator and each element.</param>
    /// <returns>The folded result.</returns>
    public TResult Fold<TResult>(TResult seed, Func<TResult, T, TResult> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return _items.Aggregate(seed, folder);
    }

    /// <summary>
    /// Merges with another list into tuples.
    /// </summary>
    /// <typeparam name="TOther">The type of elements in the other list.</typeparam>
    /// <param name="other">The other list to zip with.</param>
    /// <returns>An ImmutableNonEmptyList of tuples.</returns>
    public ImmutableNonEmptyList<(T First, TOther Second)> Zip<TOther>(ImmutableNonEmptyList<TOther> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        (T, TOther)[] zipped = _items.Zip(other, (a, b) => (a, b)).ToArray();
        return new ImmutableNonEmptyList<(T, TOther)>(zipped[0], zipped.Skip(1));
    }

    /// <summary>
    /// Matches based on whether the list has one or multiple elements.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="single">Function for single element.</param>
    /// <param name="multiple">Function for multiple elements.</param>
    /// <returns>The result of the matching function.</returns>
    public TResult Match<TResult>(Func<T, TResult> single, Func<T, ImmutableNonEmptyList<T>, TResult> multiple)
    {
        ArgumentNullException.ThrowIfNull(single);
        ArgumentNullException.ThrowIfNull(multiple);

        return Tail is null
            ? single(Head)
            : multiple(Head, Tail);
    }

    #endregion

    #region Collection Operations

    /// <summary>
    /// Concatenates with another ImmutableNonEmptyList.
    /// </summary>
    /// <param name="other">The other list.</param>
    /// <returns>A new concatenated ImmutableNonEmptyList.</returns>
    public ImmutableNonEmptyList<T> Concat(ImmutableNonEmptyList<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        T[] newItems = new T[_items.Length + other.Count];
        Array.Copy(_items, newItems, _items.Length);
        Array.Copy(other._items, 0, newItems, _items.Length, other.Count);

        return new ImmutableNonEmptyList<T>(newItems);
    }

    /// <summary>
    /// Returns a new reversed ImmutableNonEmptyList.
    /// </summary>
    /// <returns>A new reversed ImmutableNonEmptyList.</returns>
    public ImmutableNonEmptyList<T> Reverse()
    {
        T[] reversed = ((IEnumerable<T>)_items).Reverse().ToArray();
        return new ImmutableNonEmptyList<T>(reversed);
    }

    /// <summary>
    /// Returns a new ImmutableNonEmptyList with distinct elements.
    /// </summary>
    /// <returns>A new ImmutableNonEmptyList with unique elements.</returns>
    public ImmutableNonEmptyList<T> Distinct()
    {
        T[] distinct = _items.Distinct().ToArray();
        return new ImmutableNonEmptyList<T>(distinct);
    }

    /// <summary>
    /// Takes the first n elements.
    /// </summary>
    /// <param name="count">The number of elements to take.</param>
    /// <returns>A new ImmutableNonEmptyList, or null if count &lt; 1.</returns>
    public ImmutableNonEmptyList<T>? Take(int count)
    {
        if (count < 1) return null;

        T[] taken = _items.Take(count).ToArray();
        return new ImmutableNonEmptyList<T>(taken);
    }

    /// <summary>
    /// Skips the first n elements.
    /// </summary>
    /// <param name="count">The number of elements to skip.</param>
    /// <returns>A new ImmutableNonEmptyList, or null if no elements remain.</returns>
    public ImmutableNonEmptyList<T>? Skip(int count)
    {
        if (count >= _items.Length) return null;

        T[] skipped = _items.Skip(count).ToArray();
        if (skipped.Length == 0) return null;

        return new ImmutableNonEmptyList<T>(skipped);
    }

    /// <summary>
    /// Inserts a separator between each element.
    /// </summary>
    /// <param name="separator">The separator to insert.</param>
    /// <returns>A new ImmutableNonEmptyList with separators.</returns>
    public ImmutableNonEmptyList<T> Intersperse(T separator)
    {
        ArgumentNullException.ThrowIfNull(separator);

        if (_items.Length == 1)
            return this;

        T[] result = new T[_items.Length * 2 - 1];
        result[0] = _items[0];

        for (int i = 1; i < _items.Length; i++)
        {
            result[i * 2 - 1] = separator;
            result[i * 2] = _items[i];
        }

        return new ImmutableNonEmptyList<T>(result);
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Converts to a mutable NonEmptyList.
    /// </summary>
    /// <returns>A mutable NonEmptyList with the same elements.</returns>
    public NonEmptyList<T> ToMutable()
    {
        return new NonEmptyList<T>(_items[0], _items.Skip(1));
    }

    /// <summary>
    /// Converts to an array.
    /// </summary>
    /// <returns>An array with all elements.</returns>
    public T[] ToArray()
    {
        T[] result = new T[_items.Length];
        Array.Copy(_items, result, _items.Length);
        return result;
    }

    /// <summary>
    /// Converts to a List.
    /// </summary>
    /// <returns>A List with all elements.</returns>
    public List<T> ToList() => new(_items);

    #endregion

    #region Deconstruction

    /// <summary>
    /// Deconstructs into head and tail.
    /// </summary>
    /// <param name="head">The first element.</param>
    /// <param name="tail">The remaining elements.</param>
    public void Deconstruct(out T head, out ImmutableNonEmptyList<T>? tail)
    {
        head = Head;
        tail = Tail;
    }

    #endregion

    #region Equality

    /// <summary>
    /// Determines equality with another ImmutableNonEmptyList.
    /// </summary>
    /// <param name="other">The other list.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public bool Equals(ImmutableNonEmptyList<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_items.Length != other._items.Length) return false;

        return _items.SequenceEqual(other._items);
    }

    /// <summary>
    /// Determines equality with an object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ImmutableNonEmptyList<T> other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code.
    /// </summary>
    /// <returns>A hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (T item in _items)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(ImmutableNonEmptyList<T>? left, ImmutableNonEmptyList<T>? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(ImmutableNonEmptyList<T>? left, ImmutableNonEmptyList<T>? right)
    {
        return !(left == right);
    }

    #endregion

    #region IEnumerable

    /// <summary>
    /// Returns an enumerator for the list.
    /// </summary>
    /// <returns>An enumerator.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    #endregion

    /// <summary>
    /// Returns a string representation.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString() => $"ImmutableNonEmptyList: [{string.Join(", ", _items)}]";
}

/// <summary>
/// Extension methods for converting to ImmutableNonEmptyList.
/// </summary>
public static class ImmutableNonEmptyListExtensions
{
    /// <summary>
    /// Converts a NonEmptyList to an ImmutableNonEmptyList.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <returns>An ImmutableNonEmptyList with the same elements.</returns>
    public static ImmutableNonEmptyList<T> ToImmutable<T>(this NonEmptyList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new ImmutableNonEmptyList<T>(source.Head, source.Skip(1));
    }
}

/// <summary>
/// JSON converter factory for ImmutableNonEmptyList types.
/// </summary>
public class ImmutableNonEmptyListJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        return typeToConvert.GetGenericTypeDefinition() == typeof(ImmutableNonEmptyList<>);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(ImmutableNonEmptyListJsonConverter<>).MakeGenericType(elementType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// JSON converter for ImmutableNonEmptyList&lt;T&gt;.
/// </summary>
/// <typeparam name="T">The type of elements.</typeparam>
public class ImmutableNonEmptyListJsonConverter<T> : JsonConverter<ImmutableNonEmptyList<T>>
{
    /// <inheritdoc />
    public override ImmutableNonEmptyList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array");

        List<T> items = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            T? item = JsonSerializer.Deserialize<T>(ref reader, options);
            if (item is null)
                throw new JsonException("ImmutableNonEmptyList cannot contain null elements");

            items.Add(item);
        }

        if (items.Count == 0)
            throw new JsonException("ImmutableNonEmptyList cannot be empty");

        return new ImmutableNonEmptyList<T>(items[0], items.Skip(1));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ImmutableNonEmptyList<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (T item in value)
        {
            JsonSerializer.Serialize(writer, item, options);
        }

        writer.WriteEndArray();
    }
}
