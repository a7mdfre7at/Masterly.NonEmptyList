using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// A value comparer for Entity Framework Core that enables proper change tracking for ImmutableNonEmptyList&lt;T&gt; properties.
/// </summary>
/// <typeparam name="T">The type of elements in the ImmutableNonEmptyList.</typeparam>
public class ImmutableNonEmptyListValueComparer<T> : ValueComparer<ImmutableNonEmptyList<T>>
{
    /// <summary>
    /// Creates a new instance of ImmutableNonEmptyListValueComparer.
    /// </summary>
    public ImmutableNonEmptyListValueComparer()
        : base(
            (a, b) => CompareImmutableNonEmptyLists(a, b),
            list => GetHashCodeForList(list),
            list => list) // ImmutableNonEmptyList is immutable, so we can return the same instance
    {
    }

    /// <summary>
    /// Creates a new instance of ImmutableNonEmptyListValueComparer with a custom equality comparer for elements.
    /// </summary>
    /// <param name="elementComparer">The equality comparer to use for comparing elements.</param>
    public ImmutableNonEmptyListValueComparer(IEqualityComparer<T> elementComparer)
        : base(
            (a, b) => CompareImmutableNonEmptyLists(a, b, elementComparer),
            list => GetHashCodeForList(list, elementComparer),
            list => list) // ImmutableNonEmptyList is immutable, so we can return the same instance
    {
    }

    private static bool CompareImmutableNonEmptyLists(ImmutableNonEmptyList<T>? a, ImmutableNonEmptyList<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        return a.SequenceEqual(b);
    }

    private static bool CompareImmutableNonEmptyLists(ImmutableNonEmptyList<T>? a, ImmutableNonEmptyList<T>? b, IEqualityComparer<T> comparer)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        return a.SequenceEqual(b, comparer);
    }

    private static int GetHashCodeForList(ImmutableNonEmptyList<T> list)
    {
        HashCode hash = new();
        foreach (T item in list)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    private static int GetHashCodeForList(ImmutableNonEmptyList<T> list, IEqualityComparer<T> comparer)
    {
        HashCode hash = new();
        foreach (T item in list)
        {
            hash.Add(item, comparer);
        }
        return hash.ToHashCode();
    }
}
