---
id: batching-streams
title: "Batching and Streams"
description: "Split sequences, async streams and channels into batches that are never empty, and read the first element of a stream while the rest keeps streaming."
---

Code that processes data in batches usually starts with `if (batch.Count == 0) continue;`. The batching operators in `NonEmptyListBatchingExtensions` only ever produce `NonEmptyList<T>` batches, so you can drop that check. Every batch has a `Head`, and a bulk insert or an API call never receives an empty payload.

```csharp
using Masterly.NonEmptyList;

// events is an IAsyncEnumerable<Event>; bulkWriter comes from your application.
await foreach (NonEmptyList<Event> batch in events.BatchNonEmpty(size: 100, maxWait: TimeSpan.FromSeconds(1), cancellationToken))
{
    await bulkWriter.WriteAsync(batch);   // never empty
}
```

## The operators

| Method | Source | Returns | Emits a batch when |
|--------|--------|---------|--------------------|
| `BatchNonEmpty(size)` | `IEnumerable<T>` | `IEnumerable<NonEmptyList<T>>` | `size` elements are collected, or the source ends |
| `BatchNonEmpty(size, cancellationToken)` | `IAsyncEnumerable<T>` | `IAsyncEnumerable<NonEmptyList<T>>` | `size` elements are collected, or the stream ends |
| `BatchNonEmpty(size, maxWait, cancellationToken)` | `IAsyncEnumerable<T>` | `IAsyncEnumerable<NonEmptyList<T>>` | `size` elements are collected, `maxWait` has passed since the batch's first element, or the stream ends |
| `ReadAllBatchesAsync(maxSize, cancellationToken)` | `ChannelReader<T>` | `IAsyncEnumerable<NonEmptyList<T>>` | At least one item is available. It takes everything that is ready, up to `maxSize` |
| `ToNonEmptyAsync(cancellationToken)` | `IAsyncEnumerable<T>` | `ValueTask<NonEmptyAsyncEnumerable<T>?>` | Not a batch: reads the first element and leaves the rest streaming |

The `cancellationToken` parameters are optional and default to `CancellationToken.None`. `maxSize` defaults to `int.MaxValue`.

The four batching operators share these rules:

- No batch is ever empty. An empty source produces no batches at all.
- The last batch holds whatever is left, so it can be smaller than `size`.
- The arguments are checked when you call the method, not when you start enumerating. A `null` source throws `ArgumentNullException`, a `size` or `maxSize` below 1 throws `ArgumentOutOfRangeException`, and so does a `maxWait` of zero or less.
- A batch is a `NonEmptyList<T>`, so it cannot contain `null`. A `null` element in the source makes the operator throw `ArgumentNullException` when it builds that batch.

## Batching a sequence

`BatchNonEmpty(size)` on an `IEnumerable<T>` works like `Enumerable.Chunk`, but each batch is a `NonEmptyList<T>` instead of an array. It is lazy: it reads the source only as you enumerate the batches.

```csharp
// rows is an IEnumerable<CustomerRow>; repository comes from your application.
foreach (NonEmptyList<CustomerRow> batch in rows.BatchNonEmpty(500))
{
    repository.BulkInsert(batch);
}
```

With 1,234 rows, this calls `BulkInsert` three times, with 500, 500 and 234 rows. With no rows, it does not call it at all.

:::tip

To split a list you already hold, use `list.Chunk(size)`, which returns a `NonEmptyList<NonEmptyList<T>>`. See [Type-preserving LINQ](./linq.md).

:::

## Batching an async stream by size

`BatchNonEmpty(size)` on an `IAsyncEnumerable<T>` collects `size` elements, emits them as a batch and starts the next one. When the stream ends, it emits the remainder.

```csharp
// Event, eventSource and bulkWriter come from your application.
// eventSource.ReadAsync(cancellationToken) returns IAsyncEnumerable<Event>.
await foreach (NonEmptyList<Event> batch in eventSource.ReadAsync(cancellationToken).BatchNonEmpty(100, cancellationToken))
{
    await bulkWriter.WriteAsync(batch, cancellationToken);
}
```

The operator passes the token to the source's enumerator and also checks it before each element, so cancellation stops the enumeration with `OperationCanceledException` even when the source ignores the token. A source that is waiting for its next element without observing the token is only interrupted once that element arrives.

## Batching an async stream by size and time

A size-only batch can wait a long time on a slow stream: with `size: 100` and one event per minute, the first batch arrives after more than an hour and a half. `BatchNonEmpty(size, maxWait)` also emits a batch when `maxWait` has passed, whichever comes first.

The timer starts when the first element of a batch arrives, not when the previous batch was emitted. While no element is waiting, there is nothing to flush, and the operator just waits for the stream. A quiet stream therefore produces no batches, rather than a series of empty ones.

```csharp
// Items 1, 2 and 3 arrive together, then nothing for 400 ms, then 4 and 5.
await foreach (NonEmptyList<int> batch in source.BatchNonEmpty(10, TimeSpan.FromMilliseconds(100)))
{
    Console.WriteLine(string.Join(", ", batch));
}

// 1, 2, 3   (emitted when the 100 ms timer of the first batch expires)
// 4, 5      (emitted when the stream ends)
```

A typical use is a bulk writer that should flush either every 100 events or once a second, whichever comes first:

```csharp title="AuditWriter.cs"
// AuditEvent and IAuditStore are your own types.
public sealed class AuditWriter
{
    private readonly IAuditStore _store;

    public AuditWriter(IAuditStore store)
    {
        _store = store;
    }

    public async Task RunAsync(IAsyncEnumerable<AuditEvent> events, CancellationToken cancellationToken)
    {
        await foreach (NonEmptyList<AuditEvent> batch in events.BatchNonEmpty(100, TimeSpan.FromSeconds(1), cancellationToken))
        {
            // batch.Head is always there, so the log line needs no guard
            Console.WriteLine($"Writing {batch.Count} events starting at {batch.Head.Timestamp}");
            await _store.InsertManyAsync(batch, cancellationToken);
        }
    }
}
```

**Cancellation.** The token is passed to the source's enumerator and also cancels the timer. While a batch is being collected, cancelling it makes the enumeration throw `OperationCanceledException`, even if the source ignores the token. While no element is waiting, the operator is only waiting on the source, so cancellation takes effect when the source observes the token. The batch that was being collected when you cancelled is not emitted: every batch you received is complete, and nothing partial follows.

:::note

When the timer emits a batch, the operator keeps its pending read on the source. The element that arrives next is not lost: it becomes the first element of the next batch and starts a new timer.

:::

## Reading a channel in batches

`ReadAllBatchesAsync` reads a `ChannelReader<T>` in non-empty batches. It waits until at least one item is available, then takes everything that can be read immediately, up to `maxSize`, and emits that as one batch. It does not wait for a batch to fill up. A busy channel is drained in large batches, and an idle one delivers each item as soon as it arrives.

The enumeration completes when the writer completes the channel and every item has been read. If the writer completes the channel with an exception, the remaining items are still delivered, and then that exception is thrown.

```csharp title="JobWorker.cs"
using System.Threading.Channels;
using Masterly.NonEmptyList;

// Job and IJobProcessor are your own types.
public sealed class JobWorker
{
    private readonly Channel<Job> _channel = Channel.CreateUnbounded<Job>();
    private readonly IJobProcessor _processor;

    public JobWorker(IJobProcessor processor)
    {
        _processor = processor;
    }

    public ValueTask EnqueueAsync(Job job, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(job, cancellationToken);

    public void Complete() => _channel.Writer.Complete();

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await foreach (NonEmptyList<Job> batch in _channel.Reader.ReadAllBatchesAsync(maxSize: 50, cancellationToken))
        {
            await _processor.ProcessAsync(batch, cancellationToken);
        }
    }
}
```

Cancelling the token throws `OperationCanceledException` from the wait for the next item.

## The first element now, the rest later

Sometimes you need the first element of a stream before you decide what to do with the rest: to return "no content" for an empty result, to write a header from the first row, or to pick a format. `ToNonEmptyAsync` reads exactly one element. If the stream is empty, it returns `null`. Otherwise it returns a `NonEmptyAsyncEnumerable<T>` whose `Head` is already in memory and whose `Rest` continues from the same enumerator, without reading the stream twice.

```csharp
// Row and reader come from your application.
// reader.ReadRowsAsync(cancellationToken) returns IAsyncEnumerable<Row>.
await using NonEmptyAsyncEnumerable<Row>? rows = await reader.ReadRowsAsync(cancellationToken).ToNonEmptyAsync(cancellationToken);

if (rows is null)
{
    Console.WriteLine("No rows.");
    return;
}

WriteHeader(rows.Head);               // already in memory

await foreach (Row row in rows.Rest)   // the rest streams lazily
{
    WriteRow(row);
}
```

- If the first element is `null`, `ToNonEmptyAsync` throws `InvalidOperationException`.
- An exception from the source while reading the first element propagates from `ToNonEmptyAsync`, and the enumerator is disposed.

`NonEmptyAsyncEnumerable<T>` implements `IAsyncEnumerable<T>` and `IAsyncDisposable`:

| Member | Returns | What it does |
|--------|---------|--------------|
| `Head` | `T` | The first element |
| `Rest` | `IAsyncEnumerable<T>` | The remaining elements, streamed lazily |
| `GetAsyncEnumerator(cancellationToken)` | `IAsyncEnumerator<T>` | Yields `Head`, then the rest. This is what `await foreach (T x in rows)` uses |
| `ToNonEmptyListAsync(cancellationToken)` | `ValueTask<NonEmptyList<T>>` | Buffers the head and the rest. Non-empty, so not nullable |
| `Select(selector)` | `NonEmptyAsyncEnumerable<TResult>` | Transforms every element. The head is transformed immediately |
| `BatchRest(size, cancellationToken)` | `IAsyncEnumerable<NonEmptyList<T>>` | Streams the rest in non-empty batches |
| `DisposeAsync()` | `ValueTask` | Disposes the underlying enumerator. Safe to call more than once |

You can also build one yourself from a known head and a stream: `new NonEmptyAsyncEnumerable<T>(head, rest)`. It throws `ArgumentNullException` if either is `null`.

:::warning

The rest can be read **once**. `Rest`, `await foreach` over the whole stream, `ToNonEmptyListAsync`, `BatchRest` and the rest of a `Select` result all draw from the same underlying enumerator. Reading it a second time throws `InvalidOperationException`.

:::

**Disposal.** When an enumeration of `Rest` finishes, or you break out of it, the underlying enumerator is disposed for you. If you never enumerate the rest, for example because you only needed `Head`, nothing releases the source (an open database reader, say) until you dispose the `NonEmptyAsyncEnumerable<T>`. Declaring it with `await using`, as in the examples, covers every case. After disposal, the rest is empty.

**Cancellation.** Pass the token to `ToNonEmptyAsync`. That token goes to the source's enumerator and covers the whole stream, `Head` and `Rest`. The source enumerator already exists by then, so tokens you pass later, to `ToNonEmptyListAsync`, `BatchRest` or `WithCancellation` on `Rest`, cannot reach the source; they are checked before every element instead, and stop the enumeration with `OperationCanceledException`. When you build a `NonEmptyAsyncEnumerable<T>` yourself with `new(head, rest)`, the rest is not started until it is enumerated, and the token used to enumerate it is passed on to `rest`.

```csharp
// Write the first row immediately, then insert the rest in batches of 1,000.
await using NonEmptyAsyncEnumerable<Row>? rows = await reader.ReadRowsAsync(cancellationToken).ToNonEmptyAsync(cancellationToken);

if (rows is not null)
{
    WriteHeader(rows.Head);

    await foreach (NonEmptyList<Row> batch in rows.BatchRest(1000))
    {
        await repository.InsertManyAsync(batch, cancellationToken);
    }
}
```

## Related pages

- To read a whole stream into a list, use `ToNonEmptyListAsync()` on the `IAsyncEnumerable<T>`. See [Async operations](./async-operations.md).
- For lazy, possibly infinite synchronous sequences with a known head, see [Spans and lazy sequences](./spans-and-lazy-sequences.md).
