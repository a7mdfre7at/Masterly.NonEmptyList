---
id: linq
title: "Type-Preserving LINQ"
description: "Which LINQ operators keep the NonEmptyList<T> type, which fall back to IEnumerable<T>, and how query syntax, ordering and grouping work."
---

When you call a LINQ operator on a `NonEmptyList<T>` and the result can never be empty, you get a `NonEmptyList<T>` back instead of an `IEnumerable<T>`. `Select`, `OrderBy`, `GroupBy`, `Distinct` and the other operators that cannot lose every element keep the guarantee. `Where`, `Skip` and `Take` can return nothing, so they give you a plain `IEnumerable<T>`.

```csharp
using Masterly.NonEmptyList;

NonEmptyList<int> numbers = [3, 1, 2];

NonEmptyList<int> doubled = numbers.Select(n => n * 2);     // still non-empty
NonEmptyList<int> sorted = numbers.Order();                  // still non-empty
IEnumerable<int> large = numbers.Where(n => n > 10);         // may be empty
```

## How the compiler picks these operators

The operators are extension methods in `NonEmptyListLinqExtensions` and `NonEmptyListMathExtensions`, in the `Masterly.NonEmptyList` namespace. Their receiver is `NonEmptyList<T>`, while the `System.Linq.Enumerable` versions take `IEnumerable<T>`. When both are in scope, C# overload resolution prefers the exact match, so `numbers.Select(...)` binds to the non-empty version. You do not need to call anything special. A `using Masterly.NonEmptyList;` directive is enough.

Three rules follow from this:

- **The static type decides.** The non-empty overloads only apply while the compiler knows the value is a `NonEmptyList<T>`. Once you hold an `IEnumerable<T>` (a parameter, the result of `Where`, a cast), every call goes to `Enumerable` again.
- **Instance methods win over extension methods.** `NonEmptyList<T>` has its own `Concat`, `Append`, `Prepend`, `Zip`, `DistinctBy`, `ToArray`, `ToHashSet` and `ToDictionary`. A call that matches one of their signatures never reaches LINQ. See [Functional operations](./functional-operations.md).
- **Operators without a non-empty overload still work.** `Any`, `All`, `ToList`, `ToLookup` and the rest come from `Enumerable` as usual and return what they always return.

:::warning

`Reverse()` on a `NonEmptyList<T>` is the in-place `List<T>`-style method. It returns `void` and reverses the list itself. To get a reversed copy, call `ReverseList()`.

:::

## Operators that stay non-empty

These operators return a non-empty type. Unlike `Enumerable`, they run immediately and return a new, fully built list, so deferred execution does not apply. The one exception is `Cycle()`, which returns a lazy, infinite sequence.

| Operator | Returns | Why it is non-empty |
|----------|---------|---------------------|
| `Select(x => ...)`, `Select((x, i) => ...)` | `NonEmptyList<TResult>` | One output per input |
| `SelectMany(x => nonEmptyList)` | `NonEmptyList<TResult>` | Every inner list has an element |
| `SelectMany(x => immutableNonEmptyList)` | `NonEmptyList<TResult>` | Same, for `ImmutableNonEmptyList<T>` |
| `SelectMany(collectionSelector, resultSelector)` | `NonEmptyList<TResult>` | The collection selector returns `NonEmptyList<T>` |
| `Flatten()` on `NonEmptyList<NonEmptyList<T>>` | `NonEmptyList<T>` | Every inner list has an element |
| `OrderBy`, `OrderByDescending` | `OrderedNonEmptyList<T>` | Sorting keeps every element |
| `ThenBy`, `ThenByDescending` | `OrderedNonEmptyList<T>` | Refines an existing ordering |
| `Order`, `OrderDescending` | `OrderedNonEmptyList<T>` | Sorting keeps every element |
| `GroupBy` | `NonEmptyList<NonEmptyGrouping<TKey, T>>` | At least one group, each with at least one element |
| `Distinct` | `NonEmptyList<T>` | The first element always survives |
| `Union` | `NonEmptyList<T>` | One side is non-empty |
| `Concat` (an `IEnumerable<T>` followed by a `NonEmptyList<T>`) | `NonEmptyList<T>` | The second sequence is non-empty |
| `Chunk(size)` | `NonEmptyList<NonEmptyList<T>>` | At least one chunk, none empty |
| `Unzip()` on a list of pairs | `(NonEmptyList<TFirst> Firsts, NonEmptyList<TSecond> Seconds)` | One pair per element |
| `Interleave(other)` | `NonEmptyList<T>` | Both lists are non-empty |
| `Cycle()` | `NonEmptyEnumerable<T>` | Infinite repetition of a non-empty list |
| `Cycle(times)` | `NonEmptyList<T>` | `times` copies, at least one |
| `Max`, `Min` | `T` or `TResult` | Never throws, never nullable |
| `MinMax` | `(T Min, T Max)` | Both found in a single pass |

The ordering, set and grouping operators, and `Max`, `Min` and `MinMax`, have overloads that take a comparer, as their `Enumerable` counterparts do. Passing `null` uses the default comparer.

Every result is still a `NonEmptyList<T>`, so it rejects `null` elements. If a selector returns `null`, the operator throws `ArgumentNullException`.

### Projection and flattening

`SelectMany` keeps the type only when the selector returns a `NonEmptyList<T>` or an `ImmutableNonEmptyList<T>`. A selector that returns an array, a `List<T>` or any other `IEnumerable<T>` binds to `Enumerable.SelectMany`, because one of those inner sequences could be empty.

```csharp
// Order and OrderLine are your own types.
// Order.Lines is a NonEmptyList<OrderLine>.
NonEmptyList<Order> orders = GetOrders();

NonEmptyList<OrderLine> allLines = orders.SelectMany(o => o.Lines);
NonEmptyList<OrderLine> sameLines = orders.Select(o => o.Lines).Flatten();
```

### Chunk, Unzip, Interleave and Cycle

```csharp
NonEmptyList<int> list = [1, 2, 3, 4, 5];

NonEmptyList<NonEmptyList<int>> chunks = list.Chunk(2);       // [1, 2], [3, 4], [5]

(NonEmptyList<int> ids, NonEmptyList<string> labels) =
    list.Select(x => (x, $"#{x}")).Unzip();

NonEmptyList<int> mixed = NonEmptyList.Of(1, 3).Interleave(NonEmptyList.Of(2, 4, 6, 8));
// 1, 2, 3, 4, 6, 8: the rest of the longer list is appended

NonEmptyList<string> colors = ["red", "green"];
NonEmptyList<string> five = colors.Cycle().Take(5);           // red, green, red, green, red
NonEmptyList<string> twice = colors.Cycle(2);                 // red, green, red, green
```

- `Chunk(size)` throws `ArgumentOutOfRangeException` when `size` is less than 1. Unlike `Enumerable.Chunk`, each chunk is a `NonEmptyList<T>`, not an array.
- `Cycle(times)` throws `ArgumentOutOfRangeException` when `times` is less than 1.
- `Cycle()` is infinite. Its `Take(count)` returns a `NonEmptyList<T>`. See [Spans and lazy sequences](./spans-and-lazy-sequences.md#lazy-sequences).

### Max, Min and MinMax

On an `IEnumerable<T>`, `Max()` throws for an empty sequence of value types and returns `null` for an empty sequence of reference types. On a `NonEmptyList<T>`, it does neither.

```csharp
NonEmptyList<string> words = ["pear", "apple", "fig"];

string max = words.Max();                    // "pear", typed string, not string?
int longest = words.Max(w => w.Length);      // 5
(string lo, string hi) = words.MinMax();     // ("apple", "pear")
string shortest = words.MinBy(w => w.Length); // "fig"
```

`Max` and `Min` have overloads with an `IComparer<T>?` and with a selector. `MinMax` has an overload with an `IComparer<T>?`. `MinBy` and `MaxBy` are instance methods on the list. For `Sum`, `Average`, `Median` and friends, see [Generic math](./generic-math.md).

## Operators that return IEnumerable

Operators that can remove every element have no non-empty overload. They bind to `Enumerable` and return `IEnumerable<T>`, so the type never claims a guarantee it cannot keep.

| Operator | Why it can be empty |
|----------|---------------------|
| `Where`, `OfType`, `Except`, `Intersect` | The filter may reject every element |
| `Skip`, `Take`, `SkipWhile`, `TakeWhile` | The count is only known at run time, and the predicate may leave nothing |
| `SelectMany` with an `IEnumerable<T>` selector | Every inner sequence may be empty |
| `Join` | No keys may match |
| `Zip` with an `IEnumerable<T>` | The other sequence may be empty |

When you need a non-empty result after one of these, check it explicitly:

```csharp
NonEmptyList<int> numbers = [1, 2, 3];

NonEmptyList<int>? large = numbers.Where(n => n > 1).ToNonEmptyListOrNull();
if (large is not null)
{
    Console.WriteLine(large.Head);
}

if (numbers.Where(n => n > 5).TryToNonEmptyList(out NonEmptyList<int>? huge))
{
    Console.WriteLine(huge.Head);    // huge is not null here
}

NonEmptyList<int> sure = numbers.Where(n => n > 0).ToNonEmptyList();  // throws ArgumentException if empty
```

`NonEmptyList<T>` also has instance methods for the common cases: `TakeNonEmpty(count)` and `SkipNonEmpty(count)` return `NonEmptyList<T>?`, and `TakeAtLeastOne(count)` always returns at least the head. See [Functional operations](./functional-operations.md).

## Ordering

`OrderBy`, `OrderByDescending`, `Order` and `OrderDescending` return an `OrderedNonEmptyList<T>`. It is a `NonEmptyList<T>` that remembers its ordering, so `ThenBy` and `ThenByDescending` can refine it. The sort is stable: elements with equal keys keep their original order.

```csharp
// Customer is your own type:
// public sealed record Customer(string FirstName, string LastName, string Country, int Age);
NonEmptyList<Customer> customers = GetCustomers();

OrderedNonEmptyList<Customer> byName = customers
    .OrderBy(c => c.LastName)
    .ThenBy(c => c.FirstName);

NonEmptyList<Customer> oldestFirst = customers.OrderByDescending(c => c.Age);
Customer oldest = oldestFirst.Head;
```

`OrderedNonEmptyList<T>` also implements `IOrderedEnumerable<T>`, so code written against standard LINQ can continue the ordering. In that case the static type is `IOrderedEnumerable<T>`, because the call binds to `Enumerable.ThenBy`.

```csharp
IOrderedEnumerable<Customer> ordered = customers.OrderBy(c => c.LastName);
List<Customer> refined = ordered.ThenByDescending(c => c.Age).ToList();
```

## Grouping

`GroupBy` returns a `NonEmptyList<NonEmptyGrouping<TKey, T>>`. A `NonEmptyGrouping<TKey, TElement>` is a `NonEmptyList<TElement>` with a `Key` property, and it implements `IGrouping<TKey, TElement>`. Every group therefore has a `Head`, a `Count` property and the non-empty operators.

```csharp
NonEmptyList<NonEmptyGrouping<string, Customer>> byCountry = customers.GroupBy(c => c.Country);

foreach (NonEmptyGrouping<string, Customer> group in byCountry)
{
    Customer first = group.Head;
    int oldestAge = group.Max(c => c.Age);
    Console.WriteLine($"{group.Key}: {group.Count} customers, first {first.LastName}, oldest {oldestAge}");
}
```

| Overload | Returns |
|----------|---------|
| `GroupBy(keySelector)` | `NonEmptyList<NonEmptyGrouping<TKey, T>>` |
| `GroupBy(keySelector, comparer)` | `NonEmptyList<NonEmptyGrouping<TKey, T>>` |
| `GroupBy(keySelector, elementSelector)` | `NonEmptyList<NonEmptyGrouping<TKey, TElement>>` |
| `GroupBy(keySelector, elementSelector, comparer)` | `NonEmptyList<NonEmptyGrouping<TKey, TElement>>` |

Groups appear in the order their keys first occur. If an element selector returns `null`, `GroupBy` throws `ArgumentNullException`.

If you want a dictionary instead, the instance method `GroupByNonEmpty(keySelector)` returns a `Dictionary<TKey, NonEmptyList<T>>`.

## Set operations and Concat

`Distinct()` keeps the first occurrence of each element. `Union` comes in two directions: a `NonEmptyList<T>` followed by any `IEnumerable<T>`, or any `IEnumerable<T>` followed by a `NonEmptyList<T>`. Both return a `NonEmptyList<T>`.

```csharp
NonEmptyList<int> list = [1, 1, 2];
int[] extra = [2, 3];

NonEmptyList<int> unique = list.Distinct();       // 1, 2
NonEmptyList<int> union = list.Union(extra);       // 1, 2, 3
NonEmptyList<int> union2 = extra.Union(list);      // 2, 3, 1
```

:::note

`Union` keeps the guarantee whichever side is the non-empty list, including when both are: `first.Union(second)` with two `NonEmptyList<T>` values uses a dedicated overload. Each form also has an overload that takes an `IEqualityComparer<T>`.

:::

`Concat` works in both directions as well. `list.Concat(other)` is an instance method on `NonEmptyList<T>` and accepts a `NonEmptyList<T>` or any `IEnumerable<T>`. `sequence.Concat(list)` is the extension for a possibly empty sequence followed by a non-empty list.

```csharp
IEnumerable<int> maybeEmpty = Enumerable.Empty<int>();
NonEmptyList<int> list = [1, 2];

NonEmptyList<int> a = list.Concat(maybeEmpty);   // instance method
NonEmptyList<int> b = maybeEmpty.Concat(list);   // extension method
```

## Query syntax

Query expressions compile to the same method calls, so they keep the type too. You can write `orderby` with several keys, `descending`, `let`, `group ... by ... into` and a second `from` over a non-empty list, and still end with a `NonEmptyList<T>`.

```csharp
NonEmptyList<string> names =
    from c in customers
    orderby c.LastName, c.FirstName descending
    select $"{c.LastName}, {c.FirstName}";

NonEmptyList<(string Country, int Count, int MaxAge)> summary =
    from c in customers
    group c by c.Country into g
    select (g.Key, g.Count, g.Max(x => x.Age));

NonEmptyList<string> pairs =
    from x in NonEmptyList.Of(1, 2)
    from y in NonEmptyList.Of("a", "b")
    select $"{x}{y}";                     // 1a, 1b, 2a, 2b
```

Inside `into g`, `g` is a `NonEmptyGrouping<TKey, T>`, so `g.Count` is the property and `g.Max(...)` is the non-empty version.

| Clause | Result stays non-empty? |
|--------|-------------------------|
| `select`, `let` | Yes |
| `orderby` (any number of keys, `ascending` or `descending`) | Yes |
| `group ... by ...`, with or without `into` | Yes |
| a second `from` over a `NonEmptyList<T>` | Yes |
| a second `from` over any other sequence | No |
| `where`, `join` | No |

A single `where` anywhere in the query turns the rest of it into ordinary LINQ:

```csharp
IEnumerable<string> adults =
    from c in customers
    where c.Age >= 18
    select c.LastName;
```

## Entering the type

To bring an ordinary sequence into the non-empty world, use the conversion extensions in the same class:

| Method | Empty source | Returns |
|--------|--------------|---------|
| `source.ToNonEmptyList()` | Throws `ArgumentException` | `NonEmptyList<T>` |
| `source.ToNonEmptyListOrNull()` | Returns `null` | `NonEmptyList<T>?` |
| `source.TryToNonEmptyList(out NonEmptyList<T>? result)` | Returns `false` | `bool` |
| `span.ToNonEmptyList()` on a `ReadOnlySpan<T>` | Throws `ArgumentException` | `NonEmptyList<T>` |

For every other way to create a list, see [Creating lists](./creating-lists.md).

:::tip

Standard LINQ methods such as `First()`, `Last()`, `Count()` and `Aggregate(f)` still work on a `NonEmptyList<T>`, but the list has total members that say the same thing: `Head`, `Last`, `Count` and `Reduce(f)`. The bundled analyzer suggests them (rule NEL003). See [Analyzers](./analyzers.md).

:::
