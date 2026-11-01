---
id: immutable-lists
title: "Immutable Lists"
description: "ImmutableNonEmptyList<T>: a non-empty list that never changes, where every update returns a new list and removals that would empty it return null."
---

`ImmutableNonEmptyList<T>` has the same guarantee as `NonEmptyList<T>`: it always holds at least one element and never holds `null`. The difference is that it never changes. Every method that "modifies" it returns a new list and leaves the original untouched. Use it for values you share, cache or pass between layers, where a mutation behind your back would be a bug.

```csharp
using Masterly.NonEmptyList;

ImmutableNonEmptyList<string> stages = ["build", "test"];
ImmutableNonEmptyList<string> pipeline = stages.Append("deploy");

Console.WriteLine(stages.Count);    // 2, the original is unchanged
Console.WriteLine(pipeline.Count);  // 3
```

## Creating a list

| Way | Example | Empty input |
|-----|---------|-------------|
| Collection expression | `ImmutableNonEmptyList<int> xs = [1, 2, 3];` | `ArgumentException` (and NEL001 at compile time) |
| Type-inferred factory | `ImmutableNonEmptyList.Of("a", "b")` | not possible, the first element is required |
| Constructor | `new ImmutableNonEmptyList<int>(1, 2, 3)` | not possible |
| From a sequence | `ImmutableNonEmptyList.From(source)` or `ImmutableNonEmptyList<int>.From(source)` | `ArgumentException` |
| Sequence constructor | `new ImmutableNonEmptyList<int>(source)` | `ArgumentException` |
| Without throwing | `ImmutableNonEmptyList<int>.TryFrom(source, out var list)` | returns `false` |
| One element | `ImmutableNonEmptyList.Singleton(42)` | not possible |
| Repeated element | `ImmutableNonEmptyList<int>.Repeat(0, 3)` | `ArgumentOutOfRangeException` when the count is below 1 |
| From a mutable list | `nonEmptyList.ToImmutable()` | not possible |

```csharp
// source is an IEnumerable<int> from your application
ImmutableNonEmptyList<int> ids = [3, 1, 2];
var sizes = ImmutableNonEmptyList.Of("S", "M", "L");
ImmutableNonEmptyList<int> fromSource = ImmutableNonEmptyList.From(source);

if (ImmutableNonEmptyList<int>.TryFrom(source, out ImmutableNonEmptyList<int>? parsed))
{
    Console.WriteLine(parsed!.Head);
}
```

What else you should know:

- Every way of creating a list rejects `null` elements with `ArgumentNullException`. `TryFrom` returns `false` for them instead.
- `From(null)` throws `ArgumentException`. The sequence constructor `new ImmutableNonEmptyList<T>(null)` throws `ArgumentNullException`.
- The non-generic `ImmutableNonEmptyList` class has `Create`, `Of`, `From` and `Singleton`. `TryFrom` and `Repeat` are only on the generic type, `ImmutableNonEmptyList<T>`.
- `TryFrom` has no `[NotNullWhen(true)]` annotation on its `out` parameter, so the compiler still treats the result as maybe-null after a `true` return. That is why the example uses `parsed!`.

## Reading elements

Reading works as on `NonEmptyList<T>`. See [Reading elements](./reading-elements.md) for the full story.

| Member | Returns | Notes |
|--------|---------|-------|
| `Head`, `First` | `T` | never throws |
| `Last` | `T` | never throws |
| `Tail` | `ImmutableNonEmptyList<T>?` | everything after the head, `null` for a single element |
| `Init` | `ImmutableNonEmptyList<T>?` | everything before the last element, `null` for a single element |
| `Count` | `int` | always at least 1 |
| `list[i]`, `list[^1]` | `T` | `int` and `Index` indexers |
| `list[1..]`, `Slice(start, length)` | `T[]` | a copy, may be empty |
| `AsSpan()` | `ReadOnlySpan<T>` | always valid, because the list never changes |

List patterns work too. The compiler does not know the list is never empty, so keep a discard arm to make the `switch` exhaustive:

```csharp
ImmutableNonEmptyList<int> scores = [90, 72, 85];

string summary = scores switch
{
    [var only] => $"one score: {only}",
    [var best, .. var rest] => $"{best} and {rest.Length} more",
    _ => throw new System.Diagnostics.UnreachableException(),
};
```

`Deconstruct` splits the list into its head and its tail: `var (head, tail) = scores;`, where `tail` is `null` for a single element.

## Returning a new list

These methods return a new list. The original never changes.

| Method | Returns | Behaviour |
|--------|---------|-----------|
| `Append(item)` | `ImmutableNonEmptyList<T>` | adds at the end |
| `AppendRange(items)` | `ImmutableNonEmptyList<T>` | adds at the end; an empty `items` returns the same instance |
| `Prepend(item)` | `ImmutableNonEmptyList<T>` | adds at the start |
| `Insert(index, item)` | `ImmutableNonEmptyList<T>` | `index` may be `0` to `Count` |
| `SetItem(index, item)` | `ImmutableNonEmptyList<T>` | replaces one element |
| `Concat(other)` | `ImmutableNonEmptyList<T>` | `other` is another `ImmutableNonEmptyList<T>` |
| `Reverse()` | `ImmutableNonEmptyList<T>` | |
| `Distinct()` | `ImmutableNonEmptyList<T>` | keeps the first occurrence of each value |
| `Intersperse(separator)` | `ImmutableNonEmptyList<T>` | `[a, b]` becomes `[a, separator, b]` |

The methods that add or replace an element throw `ArgumentNullException` for a `null` item. `Insert`, `SetItem` and `RemoveAt` throw `ArgumentOutOfRangeException` for an index outside the list.

## Removals that would empty the list

A removal can leave nothing behind. A `NonEmptyList<T>` throws in that case (see [Safe mutation](./safe-mutation.md)). An `ImmutableNonEmptyList<T>` returns `null` instead, so the return type tells you to handle it.

| Method | Returns `null` when | Otherwise |
|--------|--------------------|-----------|
| `RemoveFirst()` | the list has one element | the tail |
| `RemoveLast()` | the list has one element | the list without its last element |
| `RemoveAt(index)` | the list has one element | the list without that element |
| `Remove(item)` | `item` is the only element | the list without the first occurrence; **the same instance** when `item` is not found |
| `Take(count)` | `count` is less than 1 | the first `count` elements |
| `Skip(count)` | `count` is `Count` or more | the remaining elements |

```csharp
ImmutableNonEmptyList<string> jobs = ["build", "test", "deploy"];

ImmutableNonEmptyList<string>? remaining = jobs.RemoveFirst();
if (remaining is null)
{
    Console.WriteLine("That was the last job.");
}
else
{
    Console.WriteLine($"Next: {remaining.Head}");   // Next: test
}

// Keep the last element instead of losing it
ImmutableNonEmptyList<string> trimmed = jobs.RemoveLast() ?? jobs;
```

:::note

`Take`, `Skip`, `Reverse`, `Distinct` and `Concat` are instance methods, so they win over the LINQ operators of the same name. `scores.Take(2)` returns `ImmutableNonEmptyList<int>?`, not `IEnumerable<int>`.

:::

## Functional operations

| Method | Returns |
|--------|---------|
| `Map(selector)` | `ImmutableNonEmptyList<TResult>` |
| `MapWithIndex((item, index) => ...)` | `ImmutableNonEmptyList<TResult>` |
| `FlatMap(selector)` | `ImmutableNonEmptyList<TResult>`; the selector returns an `ImmutableNonEmptyList<TResult>` |
| `Reduce((a, b) => ...)` | `T`, with no seed and no empty case |
| `Fold(seed, (acc, item) => ...)` | `TResult` |
| `Zip(other)` | `ImmutableNonEmptyList<(T First, TOther Second)>`, as long as the shorter list |
| `Match(single, multiple)` | `TResult`; `single` gets the only element, `multiple` gets the head and the tail |

```csharp
ImmutableNonEmptyList<decimal> prices = [9.99m, 4.50m, 12.00m];

ImmutableNonEmptyList<decimal> withVat = prices.Map(p => p * 1.2m);
decimal total = prices.Reduce((a, b) => a + b);
string label = prices.Match(
    single: p => $"one item at {p}",
    multiple: (first, rest) => $"{first} and {rest.Count} more");
```

The type-preserving LINQ operators described in [LINQ](./linq.md) are defined for `NonEmptyList<T>`. On an `ImmutableNonEmptyList<T>`, `Select`, `Where` and the other LINQ operators are the standard ones and return `IEnumerable<T>`. Use `Map` to stay in the immutable type, or convert with `ToMutable()`.

## Converting

| From | To | Call |
|------|----|------|
| `NonEmptyList<T>` | `ImmutableNonEmptyList<T>` | `list.ToImmutable()` |
| `ImmutableNonEmptyList<T>` | `NonEmptyList<T>` | `list.ToMutable()` |
| `ImmutableNonEmptyList<T>` | `T[]` | `list.ToArray()` |
| `ImmutableNonEmptyList<T>` | `List<T>` | `list.ToList()` |
| `ImmutableNonEmptyList<T>` | `NonEmptySpan<T>` | `list.AsNonEmptySpan()`, see [Spans and lazy sequences](./spans-and-lazy-sequences.md) |
| `ImmutableNonEmptyList<T>` | `NonEmptyZipper<T>` | `list.ToZipper()`, see [Modeling types](./modeling-types.md) |

Every conversion copies the elements, so changing the mutable copy never affects the immutable list.

```csharp
NonEmptyList<string> draft = ["intro", "body"];
ImmutableNonEmptyList<string> published = draft.ToImmutable();

draft.Add("appendix");
Console.WriteLine(published.Count);   // 2
```

## Equality

Two immutable lists are equal when they hold equal elements in the same order. Elements are compared with their default equality.

```csharp
ImmutableNonEmptyList<int> a = [1, 2, 3];
ImmutableNonEmptyList<int> b = ImmutableNonEmptyList.Of(1, 2, 3);

Console.WriteLine(a == b);                             // True
Console.WriteLine(a.Equals(b));                        // True
Console.WriteLine(a.GetHashCode() == b.GetHashCode()); // True
Console.WriteLine(a == a.Reverse());                   // False
```

- `==` and `!=` compare contents, not references. Two `null` references are equal.
- `GetHashCode` combines every element, so equal lists can be dictionary keys and set members.
- An `ImmutableNonEmptyList<T>` is never equal to a `NonEmptyList<T>` with the same elements, because `Equals(object)` checks the type. Use `a.SequenceEqual(b)` to compare across types.
- `ToString()` returns `ImmutableNonEmptyList: [1, 2, 3]`.

## Serialization and storage

System.Text.Json support is built in. The list is written as a JSON array. Reading an empty array or an array with a `null` element throws `JsonException`; a JSON `null` reads as a `null` reference. See [Serialization](./serialization.md) for the other serializers.

For EF Core, configure a property with `HasImmutableNonEmptyListConversion()`. See [Entity Framework Core](./entity-framework-core.md).

## Which list to use

| Type | Changes in place | Removing the last element | Extra rules |
|------|------------------|---------------------------|-------------|
| `NonEmptyList<T>` | yes | throws | none |
| `ImmutableNonEmptyList<T>` | no, returns new lists | returns `null` | none |
| `ConstrainedList<T, TConstraint>` | no, mutating members throw | `WithoutAt` throws | a type-level constraint, see [Constrained lists](./constrained-lists.md) |
