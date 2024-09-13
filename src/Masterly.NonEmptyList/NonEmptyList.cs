using System.Collections;

namespace Masterly.NonEmptyList;

public class NonEmptyList<T> : List<T>, IEnumerable<T>
{
    public T Head => this[0]; // Same as First
    public NonEmptyList<T> Tail
    {
        get
        {
            if (Count == 1)
                throw new InvalidOperationException("Cannot retrieve tail from a list with only one element.");

            T[] tailItems = this.Skip(1).ToArray();
            return new NonEmptyList<T>(tailItems[0], tailItems[1..]);
        }
    }

    public T First => Head;  // First element of the list
    public T Last => this[^1];  // Last element of the list

    public NonEmptyList(T firstItem, params T[] otherItems)
    {
        if (firstItem is null)
            throw new ArgumentNullException(nameof(firstItem), "First item cannot be null");

        Add(firstItem);
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
        //if (collection is null || !collection.Any())
        //    throw new ArgumentNullException(nameof(collection), "Collection cannot be null");

        base.AddRange(collection);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"NonEmptyList: [{string.Join(", ", this)}]";
 }