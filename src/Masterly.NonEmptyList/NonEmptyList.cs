// Ignore Spelling: Deconstruct

namespace Masterly.NonEmptyList;

public class NonEmptyList<T> : List<T>
{
    public T Head => this[0]; // Same as First

    private NonEmptyList<T>? _cachedTail;  // Cache for the tail

    public NonEmptyList<T>? Tail
    {
        get
        {
            if (_cachedTail != null)
                return _cachedTail; // Return cached version if it exists

            if (Count == 1)
                return null;

            _cachedTail = new NonEmptyList<T>(this[1], this.Skip(2));  // Cache the result
            return _cachedTail;
        }
    }

    public T First => Head;  // First element of the list
    public T Last => this[^1];  // Last element of the list

    public NonEmptyList(T firstItem, params T[] otherItems) : this(firstItem, otherItems.AsEnumerable()) { }

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

    public new void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _cachedTail = null;  // Invalidate the cache when adding a new item

        base.Add(item);
    }

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

    public static NonEmptyList<T> From(IEnumerable<T> enumerable)
    {
        if (enumerable is null || !enumerable.Any())
            throw new ArgumentException("Cannot create a NonEmptyList from null or empty", nameof(enumerable));

        T head = enumerable.First();
        IEnumerable<T> tail = enumerable.Skip(1);

        return new NonEmptyList<T>(head, tail);
    }

    public override string ToString() => $"NonEmptyList: [{string.Join(", ", this)}]";

    public new void Clear() => throw new NotSupportedException("Cannot clear a NonEmptyList");

    public new void Remove(T item)
    {
        if (Count == 1)
            throw new InvalidOperationException("Cannot use Remove method while the NonEmptyList contains only one item.");

        _cachedTail = null;  // Invalidate the cache when removing an item

        base.Remove(item);
    }

    public new void RemoveAt(int index)
    {
        if (Count == 1)
            throw new InvalidOperationException("Cannot use RemoveAt method while the NonEmptyList contains only one item.");

        _cachedTail = null;  // Invalidate the cache when removing an item

        base.RemoveAt(index);
    }

    public new void RemoveAll(Predicate<T> match) => throw new NotSupportedException("RemoveAll method is not supported!");

    public new void RemoveRange(int index, int count)
    {
        if (index is 0 && Count == count)
            throw new InvalidOperationException("Cannot remove all items from a NonEmptyList");

        _cachedTail = null;  // Invalidate the cache when removing multiple items

        base.RemoveRange(index, count);
    }

    public void Deconstruct(out T head, out NonEmptyList<T>? tail)
    {
        head = Head;
        tail = Tail;
    }
}