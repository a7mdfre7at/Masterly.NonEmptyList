using System.Numerics;

namespace Masterly.NonEmptyList;

/// <summary>
/// Numeric aggregations that are total on a non-empty list. Because there is always at least one element,
/// <c>Sum</c> needs no identity element, <c>Average</c> and <c>Median</c> never divide by zero, and none of these
/// operators has an "empty sequence" exception path. Built on generic math, so they work for any type that
/// implements the relevant <see cref="System.Numerics"/> interfaces, including user-defined types such as <c>Money</c>.
/// </summary>
public static class NonEmptyListMathExtensions
{
    #region Sum

    /// <summary>
    /// Adds all elements together using checked arithmetic. Works for any type with a <c>+</c> operator and needs no zero value.
    /// </summary>
    public static T Sum<T>(this NonEmptyList<T> source) where T : IAdditionOperators<T, T, T>
    {
        ArgumentNullException.ThrowIfNull(source);

        T total = source.Items[0];
        for (int i = 1; i < source.Count; i++)
            total = checked(total + source.Items[i]);

        return total;
    }

    /// <summary>
    /// Adds the projected values together using checked arithmetic. Works for any result type with a <c>+</c> operator.
    /// </summary>
    public static TResult Sum<T, TResult>(this NonEmptyList<T> source, Func<T, TResult> selector)
        where TResult : IAdditionOperators<TResult, TResult, TResult>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        TResult total = selector(source.Items[0]);
        for (int i = 1; i < source.Count; i++)
            total = checked(total + selector(source.Items[i]));

        return total;
    }

    /// <summary>
    /// Multiplies all elements together using checked arithmetic. Works for any type with a <c>*</c> operator and needs no one value.
    /// </summary>
    public static T Product<T>(this NonEmptyList<T> source) where T : IMultiplyOperators<T, T, T>
    {
        ArgumentNullException.ThrowIfNull(source);

        T total = source.Items[0];
        for (int i = 1; i < source.Count; i++)
            total = checked(total * source.Items[i]);

        return total;
    }

    #endregion

    #region Average

    /// <summary>
    /// Computes the average. Returns the same numeric type as the elements, so it is intended for floating-point
    /// and user-defined numeric types. Integer lists use the overloads that return <see cref="double"/>, matching LINQ.
    /// </summary>
    public static T Average<T>(this NonEmptyList<T> source) where T : INumberBase<T>
    {
        ArgumentNullException.ThrowIfNull(source);

        T total = source.Items[0];
        for (int i = 1; i < source.Count; i++)
            total += source.Items[i];

        return total / T.CreateChecked(source.Count);
    }

    /// <summary>Computes the average of the projected values in the projected numeric type.</summary>
    public static TResult Average<T, TResult>(this NonEmptyList<T> source, Func<T, TResult> selector)
        where TResult : INumberBase<TResult>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        TResult total = selector(source.Items[0]);
        for (int i = 1; i < source.Count; i++)
            total += selector(source.Items[i]);

        return total / TResult.CreateChecked(source.Count);
    }

    /// <summary>Computes the average of a list of integers as a <see cref="double"/>, matching LINQ.</summary>
    public static double Average(this NonEmptyList<int> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        long total = 0;
        foreach (int item in source.Items)
            total += item;

        return (double)total / source.Count;
    }

    /// <summary>Computes the average of a list of long integers as a <see cref="double"/>, matching LINQ.</summary>
    public static double Average(this NonEmptyList<long> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        decimal total = 0;
        foreach (long item in source.Items)
            total += item;

        return (double)(total / source.Count);
    }

    /// <summary>Computes the average of projected integers as a <see cref="double"/>, matching LINQ.</summary>
    public static double Average<T>(this NonEmptyList<T> source, Func<T, int> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        long total = 0;
        foreach (T item in source.Items)
            total += selector(item);

        return (double)total / source.Count;
    }

    /// <summary>Computes the average of projected long integers as a <see cref="double"/>, matching LINQ.</summary>
    public static double Average<T>(this NonEmptyList<T> source, Func<T, long> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        decimal total = 0;
        foreach (T item in source.Items)
            total += selector(item);

        return (double)(total / source.Count);
    }

    #endregion

    #region Median and mode

    /// <summary>
    /// Returns the median. For an even number of elements the two middle values are averaged.
    /// Integer lists use the overloads that return <see cref="double"/>.
    /// </summary>
    public static T Median<T>(this NonEmptyList<T> source) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(source);

        T[] sorted = source.ToArray();
        Array.Sort(sorted);

        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / T.CreateChecked(2);
    }

    /// <summary>Returns the median of a list of integers as a <see cref="double"/>.</summary>
    public static double Median(this NonEmptyList<int> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int[] sorted = source.ToArray();
        Array.Sort(sorted);

        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 1
            ? sorted[mid]
            : ((double)sorted[mid - 1] + sorted[mid]) / 2;
    }

    /// <summary>Returns the median of a list of long integers as a <see cref="double"/>.</summary>
    public static double Median(this NonEmptyList<long> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        long[] sorted = source.ToArray();
        Array.Sort(sorted);

        int mid = sorted.Length / 2;
        return sorted.Length % 2 == 1
            ? sorted[mid]
            : ((double)sorted[mid - 1] + sorted[mid]) / 2;
    }

    /// <summary>
    /// Returns the most frequent element. Ties are resolved in favor of the element that appears first.
    /// </summary>
    public static T Mode<T>(this NonEmptyList<T> source) where T : notnull
        => Mode(source, null);

    /// <summary>
    /// Returns the most frequent element according to a comparer. Ties are resolved in favor of the element that appears first.
    /// </summary>
    public static T Mode<T>(this NonEmptyList<T> source, IEqualityComparer<T>? comparer) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        Dictionary<T, (T Representative, int Count, int FirstIndex)> counts = new(comparer);
        T best = source.Items[0];
        int bestCount = 0;
        int bestFirstIndex = 0;

        for (int i = 0; i < source.Count; i++)
        {
            T item = source.Items[i];
            (T representative, int count, int firstIndex) = counts.TryGetValue(item, out (T Representative, int Count, int FirstIndex) existing)
                ? (existing.Representative, existing.Count + 1, existing.FirstIndex)
                : (item, 1, i);

            counts[item] = (representative, count, firstIndex);

            if (count > bestCount || (count == bestCount && firstIndex < bestFirstIndex))
            {
                best = representative;
                bestCount = count;
                bestFirstIndex = firstIndex;
            }
        }

        return best;
    }

    #endregion
}
