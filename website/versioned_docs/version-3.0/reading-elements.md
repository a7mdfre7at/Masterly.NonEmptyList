---
id: reading-elements
title: "Reading Elements"
description: "Read the head, last element and tail without emptiness checks, and use indexers, ranges, list patterns, deconstruction, searching and conversions."
---

Because a `NonEmptyList<T>` always has at least one element, the members that read "the first" or "the last" element are plain properties that cannot fail. This page covers those properties, indexing and slicing, pattern matching, searching, and converting the list to other collection types.

## Head, Last and Tail

| Member | Type | Value |
|--------|------|-------|
| `Head` | `T` | The first element. |
| `First` | `T` | The same as `Head`. This is a property, not LINQ's `First()` method. |
| `Last` | `T` | The last element. |
| `Tail` | `NonEmptyList<T>?` | Every element after the first, or `null` when the list has one element. |
| `Init` | `NonEmptyList<T>?` | Every element before the last, or `null` when the list has one element. |
| `Count` | `int` | The number of elements. Always at least 1. |

```csharp
NonEmptyList<string> names = ["Ada", "Grace", "Linus"];

string first = names.Head; // "Ada"
string last = names.Last;  // "Linus"

NonEmptyList<string>? rest = names.Tail;   // ["Grace", "Linus"]
NonEmptyList<string>? allButLast = names.Init; // ["Ada", "Grace"]
```

`Tail` and `Init` are nullable on purpose. A one-element list has no tail, and the compiler makes you deal with that instead of handing you an empty list. When the tail exists it is itself a `NonEmptyList<T>`, so you can keep reading from it safely:

```csharp
if (names.Tail is NonEmptyList<string> tail)
{
    Console.WriteLine($"Second name: {tail.Head}");
}
```

**Copies.** Every read of `Tail` or `Init` copies the elements into a new list. The copy is yours: changing it never affects the original list or a later read. A tail you read earlier is a snapshot: it does not change when the original list changes. Because each read copies, store the result in a variable instead of reading `Tail` repeatedly in a loop.

:::tip

Prefer `Head` over `First()`, `Last` over `Last()` and `Count` over `Count()`. The LINQ methods work, but the properties say what you mean and cost nothing. The bundled analyzer suggests the swap (`NEL003`). See [Analyzers and source generator](./analyzers.md).

:::

## Indexers, ranges and slices

| Member | Returns | Notes |
|--------|---------|-------|
| `this[int index]` | `T` | Read and write. Writing `null` throws `ArgumentNullException`. |
| `this[Index index]` | `T` | Read only. Enables `xs[^1]`. |
| `this[Range range]` | `List<T>` | A copy. Enables `xs[1..]`. The copy may be empty. |
| `Slice(int start, int length)` | `List<T>` | A copy. May be empty. |
| `GetRange(int index, int count)` | `List<T>` | A copy. May be empty. |

```csharp
NonEmptyList<int> numbers = [1, 2, 3, 4];

int lastOne = numbers[^1];      // 4
List<int> tail = numbers[1..];  // [2, 3, 4]
List<int> middle = numbers[1..3]; // [2, 3]
List<int> none = numbers[4..];  // [] : a range can be empty
```

A range or a slice returns a `List<T>`, not a `NonEmptyList<T>`, because it can be empty. When you need a non-empty result, use `Tail`, `TakeNonEmpty` or `SkipNonEmpty`. See [Functional operations](./functional-operations.md).

## List patterns

The `Count` property and the `Index` and `Range` indexers make C# list patterns work:

```csharp
NonEmptyList<int> numbers = [1, 2, 3];

string description = numbers switch
{
    [var only] => $"just {only}",
    [var head, .. var rest] => $"{head} then {rest.Count} more",
    _ => throw new UnreachableException("A NonEmptyList always has at least one element."),
};
// "1 then 2 more"

bool startsWithOne = numbers is [1, ..];   // true
bool hasTwoOrMore = numbers is [_, _, ..]; // true
```

The slice in `.. var rest` is a `List<T>`.

:::note

The compiler does not know that the list is never empty, so a `switch` expression with only `[var only]` and `[var head, .. var rest]` arms gets warning CS8509 (the switch expression is not exhaustive). Add a discard arm as shown. `UnreachableException` is in `System.Diagnostics`.

:::

## Deconstruction

You can split a list into its head and tail:

```csharp
var (head, tail) = names; // head is string, tail is NonEmptyList<string>?
```

Or into its first two elements and the rest:

```csharp
var (first, second, others) = names; // string, string?, IEnumerable<string>
```

:::warning

In the three-part form, `second` is `default(T)` when the list has one element. For a reference type that is `null`. For a value type it is the type's default, so on a one-element `NonEmptyList<int>` you get `0`, which you cannot tell apart from a real `0`. Check `Count` first, or use a list pattern.

:::

## Match

`Match` branches on "one element" or "more than one". The `multiple` branch receives the head and a tail that is not `null`.

```csharp
string summary = names.Match(
    single: only => $"Only {only}",
    multiple: (head, tail) => $"{head} and {tail.Count} more");

names.Match(
    single: only => Console.WriteLine($"Only {only}"),
    multiple: (head, tail) => Console.WriteLine($"{head} and {tail.Count} more"));
```

`Match<TResult>(Func<T, TResult> single, Func<T, NonEmptyList<T>, TResult> multiple)` returns a value. `Match(Action<T> single, Action<T, NonEmptyList<T>> multiple)` runs an action.

## Searching

The list has the search members you know from `List<T>`. They behave the same way, including what they return when nothing matches.

| Member | Returns when nothing matches |
|--------|------------------------------|
| `Contains(T item)`, `Exists(Predicate<T> match)` | `false` |
| `TrueForAll(Predicate<T> match)` | `true` only if every element matches |
| `IndexOf`, `LastIndexOf`, `FindIndex`, `FindLastIndex` (with optional start index and count) | `-1` |
| `Find(Predicate<T> match)`, `FindLast(Predicate<T> match)` | `default(T)`, typed `T?` |
| `FindAll(Predicate<T> match)` | an empty `List<T>` |
| `BinarySearch(T item)` and overloads with a comparer and a range | a negative number, as on `List<T>` |

## Picking one element

These members return a single element and never fail for lack of elements:

| Member | Returns |
|--------|---------|
| `Random()` | A random element, using `Random.Shared`. |
| `Random(Random random)` | A random element, using your `Random` instance. Pass a seeded one for repeatable tests. |
| `MinBy<TKey>(Func<T, TKey> keySelector)` and `MinBy<TKey>(keySelector, IComparer<TKey>? comparer)` | The element with the smallest key. |
| `MaxBy<TKey>(Func<T, TKey> keySelector)` and `MaxBy<TKey>(keySelector, IComparer<TKey>? comparer)` | The element with the largest key. |

```csharp
// Product is your own class with a decimal Price
NonEmptyList<Product> products = GetProducts();

Product cheapest = products.MinBy(p => p.Price);
Product priciest = products.MaxBy(p => p.Price);
Product featured = products.Random();
```

`MinBy` and `MaxBy` make a single pass over the list. When several elements share the smallest or largest key, they return the first of them.

For `Min`, `Max`, `Sum`, `Average` and the other aggregates, see [Type-preserving LINQ](./linq.md) and [Generic math](./generic-math.md).

## Spans and memory

| Member | Returns | Notes |
|--------|---------|-------|
| `AsSpan()` | `ReadOnlySpan<T>` | A view over the list's own storage, with no copy. Do not add or remove elements while you use the span. |
| `AsMemory()` | `ReadOnlyMemory<T>` | Over a copy of the elements, so later changes to the list are not visible. |

For `NonEmptySpan<T>`, a span that keeps the non-empty guarantee, see [Spans and lazy sequences](./spans-and-lazy-sequences.md).

## Converting to other collections

| Member | Returns | Notes |
|--------|---------|-------|
| `ToArray()`, `ToArrayNonEmpty()` | `T[]` | Always at least one element. |
| `ToHashSet()`, `ToHashSet(IEqualityComparer<T>? comparer)` | `HashSet<T>` | Duplicates collapse. |
| `ToDictionary<TKey>(Func<T, TKey> keySelector)` | `Dictionary<TKey, T>` | Throws `ArgumentException` on a duplicate key, like LINQ's `ToDictionary`. |
| `ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector)` | `Dictionary<TKey, TValue>` | Same. |
| `ToQueue()` | `Queue<T>` | The head is dequeued first. |
| `ToStack()` | `Stack<T>` | The last element is on top. |
| `ToLinkedList()` | `LinkedList<T>` | |
| `AsReadOnly()` | `ReadOnlyCollection<T>` | A live, read-only view: later changes to the list are visible through it. |
| `ToImmutable()` | `ImmutableNonEmptyList<T>` | An immutable copy that keeps the non-empty guarantee. See [Immutable lists](./immutable-lists.md). |
| `ConvertAll<TOutput>(Converter<T, TOutput> converter)` | `NonEmptyList<TOutput>` | Converts every element. |

All of these except `AsReadOnly()` copy the elements. LINQ's `ToList()` also works and returns a plain `List<T>` copy. To keep the guarantee in a set or a dictionary, use `ToNonEmptySet` or `ToNonEmptyDictionary`. See [Sets, dictionaries and strings](./sets-dictionaries-strings.md).

## Comparing lists

`NonEmptyList<T>` has value equality. Two lists are equal when they have the same elements in the same order.

```csharp
NonEmptyList<int> a = [1, 2, 3];
NonEmptyList<int> b = [1, 2, 3];

bool same = a == b;          // true
bool equal = a.Equals(b);    // true
bool hashes = a.GetHashCode() == b.GetHashCode(); // true

NonEmptyList<string> upper = ["ADA"];
NonEmptyList<string> lower = ["ada"];
bool ignoringCase = upper.SequenceEqual(lower, StringComparer.OrdinalIgnoreCase); // true
```

The hash code is computed from the elements, so do not change a list while it is a key in a dictionary or an element of a hash set.

`ToString()` lists the elements, for example `NonEmptyList: [1, 2, 3]`.
