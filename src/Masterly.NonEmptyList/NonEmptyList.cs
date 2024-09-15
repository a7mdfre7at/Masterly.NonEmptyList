namespace Masterly.NonEmptyList;

public class NonEmptyList<T> : List<T>
{
    public T Head => this[0]; // Same as First

    public NonEmptyList<T>? Tail
    {
        get
        {
            if (Count == 1)
                return null;

            T[] tailItems = this.Skip(1).ToArray();
            return new NonEmptyList<T>(tailItems[0], tailItems[1..]);
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

        if (otherItems is not null && otherItems.Any())
            AddRange(otherItems);
    }

    public new void Add(T item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item), "Item cannot be null");

        base.Add(item);
    }

    public new void AddRange(IEnumerable<T> collection)
    {
        if (collection is null || !collection.Any())
            throw new ArgumentNullException(nameof(collection), "Collection cannot be null");

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
}