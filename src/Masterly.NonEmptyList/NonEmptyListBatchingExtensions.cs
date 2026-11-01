using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Masterly.NonEmptyList;

/// <summary>
/// Batching operators for streams, channels and sequences that can only emit non-empty batches, and
/// <see cref="NonEmptyAsyncEnumerable{T}"/> for "the first item is already here, the rest streams".
/// Every batch consumer starts with <c>if (batch.Count == 0) continue;</c>. These operators delete that line.
/// </summary>
public static class NonEmptyListBatchingExtensions
{
    /// <summary>
    /// Groups a sequence into batches of at most <paramref name="size"/> elements. Every batch is non-empty, and the
    /// last batch holds the remainder. An empty source yields nothing.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if size is less than 1.</exception>
    public static IEnumerable<NonEmptyList<T>> BatchNonEmpty<T>(this IEnumerable<T> source, int size)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1");

        return Iterator();

        IEnumerable<NonEmptyList<T>> Iterator()
        {
            List<T>? batch = null;
            foreach (T item in source)
            {
                batch ??= new List<T>(size);
                batch.Add(item);
                if (batch.Count == size)
                {
                    yield return NonEmptyList<T>.FromTrustedList(batch);
                    batch = null;
                }
            }

            if (batch is not null)
                yield return NonEmptyList<T>.FromTrustedList(batch);
        }
    }

    /// <summary>
    /// Groups an async stream into batches of at most <paramref name="size"/> elements. Every batch is non-empty.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if size is less than 1.</exception>
    public static IAsyncEnumerable<NonEmptyList<T>> BatchNonEmpty<T>(this IAsyncEnumerable<T> source, int size, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1");

        return Iterator(cancellationToken);

        async IAsyncEnumerable<NonEmptyList<T>> Iterator([EnumeratorCancellation] CancellationToken ct)
        {
            List<T>? batch = null;
            await foreach (T item in source.WithCancellation(ct).ConfigureAwait(false))
            {
                batch ??= new List<T>(size);
                batch.Add(item);
                if (batch.Count == size)
                {
                    yield return NonEmptyList<T>.FromTrustedList(batch);
                    batch = null;
                }
            }

            if (batch is not null)
                yield return NonEmptyList<T>.FromTrustedList(batch);
        }
    }

    /// <summary>
    /// Groups an async stream into batches, emitting a batch when it reaches <paramref name="size"/> elements or when
    /// <paramref name="maxWait"/> has elapsed since the batch's first element, whichever comes first. A batch is never
    /// emitted empty: the timer only starts once an element is waiting.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if size is less than 1 or maxWait is not positive.</exception>
    public static IAsyncEnumerable<NonEmptyList<T>> BatchNonEmpty<T>(this IAsyncEnumerable<T> source, int size, TimeSpan maxWait, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be at least 1");
        if (maxWait <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxWait), "Wait must be positive");

        return Iterator(cancellationToken);

        async IAsyncEnumerable<NonEmptyList<T>> Iterator([EnumeratorCancellation] CancellationToken ct)
        {
            IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator(ct);
            try
            {
                Task<bool>? pending = null;
                List<T>? batch = null;
                long deadline = 0;

                while (true)
                {
                    pending ??= enumerator.MoveNextAsync().AsTask();

                    Task completed;
                    if (batch is null)
                    {
                        completed = pending;
                        await completed.ConfigureAwait(false);
                    }
                    else
                    {
                        long remaining = deadline - Environment.TickCount64;
                        if (remaining <= 0)
                        {
                            yield return NonEmptyList<T>.FromTrustedList(batch);
                            batch = null;
                            continue;
                        }

                        using (CancellationTokenSource delayCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            Task delay = Task.Delay(TimeSpan.FromMilliseconds(remaining), delayCts.Token);
                            completed = await Task.WhenAny(pending, delay).ConfigureAwait(false);
                            delayCts.Cancel();
                        }

                        if (completed != pending)
                        {
                            ct.ThrowIfCancellationRequested();
                            yield return NonEmptyList<T>.FromTrustedList(batch);
                            batch = null;
                            continue;
                        }
                    }

                    bool hasNext = await pending.ConfigureAwait(false);
                    pending = null;
                    if (!hasNext)
                        break;

                    if (batch is null)
                    {
                        batch = new List<T>(size);
                        deadline = Environment.TickCount64 + (long)maxWait.TotalMilliseconds;
                    }

                    batch.Add(enumerator.Current);
                    if (batch.Count == size)
                    {
                        yield return NonEmptyList<T>.FromTrustedList(batch);
                        batch = null;
                    }
                }

                if (batch is not null)
                    yield return NonEmptyList<T>.FromTrustedList(batch);
            }
            finally
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Reads a channel in non-empty batches. Each batch contains everything that was immediately available, up to
    /// <paramref name="maxSize"/>, so a busy channel is drained in large batches and an idle one delivers each item promptly.
    /// Completes when the channel completes.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxSize is less than 1.</exception>
    public static IAsyncEnumerable<NonEmptyList<T>> ReadAllBatchesAsync<T>(this ChannelReader<T> reader, int maxSize = int.MaxValue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (maxSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Size must be at least 1");

        return Iterator(cancellationToken);

        async IAsyncEnumerable<NonEmptyList<T>> Iterator([EnumeratorCancellation] CancellationToken ct)
        {
            while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                List<T>? batch = null;
                while ((batch is null || batch.Count < maxSize) && reader.TryRead(out T? item))
                {
                    batch ??= new List<T>();
                    batch.Add(item);
                }

                if (batch is not null)
                    yield return NonEmptyList<T>.FromTrustedList(batch);
            }
        }
    }

    /// <summary>
    /// Pulls the first element of an async stream. Returns <see langword="null"/> if the stream is empty; otherwise the
    /// head is in memory and <see cref="NonEmptyAsyncEnumerable{T}.Rest"/> continues lazily from the same enumerator.
    /// </summary>
    public static async ValueTask<NonEmptyAsyncEnumerable<T>?> ToNonEmptyAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator(cancellationToken);
        bool hasFirst;
        try
        {
            hasFirst = await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
        catch
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        if (!hasFirst)
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
            return null;
        }

        T head = enumerator.Current;
        if (head is null)
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException("Stream cannot contain null values");
        }

        return new NonEmptyAsyncEnumerable<T>(head, enumerator);
    }
}

/// <summary>
/// An async stream whose first element has already been read. <see cref="Head"/> is in memory; <see cref="Rest"/>
/// streams the remaining elements lazily from the original enumerator and can be consumed once.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public sealed class NonEmptyAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncDisposable
{
    private IAsyncEnumerator<T>? _rest;
    private bool _consumed;

    internal NonEmptyAsyncEnumerable(T head, IAsyncEnumerator<T> rest)
    {
        Head = head;
        _rest = rest;
    }

    /// <summary>Creates a stream from a known head and a lazily produced rest.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the head or the rest is null.</exception>
    public NonEmptyAsyncEnumerable(T head, IAsyncEnumerable<T> rest)
    {
        ArgumentNullException.ThrowIfNull(head);
        ArgumentNullException.ThrowIfNull(rest);
        Head = head;
        _rest = rest.GetAsyncEnumerator();
    }

    /// <summary>The first element. Already in memory.</summary>
    public T Head { get; }

    /// <summary>The remaining elements, streamed lazily. Can be enumerated once.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the rest has already been enumerated.</exception>
    public IAsyncEnumerable<T> Rest => RestIterator();

    /// <summary>Enumerates the head followed by the rest. Can be enumerated once.</summary>
    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        yield return Head;
        await foreach (T item in RestIterator().WithCancellation(cancellationToken).ConfigureAwait(false))
            yield return item;
    }

    /// <summary>Buffers the whole stream. The result is non-empty because the head is known.</summary>
    public async ValueTask<NonEmptyList<T>> ToNonEmptyListAsync(CancellationToken cancellationToken = default)
    {
        List<T> items = new() { Head };
        await foreach (T item in RestIterator().WithCancellation(cancellationToken).ConfigureAwait(false))
            items.Add(item);
        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>Transforms every element. The head is transformed eagerly.</summary>
    public NonEmptyAsyncEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new NonEmptyAsyncEnumerable<TResult>(selector(Head), SelectRest(selector));
    }

    /// <summary>Streams the remaining elements in non-empty batches.</summary>
    public IAsyncEnumerable<NonEmptyList<T>> BatchRest(int size, CancellationToken cancellationToken = default)
        => RestIterator().BatchNonEmpty(size, cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        IAsyncEnumerator<T>? rest = Interlocked.Exchange(ref _rest, null);
        if (rest is not null)
            await rest.DisposeAsync().ConfigureAwait(false);
    }

    private async IAsyncEnumerable<T> RestIterator()
    {
        if (_consumed)
            throw new InvalidOperationException("The rest of a NonEmptyAsyncEnumerable can only be enumerated once");

        _consumed = true;
        IAsyncEnumerator<T>? rest = _rest;
        if (rest is null)
            yield break;

        try
        {
            while (await rest.MoveNextAsync().ConfigureAwait(false))
                yield return rest.Current;
        }
        finally
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    private async IAsyncEnumerable<TResult> SelectRest<TResult>(Func<T, TResult> selector)
    {
        await foreach (T item in RestIterator().ConfigureAwait(false))
            yield return selector(item);
    }
}
