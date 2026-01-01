namespace Masterly.NonEmptyList;

/// <summary>
/// Provides async extension methods for NonEmptyList.
/// </summary>
public static class NonEmptyListAsyncExtensions
{
    /// <summary>
    /// Asynchronously projects each element of the list into a new form.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="selector">An async transform function to apply to each element.</param>
    /// <returns>A task that represents the async operation, containing the transformed NonEmptyList.</returns>
    public static async Task<NonEmptyList<TResult>> MapAsync<T, TResult>(
        this NonEmptyList<T> source,
        Func<T, Task<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult> results = new(source.Count);

        foreach (T item in source)
        {
            TResult result = await selector(item).ConfigureAwait(false);
            results.Add(result);
        }

        return new NonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Asynchronously projects each element of the list into a new form with its index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="selector">An async transform function to apply to each element and its index.</param>
    /// <returns>A task that represents the async operation, containing the transformed NonEmptyList.</returns>
    public static async Task<NonEmptyList<TResult>> MapWithIndexAsync<T, TResult>(
        this NonEmptyList<T> source,
        Func<T, int, Task<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult> results = new(source.Count);

        for (int i = 0; i < source.Count; i++)
        {
            TResult result = await selector(source[i], i).ConfigureAwait(false);
            results.Add(result);
        }

        return new NonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Asynchronously projects each element in parallel and returns results in order.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="selector">An async transform function to apply to each element.</param>
    /// <returns>A task that represents the async operation, containing the transformed NonEmptyList.</returns>
    public static async Task<NonEmptyList<TResult>> MapParallelAsync<T, TResult>(
        this NonEmptyList<T> source,
        Func<T, Task<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        Task<TResult>[] tasks = source.Select(selector).ToArray();
        TResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return new NonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Asynchronously projects each element in parallel with a maximum degree of parallelism.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of the resulting elements.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="selector">An async transform function to apply to each element.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent operations.</param>
    /// <returns>A task that represents the async operation, containing the transformed NonEmptyList.</returns>
    public static async Task<NonEmptyList<TResult>> MapParallelAsync<T, TResult>(
        this NonEmptyList<T> source,
        Func<T, Task<TResult>> selector,
        int maxDegreeOfParallelism)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        if (maxDegreeOfParallelism < 1)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Must be at least 1");

        using SemaphoreSlim semaphore = new(maxDegreeOfParallelism);
        Task<TResult>[] tasks = source.Select(async item =>
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return await selector(item).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        TResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return new NonEmptyList<TResult>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Asynchronously executes an action for each element.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="action">An async action to execute for each element.</param>
    /// <returns>A task that represents the async operation.</returns>
    public static async Task ForEachAsync<T>(
        this NonEmptyList<T> source,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (T item in source)
        {
            await action(item).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously executes an action for each element with its index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="action">An async action to execute for each element and its index.</param>
    /// <returns>A task that represents the async operation.</returns>
    public static async Task ForEachWithIndexAsync<T>(
        this NonEmptyList<T> source,
        Func<T, int, Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        for (int i = 0; i < source.Count; i++)
        {
            await action(source[i], i).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously executes actions for all elements in parallel.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="action">An async action to execute for each element.</param>
    /// <returns>A task that represents the async operation.</returns>
    public static async Task ForEachParallelAsync<T>(
        this NonEmptyList<T> source,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        await Task.WhenAll(source.Select(action)).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously filters elements based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="predicate">An async predicate to test each element.</param>
    /// <returns>A task containing the filtered list, or null if no elements match.</returns>
    public static async Task<NonEmptyList<T>?> FilterAsync<T>(
        this NonEmptyList<T> source,
        Func<T, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        List<T> results = new();

        foreach (T item in source)
        {
            if (await predicate(item).ConfigureAwait(false))
            {
                results.Add(item);
            }
        }

        if (results.Count == 0)
            return null;

        return new NonEmptyList<T>(results[0], results.Skip(1));
    }

    /// <summary>
    /// Asynchronously applies a folder function with a seed value.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="folder">An async function that combines the accumulator and each element.</param>
    /// <returns>A task containing the folded result.</returns>
    public static async Task<TResult> FoldAsync<T, TResult>(
        this NonEmptyList<T> source,
        TResult seed,
        Func<TResult, T, Task<TResult>> folder)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(folder);

        TResult accumulator = seed;

        foreach (T item in source)
        {
            accumulator = await folder(accumulator, item).ConfigureAwait(false);
        }

        return accumulator;
    }

    /// <summary>
    /// Asynchronously checks if all elements satisfy a predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="predicate">An async predicate to test each element.</param>
    /// <returns>True if all elements satisfy the predicate; otherwise, false.</returns>
    public static async Task<bool> AllAsync<T>(
        this NonEmptyList<T> source,
        Func<T, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (T item in source)
        {
            if (!await predicate(item).ConfigureAwait(false))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Asynchronously checks if any element satisfies a predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="predicate">An async predicate to test each element.</param>
    /// <returns>True if any element satisfies the predicate; otherwise, false.</returns>
    public static async Task<bool> AnyAsync<T>(
        this NonEmptyList<T> source,
        Func<T, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (T item in source)
        {
            if (await predicate(item).ConfigureAwait(false))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Asynchronously finds the first element satisfying a predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <param name="predicate">An async predicate to test each element.</param>
    /// <returns>The first matching element, or default if none found.</returns>
    public static async Task<T?> FirstOrDefaultAsync<T>(
        this NonEmptyList<T> source,
        Func<T, Task<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (T item in source)
        {
            if (await predicate(item).ConfigureAwait(false))
                return item;
        }

        return default;
    }

    /// <summary>
    /// Attempts to asynchronously create a NonEmptyList from an async enumerable.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="source">The async enumerable source.</param>
    /// <returns>A task containing the NonEmptyList, or null if the source is empty.</returns>
    public static async Task<NonEmptyList<T>?> ToNonEmptyListAsync<T>(
        this IAsyncEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<T> items = new();

        await foreach (T item in source.ConfigureAwait(false))
        {
            items.Add(item);
        }

        if (items.Count == 0)
            return null;

        return new NonEmptyList<T>(items[0], items.Skip(1));
    }

    /// <summary>
    /// Converts a NonEmptyList to an async enumerable.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    /// <param name="source">The source NonEmptyList.</param>
    /// <returns>An async enumerable of the elements.</returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this NonEmptyList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (T item in source)
        {
            yield return item;
            await Task.CompletedTask.ConfigureAwait(false); // Allow async context switching
        }
    }
}
