// Ignore Spelling: Deconstruct Intersperse

using System.Collections;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// A generic list that guarantees at least one element.
/// Provides convenient access to the first (Head) and last elements, and can return the tail (rest of the list).
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
[JsonConverter(typeof(NonEmptyListJsonConverterFactory))]
public class NonEmptyList<T> : List<T>, IReadOnlyList<T>, IEquatable<NonEmptyList<T>>
{
    private NonEmptyList<T>? _cachedTail;  // Cache for the tail
    private NonEmptyList<T>? _cachedInit;  // Cache for the init

    #region Properties

    /// <summary>
    /// Gets the first element of the list (same as First).
    /// </summary>
    public T Head => this[0]; // Same as First

    /// <summary>
    /// Gets the list of elements excluding the first (tail of the list).
    /// Returns null if there is only one element.
    /// </summary>
    public NonEmptyList<T>? Tail
    {
        get
        {
            if (_cachedTail != null) return _cachedTail; // Return cached version if it exists

            if (Count is 1) return null;

            _cachedTail = new NonEmptyList<T>(this[1], this.Skip(2));  // Cache the result
            return _cachedTail;
        }
    }

    /// <summary>
    /// Gets the list of elements excluding the last (init of the list).
    /// Returns null if there is only one element.
    /// </summary>
    public NonEmptyList<T>? Init
    {
        get
        {
            if (_cachedInit != null) return _cachedInit;

            if (Count is 1) return null;

            _cachedInit = new NonEmptyList<T>(this[0], this.Take(Count - 1).Skip(1));
            return _cachedInit;
        }
    }

    /// <summary>
    /// Gets the first element of the list.
    /// </summary>
    public T First => Head;  // First element of the list

    /// <summary>
    /// Gets the last element of the list.
    /// </summary>
    public T Last => this[^1];  // Last element of the list

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentNullException">Thrown if trying to set a null value.</exception>
    public new T this[int index]
    {
        get => base[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            InvalidateCaches();
            base[index] = value;
        }
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
    /// Creates a new NonEmptyList with the first element and an IEnumerable of other elements.
    /// </summary>
    /// <param name="firstItem">The first item in the list (required).</param>
    /// <param name="otherItems">An IEnumerable containing the other items in the list.</param>
    /// <exception cref="ArgumentNullException">Thrown if the first item or any of the other items are null.</exception>
    public NonEmptyList(T firstItem, IEnumerable<T> otherItems)
    {
        if (firstItem is null)
            throw new ArgumentNullException(nameof(firstItem), "First item cannot be null");

        base.Add(firstItem);

        if (otherItems is not null)
        {
            IList<T> items = otherItems as IList<T> ?? otherItems.ToList();

            if (items.Any(item => item is null))
                throw new ArgumentNullException(nameof(otherItems), "Other items cannot contain null values");

            if (items.Count > 0)
                base.AddRange(items);
        }
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

        IList<T> items = enumerable as IList<T> ?? enumerable.ToList();

        if (items.Count == 0)
            throw new ArgumentException("Cannot create a NonEmptyList from null or empty", nameof(enumerable));

        T head = items[0];
        IEnumerable<T> tail = items.Skip(1);

        return new NonEmptyList<T>(head, tail);
    }

    /// <summary>
    /// Attempts to create a NonEmptyList from an existing IEnumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to create the NonEmptyList from.</param>
    /// <param name="result">When successful, contains the NonEmptyList; otherwise, null.</param>
    /// <returns>True if the NonEmptyList was created successfully; otherwise, false.</returns>
    public static bool TryFrom(IEnumerable<T>? enumerable, out NonEmptyList<T>? result)
    {
        result = null;

        if (enumerable is null)
            return false;

        IList<T> items = enumerable as IList<T> ?? enumerable.ToList();

        if (items.Count == 0)
            return false;

        if (items.Any(item => item is null))
            return false;

        result = new NonEmptyList<T>(items[0], items.Skip(1));
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

        return new NonEmptyList<T>(item, Enumerable.Repeat(item, count - 1));
    }

    #endregion

    #region Add/Insert Operations

    /// <summary>
    /// Adds an item to the NonEmptyList, ensuring the item is not null.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public new void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        InvalidateCaches();

        base.Add(item);
    }

    /// <summary>
    /// Adds a range of items to the NonEmptyList, ensuring the collection is not null or empty.
    /// </summary>
    /// <param name="collection">The collection of items to add.</param>
    /// <exception cref="ArgumentException">Thrown if the collection is empty or contains null values.</exception>
    public new void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        IList<T> items = collection as IList<T> ?? collection.ToList();

        if (items.Count == 0)
            throw new ArgumentException("Collection cannot be empty", nameof(collection));

        if (items.Any(item => item is null))
            throw new ArgumentNullException(nameof(collection), "Collection cannot contain null values");

        InvalidateCaches();

        base.AddRange(items);
    }

    /// <summary>
    /// Inserts an item at the specified index in the NonEmptyList, ensuring the item is not null.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public new void Insert(int index, T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        InvalidateCaches();

        base.Insert(index, item);
    }

    /// <summary>
    /// Inserts a range of items at the specified index in the NonEmptyList, ensuring no items are null.
    /// </summary>
    /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
    /// <param name="collection">The collection of items to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown if the collection is null or contains null values.</exception>
    /// <exception cref="ArgumentException">Thrown if the collection is empty.</exception>
    public new void InsertRange(int index, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        IList<T> items = collection as IList<T> ?? collection.ToList();

        if (items.Count == 0)
            throw new ArgumentException("Collection cannot be empty", nameof(collection));

        if (items.Any(item => item is null))
            throw new ArgumentNullException(nameof(collection), "Collection cannot contain null values");

        InvalidateCaches();

        base.InsertRange(index, items);
    }

    /// <summary>
    /// Prepends an item to the beginning of the NonEmptyList.
    /// </summary>
    /// <param name="item">The item to prepend.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public void Prepend(T item)
    {
        Insert(0, item);
    }

    #endregion

    #region Remove Operations

    /// <summary>
    /// Clears the NonEmptyList. This operation is not supported as a NonEmptyList cannot be empty.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public new void Clear() => throw new NotSupportedException("Cannot clear a NonEmptyList");

    /// <summary>
    /// Removes an item from the NonEmptyList, but ensures that the list is never empty.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if there is only one item in the list.</exception>
    public new void Remove(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (Count == 1)
            throw new InvalidOperationException("Cannot use Remove method while the NonEmptyList contains only one item.");

        InvalidateCaches();

        base.Remove(item);
    }

    /// <summary>
    /// Removes an item at a specified index from the NonEmptyList, but ensures that the list is never empty.
    /// </summary>
    /// <param name="index">The index at which to remove the item.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is only one item in the list.</exception>
    public new void RemoveAt(int index)
    {
        if (Count == 1)
            throw new InvalidOperationException("Cannot use RemoveAt method while the NonEmptyList contains only one item.");

        InvalidateCaches();

        base.RemoveAt(index);
    }

    /// <summary>
    /// Removes a range of items from the NonEmptyList, ensuring that the list is never completely emptied.
    /// </summary>
    /// <param name="index">The starting index of the range to remove.</param>
    /// <param name="count">The number of items to remove.</param>
    /// <exception cref="InvalidOperationException">Thrown if trying to remove all items from the list.</exception>
    public new void RemoveRange(int index, int count)
    {
        if (Count - count < 1)
            throw new InvalidOperationException("Cannot remove all items from a NonEmptyList");

        InvalidateCaches();

        base.RemoveRange(index, count);
    }

    /// <summary>
    /// Removes all items that match the predicate, ensuring the list is never completely emptied.
    /// </summary>
    /// <param name="match">The predicate to match items for removal.</param>
    /// <returns>The number of items removed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if all items would be removed.</exception>
    public new int RemoveAll(Predicate<T> match)
    {
        ArgumentNullException.ThrowIfNull(match);

        int matchCount = this.Count(x => match(x));

        if (matchCount >= Count)
            throw new InvalidOperationException("Cannot remove all items from a NonEmptyList");

        InvalidateCaches();

        return base.RemoveAll(match);
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

        List<TResult> results = this.Select(selector).ToList();
        return new NonEmptyList<TResult>(results[0], results.Skip(1));
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

        List<TResult> results = this.Select((item, index) => selector(item, index)).ToList();
        return new NonEmptyList<TResult>(results[0], results.Skip(1));
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

        List<TResult> results = this.SelectMany(selector).ToList();
        return new NonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Applies a reducer function to produce a single value. No seed is needed since the list is never empty.
    /// </summary>
    /// <param name="reducer">A function that combines two elements.</param>
    /// <returns>The reduced value.</returns>
    public T Reduce(Func<T, T, T> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);

        return this.Aggregate(reducer);
    }

    /// <summary>
    /// Applies a reducer function from right to left to produce a single value.
    /// </summary>
    /// <param name="reducer">A function that combines two elements.</param>
    /// <returns>The reduced value.</returns>
    public T ReduceRight(Func<T, T, T> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);

        return this.Reverse<T>().Aggregate((a, b) => reducer(b, a));
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

        return this.Aggregate(seed, folder);
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

        return this.Reverse<T>().Aggregate(seed, (acc, item) => folder(item, acc));
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

        List<(T, TOther)> zipped = this.Zip(other, (a, b) => (a, b)).ToList();
        return new NonEmptyList<(T, TOther)>(zipped[0], zipped.Skip(1));
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

        List<TResult> zipped = this.Zip(other, selector).ToList();
        return new NonEmptyList<TResult>(zipped[0], zipped.Skip(1));
    }

    /// <summary>
    /// Zips the list with indices.
    /// </summary>
    /// <returns>A NonEmptyList of tuples containing elements and their indices.</returns>
    public NonEmptyList<(T Item, int Index)> ZipWithIndex()
    {
        List<(T item, int index)> zipped = this.Select((item, index) => (item, index)).ToList();
        return new NonEmptyList<(T, int)>(zipped[0], zipped.Skip(1));
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

        foreach (T item in this)
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

        return this
            .GroupBy(keySelector)
            .ToDictionary(
                g => g.Key,
                g => From(g)
            );
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

        return this
            .GroupBy(keySelector, elementSelector)
            .ToDictionary(
                g => g.Key,
                g => NonEmptyList<TElement>.From(g)
            );
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
        second = Count > 1 ? this[1] : default;
        rest = Count > 2 ? this.Skip(2) : Enumerable.Empty<T>();
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

        NonEmptyList<T> result = new(Head, this.Skip(1));
        result.AddRange(other);
        return result;
    }

    /// <summary>
    /// Concatenates this list with an IEnumerable.
    /// </summary>
    /// <param name="other">The enumerable to concatenate.</param>
    /// <returns>A new NonEmptyList with all elements.</returns>
    public NonEmptyList<T> Concat(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        IList<T> otherList = other as IList<T> ?? other.ToList();
        if (otherList.Any(item => item is null))
            throw new ArgumentNullException(nameof(other), "Collection cannot contain null values");

        NonEmptyList<T> result = new(Head, this.Skip(1));
        if (otherList.Count > 0)
            result.AddRange(otherList);
        return result;
    }

    /// <summary>
    /// Returns a new NonEmptyList with elements in reverse order.
    /// </summary>
    /// <returns>A new reversed NonEmptyList.</returns>
    public NonEmptyList<T> ReverseList()
    {
        List<T> reversed = this.Reverse<T>().ToList();
        return new NonEmptyList<T>(reversed[0], reversed.Skip(1));
    }

    /// <summary>
    /// Returns a new NonEmptyList with distinct elements.
    /// </summary>
    /// <returns>A new NonEmptyList with unique elements.</returns>
    public NonEmptyList<T> DistinctList()
    {
        List<T> distinct = this.Distinct().ToList();
        return new NonEmptyList<T>(distinct[0], distinct.Skip(1));
    }

    /// <summary>
    /// Returns a new NonEmptyList with distinct elements using a comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>A new NonEmptyList with unique elements.</returns>
    public NonEmptyList<T> DistinctList(IEqualityComparer<T> comparer)
    {
        List<T> distinct = this.Distinct(comparer).ToList();
        return new NonEmptyList<T>(distinct[0], distinct.Skip(1));
    }

    /// <summary>
    /// Returns a new NonEmptyList with distinct elements by a key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <returns>A new NonEmptyList with unique elements by key.</returns>
    public NonEmptyList<T> DistinctBy<TKey>(Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        List<T> distinct = this.GroupBy(keySelector).Select(g => g.First()).ToList();
        return new NonEmptyList<T>(distinct[0], distinct.Skip(1));
    }

    /// <summary>
    /// Takes the first n elements if they form a non-empty collection.
    /// </summary>
    /// <param name="count">The number of elements to take.</param>
    /// <returns>A NonEmptyList if count >= 1, null otherwise.</returns>
    public NonEmptyList<T>? TakeNonEmpty(int count)
    {
        if (count < 1) return null;

        List<T> taken = this.Take(count).ToList();
        return new NonEmptyList<T>(taken[0], taken.Skip(1));
    }

    /// <summary>
    /// Skips the first n elements if remaining elements form a non-empty collection.
    /// </summary>
    /// <param name="count">The number of elements to skip.</param>
    /// <returns>A NonEmptyList if elements remain, null otherwise.</returns>
    public NonEmptyList<T>? SkipNonEmpty(int count)
    {
        if (count >= Count) return null;

        List<T> skipped = this.Skip(count).ToList();
        if (skipped.Count == 0) return null;

        return new NonEmptyList<T>(skipped[0], skipped.Skip(1));
    }

    /// <summary>
    /// Takes at least one element, up to the specified count.
    /// </summary>
    /// <param name="count">The maximum number of elements to take.</param>
    /// <returns>A NonEmptyList with at least one element.</returns>
    public NonEmptyList<T> TakeAtLeastOne(int count)
    {
        int actualCount = Math.Max(1, Math.Min(count, Count));
        List<T> taken = this.Take(actualCount).ToList();
        return new NonEmptyList<T>(taken[0], taken.Skip(1));
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
        {
            List<T> window = this.Skip(i).Take(size).ToList();
            yield return new NonEmptyList<T>(window[0], window.Skip(1));
        }
    }

    /// <summary>
    /// Splits the list into chunks of the specified size.
    /// </summary>
    /// <param name="size">The size of each chunk.</param>
    /// <returns>An enumerable of NonEmptyList chunks.</returns>
    public IEnumerable<NonEmptyList<T>> ChunkNonEmpty(int size)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1");

        for (int i = 0; i < Count; i += size)
        {
            List<T> chunk = this.Skip(i).Take(size).ToList();
            yield return new NonEmptyList<T>(chunk[0], chunk.Skip(1));
        }
    }

    /// <summary>
    /// Inserts a separator between each element.
    /// </summary>
    /// <param name="separator">The separator to insert.</param>
    /// <returns>A new NonEmptyList with separators between elements.</returns>
    public NonEmptyList<T> Intersperse(T separator)
    {
        ArgumentNullException.ThrowIfNull(separator);

        if (Count == 1)
            return new NonEmptyList<T>(Head);

        List<T> result = new() { Head };
        foreach (T item in this.Skip(1))
        {
            result.Add(separator);
            result.Add(item);
        }

        return new NonEmptyList<T>(result[0], result.Skip(1));
    }

    /// <summary>
    /// Rotates elements to the left by the specified number of positions.
    /// </summary>
    /// <param name="positions">The number of positions to rotate.</param>
    /// <returns>A new rotated NonEmptyList.</returns>
    public NonEmptyList<T> RotateLeft(int positions)
    {
        if (Count == 1) return new NonEmptyList<T>(Head);

        int actualPositions = ((positions % Count) + Count) % Count;
        if (actualPositions == 0) return new NonEmptyList<T>(Head, this.Skip(1));

        List<T> rotated = this.Skip(actualPositions).Concat(this.Take(actualPositions)).ToList();
        return new NonEmptyList<T>(rotated[0], rotated.Skip(1));
    }

    /// <summary>
    /// Rotates elements to the right by the specified number of positions.
    /// </summary>
    /// <param name="positions">The number of positions to rotate.</param>
    /// <returns>A new rotated NonEmptyList.</returns>
    public NonEmptyList<T> RotateRight(int positions)
    {
        return RotateLeft(-positions);
    }

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

        InvalidateCaches();

        for (int i = 0; i < itemList.Count; i++)
        {
            base[startIndex + i] = itemList[i];
        }
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

        if (!this.All(predicate))
            throw new InvalidOperationException(errorMessage);

        return this;
    }

    /// <summary>
    /// Validates all elements and returns the first failing element if any.
    /// </summary>
    /// <param name="predicate">The validation predicate.</param>
    /// <param name="failingElement">The first element that failed validation.</param>
    /// <returns>True if all elements pass; false otherwise.</returns>
    public bool TryValidate(Func<T, bool> predicate, out T? failingElement)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (T item in this)
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
    public T[] ToArrayNonEmpty() => ToArray();

    /// <summary>
    /// Converts the NonEmptyList to a HashSet.
    /// </summary>
    /// <returns>A HashSet containing all unique elements.</returns>
    public HashSet<T> ToHashSet() => new(this);

    /// <summary>
    /// Converts the NonEmptyList to a HashSet with a comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer.</param>
    /// <returns>A HashSet containing all unique elements.</returns>
    public HashSet<T> ToHashSet(IEqualityComparer<T> comparer) => new(this, comparer);

    /// <summary>
    /// Converts the NonEmptyList to a Dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
    /// <param name="keySelector">A function to extract keys from elements.</param>
    /// <returns>A Dictionary with keys from the selector.</returns>
    public Dictionary<TKey, T> ToDictionary<TKey>(Func<T, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return this.ToDictionary(keySelector, x => x);
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
        return Enumerable.ToDictionary(this, keySelector, valueSelector);
    }

    /// <summary>
    /// Converts the NonEmptyList to a Queue.
    /// </summary>
    /// <returns>A Queue containing all elements in order.</returns>
    public Queue<T> ToQueue() => new(this);

    /// <summary>
    /// Converts the NonEmptyList to a Stack.
    /// </summary>
    /// <returns>A Stack containing all elements (last element on top).</returns>
    public Stack<T> ToStack() => new(this);

    /// <summary>
    /// Converts the NonEmptyList to a LinkedList.
    /// </summary>
    /// <returns>A LinkedList containing all elements.</returns>
    public LinkedList<T> ToLinkedList() => new(this);

    /// <summary>
    /// Returns a read-only wrapper for the list.
    /// </summary>
    /// <returns>A read-only list.</returns>
    public new IReadOnlyList<T> AsReadOnly() => this;

#if NET6_0_OR_GREATER
    /// <summary>
    /// Returns the list as a ReadOnlySpan.
    /// </summary>
    /// <returns>A ReadOnlySpan over the list elements.</returns>
    public ReadOnlySpan<T> AsSpan() => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this);

    /// <summary>
    /// Returns the list as a ReadOnlyMemory.
    /// </summary>
    /// <returns>A ReadOnlyMemory over the list elements.</returns>
    public ReadOnlyMemory<T> AsMemory() => ToArray().AsMemory();
#endif

    #endregion

    #region Utility Methods

    /// <summary>
    /// Returns a random element from the list.
    /// </summary>
    /// <returns>A randomly selected element.</returns>
    public T Random() => Random(new Random());

    /// <summary>
    /// Returns a random element from the list using the specified Random instance.
    /// </summary>
    /// <param name="random">The Random instance to use.</param>
    /// <returns>A randomly selected element.</returns>
    public T Random(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return this[random.Next(Count)];
    }

    /// <summary>
    /// Returns the element with the minimum value according to a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the comparison key.</param>
    /// <returns>The element with the minimum key value.</returns>
    public T MinBy<TKey>(Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return this.OrderBy(keySelector).First();
    }

    /// <summary>
    /// Returns the element with the maximum value according to a key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the comparison key.</param>
    /// <returns>The element with the maximum key value.</returns>
    public T MaxBy<TKey>(Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        return this.OrderByDescending(keySelector).First();
    }

    /// <summary>
    /// Executes an action for each element with its index.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void ForEachWithIndex(Action<T, int> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        for (int i = 0; i < Count; i++)
        {
            action(this[i], i);
        }
    }

    /// <summary>
    /// Returns a string representation of the NonEmptyList, showing its elements.
    /// </summary>
    /// <returns>A string representing the NonEmptyList.</returns>
    public override string ToString() => $"NonEmptyList: [{string.Join(", ", this)}]";

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

        return this.SequenceEqual(other);
    }

    /// <summary>
    /// Determines whether the specified NonEmptyList is equal to this instance using a comparer.
    /// </summary>
    /// <param name="other">The other NonEmptyList to compare.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>True if the lists are equal; otherwise, false.</returns>
    public bool SequenceEqual(NonEmptyList<T> other, IEqualityComparer<T> comparer)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Count != other.Count) return false;

        return Enumerable.SequenceEqual(this, other, comparer);
    }

    /// <summary>
    /// Determines whether the specified object is equal to this instance.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is NonEmptyList<T> other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (T item in this)
        {
            hash.Add(item);
        }
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
    public static bool operator !=(NonEmptyList<T>? left, NonEmptyList<T>? right)
    {
        return !(left == right);
    }

    #endregion

    #region Private Helpers

    private void InvalidateCaches()
    {
        _cachedTail = null;
        _cachedInit = null;
    }

    #endregion
}
