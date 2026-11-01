// Ignore Spelling: Deconstruct Intersperse Parsable Unzip

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// A generic list that guarantees at least one element.
/// Provides convenient access to the first (Head) and last elements, and can return the tail (rest of the list).
/// </summary>
/// <remarks>
/// <para>
/// The list wraps its storage instead of inheriting from <see cref="List{T}"/>, so the non-empty
/// guarantee holds no matter which interface the list is accessed through: <see cref="IList{T}"/>,
/// <see cref="ICollection{T}"/>, <see cref="IList"/> and <see cref="IReadOnlyList{T}"/> all route
/// through the same guarded operations.
/// </para>
/// <para>
/// Supports collection expressions (<c>NonEmptyList&lt;int&gt; xs = [1, 2, 3];</c>), list patterns,
/// range slicing, and <see cref="ISpanParsable{TSelf}"/> so it can be bound directly from query strings.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of elements in the list.</typeparam>
[JsonConverter(typeof(NonEmptyListJsonConverterFactory))]
[TypeConverter(typeof(NonEmptyListTypeConverter))]
[CollectionBuilder(typeof(NonEmptyList), nameof(NonEmptyList.Create))]
[DebuggerDisplay("Count = {Count}, Head = {Head}")]
[DebuggerTypeProxy(typeof(NonEmptyListDebugView<>))]
public class NonEmptyList<T> :
    IList<T>,
    IReadOnlyList<T>,
    IList,
    IEquatable<NonEmptyList<T>>,
    ISpanParsable<NonEmptyList<T>>
{
    private readonly List<T> _items;

    #region Properties

    /// <summary>
    /// Gets the first element of the list (same as First).
    /// </summary>
    public T Head => _items[0];

    /// <summary>
    /// Gets a new list of the elements excluding the first (tail of the list), or null if there is only one element.
    /// Each read returns a fresh copy, so changing it never affects this list or a later read.
    /// </summary>
    public NonEmptyList<T>? Tail => Count == 1 ? null : new NonEmptyList<T>(_items.GetRange(1, Count - 1));

    /// <summary>
    /// Gets a new list of the elements excluding the last (init of the list), or null if there is only one element.
    /// Each read returns a fresh copy, so changing it never affects this list or a later read.
    /// </summary>
    public NonEmptyList<T>? Init => Count == 1 ? null : new NonEmptyList<T>(_items.GetRange(0, Count - 1));

    /// <summary>
    /// Gets the first element of the list.
    /// </summary>
    public T First => Head;

    /// <summary>
    /// Gets the last element of the list.
    /// </summary>
    public T Last => _items[^1];

    /// <summary>
    /// Gets the number of elements in the list. Always at least 1.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets or sets the total number of elements the internal storage can hold without resizing.
    /// </summary>
    public int Capacity
    {
        get => _items.Capacity;
        set => _items.Capacity = value;
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentNullException">Thrown if trying to set a null value.</exception>
    public T this[int index]
    {
        get => _items[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureMutable();
            ValidateReplacement(index, value);
            _items[index] = value;
        }
    }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to get.</param>
    public T this[Index index] => _items[index.GetOffset(Count)];

    /// <summary>
    /// Gets a copy of the elements in the specified range. Enables <c>list[1..]</c> and list patterns such as
    /// <c>[var head, .. var rest]</c>.
    /// </summary>
    /// <param name="range">The range of elements to copy.</param>
    public List<T> this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(Count);
            return _items.GetRange(offset, length);
        }
    }

    bool ICollection<T>.IsReadOnly => IsReadOnly;
    bool IList.IsReadOnly => IsReadOnly;
    bool IList.IsFixedSize => false;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

    object? IList.this[int index]
    {
        get => _items[index];
        set => this[index] = CastItem(value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new NonEmptyList with the first element and additional optional elements.
    /// </summary>
    /// <param name="firstItem">The first item in the list (required).</param>
    /// <param name="otherItems">Optional additional items to include in the list.</param>
    public NonEmptyList(T firstItem, params T[] otherItems) : this(firstItem, otherItems.AsEnumerable()) { }

    /// <summary>
    /// Creates a new NonEmptyList with the first element and additional optional elements without allocating an array.
    /// </summary>
    /// <param name="firstItem">The first item in the list (required).</param>
    /// <param name="otherItems">Optional additional items to include in the list.</param>
    /// <exception cref="ArgumentNullException">Thrown if the first item or any of the other items are null.</exception>
    public NonEmptyList(T firstItem, params ReadOnlySpan<T> otherItems)
    {
        if (firstItem is null)
            throw new ArgumentNullException(nameof(firstItem), "First item cannot be null");

        _items = new List<T>(otherItems.Length + 1) { firstItem };

        foreach (T item in otherItems)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(otherItems), "Other items cannot contain null values");

            _items.Add(item);
        }
    }

    /// <summary>
    /// Creates a new NonEmptyList with the first element and an IEnumerable of other elements.
    /// </summary>
    /// <param name="firstItem">The first item in the list (required).</param>
    /// <param name="otherItems">An IEnumerable containing the other items in the list.</param>
    /// <exception cref="ArgumentNullException">Thrown if the first item or any of the other items are null.</exception>
    public NonEmptyList(T firstItem, IEnumerable<T> otherItems)
    {
        if (firstItem is null)
            throw new ArgumentNullException(nameof(firstItem), "First item cannot be null");

        if (otherItems is null)
        {
            _items = new List<T>(1) { firstItem };
            return;
        }

        IList<T> items = otherItems as IList<T> ?? otherItems.ToList();

        if (items.Any(item => item is null))
            throw new ArgumentNullException(nameof(otherItems), "Other items cannot contain null values");

        _items = new List<T>(items.Count + 1) { firstItem };
        _items.AddRange(items);
    }

    /// <summary>
    /// Creates a new NonEmptyList by copying a sequence.
    /// </summary>
    /// <param name="items">The elements. Must contain at least one element and no nulls.</param>
    /// <exception cref="ArgumentNullException">Thrown if the sequence is null or contains null.</exception>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty.</exception>
    public NonEmptyList(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<T> copy = new(items);
        if (copy.Count == 0)
            throw new ArgumentException("Sequence cannot be empty", nameof(items));

        if (copy.Exists(static item => item is null))
            throw new ArgumentNullException(nameof(items), "Sequence cannot contain null values");

        _items = copy;
    }

    /// <summary>
    /// Wraps an already validated, non-empty, null-free list without copying it.
    /// Callers must not keep a reference to <paramref name="trustedItems"/>.
    /// </summary>
    private protected NonEmptyList(List<T> trustedItems)
    {
        Debug.Assert(trustedItems.Count > 0, "Trusted list must be non-empty");
        _items = trustedItems;
    }

    /// <summary>
    /// Creates a list from an existing non-empty list, sharing no storage. Intended for derived types that add
    /// invariants on top of the non-empty guarantee; the derived constructor validates its own invariant afterwards.
    /// </summary>
    /// <param name="source">The list to copy.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    protected NonEmptyList(NonEmptyList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _items = new List<T>(source._items);
    }

    /// <summary>
    /// Wraps a list that is known to be non-empty. Validates that no element is null.
    /// The list is used as the backing store, so callers must not keep a reference to it.
    /// </summary>
    internal static NonEmptyList<T> FromTrustedList(List<T> items)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("Internal error: expected a non-empty list.");

        foreach (T item in items)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(items), "NonEmptyList cannot contain null values");
        }

        return new NonEmptyList<T>(items);
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a NonEmptyList from an existing IEnumerable, ensuring the enumerable is not null or empty.
    /// </summary>
    /// <param name="enumerable">The enumerable to create the NonEmptyList from.</param>
    /// <returns>A NonEmptyList containing the elements of the enumerable.</returns>
    /// <exception cref="ArgumentException">Thrown if the enumerable is null or empty.</exception>
    public static NonEmptyList<T> From(IEnumerable<T> enumerable)
    {
        if (enumerable is null)
            throw new ArgumentException("Cannot create a NonEmptyList from null or empty", nameof(enumerable));

        List<T> items = new(enumerable);

        if (items.Count == 0)
            throw new ArgumentException("Cannot create a NonEmptyList from null or empty", nameof(enumerable));

        return FromTrustedList(items);
    }

    /// <summary>
    /// Creates a NonEmptyList from a span, ensuring the span is not empty.
    /// </summary>
    /// <param name="items">The items to copy.</param>
    /// <exception cref="ArgumentException">Thrown if the span is empty.</exception>
    public static NonEmptyList<T> FromSpan(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty)
            throw new ArgumentException("Cannot create a NonEmptyList from an empty span", nameof(items));

        return new NonEmptyList<T>(items[0], items[1..]);
    }

    /// <summary>
    /// Attempts to create a NonEmptyList from an existing IEnumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to create the NonEmptyList from.</param>
    /// <param name="result">When successful, contains the NonEmptyList; otherwise, null.</param>
    /// <returns>True if the NonEmptyList was created successfully; otherwise, false.</returns>
    public static bool TryFrom(IEnumerable<T>? enumerable, [NotNullWhen(true)] out NonEmptyList<T>? result)
    {
        result = null;

        if (enumerable is null)
            return false;

        List<T> items = new(enumerable);

        if (items.Count == 0)
            return false;

        foreach (T item in items)
        {
            if (item is null)
                return false;
        }

        result = new NonEmptyList<T>(items);
        return true;
    }

    /// <summary>
    /// Creates a NonEmptyList containing a single element.
    /// </summary>
    /// <param name="item">The single item.</param>
    /// <returns>A NonEmptyList with one element.</returns>
    public static NonEmptyList<T> Singleton(T item) => new(item);

    /// <summary>
    /// Creates a NonEmptyList by repeating an element a specified number of times.
    /// </summary>
    /// <param name="item">The item to repeat.</param>
    /// <param name="count">The number of times to repeat (must be at least 1).</param>
    /// <returns>A NonEmptyList with the repeated element.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is less than 1.</exception>
    public static NonEmptyList<T> Repeat(T item, int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");

        ArgumentNullException.ThrowIfNull(item);

        List<T> items = new(count);
        for (int i = 0; i < count; i++)
            items.Add(item);

        return new NonEmptyList<T>(items);
    }

    #endregion

    #region Parsing

    /// <summary>
    /// Parses a comma-separated string into a NonEmptyList. Elements are parsed with the element type's
    /// <see cref="ISpanParsable{TSelf}"/> or <see cref="IParsable{TSelf}"/> implementation, or its <see cref="TypeConverter"/>.
    /// This is the method ASP.NET Core uses to bind the list from a query string or route value.
    /// </summary>
    /// <param name="s">The text to parse, for example <c>"1,2,3"</c>.</param>
    /// <param name="provider">The culture used to parse the elements.</param>
    /// <exception cref="FormatException">Thrown if the text is empty or any element fails to parse.</exception>
    public static NonEmptyList<T> Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);
        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses a comma-separated span of characters into a NonEmptyList.
    /// </summary>
    /// <param name="s">The text to parse, for example <c>"1,2,3"</c>.</param>
    /// <param name="provider">The culture used to parse the elements.</param>
    /// <exception cref="FormatException">Thrown if the text is empty or any element fails to parse.</exception>
    public static NonEmptyList<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        return TryParse(s, provider, out NonEmptyList<T>? result)
            ? result
            : throw new FormatException($"'{s}' is not a valid comma-separated, non-empty list of {typeof(T).Name}.");
    }

    /// <summary>
    /// Attempts to parse a comma-separated string into a NonEmptyList.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to parse the elements.</param>
    /// <param name="result">The parsed list, or null when parsing fails.</param>
    /// <returns>True if the text contained at least one parsable element; otherwise, false.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonEmptyList<T> result)
    {
        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Attempts to parse a comma-separated string into a NonEmptyList using the current culture.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="result">The parsed list, or null when parsing fails.</param>
    /// <returns>True if the text contained at least one parsable element; otherwise, false.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out NonEmptyList<T> result)
    {
        return TryParse(s.AsSpan(), null, out result);
    }

    /// <summary>
    /// Attempts to parse a comma-separated span of characters into a NonEmptyList.
    /// </summary>
    /// <param name="s">The text to parse.</param>
    /// <param name="provider">The culture used to parse the elements.</param>
    /// <param name="result">The parsed list, or null when parsing fails.</param>
    /// <returns>True if the text contained at least one parsable element; otherwise, false.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonEmptyList<T> result)
    {
        result = null;

        if (s.IsWhiteSpace())
            return false;

        IElementParser<T>? parser = ElementParser<T>.Instance;
        if (parser is null)
            return false;

        List<T> items = new();
        ReadOnlySpan<char> remaining = s;

        while (true)
        {
            int comma = remaining.IndexOf(',');
            ReadOnlySpan<char> token = (comma < 0 ? remaining : remaining[..comma]).Trim();

            if (token.IsEmpty)
                return false;

            if (!parser.TryParse(token, provider, out T? value) || value is null)
                return false;

            items.Add(value);

            if (comma < 0)
                break;

            remaining = remaining[(comma + 1)..];
        }

        result = new NonEmptyList<T>(items);
        return true;
    }

    #endregion

    #region Add/Insert Operations

    /// <summary>
    /// Adds an item to the end of the NonEmptyList, ensuring the item is not null.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureMutable();
        ValidateItem(item);
        ValidateCount(Count + 1);
        _items.Add(item);
    }

    /// <summary>
    /// Adds a range of items to the NonEmptyList, ensuring the collection is not null or empty.
    /// </summary>
    /// <param name="collection">The collection of items to add.</param>
    /// <exception cref="ArgumentException">Thrown if the collection is empty or contains null values.</exception>
    public void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        List<T> items = collection as List<T> ?? collection.ToList();

        if (items.Count == 0)
            throw new ArgumentException("Collection cannot be empty", nameof(collection));

        if (items.Exists(static item => item is null))
            throw new ArgumentNullException(nameof(collection), "Collection cannot contain null values");

        EnsureMutable();
        ValidateRange(items);
        ValidateCount(Count + items.Count);
        _items.AddRange(items);
    }

    /// <summary>
    /// Inserts an item at the beginning of the NonEmptyList, mutating it in place.
    /// Use <see cref="Prepend"/> for a non-mutating version that returns a new list.
    /// </summary>
    /// <param name="item">The item to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public void AddFirst(T item) => Insert(0, item);

    /// <summary>
    /// Inserts an item at the specified index in the NonEmptyList, ensuring the item is not null.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public void Insert(int index, T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureMutable();
        ValidateItem(item);
        ValidateCount(Count + 1);
        _items.Insert(index, item);
    }

    /// <summary>
    /// Inserts a range of items at the specified index in the NonEmptyList, ensuring no items are null.
    /// </summary>
    /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
    /// <param name="collection">The collection of items to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown if the collection is null or contains null values.</exception>
    /// <exception cref="ArgumentException">Thrown if the collection is empty.</exception>
    public void InsertRange(int index, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        List<T> items = collection as List<T> ?? collection.ToList();

        if (items.Count == 0)
            throw new ArgumentException("Collection cannot be empty", nameof(collection));

        if (items.Exists(static item => item is null))
            throw new ArgumentNullException(nameof(collection), "Collection cannot contain null values");

        EnsureMutable();
        ValidateRange(items);
        ValidateCount(Count + items.Count);
        _items.InsertRange(index, items);
    }

    #endregion

    #region Remove Operations

    /// <summary>
    /// Clearing is not supported because a NonEmptyList cannot be empty.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public void Clear()
    {
        EnsureMutable();
        throw new NotSupportedException("Cannot clear a NonEmptyList");
    }

    /// <summary>
    /// Removes the first occurrence of an item from the NonEmptyList, ensuring the list never becomes empty.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was found and removed; false if it was not present.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if removing the item would empty the list.</exception>
    public bool Remove(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureMutable();

        int index = _items.IndexOf(item);
        if (index < 0)
            return false;

        if (Count == 1)
            throw new InvalidOperationException("Cannot use Remove method while the NonEmptyList contains only one item.");

        ValidateCount(Count - 1);
        _items.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes an item at a specified index from the NonEmptyList, ensuring the list never becomes empty.
    /// </summary>
    /// <param name="index">The index at which to remove the item.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is only one item in the list.</exception>
    public void RemoveAt(int index)
    {
        EnsureMutable();
        if (Count == 1)
            throw new InvalidOperationException("Cannot use RemoveAt method while the NonEmptyList contains only one item.");

        ValidateCount(Count - 1);
        _items.RemoveAt(index);
    }

    /// <summary>
    /// Removes a range of items from the NonEmptyList, ensuring the list never becomes empty.
    /// </summary>
    /// <param name="index">The starting index of the range to remove.</param>
    /// <param name="count">The number of items to remove.</param>
    /// <exception cref="InvalidOperationException">Thrown if trying to remove all items from the list.</exception>
    public void RemoveRange(int index, int count)
    {
        EnsureMutable();
        if (Count - count < 1)
            throw new InvalidOperationException("Cannot remove all items from a NonEmptyList");

        ValidateCount(Count - count);
        _items.RemoveRange(index, count);
    }

    /// <summary>
    /// Removes all items that match the predicate, ensuring the list never becomes empty.
    /// </summary>
    /// <param name="match">The predicate to match items for removal.</param>
    /// <returns>The number of items removed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if all items would be removed.</exception>
    public int RemoveAll(Predicate<T> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        EnsureMutable();

        int matchCount = 0;
        foreach (T item in _items)
        {
            if (match(item))
                matchCount++;
        }

        if (matchCount >= Count)
            throw new InvalidOperationException("Cannot remove all items from a NonEmptyList");

        if (matchCount == 0)
            return 0;

        ValidateCount(Count - matchCount);
        return _items.RemoveAll(match);
    }

    #endregion

    #region List Compatibility

    /// <summary>Determines whether an element is in the list.</summary>
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>Searches for the specified object and returns the zero-based index of the first occurrence.</summary>
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <summary>Searches for the specified object starting at the given index.</summary>
    public int IndexOf(T item, int index) => _items.IndexOf(item, index);

    /// <summary>Searches for the specified object within the given range.</summary>
    public int IndexOf(T item, int index, int count) => _items.IndexOf(item, index, count);

    /// <summary>Searches for the specified object and returns the zero-based index of the last occurrence.</summary>
    public int LastIndexOf(T item) => _items.LastIndexOf(item);

    /// <summary>Searches backward for the specified object starting at the given index.</summary>
    public int LastIndexOf(T item, int index) => _items.LastIndexOf(item, index);

    /// <summary>Searches backward for the specified object within the given range.</summary>
    public int LastIndexOf(T item, int index, int count) => _items.LastIndexOf(item, index, count);

    /// <summary>Copies the entire list to an array, starting at the beginning of the target array.</summary>
    public void CopyTo(T[] array) => _items.CopyTo(array);

    /// <summary>Copies the entire list to an array, starting at the specified index of the target array.</summary>
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>Copies a range of elements to an array.</summary>
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => _items.CopyTo(index, array, arrayIndex, count);

    /// <summary>Determines whether the list contains elements that match the predicate.</summary>
    public bool Exists(Predicate<T> match) => _items.Exists(match);

    /// <summary>Returns the first element that matches the predicate, or the default value.</summary>
    public T? Find(Predicate<T> match) => _items.Find(match);

    /// <summary>Returns all elements that match the predicate. The result may be empty.</summary>
    public List<T> FindAll(Predicate<T> match) => _items.FindAll(match);

    /// <summary>Returns the index of the first element that matches the predicate, or -1.</summary>
    public int FindIndex(Predicate<T> match) => _items.FindIndex(match);

    /// <summary>Returns the index of the first matching element starting at the given index, or -1.</summary>
    public int FindIndex(int startIndex, Predicate<T> match) => _items.FindIndex(startIndex, match);

    /// <summary>Returns the index of the first matching element within the given range, or -1.</summary>
    public int FindIndex(int startIndex, int count, Predicate<T> match) => _items.FindIndex(startIndex, count, match);

    /// <summary>Returns the last element that matches the predicate, or the default value.</summary>
    public T? FindLast(Predicate<T> match) => _items.FindLast(match);

    /// <summary>Returns the index of the last element that matches the predicate, or -1.</summary>
    public int FindLastIndex(Predicate<T> match) => _items.FindLastIndex(match);

    /// <summary>Returns the index of the last matching element searching backward from the given index, or -1.</summary>
    public int FindLastIndex(int startIndex, Predicate<T> match) => _items.FindLastIndex(startIndex, match);

    /// <summary>Returns the index of the last matching element within the given range, or -1.</summary>
    public int FindLastIndex(int startIndex, int count, Predicate<T> match) => _items.FindLastIndex(startIndex, count, match);

    /// <summary>Performs the specified action on each element of the list.</summary>
    public void ForEach(Action<T> action) => _items.ForEach(action);

    /// <summary>Determines whether every element matches the predicate.</summary>
    public bool TrueForAll(Predicate<T> match) => _items.TrueForAll(match);

    /// <summary>Searches the sorted list for an element using the default comparer.</summary>
    public int BinarySearch(T item) => _items.BinarySearch(item);

    /// <summary>Searches the sorted list for an element using the specified comparer.</summary>
    public int BinarySearch(T item, IComparer<T>? comparer) => _items.BinarySearch(item, comparer);

    /// <summary>Searches a range of the sorted list for an element using the specified comparer.</summary>
    public int BinarySearch(int index, int count, T item, IComparer<T>? comparer) => _items.BinarySearch(index, count, item, comparer);

    /// <summary>
    /// Converts every element to another type. The result has the same number of elements, so it is also non-empty.
    /// </summary>
    /// <typeparam name="TOutput">The target element type.</typeparam>
    /// <param name="converter">The conversion function.</param>
    public NonEmptyList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        return NonEmptyList<TOutput>.FromTrustedList(_items.ConvertAll(converter));
    }

    /// <summary>Creates a shallow copy of a range of elements. The result may be empty when <paramref name="count"/> is 0.</summary>
    public List<T> GetRange(int index, int count) => _items.GetRange(index, count);

    /// <summary>
    /// Creates a shallow copy of a range of elements. Used by the compiler for range indexing and list patterns.
    /// </summary>
    /// <param name="start">The zero-based index at which the range starts.</param>
    /// <param name="length">The number of elements in the range.</param>
    public List<T> Slice(int start, int length) => _items.GetRange(start, length);

    /// <summary>Reverses the order of the elements in place.</summary>
    public void Reverse()
    {
        EnsureMutable();
        _items.Reverse();
    }

    /// <summary>Reverses the order of the elements in the specified range in place.</summary>
    public void Reverse(int index, int count)
    {
        EnsureMutable();
        _items.Reverse(index, count);
    }

    /// <summary>Sorts the elements in place using the default comparer.</summary>
    public void Sort()
    {
        EnsureMutable();
        _items.Sort();
    }

    /// <summary>Sorts the elements in place using the specified comparison.</summary>
    public void Sort(Comparison<T> comparison)
    {
        EnsureMutable();
        _items.Sort(comparison);
    }

    /// <summary>Sorts the elements in place using the specified comparer.</summary>
    public void Sort(IComparer<T>? comparer)
    {
        EnsureMutable();
        _items.Sort(comparer);
    }

    /// <summary>Sorts a range of elements in place using the specified comparer.</summary>
    public void Sort(int index, int count, IComparer<T>? comparer)
    {
        EnsureMutable();
        _items.Sort(index, count, comparer);
    }

    /// <summary>Copies the elements to a new array. The array always has at least one element.</summary>
    public T[] ToArray() => _items.ToArray();

    /// <summary>Sets the capacity to the actual number of elements.</summary>
    public void TrimExcess() => _items.TrimExcess();

    /// <summary>Ensures that the capacity is at least the specified value.</summary>
    public int EnsureCapacity(int capacity) => _items.EnsureCapacity(capacity);

    /// <summary>Returns an allocation-free enumerator for the list.</summary>
    public List<T>.Enumerator GetEnumerator() => _items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    #endregion

    #region Non-generic IList

    int IList.Add(object? value)
    {
        Add(CastItem(value));
        return Count - 1;
    }

    bool IList.Contains(object? value) => value is T item && Contains(item);

    int IList.IndexOf(object? value) => value is T item ? IndexOf(item) : -1;

    void IList.Insert(int index, object? value) => Insert(index, CastItem(value));

    void IList.Remove(object? value)
    {
        if (value is T item)
            Remove(item);
    }

    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    private static T CastItem(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value is T item
            ? item
            : throw new ArgumentException($"Value must be of type {typeof(T).Name}", nameof(value));
    }

    #endregion

    #region Functional Operations

    /// <summary>
    /// Projects each element of the list into a new form, preserving the non-empty guarantee.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>A new NonEmptyList with the transformed elements.</returns>
    public NonEmptyList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult> results = new(Count);
        foreach (T item in _items)
            results.Add(selector(item));

        return NonEmptyList<TResult>.FromTrustedList(results);
    }

    /// <summary>
    /// Projects each element of the list into a new form with its index, preserving the non-empty guarantee.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="selector">A transform function to apply to each element and its index.</param>
    /// <returns>A new NonEmptyList with the transformed elements.</returns>
    public NonEmptyList<TResult> MapWithIndex<TResult>(Func<T, int, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult> results = new(Count);
        for (int i = 0; i < _items.Count; i++)
            results.Add(selector(_items[i], i));

        return NonEmptyList<TResult>.FromTrustedList(results);
    }

    /// <summary>
    /// Projects each element to a NonEmptyList and flattens the resulting lists into one.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="selector">A transform function that returns a NonEmptyList for each element.</param>
    /// <returns>A new NonEmptyList with all elements from the projected lists.</returns>
    public NonEmptyList<TResult> FlatMap<TResult>(Func<T, NonEmptyList<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult> results = new();
        foreach (T item in _items)
        {
            NonEmptyList<TResult> inner = selector(item);
            ArgumentNullException.ThrowIfNull(inner, nameof(selector));
            results.AddRange(inner._items);
        }

        return NonEmptyList<TResult>.FromTrustedList(results);
    }

    /// <summary>
    /// Applies a reducer function to produce a single value. No seed is needed since the list is never empty.
    /// </summary>
    /// <param name="reducer">A function that combines two elements.</param>
    /// <returns>The reduced value.</returns>
    public T Reduce(Func<T, T, T> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);

        T accumulator = _items[0];
        for (int i = 1; i < _items.Count; i++)
            accumulator = reducer(accumulator, _items[i]);

        return accumulator;
    }

    /// <summary>
    /// Applies a reducer function from right to left to produce a single value.
    /// </summary>
    /// <param name="reducer">A function that combines two elements.</param>
    /// <returns>The reduced value.</returns>
    public T ReduceRight(Func<T, T, T> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);

        T accumulator = _items[^1];
        for (int i = _items.Count - 2; i >= 0; i--)
            accumulator = reducer(_items[i], accumulator);

        return accumulator;
    }

    /// <summary>
    /// Applies a folder function with a seed value to produce a single result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="folder">A function that combines the accumulator and each element.</param>
    /// <returns>The folded result.</returns>
    public TResult Fold<TResult>(TResult seed, Func<TResult, T, TResult> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        TResult accumulator = seed;
        foreach (T item in _items)
            accumulator = folder(accumulator, item);

        return accumulator;
    }

    /// <summary>
    /// Applies a folder function from right to left with a seed value.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="folder">A function that combines each element and the accumulator.</param>
    /// <returns>The folded result.</returns>
    public TResult FoldRight<TResult>(TResult seed, Func<T, TResult, TResult> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        TResult accumulator = seed;
        for (int i = _items.Count - 1; i >= 0; i--)
            accumulator = folder(_items[i], accumulator);

        return accumulator;
    }

    /// <summary>
    /// Returns the running accumulation of a fold: the seed followed by every intermediate result.
    /// The result always has <c>Count + 1</c> elements.
    /// </summary>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="folder">A function that combines the accumulator and each element.</param>
    public NonEmptyList<TAccumulate> Scan<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        List<TAccumulate> results = new(Count + 1) { seed };
        TAccumulate accumulator = seed;
        foreach (T item in _items)
        {
            accumulator = folder(accumulator, item);
            results.Add(accumulator);
        }

        return NonEmptyList<TAccumulate>.FromTrustedList(results);
    }

    /// <summary>
    /// Returns the running accumulation of a reduce: the head followed by every intermediate result.
    /// The result has the same number of elements as this list.
    /// </summary>
    /// <param name="reducer">A function that combines two elements.</param>
    public NonEmptyList<T> Scan(Func<T, T, T> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);

        List<T> results = new(Count) { _items[0] };
        T accumulator = _items[0];
        for (int i = 1; i < _items.Count; i++)
        {
            accumulator = reducer(accumulator, _items[i]);
            results.Add(accumulator);
        }

        return FromTrustedList(results);
    }

    /// <summary>
    /// Merges two lists element by element into tuples.
    /// </summary>
    /// <typeparam name="TOther">The type of elements in the other list.</typeparam>
    /// <param name="other">The other NonEmptyList to zip with.</param>
    /// <returns>A NonEmptyList of tuples containing paired elements.</returns>
    public NonEmptyList<(T First, TOther Second)> Zip<TOther>(NonEmptyList<TOther> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        int count = Math.Min(Count, other.Count);
        List<(T, TOther)> zipped = new(count);
        for (int i = 0; i < count; i++)
            zipped.Add((_items[i], other._items[i]));

        return new NonEmptyList<(T, TOther)>(zipped);
    }

    /// <summary>
    /// Merges two lists element by element using a selector function.
    /// </summary>
    /// <typeparam name="TOther">The type of elements in the other list.</typeparam>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="other">The other NonEmptyList to zip with.</param>
    /// <param name="selector">A function that combines elements from both lists.</param>
    /// <returns>A NonEmptyList with the combined elements.</returns>
    public NonEmptyList<TResult> ZipWith<TOther, TResult>(NonEmptyList<TOther> other, Func<T, TOther, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(other);
        ArgumentNullException.ThrowIfNull(selector);

        int count = Math.Min(Count, other.Count);
        List<TResult> zipped = new(count);
        for (int i = 0; i < count; i++)
            zipped.Add(selector(_items[i], other._items[i]));

        return NonEmptyList<TResult>.FromTrustedList(zipped);
    }

    /// <summary>
    /// Zips the list with indices.
    /// </summary>
    /// <returns>A NonEmptyList of tuples containing elements and their indices.</returns>
    public NonEmptyList<(T Item, int Index)> ZipWithIndex()
    {
        List<(T, int)> zipped = new(Count);
        for (int i = 0; i < _items.Count; i++)
            zipped.Add((_items[i], i));

        return new NonEmptyList<(T, int)>(zipped);
    }

    /// <summary>
    /// Partitions the list into two groups based on a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to test elements.</param>
    /// <returns>A tuple with matching and non-matching elements.</returns>
    public (List<T> Matching, List<T> NotMatching) Partition(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        List<T> matching = new();
        List<T> notMatching = new();

        foreach (T item in _items)
        {
            if (predicate(item))
                matching.Add(item);
            else
                notMatching.Add(item);
        }

        return (matching, notMatching);
    }

    /// <summary>
    /// Groups elements by a key, with each group as a NonEmptyList.
    /// </summary>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <returns>A dictionary mapping keys to NonEmptyList values.</returns>
    public Dictionary<TKey, NonEmptyList<T>> GroupByNonEmpty<TKey>(Func<T, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        return _items
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => From(g));
    }

    /// <summary>
    /// Groups elements by a key and projects each element.
    /// </summary>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    /// <typeparam name="TElement">The type of the projected elements.</typeparam>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <param name="elementSelector">A function to project each element.</param>
    /// <returns>A dictionary mapping keys to NonEmptyList values.</returns>
    public Dictionary<TKey, NonEmptyList<TElement>> GroupByNonEmpty<TKey, TElement>(
        Func<T, TKey> keySelector,
        Func<T, TElement> elementSelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(elementSelector);

        return _items
            .GroupBy(keySelector, elementSelector)
            .ToDictionary(g => g.Key, g => NonEmptyList<TElement>.From(g));
    }

    #endregion

    #region Pattern Matching & Deconstruction

    /// <summary>
    /// Deconstructs the NonEmptyList into the first element (Head) and the rest (Tail).
    /// </summary>
    /// <param name="head">The first element of the list.</param>
    /// <param name="tail">The rest of the list as a NonEmptyList.</param>
    public void Deconstruct(out T head, out NonEmptyList<T>? tail)
    {
        head = Head;
        tail = Tail;
    }

    /// <summary>
    /// Deconstructs the NonEmptyList into first, second, and rest.
    /// </summary>
    /// <param name="first">The first element.</param>
    /// <param name="second">The second element (default if only one element).</param>
    /// <param name="rest">The remaining elements after the first two.</param>
    public void Deconstruct(out T first, out T? second, out IEnumerable<T> rest)
    {
        first = Head;
        second = Count > 1 ? _items[1] : default;
        rest = Count > 2 ? _items.Skip(2) : Enumerable.Empty<T>();
    }

    /// <summary>
    /// Matches the list based on whether it has one or multiple elements.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="single">Function to call when the list has exactly one element.</param>
    /// <param name="multiple">Function to call when the list has more than one element.</param>
    /// <returns>The result of the matching function.</returns>
    public TResult Match<TResult>(Func<T, TResult> single, Func<T, NonEmptyList<T>, TResult> multiple)
    {
        ArgumentNullException.ThrowIfNull(single);
        ArgumentNullException.ThrowIfNull(multiple);

        return Tail is null
            ? single(Head)
            : multiple(Head, Tail);
    }

    /// <summary>
    /// Executes an action based on whether the list has one or multiple elements.
    /// </summary>
    /// <param name="single">Action to execute when the list has exactly one element.</param>
    /// <param name="multiple">Action to execute when the list has more than one element.</param>
    public void Match(Action<T> single, Action<T, NonEmptyList<T>> multiple)
    {
        ArgumentNullException.ThrowIfNull(single);
        ArgumentNullException.ThrowIfNull(multiple);

        if (Tail is null)
            single(Head);
        else
            multiple(Head, Tail);
    }

    #endregion

    #region Collection Operations

    /// <summary>
    /// Concatenates this list with another NonEmptyList.
    /// </summary>
    /// <param name="other">The other list to concatenate.</param>
    /// <returns>A new NonEmptyList with all elements from both lists.</returns>
    public NonEmptyList<T> Concat(NonEmptyList<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        List<T> result = new(Count + other.Count);
        result.AddRange(_items);
        result.AddRange(other._items);
        return new NonEmptyList<T>(result);
    }

    /// <summary>
    /// Concatenates this list with an IEnumerable.
    /// </summary>
    /// <param name="other">The enumerable to concatenate.</param>
    /// <returns>A new NonEmptyList with all elements.</returns>
    public NonEmptyList<T> Concat(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        List<T> result = new(_items);
        result.AddRange(other);
        return FromTrustedList(result);
    }

    /// <summary>
    /// Returns a new NonEmptyList with the item added at the beginning. This list is not modified.
    /// Use <see cref="AddFirst"/> to insert in place.
    /// </summary>
    /// <param name="item">The item to prepend.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public NonEmptyList<T> Prepend(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        List<T> result = new(Count + 1) { item };
        result.AddRange(_items);
        return new NonEmptyList<T>(result);
    }

    /// <summary>
    /// Returns a new NonEmptyList with the item added at the end. This list is not modified.
    /// Use <see cref="Add"/> to append in place.
    /// </summary>
    /// <param name="item">The item to append.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public NonEmptyList<T> Append(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        List<T> result = new(Count + 1);
        result.AddRange(_items);
        result.Add(item);
        return new NonEmptyList<T>(result);
    }

    /// <summary>
    /// Returns a new NonEmptyList with elements in reverse order. This list is not modified.
    /// </summary>
    /// <returns>A new reversed NonEmptyList.</returns>
    public NonEmptyList<T> ReverseList()
    {
        List<T> reversed = new(_items);
        reversed.Reverse();
        return new NonEmptyList<T>(reversed);
    }

    /// <summary>
    /// Returns a new NonEmptyList with distinct elements.
    /// </summary>
    /// <returns>A new NonEmptyList with unique elements.</returns>
    public NonEmptyList<T> DistinctList() => new(_items.Distinct().ToList());

    /// <summary>
    /// Returns a new NonEmptyList with distinct elements using a comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>A new NonEmptyList with unique elements.</returns>
    public NonEmptyList<T> DistinctList(IEqualityComparer<T>? comparer) => new(_items.Distinct(comparer).ToList());

    /// <summary>
    /// Returns a new NonEmptyList with distinct elements by a key. The first element of each key is kept.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <returns>A new NonEmptyList with unique elements by key.</returns>
    public NonEmptyList<T> DistinctBy<TKey>(Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return new NonEmptyList<T>(_items.DistinctBy(keySelector).ToList());
    }

    /// <summary>
    /// Takes the first n elements if they form a non-empty collection.
    /// </summary>
    /// <param name="count">The number of elements to take.</param>
    /// <returns>A NonEmptyList if count >= 1, null otherwise.</returns>
    public NonEmptyList<T>? TakeNonEmpty(int count)
    {
        if (count < 1) return null;
        return new NonEmptyList<T>(_items.GetRange(0, Math.Min(count, Count)));
    }

    /// <summary>
    /// Skips the first n elements if remaining elements form a non-empty collection.
    /// </summary>
    /// <param name="count">The number of elements to skip.</param>
    /// <returns>A NonEmptyList if elements remain, null otherwise.</returns>
    public NonEmptyList<T>? SkipNonEmpty(int count)
    {
        if (count >= Count) return null;
        if (count <= 0) return new NonEmptyList<T>(new List<T>(_items));
        return new NonEmptyList<T>(_items.GetRange(count, Count - count));
    }

    /// <summary>
    /// Takes at least one element, up to the specified count.
    /// </summary>
    /// <param name="count">The maximum number of elements to take.</param>
    /// <returns>A NonEmptyList with at least one element.</returns>
    public NonEmptyList<T> TakeAtLeastOne(int count)
    {
        int actualCount = Math.Max(1, Math.Min(count, Count));
        return new NonEmptyList<T>(_items.GetRange(0, actualCount));
    }

    /// <summary>
    /// Returns sliding windows of the specified size.
    /// </summary>
    /// <param name="size">The size of each window.</param>
    /// <param name="step">The step between windows (default 1).</param>
    /// <returns>An enumerable of NonEmptyList windows.</returns>
    public IEnumerable<NonEmptyList<T>> Sliding(int size, int step = 1)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1");
        if (step < 1)
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be at least 1");

        for (int i = 0; i <= Count - size; i += step)
            yield return new NonEmptyList<T>(_items.GetRange(i, size));
    }

    /// <summary>
    /// Splits the list into chunks of the specified size. The last chunk may be smaller but is never empty.
    /// </summary>
    /// <param name="size">The size of each chunk.</param>
    /// <returns>An enumerable of NonEmptyList chunks.</returns>
    public IEnumerable<NonEmptyList<T>> ChunkNonEmpty(int size)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1");

        for (int i = 0; i < Count; i += size)
            yield return new NonEmptyList<T>(_items.GetRange(i, Math.Min(size, Count - i)));
    }

    /// <summary>
    /// Returns every non-empty suffix of the list, starting with the whole list and ending with the last element alone.
    /// </summary>
    public NonEmptyList<NonEmptyList<T>> Tails()
    {
        List<NonEmptyList<T>> tails = new(Count);
        for (int i = 0; i < Count; i++)
            tails.Add(new NonEmptyList<T>(_items.GetRange(i, Count - i)));

        return new NonEmptyList<NonEmptyList<T>>(tails);
    }

    /// <summary>
    /// Returns every non-empty prefix of the list, starting with the first element alone and ending with the whole list.
    /// </summary>
    public NonEmptyList<NonEmptyList<T>> Inits()
    {
        List<NonEmptyList<T>> inits = new(Count);
        for (int i = 1; i <= Count; i++)
            inits.Add(new NonEmptyList<T>(_items.GetRange(0, i)));

        return new NonEmptyList<NonEmptyList<T>>(inits);
    }

    /// <summary>
    /// Inserts a separator between each element.
    /// </summary>
    /// <param name="separator">The separator to insert.</param>
    /// <returns>A new NonEmptyList with separators between elements.</returns>
    public NonEmptyList<T> Intersperse(T separator)
    {
        ArgumentNullException.ThrowIfNull(separator);

        List<T> result = new(Count * 2 - 1) { Head };
        for (int i = 1; i < _items.Count; i++)
        {
            result.Add(separator);
            result.Add(_items[i]);
        }

        return new NonEmptyList<T>(result);
    }

    /// <summary>
    /// Rotates elements to the left by the specified number of positions.
    /// </summary>
    /// <param name="positions">The number of positions to rotate.</param>
    /// <returns>A new rotated NonEmptyList.</returns>
    public NonEmptyList<T> RotateLeft(int positions)
    {
        int actualPositions = ((positions % Count) + Count) % Count;
        if (actualPositions == 0) return new NonEmptyList<T>(new List<T>(_items));

        List<T> rotated = new(Count);
        rotated.AddRange(_items.GetRange(actualPositions, Count - actualPositions));
        rotated.AddRange(_items.GetRange(0, actualPositions));
        return new NonEmptyList<T>(rotated);
    }

    /// <summary>
    /// Rotates elements to the right by the specified number of positions.
    /// </summary>
    /// <param name="positions">The number of positions to rotate.</param>
    /// <returns>A new rotated NonEmptyList.</returns>
    public NonEmptyList<T> RotateRight(int positions) => RotateLeft(-positions);

    #endregion

    #region Validation

    /// <summary>
    /// Updates a range of elements with null validation.
    /// </summary>
    /// <param name="startIndex">The starting index.</param>
    /// <param name="items">The items to set.</param>
    public void SetRange(int startIndex, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        IList<T> itemList = items as IList<T> ?? items.ToList();

        if (itemList.Any(item => item is null))
            throw new ArgumentNullException(nameof(items), "Items cannot contain null values");

        if (startIndex < 0 || startIndex + itemList.Count > Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Range exceeds list bounds");

        EnsureMutable();
        for (int i = 0; i < itemList.Count; i++)
            ValidateReplacement(startIndex + i, itemList[i]);

        for (int i = 0; i < itemList.Count; i++)
            _items[startIndex + i] = itemList[i];
    }

    /// <summary>
    /// Validates all elements against a predicate and throws if any fail.
    /// </summary>
    /// <param name="predicate">The validation predicate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <returns>This list if validation passes.</returns>
    /// <exception cref="InvalidOperationException">Thrown if any element fails validation.</exception>
    public NonEmptyList<T> Validate(Func<T, bool> predicate, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (!_items.All(predicate))
            throw new InvalidOperationException(errorMessage);

        return this;
    }

    /// <summary>
    /// Validates all elements and returns the first failing element if any.
    /// </summary>
    /// <param name="predicate">The validation predicate.</param>
    /// <param name="failingElement">The first element that failed validation.</param>
    /// <returns>True if all elements pass; false otherwise.</returns>
    public bool TryValidate(Func<T, bool> predicate, [MaybeNullWhen(true)] out T failingElement)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (T item in _items)
        {
            if (!predicate(item))
            {
                failingElement = item;
                return false;
            }
        }

        failingElement = default;
        return true;
    }

    #endregion

    #region Conversion Methods

    /// <summary>
    /// Converts the NonEmptyList to an array.
    /// </summary>
    /// <returns>An array containing all elements.</returns>
    public T[] ToArrayNonEmpty() => _items.ToArray();

    /// <summary>
    /// Converts the NonEmptyList to a HashSet.
    /// </summary>
    /// <returns>A HashSet containing all unique elements.</returns>
    public HashSet<T> ToHashSet() => new(_items);

    /// <summary>
    /// Converts the NonEmptyList to a HashSet with a comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer.</param>
    /// <returns>A HashSet containing all unique elements.</returns>
    public HashSet<T> ToHashSet(IEqualityComparer<T>? comparer) => new(_items, comparer);

    /// <summary>
    /// Converts the NonEmptyList to a Dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
    /// <param name="keySelector">A function to extract keys from elements.</param>
    /// <returns>A Dictionary with keys from the selector.</returns>
    public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return _items.ToDictionary(keySelector, x => x);
    }

    /// <summary>
    /// Converts the NonEmptyList to a Dictionary with key and value selectors.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
    /// <param name="keySelector">A function to extract keys from elements.</param>
    /// <param name="valueSelector">A function to extract values from elements.</param>
    /// <returns>A Dictionary with keys and values from the selectors.</returns>
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(valueSelector);
        return _items.ToDictionary(keySelector, valueSelector);
    }

    /// <summary>
    /// Converts the NonEmptyList to a Queue.
    /// </summary>
    /// <returns>A Queue containing all elements in order.</returns>
    public Queue<T> ToQueue() => new(_items);

    /// <summary>
    /// Converts the NonEmptyList to a Stack.
    /// </summary>
    /// <returns>A Stack containing all elements (last element on top).</returns>
    public Stack<T> ToStack() => new(_items);

    /// <summary>
    /// Converts the NonEmptyList to a LinkedList.
    /// </summary>
    /// <returns>A LinkedList containing all elements.</returns>
    public LinkedList<T> ToLinkedList() => new(_items);

    /// <summary>
    /// Returns a read-only wrapper over this list. Changes to this list are visible through the wrapper.
    /// </summary>
    /// <returns>A read-only list.</returns>
    public ReadOnlyCollection<T> AsReadOnly() => _items.AsReadOnly();

    /// <summary>
    /// Returns a span over the list's storage. Do not add or remove elements while the span is in use.
    /// </summary>
    /// <returns>A ReadOnlySpan over the list elements.</returns>
    public ReadOnlySpan<T> AsSpan() => CollectionsMarshal.AsSpan(_items);

    /// <summary>
    /// Copies the list into a ReadOnlyMemory.
    /// </summary>
    /// <returns>A ReadOnlyMemory over a copy of the list elements.</returns>
    public ReadOnlyMemory<T> AsMemory() => _items.ToArray().AsMemory();

    #endregion

    #region Utility Methods

    /// <summary>
    /// Returns a random element from the list.
    /// </summary>
    /// <returns>A randomly selected element.</returns>
    public T Random() => Random(System.Random.Shared);

    /// <summary>
    /// Returns a random element from the list using the specified Random instance.
    /// </summary>
    /// <param name="random">The Random instance to use.</param>
    /// <returns>A randomly selected element.</returns>
    public T Random(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return _items[random.Next(Count)];
    }

    /// <summary>
    /// Returns the element with the minimum value according to a key selector. Runs in O(n).
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the comparison key.</param>
    /// <returns>The element with the minimum key value.</returns>
    public T MinBy<TKey>(Func<T, TKey> keySelector) => MinBy(keySelector, null);

    /// <summary>
    /// Returns the element with the minimum value according to a key selector and comparer. Runs in O(n).
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the comparison key.</param>
    /// <param name="comparer">The comparer for keys, or null for the default.</param>
    public T MinBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        comparer ??= Comparer<TKey>.Default;

        T best = _items[0];
        TKey bestKey = keySelector(best);
        for (int i = 1; i < _items.Count; i++)
        {
            TKey key = keySelector(_items[i]);
            if (comparer.Compare(key, bestKey) < 0)
            {
                best = _items[i];
                bestKey = key;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns the element with the maximum value according to a key selector. Runs in O(n).
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the comparison key.</param>
    /// <returns>The element with the maximum key value.</returns>
    public T MaxBy<TKey>(Func<T, TKey> keySelector) => MaxBy(keySelector, null);

    /// <summary>
    /// Returns the element with the maximum value according to a key selector and comparer. Runs in O(n).
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the comparison key.</param>
    /// <param name="comparer">The comparer for keys, or null for the default.</param>
    public T MaxBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        comparer ??= Comparer<TKey>.Default;

        T best = _items[0];
        TKey bestKey = keySelector(best);
        for (int i = 1; i < _items.Count; i++)
        {
            TKey key = keySelector(_items[i]);
            if (comparer.Compare(key, bestKey) > 0)
            {
                best = _items[i];
                bestKey = key;
            }
        }

        return best;
    }

    /// <summary>
    /// Executes an action for each element with its index.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void ForEachWithIndex(Action<T, int> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        for (int i = 0; i < _items.Count; i++)
            action(_items[i], i);
    }

    /// <summary>
    /// Returns a string representation of the NonEmptyList, showing its elements.
    /// </summary>
    /// <returns>A string representing the NonEmptyList.</returns>
    public override string ToString() => $"NonEmptyList: [{string.Join(", ", _items)}]";

    #endregion

    #region Equality

    /// <summary>
    /// Determines whether the specified NonEmptyList is equal to this instance.
    /// </summary>
    /// <param name="other">The other NonEmptyList to compare.</param>
    /// <returns>True if the lists are equal; otherwise, false.</returns>
    public bool Equals(NonEmptyList<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Count != other.Count) return false;

        return _items.SequenceEqual(other._items);
    }

    /// <summary>
    /// Determines whether the specified NonEmptyList is equal to this instance using a comparer.
    /// </summary>
    /// <param name="other">The other NonEmptyList to compare.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>True if the lists are equal; otherwise, false.</returns>
    public bool SequenceEqual(NonEmptyList<T> other, IEqualityComparer<T>? comparer)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Count != other.Count) return false;

        return _items.SequenceEqual(other._items, comparer);
    }

    /// <summary>
    /// Determines whether the specified object is equal to this instance.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is NonEmptyList<T> other && Equals(other);

    /// <summary>
    /// Returns a hash code for this instance based on its elements.
    /// </summary>
    /// <returns>A hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (T item in _items)
            hash.Add(item);

        return hash.ToHashCode();
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(NonEmptyList<T>? left, NonEmptyList<T>? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(NonEmptyList<T>? left, NonEmptyList<T>? right) => !(left == right);

    #endregion

    #region Invariant Hooks

    /// <summary>
    /// Gets a value indicating whether the list rejects every mutation. Derived types such as
    /// <see cref="ConstrainedList{T, TConstraint}"/> override this to report <see langword="true"/>,
    /// and the value is surfaced through <see cref="ICollection{T}.IsReadOnly"/> and <see cref="IList.IsReadOnly"/>.
    /// </summary>
    protected virtual bool IsReadOnly => false;

    /// <summary>
    /// Called before every operation that changes the contents or order of the list. Derived types that
    /// must stay immutable override this to throw <see cref="NotSupportedException"/>. The default does nothing.
    /// </summary>
    protected virtual void EnsureMutable()
    {
    }

    /// <summary>
    /// Called for every element about to be added or inserted, before the list changes. Override to enforce
    /// element-level invariants such as uniqueness. The element is never <see langword="null"/>. The default does nothing.
    /// </summary>
    /// <param name="item">The element about to be added.</param>
    protected virtual void ValidateItem(T item)
    {
    }

    /// <summary>
    /// Called once for a batch of elements about to be added or inserted, before the list changes. The default calls
    /// <see cref="ValidateItem"/> for each element; override to also check invariants between the new elements themselves,
    /// such as duplicates within the batch. Elements are never <see langword="null"/>.
    /// </summary>
    /// <param name="items">The elements about to be added, in order.</param>
    protected virtual void ValidateRange(IReadOnlyList<T> items)
    {
        foreach (T item in items)
            ValidateItem(item);
    }

    /// <summary>
    /// Called before an existing element is replaced through the indexer or <see cref="SetRange"/>.
    /// The element is never <see langword="null"/>. The default does nothing.
    /// </summary>
    /// <param name="index">The index being written.</param>
    /// <param name="item">The replacement element.</param>
    protected virtual void ValidateReplacement(int index, T item)
    {
    }

    /// <summary>
    /// Called before the number of elements changes, with the count the list would have after the operation.
    /// Override to enforce size bounds. The proposed count is always at least one. The default does nothing.
    /// </summary>
    /// <param name="proposedCount">The number of elements the list would contain after the operation.</param>
    protected virtual void ValidateCount(int proposedCount)
    {
    }

    /// <summary>
    /// Gives same-assembly helpers direct access to the backing list for read-only, allocation-free iteration.
    /// </summary>
    internal List<T> Items => _items;

    #endregion
}

/// <summary>
/// Debugger view that shows the elements of a <see cref="NonEmptyList{T}"/> as a flat array.
/// </summary>
internal sealed class NonEmptyListDebugView<T>
{
    private readonly NonEmptyList<T> _list;

    public NonEmptyListDebugView(NonEmptyList<T> list) => _list = list;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _list.ToArray();
}
