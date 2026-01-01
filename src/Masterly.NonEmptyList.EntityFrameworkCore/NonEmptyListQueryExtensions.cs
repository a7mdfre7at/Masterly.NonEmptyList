using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// Extension methods for querying entities with NonEmptyList properties.
/// </summary>
public static class NonEmptyListQueryExtensions
{
    /// <summary>
    /// Converts the query results to a NonEmptyList, throwing if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <returns>A NonEmptyList containing the query results.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query returns no results.</exception>
    public static NonEmptyList<T> ToNonEmptyList<T>(this IQueryable<T> query)
    {
        List<T> results = query.ToList();

        if (results.Count == 0)
            throw new InvalidOperationException("Query returned no results. Cannot create a NonEmptyList from an empty result set.");

        return NonEmptyList<T>.From(results);
    }

    /// <summary>
    /// Asynchronously converts the query results to a NonEmptyList, throwing if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a NonEmptyList with the query results.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query returns no results.</exception>
    public static async Task<NonEmptyList<T>> ToNonEmptyListAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        List<T> results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        if (results.Count == 0)
            throw new InvalidOperationException("Query returned no results. Cannot create a NonEmptyList from an empty result set.");

        return NonEmptyList<T>.From(results);
    }

    /// <summary>
    /// Attempts to convert the query results to a NonEmptyList.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <param name="result">When successful, contains the NonEmptyList; otherwise, null.</param>
    /// <returns>True if the query returned at least one result; otherwise, false.</returns>
    public static bool TryToNonEmptyList<T>(this IQueryable<T> query, out NonEmptyList<T>? result)
    {
        List<T> results = query.ToList();

        if (results.Count == 0)
        {
            result = null;
            return false;
        }

        result = NonEmptyList<T>.From(results);
        return true;
    }

    /// <summary>
    /// Asynchronously attempts to convert the query results to a NonEmptyList.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a tuple of success flag and the NonEmptyList (or null).</returns>
    public static async Task<(bool Success, NonEmptyList<T>? Result)> TryToNonEmptyListAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        List<T> results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        if (results.Count == 0)
            return (false, null);

        return (true, NonEmptyList<T>.From(results));
    }

    /// <summary>
    /// Converts the query results to a NonEmptyList, or returns null if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <returns>A NonEmptyList if results exist; otherwise, null.</returns>
    public static NonEmptyList<T>? ToNonEmptyListOrNull<T>(this IQueryable<T> query)
    {
        List<T> results = query.ToList();

        if (results.Count == 0)
            return null;

        return NonEmptyList<T>.From(results);
    }

    /// <summary>
    /// Asynchronously converts the query results to a NonEmptyList, or returns null if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a NonEmptyList if results exist; otherwise, null.</returns>
    public static async Task<NonEmptyList<T>?> ToNonEmptyListOrNullAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        List<T> results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        if (results.Count == 0)
            return null;

        return NonEmptyList<T>.From(results);
    }

    /// <summary>
    /// Converts the query results to an ImmutableNonEmptyList, throwing if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <returns>An ImmutableNonEmptyList containing the query results.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query returns no results.</exception>
    public static ImmutableNonEmptyList<T> ToImmutableNonEmptyList<T>(this IQueryable<T> query)
    {
        List<T> results = query.ToList();

        if (results.Count == 0)
            throw new InvalidOperationException("Query returned no results. Cannot create an ImmutableNonEmptyList from an empty result set.");

        return ImmutableNonEmptyList<T>.From(results);
    }

    /// <summary>
    /// Asynchronously converts the query results to an ImmutableNonEmptyList, throwing if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing an ImmutableNonEmptyList with the query results.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the query returns no results.</exception>
    public static async Task<ImmutableNonEmptyList<T>> ToImmutableNonEmptyListAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        List<T> results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        if (results.Count == 0)
            throw new InvalidOperationException("Query returned no results. Cannot create an ImmutableNonEmptyList from an empty result set.");

        return ImmutableNonEmptyList<T>.From(results);
    }

    /// <summary>
    /// Converts the query results to an ImmutableNonEmptyList, or returns null if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <returns>An ImmutableNonEmptyList if results exist; otherwise, null.</returns>
    public static ImmutableNonEmptyList<T>? ToImmutableNonEmptyListOrNull<T>(this IQueryable<T> query)
    {
        List<T> results = query.ToList();

        if (results.Count == 0)
            return null;

        return ImmutableNonEmptyList<T>.From(results);
    }

    /// <summary>
    /// Asynchronously converts the query results to an ImmutableNonEmptyList, or returns null if the result is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing an ImmutableNonEmptyList if results exist; otherwise, null.</returns>
    public static async Task<ImmutableNonEmptyList<T>?> ToImmutableNonEmptyListOrNullAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        List<T> results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        if (results.Count == 0)
            return null;

        return ImmutableNonEmptyList<T>.From(results);
    }
}
