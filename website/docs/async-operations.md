---
id: async-operations
title: "Async Operations"
description: "Map, filter, fold and iterate a non-empty list with async delegates, sequentially or in parallel, and convert to and from IAsyncEnumerable."
---

`NonEmptyListAsyncExtensions` adds async versions of the everyday list operations. You pass a delegate that returns a `Task`, and you get back a `Task` of the result. Where the result can never be empty, it is a `NonEmptyList<T>`, so mapping a non-empty list of IDs to their records gives you a non-empty list of records.

```csharp
using Masterly.NonEmptyList;

// customerIds and repository come from your application.
// repository.GetAsync(int id) returns Task<Customer>.
NonEmptyList<int> customerIds = [17, 42, 99];

NonEmptyList<Customer> customers = await customerIds.MapAsync(id => repository.GetAsync(id));
Customer first = customers.Head;
```

## The methods

| Method | Delegate | Returns | Runs |
|--------|----------|---------|------|
| `MapAsync(selector)` | `Func<T, Task<TResult>>` | `Task<NonEmptyList<TResult>>` | One at a time, in order |
| `MapWithIndexAsync(selector)` | `Func<T, int, Task<TResult>>` | `Task<NonEmptyList<TResult>>` | One at a time, in order |
| `MapParallelAsync(selector)` | `Func<T, Task<TResult>>` | `Task<NonEmptyList<TResult>>` | All at once |
| `MapParallelAsync(selector, maxDegreeOfParallelism)` | `Func<T, Task<TResult>>` | `Task<NonEmptyList<TResult>>` | At most `maxDegreeOfParallelism` at once |
| `ForEachAsync(action)` | `Func<T, Task>` | `Task` | One at a time, in order |
| `ForEachWithIndexAsync(action)` | `Func<T, int, Task>` | `Task` | One at a time, in order |
| `ForEachParallelAsync(action)` | `Func<T, Task>` | `Task` | All at once |
| `FilterAsync(predicate)` | `Func<T, Task<bool>>` | `Task<NonEmptyList<T>?>` | One at a time, in order |
| `FoldAsync(seed, folder)` | `Func<TResult, T, Task<TResult>>` | `Task<TResult>` | One at a time, in order |
| `AllAsync(predicate)` | `Func<T, Task<bool>>` | `Task<bool>` | One at a time, stops at the first `false` |
| `AnyAsync(predicate)` | `Func<T, Task<bool>>` | `Task<bool>` | One at a time, stops at the first `true` |
| `FirstOrDefaultAsync(predicate)` | `Func<T, Task<bool>>` | `Task<T?>` | One at a time, stops at the first match |
| `ToNonEmptyListAsync()` on `IAsyncEnumerable<T>` | none | `Task<NonEmptyList<T>?>` | Reads the whole stream |
| `ToAsyncEnumerable()` | none | `IAsyncEnumerable<T>` | Yields each element |

"One at a time" means the next delegate call starts only after the previous task has completed. Every method throws `ArgumentNullException` when the list or the delegate is `null`.

## Mapping

`MapAsync` and `MapWithIndexAsync` await each element in turn and keep the order of the source. Use them when the work must be sequential, for example because it shares a database connection or a `DbContext`.

```csharp
NonEmptyList<string> labels = await customerIds.MapWithIndexAsync(
    async (id, index) => $"{index + 1}. {await repository.GetNameAsync(id)}");
```

Use `MapParallelAsync` when the calls are independent and can overlap. The results still come back in the order of the source list, not in completion order.

```csharp
// httpClient comes from your application.
NonEmptyList<string> urls = ["https://example.com/a", "https://example.com/b", "https://example.com/c"];

// Starts every request at once
NonEmptyList<string> pages = await urls.MapParallelAsync(url => httpClient.GetStringAsync(url));

// At most 2 requests in flight at any time
NonEmptyList<string> throttled = await urls.MapParallelAsync(url => httpClient.GetStringAsync(url), maxDegreeOfParallelism: 2);
```

- Without a limit, `MapParallelAsync` calls the selector for every element straight away and waits for all the tasks with `Task.WhenAll`.
- With `maxDegreeOfParallelism`, each call is scheduled with `Task.Run` and a semaphore lets at most that many run at the same time. Your selector therefore runs on thread-pool threads, so do not use it for work that needs the caller's context or is not thread-safe.
- `maxDegreeOfParallelism` must be at least 1. Otherwise `MapParallelAsync` throws `ArgumentOutOfRangeException`.

:::note

The mapped list is a `NonEmptyList<TResult>`, so it cannot hold `null`. If a selector returns `null`, the map throws `ArgumentNullException` once all the results are in.

:::

## Iterating

`ForEachAsync` and `ForEachWithIndexAsync` run an async action for each element, one after another. `ForEachParallelAsync` starts the action for every element at once and completes when all of them have completed. It has no parallelism limit, so for large lists or rate-limited services, use `MapParallelAsync` with `maxDegreeOfParallelism` instead.

```csharp
// mailer comes from your application.
await recipients.ForEachAsync(r => mailer.SendAsync(r));

await recipients.ForEachWithIndexAsync((r, i) => mailer.SendAsync(r, priority: i == 0));

await recipients.ForEachParallelAsync(r => mailer.SendAsync(r));
```

## Filtering and searching

`FilterAsync` can reject every element, so it returns `NonEmptyList<T>?`. The result is `null` when nothing matches.

```csharp
// inventory comes from your application. IsInStockAsync returns Task<bool>.
NonEmptyList<string>? available = await skus.FilterAsync(sku => inventory.IsInStockAsync(sku));

if (available is null)
{
    // Nothing in the order is in stock
    return;
}

string firstAvailable = available.Head;
```

`AllAsync` and `AnyAsync` stop calling the predicate as soon as the answer is known. `FirstOrDefaultAsync` returns the first element that matches, or `default` if none does.

```csharp
bool allValid = await addresses.AllAsync(a => validator.IsValidAsync(a));
bool anyBlocked = await accounts.AnyAsync(a => fraud.IsBlockedAsync(a));
Account? suspicious = await accounts.FirstOrDefaultAsync(a => fraud.IsBlockedAsync(a));
```

:::warning

For a list of value types, `FirstOrDefaultAsync` returns `default(T)` when nothing matches, for example `0` for `int`, not `null`. You cannot tell "no match" from a matching `0`. Use `FilterAsync` and check for `null`, or project to a nullable type first.

:::

## Folding

`FoldAsync` combines the elements with an async folder, starting from a seed, from left to right. The folder for one element runs only after the previous one has completed.

```csharp
// pricing comes from your application. ApplyAsync(decimal total, Discount d) returns Task<decimal>.
decimal finalPrice = await discounts.FoldAsync(basePrice, (total, discount) => pricing.ApplyAsync(total, discount));
```

## Converting to and from IAsyncEnumerable

`ToAsyncEnumerable()` exposes a list as an `IAsyncEnumerable<T>`, for APIs that consume async streams. `ToNonEmptyListAsync()` goes the other way: it reads an `IAsyncEnumerable<T>` to the end and returns a `NonEmptyList<T>`, or `null` if the stream had no elements.

```csharp
// reader.ReadOrdersAsync(cancellationToken) returns IAsyncEnumerable<Order>.
NonEmptyList<Order>? orders = await reader.ReadOrdersAsync(cancellationToken).ToNonEmptyListAsync();

if (orders is null)
{
    return;
}

IAsyncEnumerable<Order> stream = orders.ToAsyncEnumerable();
```

`ToNonEmptyListAsync()` buffers the whole stream before it returns. To get the first element as soon as it arrives and stream the rest, use `ToNonEmptyAsync()` instead. To process a long stream in non-empty chunks, use `BatchNonEmpty`. Both are covered in [Batching and streams](./batching-streams.md).

## Cancellation

None of the methods in `NonEmptyListAsyncExtensions` takes a `CancellationToken`. Pass your token to the async calls inside the delegate. When that call throws `OperationCanceledException`, the operation stops and the exception propagates.

```csharp
NonEmptyList<Customer> customers = await customerIds.MapAsync(
    id => repository.GetAsync(id, cancellationToken));
```

For `ToNonEmptyListAsync()`, give the token to the source stream, as in the `ReadOrdersAsync(cancellationToken)` example above.

## Errors

- The sequential methods stop at the first delegate that throws. The exception propagates, and the remaining elements are not processed.
- When a task faults in one of the parallel methods, the method still waits for every other task, and then the await throws the first exception, as `Task.WhenAll` does.
- The methods use `ConfigureAwait(false)` internally. After the first awaited call, your delegates may run on a thread-pool thread rather than on the caller's synchronization context.

For the synchronous versions of these operations (`Map`, `MapWithIndex`, `Fold`, `ForEachWithIndex` and others), see [Functional operations](./functional-operations.md).
