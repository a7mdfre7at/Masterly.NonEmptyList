---
id: functional-operations
title: "Functional Operations"
description: "Map, reduce, fold, scan, zip, split, take, rotate and validate a NonEmptyList, with the non-empty guarantee carried through every result that can keep it."
---

`NonEmptyList<T>` has a set of functional operators as instance methods. None of them changes the list they are called on. Each one returns a new value, and whenever the result provably has at least one element, that value is a `NonEmptyList<T>` again. When the result could be empty, the return type says so: a nullable list, a `List<T>`, or an `IEnumerable<T>`.

Most of these operators also have a LINQ spelling, such as `Select` for `Map`. Both work. See [Type-preserving LINQ](./linq.md).

## Projecting

| Method | Returns |
|--------|---------|
| `Map<TResult>(Func<T, TResult> selector)` | `NonEmptyList<TResult>` with one result per element |
| `MapWithIndex<TResult>(Func<T, int, TResult> selector)` | The same, with the element's index passed to the selector |
| `FlatMap<TResult>(Func<T, NonEmptyList<TResult>> selector)` | `NonEmptyList<TResult>` with every inner list flattened into one |
| `ConvertAll<TOutput>(Converter<T, TOutput> converter)` | `NonEmptyList<TOutput>`, the `List<T>`-style name for `Map` |

```csharp
NonEmptyList<string> names = ["Ada", "Grace"];

NonEmptyList<int> lengths = names.Map(n => n.Length);                 // [3, 5]
NonEmptyList<string> numbered = names.MapWithIndex((n, i) => $"{i + 1}. {n}"); // ["1. Ada", "2. Grace"]
NonEmptyList<char> initials = names.FlatMap(n => NonEmptyList.Of(n[0], n[^1])); // ['A', 'a', 'G', 'e']
```

A `NonEmptyList<T>` cannot hold `null`, so a selector that returns `null` makes the call throw `ArgumentNullException`. So does a `FlatMap` selector that returns `null` instead of a list.

## Reducing and folding

Because the list is never empty, you can combine its elements without a seed. LINQ's `Aggregate` without a seed throws on an empty sequence. `Reduce` has no such case.

| Method | Returns | Order |
|--------|---------|-------|
| `Reduce(Func<T, T, T> reducer)` | `T` | Left to right, starting from `Head`: `f(f(a, b), c)` |
| `ReduceRight(Func<T, T, T> reducer)` | `T` | Right to left, starting from `Last`: `f(a, f(b, c))` |
| `Fold<TResult>(TResult seed, Func<TResult, T, TResult> folder)` | `TResult` | Left to right. The folder receives `(accumulator, element)`. |
| `FoldRight<TResult>(TResult seed, Func<T, TResult, TResult> folder)` | `TResult` | Right to left. The folder receives `(element, accumulator)`. |
| `Scan<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> folder)` | `NonEmptyList<TAccumulate>` | The seed followed by every intermediate result: `Count + 1` elements |
| `Scan(Func<T, T, T> reducer)` | `NonEmptyList<T>` | `Head` followed by every intermediate result: `Count` elements |

```csharp
NonEmptyList<int> numbers = [1, 2, 3];

int total = numbers.Reduce((a, b) => a + b);              // 6
int product = numbers.Fold(1, (acc, n) => acc * n);       // 6
NonEmptyList<int> running = numbers.Scan((a, b) => a + b);     // [1, 3, 6]
NonEmptyList<int> fromZero = numbers.Scan(0, (acc, n) => acc + n); // [0, 1, 3, 6]

NonEmptyList<string> letters = ["a", "b", "c"];
string nested = letters.ReduceRight((x, acc) => $"({x}{acc})"); // "(a(bc))"
string joined = letters.FoldRight("z", (x, acc) => x + acc);    // "abcz"
```

For `Sum`, `Average`, `Min`, `Max` and other numeric aggregates, see [Generic math](./generic-math.md).

## Combining lists

| Method | Returns |
|--------|---------|
| `Zip<TOther>(NonEmptyList<TOther> other)` | `NonEmptyList<(T First, TOther Second)>` |
| `ZipWith<TOther, TResult>(NonEmptyList<TOther> other, Func<T, TOther, TResult> selector)` | `NonEmptyList<TResult>` |
| `ZipWithIndex()` | `NonEmptyList<(T Item, int Index)>` |
| `Concat(NonEmptyList<T> other)` | `NonEmptyList<T>`: this list followed by `other` |
| `Concat(IEnumerable<T> other)` | `NonEmptyList<T>`: `other` may be empty |
| `Append(T item)` | `NonEmptyList<T>` with `item` at the end |
| `Prepend(T item)` | `NonEmptyList<T>` with `item` at the start |
| `Intersperse(T separator)` | `NonEmptyList<T>` with `separator` between every pair of elements |

`Zip` and `ZipWith` stop at the end of the shorter list. Both lists have at least one element, so the result does too.

```csharp
NonEmptyList<int> ids = [1, 2, 3];
NonEmptyList<string> labels = ["a", "b"];

NonEmptyList<(int First, string Second)> pairs = ids.Zip(labels); // [(1, "a"), (2, "b")]
NonEmptyList<string> keys = ids.ZipWith(labels, (id, label) => $"{label}{id}"); // ["a1", "b2"]

foreach ((string label, int index) in labels.ZipWithIndex())
{
    Console.WriteLine($"{index}: {label}");
}

NonEmptyList<string> path = NonEmptyList.Of("home", "user", "docs").Intersperse("/"); // ["home", "/", "user", "/", "docs"]
```

:::warning

`Prepend(item)` returns a new list in version 3. In version 2 it inserted the item in place. To insert in place, call `AddFirst(item)`. See [Safe Mutation](./safe-mutation.md).

:::

`Append`, `Prepend`, `Concat(IEnumerable<T>)` and `Intersperse` throw `ArgumentNullException` for a `null` item, separator or element. For `Interleave`, `Unzip`, `Flatten` and `Cycle`, see [Type-preserving LINQ](./linq.md).

## Splitting and grouping

| Method | Returns |
|--------|---------|
| `Partition(Func<T, bool> predicate)` | `(List<T> Matching, List<T> NotMatching)`. Either list may be empty. |
| `GroupByNonEmpty<TKey>(Func<T, TKey> keySelector)` | `Dictionary<TKey, NonEmptyList<T>>`. Every group has at least one element. |
| `GroupByNonEmpty<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector)` | `Dictionary<TKey, NonEmptyList<TElement>>` |
| `Sliding(int size, int step = 1)` | `IEnumerable<NonEmptyList<T>>`: overlapping windows of exactly `size` elements |
| `ChunkNonEmpty(int size)` | `IEnumerable<NonEmptyList<T>>`: consecutive chunks; the last may be smaller |
| `Tails()` | `NonEmptyList<NonEmptyList<T>>`: every non-empty suffix, longest first |
| `Inits()` | `NonEmptyList<NonEmptyList<T>>`: every non-empty prefix, shortest first |

```csharp
NonEmptyList<int> numbers = [1, 2, 3, 4, 5];

var (even, odd) = numbers.Partition(n => n % 2 == 0); // [2, 4] and [1, 3, 5]

Dictionary<char, NonEmptyList<string>> byLetter =
    NonEmptyList.Of("apple", "avocado", "banana").GroupByNonEmpty(s => s[0]);
// 'a': ["apple", "avocado"], 'b': ["banana"]

IEnumerable<NonEmptyList<int>> windows = numbers.Sliding(3, step: 2); // [1, 2, 3], [3, 4, 5]
IEnumerable<NonEmptyList<int>> chunks = numbers.ChunkNonEmpty(2);     // [1, 2], [3, 4], [5]

NonEmptyList<NonEmptyList<int>> tails = NonEmptyList.Of(1, 2, 3).Tails(); // [1, 2, 3], [2, 3], [3]
NonEmptyList<NonEmptyList<int>> inits = NonEmptyList.Of(1, 2, 3).Inits(); // [1], [1, 2], [1, 2, 3]
```

What else you should know:

- `Sliding` and `ChunkNonEmpty` are lazy. They throw `ArgumentOutOfRangeException` for a `size` (or `step`) below 1, but only when you start enumerating the result, not when you call them.
- `Sliding` yields no windows at all when `size` is larger than the list. `ChunkNonEmpty` always yields at least one chunk.
- LINQ's `Chunk(size)` on a `NonEmptyList<T>` returns a `NonEmptyList<NonEmptyList<T>>` right away instead of a lazy sequence. See [Type-preserving LINQ](./linq.md).
- For grouping that returns a non-empty list of groups and works in query syntax, use LINQ's `GroupBy`.

## Taking and skipping

LINQ's `Take` and `Skip` may return an empty sequence. These versions return a non-empty list, or `null` when nothing would be left.

| Method | Returns |
|--------|---------|
| `TakeNonEmpty(int count)` | The first `count` elements (all of them if `count` is larger), or `null` when `count` is less than 1. |
| `SkipNonEmpty(int count)` | The elements after the first `count`, or `null` when `count` is greater than or equal to `Count`. A `count` of 0 or less returns a copy of the whole list. |
| `TakeAtLeastOne(int count)` | The first `count` elements, but never fewer than one, so the result is never `null`. |

```csharp
NonEmptyList<int> numbers = [1, 2, 3];

NonEmptyList<int>? firstTwo = numbers.TakeNonEmpty(2);  // [1, 2]
NonEmptyList<int>? nothing = numbers.TakeNonEmpty(0);   // null
NonEmptyList<int>? afterOne = numbers.SkipNonEmpty(1);  // [2, 3]
NonEmptyList<int>? gone = numbers.SkipNonEmpty(3);      // null
NonEmptyList<int> atLeastOne = numbers.TakeAtLeastOne(0); // [1]
```

## Reordering and removing duplicates

| Method | Returns |
|--------|---------|
| `RotateLeft(int positions)` | A new list rotated left: `[1, 2, 3]` by 1 gives `[2, 3, 1]` |
| `RotateRight(int positions)` | A new list rotated right: `[1, 2, 3]` by 1 gives `[3, 1, 2]` |
| `ReverseList()` | A new list in reverse order |
| `DistinctList()`, `DistinctList(IEqualityComparer<T>? comparer)` | A new list without duplicates |
| `DistinctBy<TKey>(Func<T, TKey> keySelector)` | A new list with one element per key; the first element for each key is kept |

`RotateLeft` and `RotateRight` accept any number of positions. Positions wrap around the length of the list, and a negative value rotates the other way.

The first element always survives `DistinctList` and `DistinctBy`, so the result is never empty. These methods never change the list you call them on. `Reverse()` and `Sort()` do: they are the in-place `List<T>`-style members. See [Safe Mutation](./safe-mutation.md).

## Validating every element

| Method | Returns |
|--------|---------|
| `Validate(Func<T, bool> predicate, string errorMessage)` | The same list when every element passes. Throws `InvalidOperationException` with `errorMessage` otherwise. |
| `TryValidate(Func<T, bool> predicate, out T failingElement)` | `true` when every element passes. Otherwise `false`, with the first failing element in `failingElement`. |

```csharp
// Order is your own class with a decimal Total
NonEmptyList<Order> orders = GetOrders();

NonEmptyList<Order> checkedOrders = orders.Validate(o => o.Total > 0, "Every order must have a positive total.");

if (!orders.TryValidate(o => o.Total > 0, out Order? bad))
{
    Console.WriteLine($"Order with total {bad.Total} is invalid.");
}
```

`Validate` returns the same instance, not a copy, so you can chain it. `TryValidate` marks `failingElement` with `[MaybeNullWhen(true)]`: it is set only when the method returns `false`.

These two methods stop at the first failure and report one problem. To collect every failure, see [Validation](./validation.md).

## Iterating with an index

`ForEach(Action<T> action)` runs an action for each element, as on `List<T>`. `ForEachWithIndex(Action<T, int> action)` also passes the index.

```csharp
names.ForEachWithIndex((name, index) => Console.WriteLine($"{index}: {name}"));
```

For the async versions of these operators, such as `MapAsync` and `ForEachAsync`, see [Async operations](./async-operations.md).
