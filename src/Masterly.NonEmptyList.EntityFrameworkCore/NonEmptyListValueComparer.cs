using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// A value comparer for Entity Framework Core that enables proper change tracking for NonEmptyList&lt;T&gt; properties.
/// </summary>
/// <typeparam name="T">The type of elements in the NonEmptyList.</typeparam>
public class NonEmptyListValueComparer<T> : ValueComparer<NonEmptyList<T>>
{
    /// <summary>
    /// Creates a new instance of NonEmptyListValueComparer.
    /// </summary>
    public NonEmptyListValueComparer()
        : base(
            (a, b) => CompareNonEmptyLists(a, b),
            list => GetHashCodeForList(list),
            list => CreateSnapshot(list))
    {
    }

    /// <summary>
    /// Creates a new instance of NonEmptyListValueComparer with a custom equality comparer for elements.
    /// </summary>
    /// <param name="elementComparer">The equality comparer to use for comparing elements.</param>
    public NonEmptyListValueComparer(IEqualityComparer<T> elementComparer)
        : base(
            (a, b) => CompareNonEmptyLists(a, b, elementComparer),
            list => GetHashCodeForList(list, elementComparer),
            list => CreateSnapshot(list))
    {
    }

    private static bool CompareNonEmptyLists(NonEmptyList<T>? a, NonEmptyList<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        return a.SequenceEqual(b);
    }

    private static bool CompareNonEmptyLists(NonEmptyList<T>? a, NonEmptyList<T>? b, IEqualityComparer<T> comparer)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        return a.SequenceEqual(b, comparer);
    }

    private static int GetHashCodeForList(NonEmptyList<T> list)
    {
        HashCode hash = new();
        foreach (T item in list)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    private static int GetHashCodeForList(NonEmptyList<T> list, IEqualityComparer<T> comparer)
    {
        HashCode hash = new();
        foreach (T item in list)
        {
            hash.Add(item, comparer);
        }
        return hash.ToHashCode();
    }

    private static NonEmptyList<T> CreateSnapshot(NonEmptyList<T> source)
    {
        return new NonEmptyList<T>(source.Head, source.Skip(1).ToList());
    }
}
