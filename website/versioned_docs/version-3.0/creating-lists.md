---
id: creating-lists
title: "Creating Lists"
description: "Build a NonEmptyList from constructors, collection expressions, factories and existing sequences, and know which calls throw and which return null."
---

There are four ways into a `NonEmptyList<T>`: constructors, collection expressions, the non-generic `NonEmptyList` factory class, and extension methods on sequences you already have. When the compiler can see at least one element (a constructor's first argument, a non-empty collection expression), creation cannot fail for lack of elements. When the elements come from a sequence that might be empty, you choose between a method that throws and one that tells you it failed.

Every way of creating a list also rejects `null` elements. A `NonEmptyList<T>` never contains `null`.

## Constructors

The constructors that take a first element separately make "at least one" part of the call. You cannot call them without an element.

```csharp
NonEmptyList<string> one = new("Ada");
NonEmptyList<string> three = new("Ada", "Grace", "Linus");

// The rest can be any sequence, and it may be empty
List<string> others = new();
NonEmptyList<string> stillOne = new("Ada", others);
```

| Constructor | Notes |
|-------------|-------|
| `NonEmptyList(T firstItem, params T[] otherItems)` | The first item is required. |
| `NonEmptyList(T firstItem, params ReadOnlySpan<T> otherItems)` | Same, without allocating an array. |
| `NonEmptyList(T firstItem, IEnumerable<T> otherItems)` | `otherItems` may be empty. A `null` `otherItems` is treated as "no other items". |
| `NonEmptyList(IEnumerable<T> items)` | Copies the sequence. Throws `ArgumentException` if it is empty and `ArgumentNullException` if it is `null`. |

All four throw `ArgumentNullException` when the first item, or any other element, is `null`. The list copies its input, so changing the source sequence later does not change the list.

:::warning

`new NonEmptyList<string>(null)` does not compile. The call is ambiguous between the `IEnumerable<T>` constructor and the first-item constructors. Cast the argument, for example `new NonEmptyList<string>((IEnumerable<string>)source)`, to say which one you mean.

:::

## Collection expressions

`NonEmptyList<T>` supports C# 12 collection expressions, including spreads:

```csharp
NonEmptyList<int> numbers = [1, 2, 3];

NonEmptyList<int> more = [.. numbers, 4, .. new[] { 5, 6 }]; // 1, 2, 3, 4, 5, 6
```

The compiler calls `NonEmptyList.Create(ReadOnlySpan<T>)` for you. An expression that turns out to be empty at run time, for example `[.. someEmptyList]`, throws `ArgumentException`.

:::note

`NonEmptyList<int> none = [];` is always an error. The bundled analyzer reports it at compile time as `NEL001`, so it never reaches run time. See [Analyzers and source generator](./analyzers.md).

:::

`ImmutableNonEmptyList<T>` accepts collection expressions too. See [Immutable lists](./immutable-lists.md).

## The NonEmptyList factory class

The static, non-generic `NonEmptyList` class infers the element type from the arguments, so you rarely need to spell out `<T>`.

```csharp
var names = NonEmptyList.Of("Ada", "Grace", "Linus"); // NonEmptyList<string>
var single = NonEmptyList.Singleton(42);              // [42]
var zeros = NonEmptyList.Repeat(0, 3);                // [0, 0, 0]
var days = NonEmptyList.Range(1, 7);                  // [1, 2, 3, 4, 5, 6, 7]
var powers = NonEmptyList.Iterate(1, x => x * 2, 5);  // [1, 2, 4, 8, 16]
```

| Method | Returns | What it does | Throws |
|--------|---------|--------------|--------|
| `Of<T>(T first, params T[] rest)` and `Of<T>(T first, params ReadOnlySpan<T> rest)` | `NonEmptyList<T>` | A first element and any number of others. | `ArgumentNullException` for a `null` element |
| `Create<T>(ReadOnlySpan<T> items)` | `NonEmptyList<T>` | Copies a span. This is the method collection expressions call. | `ArgumentException` when the span is empty; `ArgumentNullException` for a `null` element |
| `From<T>(IEnumerable<T> source)` | `NonEmptyList<T>` | Copies a sequence. | `ArgumentException` when the sequence is `null` or empty; `ArgumentNullException` when it contains `null` |
| `TryFrom<T>(IEnumerable<T>? source, out NonEmptyList<T>? result)` | `bool` | Copies a sequence if it can. | Never throws for bad input; returns `false` |
| `Singleton<T>(T item)` | `NonEmptyList<T>` | A one-element list. | `ArgumentNullException` when `item` is `null` |
| `Repeat<T>(T item, int count)` | `NonEmptyList<T>` | `item` repeated `count` times. | `ArgumentOutOfRangeException` when `count` is less than 1 |
| `Range(int start, int count)` | `NonEmptyList<int>` | Consecutive integers from `start`. | `ArgumentOutOfRangeException` when `count` is less than 1 |
| `Iterate<T>(T seed, Func<T, T> next, int count)` | `NonEmptyList<T>` | `seed, next(seed), next(next(seed)), ...`, `count` elements in total. | `ArgumentOutOfRangeException` when `count` is less than 1; `ArgumentNullException` when `next` is `null` |
| `Iterate<T>(T seed, Func<T, T> next)` | `NonEmptyEnumerable<T>` | The same, as an infinite lazy sequence. | `ArgumentNullException` when `next` is `null` |
| `Unfold<T>(T seed, Func<T, T?> next)` where `T : class` | `NonEmptyList<T>` | Applies `next` until it returns `null`. | `ArgumentNullException` when `seed` or `next` is `null` |
| `UnfoldValue<T>(T seed, Func<T, T?> next)` where `T : struct` | `NonEmptyList<T>` | The same for value types. | `ArgumentNullException` when `next` is `null` |

`Unfold` and `UnfoldValue` always include the seed, so the result is never empty, even when `next` stops straight away:

```csharp
// "a", "aa", "aaa"
NonEmptyList<string> steps = NonEmptyList.Unfold("a", s => s.Length < 3 ? s + "a" : null);

// 3, 2, 1
NonEmptyList<int> countdown = NonEmptyList.UnfoldValue(3, n => n > 1 ? n - 1 : null);
```

For the lazy `Iterate` overload and `NonEmptyEnumerable<T>`, see [Spans and lazy sequences](./spans-and-lazy-sequences.md).

### Static methods on `NonEmptyList<T>`

The generic type has its own static factories. They behave like the factory class above:

| Method | Behaves like |
|--------|--------------|
| `NonEmptyList<T>.From(IEnumerable<T> enumerable)` | `NonEmptyList.From` |
| `NonEmptyList<T>.TryFrom(IEnumerable<T>? enumerable, out NonEmptyList<T>? result)` | `NonEmptyList.TryFrom` |
| `NonEmptyList<T>.FromSpan(ReadOnlySpan<T> items)` | `NonEmptyList.Create`; throws `ArgumentException` for an empty span |
| `NonEmptyList<T>.Singleton(T item)` | `NonEmptyList.Singleton` |
| `NonEmptyList<T>.Repeat(T item, int count)` | `NonEmptyList.Repeat` |

`NonEmptyList<T>` also has `Parse` and `TryParse` for comma-separated text such as `"1,2,3"`. That is what lets ASP.NET Core bind a list straight from a query string. See [ASP.NET Core](./aspnetcore.md).

## From an existing sequence

Three extension methods turn any `IEnumerable<T>` into a non-empty list. Pick the one that matches what an empty sequence means for your code.

```csharp
// Order is your own class; orders is an IEnumerable<Order> from your application
NonEmptyList<Order> required = orders.ToNonEmptyList();        // empty is a bug: throws

NonEmptyList<Order>? maybe = orders.ToNonEmptyListOrNull();    // empty is normal: null

if (orders.TryToNonEmptyList(out NonEmptyList<Order>? batch))  // empty is normal: branch
{
    Console.WriteLine($"Processing {batch.Count} orders, starting with {batch.Head}");
}
```

| Method | Returns |
|--------|---------|
| `ToNonEmptyList<T>(this IEnumerable<T> source)` | The list. Throws `ArgumentException` when the sequence is `null` or empty. |
| `ToNonEmptyList<T>(this ReadOnlySpan<T> source)` | The list. Throws `ArgumentException` when the span is empty. |
| `ToNonEmptyListOrNull<T>(this IEnumerable<T>? source)` | The list, or `null`. |
| `TryToNonEmptyList<T>(this IEnumerable<T>? source, out NonEmptyList<T>? result)` | `true` and the list, or `false` and `null`. |

`TryFrom` and `TryToNonEmptyList` mark `result` with `[NotNullWhen(true)]`, so inside the `if` the compiler knows the list is not `null` and you need no `!`.

## What throws and what returns null

The methods that take a sequence fall into two groups. The throwing ones are for input you expect to have elements; the `Try` and `OrNull` ones are for input that may legitimately be empty.

| Input | `From`, `ToNonEmptyList` | `new NonEmptyList<T>(IEnumerable<T>)` | `TryFrom`, `TryToNonEmptyList` | `ToNonEmptyListOrNull` |
|-------|--------------------------|----------------------------------------|--------------------------------|------------------------|
| `null` sequence | `ArgumentException` | `ArgumentNullException` | `false` | `null` |
| Empty sequence | `ArgumentException` | `ArgumentException` | `false` | `null` |
| Contains a `null` element | `ArgumentNullException` | `ArgumentNullException` | `false` | `null` |
| At least one element, no `null` | the list | the list | `true` and the list | the list |

:::note

`From` and `ToNonEmptyList` throw `ArgumentException`, not `ArgumentNullException`, for a `null` sequence. The constructor throws `ArgumentNullException`. Because `ArgumentNullException` derives from `ArgumentException`, `catch (ArgumentException)` handles both.

:::

:::tip

The analyzer reports a factory call whose argument is provably empty, such as `new List<int>().ToNonEmptyList()` or `NonEmptyList.From(Array.Empty<int>())`, as error `NEL002`. Calls on variables and method results are checked at run time as described above.

:::
