---
id: spans-and-lazy-sequences
title: "Spans and Lazy Sequences"
description: "NonEmptySpan<T> for allocation-free access to at least one element, and NonEmptyEnumerable<T> for lazy, possibly infinite sequences whose first element is known."
---

`NonEmptyList<T>` is a materialized list on the heap. Two other types carry the same "at least one element" guarantee for different situations:

- `NonEmptySpan<T>` is a stack-only view over existing memory. Use it in hot paths where you want `Head`, `Max` or `Sum` without an empty check and without allocating.
- `NonEmptyEnumerable<T>` is a lazy sequence whose first element is already known and whose rest is produced on demand, possibly forever. Use it for generated sequences such as powers of two, retry delays or a repeating rotation.

## Non-empty spans

`NonEmptySpan<T>` is a `readonly ref struct` that wraps a `ReadOnlySpan<T>` with at least one element. Every member works without an emptiness check, and `Head`, `Last`, `Max`, `Min`, `Sum` and `Average` do not allocate.

```csharp
using Masterly.NonEmptyList;

NonEmptySpan<int> readings = NonEmptySpan.From(stackalloc int[] { 3, 1, 2 });

int first = readings.Head;       // 3
int highest = readings.Max();    // 3
int total = readings.Sum();      // 6, checked
```

### Getting a NonEmptySpan

| Method | Input | Empty input |
|--------|-------|-------------|
| `NonEmptySpan.From(span)` | `ReadOnlySpan<T>` or `Span<T>` | Throws `ArgumentException` |
| `NonEmptySpan.From(array)` | `T[]` | Throws `ArgumentException`; a `null` array throws `ArgumentNullException` |
| `NonEmptySpan.TryFrom(span, out NonEmptySpan<T> result)` | `ReadOnlySpan<T>` | Returns `false` |
| `list.AsNonEmptySpan()` | `NonEmptyList<T>` | Cannot be empty |
| `list.AsNonEmptySpan()` | `ImmutableNonEmptyList<T>` | Cannot be empty |

`AsNonEmptySpan()` does not copy. It views the list's current storage directly, so it is the cheapest way to run a tight loop over a list.

```csharp
NonEmptyList<double> samples = [12.5, 9.75, 14.0];

NonEmptySpan<double> view = samples.AsNonEmptySpan();
double peak = view.Max();
double mean = view.Average();

// buffer is a double[] and count an int from your application
if (NonEmptySpan.TryFrom(buffer.AsSpan(0, count), out NonEmptySpan<double> received))
{
    Console.WriteLine(received.Last);
}
```

:::warning

Do not change a `NonEmptyList<T>` while a span from `AsNonEmptySpan()` is in use. Adding or removing elements can move the list's storage, and the span keeps pointing at the old memory.

:::

### Members

| Member | Returns | Notes |
|--------|---------|-------|
| `Head` | `T` | The first element |
| `Last` | `T` | The last element |
| `Tail` | `ReadOnlySpan<T>` | The elements after the first. May be empty |
| `Init` | `ReadOnlySpan<T>` | The elements before the last. May be empty |
| `Length` | `int` | Always at least 1 |
| `this[int index]` | `T` | Read-only indexer |
| `Span` | `ReadOnlySpan<T>` | The underlying span. There is also an implicit conversion to `ReadOnlySpan<T>` |
| `Max()`, `Min()` | `T` | Use `Comparer<T>.Default` |
| `Reduce(reducer)` | `T` | Combines left to right, no seed needed |
| `Fold(seed, folder)` | `TResult` | Combines left to right from a seed |
| `Sum()` | `T` | Extension, `T : IAdditionOperators<T, T, T>`, checked |
| `Average()` | `T` | Extension, `T : INumberBase<T>` |
| `ToNonEmptyList()` | `NonEmptyList<T>` | Copies the elements |
| `ToArray()` | `T[]` | Copies the elements. Never empty |
| `GetEnumerator()` | `ReadOnlySpan<T>.Enumerator` | So `foreach` works |

`Sum` uses checked arithmetic, like `Sum` on a list: an `int` overflow throws `OverflowException`. `Average` always returns `T`, so for integer spans it uses integer division. The average of `{ 3, 1, 2 }` as `int` is `2`, and the average of `{ 1, 2 }` is `1`. Use a `double` span when you need the fraction.

```csharp
NonEmptySpan<int> span = NonEmptySpan.From(stackalloc int[] { 3, 1, 2 });

int product = span.Reduce((a, b) => a * b);           // 6
string digits = span.Fold("", (acc, x) => acc + x);   // "312"
ReadOnlySpan<int> rest = span.Tail;                    // { 1, 2 }

foreach (int value in span)
{
    Console.WriteLine(value);
}
```

Because it is a `ref struct`, a `NonEmptySpan<T>` follows the usual span rules. You cannot store it in a field of a class, box it, capture it in a lambda, or keep it across an `await`. When you need to keep the elements, call `ToNonEmptyList()`.

## Lazy sequences

`NonEmptyEnumerable<T>` is a sequence whose `Head` is already evaluated and whose `Rest` is an `IEnumerable<T>` that runs on demand. The rest may be infinite. Because the head always exists, taking a prefix is total: `Take(n)`, `TakeWhile` and `TakeUntil` return a `NonEmptyList<T>`.

```csharp
NonEmptyEnumerable<int> powersOfTwo = NonEmptyList.Iterate(1, x => x * 2);

NonEmptyList<int> firstTen = powersOfTwo.Take(10);           // 1, 2, 4, ..., 512
NonEmptyList<int> belowHundred = powersOfTwo.TakeWhile(x => x < 100);   // 1, 2, 4, ..., 64
int tenth = powersOfTwo.ElementAt(10);                       // 1024
```

### Creating one

| Factory | Produces |
|---------|----------|
| `NonEmptyList.Iterate(seed, next)` | `seed, next(seed), next(next(seed)), ...`, infinite |
| `NonEmptyEnumerable.Iterate(seed, next)` | The same |
| `NonEmptyEnumerable.Repeat(value)` | `value` forever |
| `NonEmptyEnumerable.From(start)` | `start, start + 1, start + 2, ...` as `int`, infinite |
| `NonEmptyEnumerable.Generate(i => ...)` | `f(0), f(1), f(2), ...`, infinite |
| `NonEmptyEnumerable.Create(head, rest)` | `head` followed by an `IEnumerable<T>` |
| `NonEmptyEnumerable.Create(head, a, b, c)` | `head` followed by the listed values |
| `new NonEmptyEnumerable<T>(head, rest)` | Same as `Create(head, rest)` |
| `NonEmptyEnumerable.FromEnumerable(source)` | The source, with its first element pulled; `null` if the source is empty |
| `list.Cycle()` on a `NonEmptyList<T>` | The list repeated forever |

- The constructor and `Create` throw `ArgumentNullException` when the head or the rest is `null`. `Repeat` throws it for a `null` value, and every factory that takes a delegate throws it for a `null` delegate.
- `FromEnumerable` reads only the first element of the source. The rest stays lazy and continues from the same enumerator, so enumerate it once. It throws `ArgumentException` if the first element is `null`.

```csharp
// Exponential backoff: 100 ms, 200 ms, 400 ms, ... capped at five attempts
NonEmptyList<TimeSpan> delays = NonEmptyList.Iterate(TimeSpan.FromMilliseconds(100), d => d * 2).Take(5);

// Round-robin assignment over a fixed set of workers
NonEmptyList<string> workers = ["alpha", "beta", "gamma"];
NonEmptyList<(string Worker, string Job)> assignments = workers.Cycle()
    .Zip(NonEmptyEnumerable.Create("job-1", "job-2", "job-3", "job-4"))
    .ToNonEmptyList();
// (alpha, job-1), (beta, job-2), (gamma, job-3), (alpha, job-4)
```

### Taking a finite list

| Member | Returns | Notes |
|--------|---------|-------|
| `Take(count)` | `NonEmptyList<T>` | The first `count` elements. Throws `ArgumentOutOfRangeException` if `count` is less than 1 |
| `TakeWhile(predicate)` | `NonEmptyList<T>` | The head, then elements of the rest while the predicate holds |
| `TakeUntil(predicate)` | `NonEmptyList<T>` | Elements up to and including the first that satisfies the predicate |
| `ElementAt(index)` | `T` | Evaluates up to `index`. Throws `ArgumentOutOfRangeException` for a negative index, or one past the end of a finite sequence |
| `ToNonEmptyList()` | `NonEmptyList<T>` | Materializes the whole sequence. Never returns for an infinite one |

:::note

`TakeWhile` always includes the head, even when the head fails the predicate. `NonEmptyEnumerable.From(10).TakeWhile(x => x < 5)` returns `[10]`, not an empty list. That is the price of a non-empty result. If the head must pass too, check it yourself first.

:::

`TakeUntil` is inclusive: `NonEmptyEnumerable.From(5).TakeUntil(x => x == 8)` returns `5, 6, 7, 8`, and if the head already satisfies the predicate, the result is just the head.

### Lazy transformations

These return another `NonEmptyEnumerable<T>` without evaluating the rest. The head of the result is computed immediately.

| Member | Returns | Notes |
|--------|---------|-------|
| `Select(selector)` | `NonEmptyEnumerable<TResult>` | The selector runs on the head now and on the rest on demand |
| `Index()` | `NonEmptyEnumerable<(T Item, int Index)>` | Pairs each element with its position, starting at 0 |
| `Zip(other)` | `NonEmptyEnumerable<(T First, TOther Second)>` | `other` must be a `NonEmptyEnumerable<TOther>`. Stops at the shorter sequence |
| `Prepend(item)` | `NonEmptyEnumerable<T>` | `item` becomes the new head. Throws `ArgumentNullException` for `null` |
| `Scan(seed, folder)` | `NonEmptyEnumerable<TAccumulate>` | Running fold. The first element is `folder(seed, Head)` |

```csharp
NonEmptyEnumerable<int> naturals = NonEmptyEnumerable.From(1);

NonEmptyList<int> evens = naturals.Select(x => x * 2).Take(3);                    // 2, 4, 6
NonEmptyList<int> runningTotals = naturals.Scan(0, (acc, x) => acc + x).Take(4);  // 1, 3, 6, 10
```

:::note

`Scan` here does not include the seed: the first element is `folder(seed, Head)`. That differs from `Scan(seed, folder)` on a `NonEmptyList<T>`, which starts with the seed itself and has one more element than the list.

:::

### Standard LINQ on a lazy sequence

`NonEmptyEnumerable<T>` implements `IEnumerable<T>`, enumerating `Head` and then `Rest`. Members it does not define, such as `Where`, `Skip`, `Count` and `ToList`, come from `Enumerable`. `Where` and `Skip` stay lazy and return `IEnumerable<T>`. `Count()`, `ToList()`, `Last()`, `Sum()` and anything else that reads to the end never return on an infinite sequence.

```csharp
IEnumerable<int> odd = naturals.Where(x => x % 2 == 1);   // lazy, still infinite
List<int> firstFive = odd.Take(5).ToList();               // 1, 3, 5, 7, 9

// naturals.Count();   // never returns
```

Sequences built by `Iterate`, `Repeat`, `From`, `Generate` and `Cycle()` restart from the beginning each time you enumerate them. One built by `FromEnumerable` follows its source and can be enumerated once.

The elements are not checked for `null` as the rest is produced. The check happens when you build a list: `Take`, `TakeWhile`, `TakeUntil` and `ToNonEmptyList` throw `ArgumentNullException` if they meet a `null` element.

### Finite versions

When you know the length in advance, or the sequence ends by itself, build a `NonEmptyList<T>` directly instead:

| Method | Produces |
|--------|----------|
| `NonEmptyList.Iterate(seed, next, count)` | The first `count` elements of `Iterate(seed, next)`. Throws `ArgumentOutOfRangeException` if `count` is less than 1 |
| `NonEmptyList.Unfold(seed, next)` | `seed`, then `next(...)` until it returns `null`. For reference types |
| `NonEmptyList.UnfoldValue(seed, next)` | The same for value types, with `next` returning `T?` |
| `list.Cycle(times)` | The list repeated `times` times |

```csharp
// Category is your own class with a nullable Parent property.
NonEmptyList<Category> breadcrumb = NonEmptyList.Unfold(category, c => c.Parent);

NonEmptyList<int> doublings = NonEmptyList.UnfoldValue(1, n => n < 100 ? n * 2 : null);   // 1, 2, 4, ..., 128
NonEmptyList<int> powers = NonEmptyList.Iterate(1, x => x * 2, count: 5);                  // 1, 2, 4, 8, 16
```

The seed is always included, so all of these are non-empty even when `next` stops straight away. For the other ways to create lists, see [Creating lists](./creating-lists.md).
