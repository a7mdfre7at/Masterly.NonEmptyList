// Ignore Spelling: Deconstruct

namespace Masterly.NonEmptyList;

/// <summary>
/// A generic list that guarantees at least one element. 
/// Provides convenient access to the first (Head) and last elements, and can return the tail (rest of the list).
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class NonEmptyList<T> : List<T>
{
    private NonEmptyList<T>? _cachedTail;  // Cache for the tail

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
    /// Gets the first element of the list.
    /// </summary>
    public T First => Head;  // First element of the list

    /// <summary>
    /// Gets the last element of the list.
    /// </summary>
    public T Last => this[^1];  // Last element of the list

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

        Add(firstItem);

        if (otherItems is not null)
        {
            if (otherItems.Any(item => item is null))
                throw new ArgumentNullException(nameof(otherItems), "Other items cannot contain null values");

            if (otherItems.Any())
                AddRange(otherItems);
        }
    }

    /// <summary>
    /// Adds an item to the NonEmptyList, ensuring the item is not null.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the item is null.</exception>
    public new void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _cachedTail = null;  // Invalidate the cache when adding a new item

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

        if (!collection.Any())
            throw new ArgumentException("Collection cannot be empty", nameof(collection));

        if (collection.Any(item => item is null))
            throw new ArgumentNullException(nameof(collection), "Collection cannot contain null values");

        _cachedTail = null;  // Invalidate the cache when adding multiple items

        base.AddRange(collection);
    }

    /// <summary>
    /// Creates a NonEmptyList from an existing IEnumerable, ensuring the enumerable is not null or empty.
    /// </summary>
    /// <param name="enumerable">The enumerable to create the NonEmptyList from.</param>
    /// <returns>A NonEmptyList containing the elements of the enumerable.</returns>
    /// <exception cref="ArgumentException">Thrown if the enumerable is null or empty.</exception>
    public static NonEmptyList<T> From(IEnumerable<T> enumerable)
    {
        if (enumerable is null || !enumerable.Any())
            throw new ArgumentException("Cannot create a NonEmptyList from null or empty", nameof(enumerable));

        T head = enumerable.First();
        IEnumerable<T> tail = enumerable.Skip(1);

        return new NonEmptyList<T>(head, tail);
    }

    /// <summary>
    /// Returns a string representation of the NonEmptyList, showing its elements.
    /// </summary>
    /// <returns>A string representing the NonEmptyList.</returns>
    public override string ToString() => $"NonEmptyList: [{string.Join(", ", this)}]";

    /// <summary>
    /// Clears the NonEmptyList. This operation is not supported as a NonEmptyList cannot be empty.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public new void Clear() => throw new NotSupportedException("Cannot clear a NonEmptyList");

    /// <summary>
    /// Removes an item from the NonEmptyList, but ensures that the list is never empty.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <exception cref="InvalidOperationException">Thrown if there is only one item in the list.</exception>
    public new void Remove(T item)
    {
        if (Count == 1)
            throw new InvalidOperationException("Cannot use Remove method while the NonEmptyList contains only one item.");

        _cachedTail = null;  // Invalidate the cache when removing an item

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

        _cachedTail = null;  // Invalidate the cache when removing an item

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
        if (index is 0 && Count == count)
            throw new InvalidOperationException("Cannot remove all items from a NonEmptyList");

        _cachedTail = null;  // Invalidate the cache when removing multiple items

        base.RemoveRange(index, count);
    }

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
}