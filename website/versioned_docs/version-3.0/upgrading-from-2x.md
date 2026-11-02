---
id: upgrading-from-2x
title: "Upgrading from 2.x"
description: "What changed in 3.0, why, and how to move code written against Masterly.NonEmptyList 2.x."
---

Version 3.0 changes how `NonEmptyList<T>` is built: it wraps a list instead of deriving from `List<T>`. Most code that reads, adds and transforms elements compiles unchanged. Code that treats the list as a `List<T>`, or relies on a few 2.x return types, needs small edits. This page lists every breaking change, with the reason and before/after code.

**Rebuild everything.** The base type of `NonEmptyList<T>` changed, so 3.0 is a binary break. Rebuild every assembly that references Masterly.NonEmptyList, and upgrade every `Masterly.NonEmptyList.*` package to 3.0 at the same time. The 2.x EF Core package, for example, was built against the 2.x core.

## Target frameworks

2.x targeted .NET 6 and .NET 8. Every 3.0 package targets .NET 8, .NET 9 and .NET 10. .NET 6 is no longer supported.

Why: .NET 6 is out of support, and 3.0 relies on generic math and static abstract interface members, which .NET 6 does not have.

```xml
<!-- Before -->
<TargetFramework>net6.0</TargetFramework>

<!-- After: net8.0, net9.0 or net10.0 -->
<TargetFramework>net8.0</TargetFramework>
```

## `NonEmptyList<T>` no longer derives from `List<T>`

In 2.x, `NonEmptyList<T>` inherited from `List<T>` and hid the members that could empty it with `new`. Hiding only works when the compiler sees the static type `NonEmptyList<T>`. Through an interface or a base-class reference, the `List<T>` members ran, and the list could become empty:

```csharp
// 2.x
var list = new NonEmptyList<int>(1, 2);
((IList<int>)list).Clear();        // list.Count is now 0

var single = new NonEmptyList<int>(1);
List<int> plain = single;
plain.RemoveAt(0);                 // List<T>.RemoveAt runs: single.Count is now 0
```

In 3.0, `NonEmptyList<T>` wraps a private `List<T>` and implements `IList<T>`, `IReadOnlyList<T>` and `IList` itself. Every path goes through the guarded members:

```csharp
// 3.0
var list = new NonEmptyList<int>(1, 2);
((IList<int>)list).Clear();        // NotSupportedException
var single = new NonEmptyList<int>(1);
((ICollection<int>)single).Remove(1);   // InvalidOperationException
```

The public `List<T>` surface is still there: `Add`, `Insert`, `Sort`, `Find`, `BinarySearch`, `GetRange`, `Capacity`, `ForEach` and the rest. What no longer compiles is code that uses a `NonEmptyList<T>` *as* a `List<T>`:

| Code | Compiler error in 3.0 |
|------|-----------------------|
| `List<int> l = list;` | CS0029 |
| `Save(list)` where the parameter is `List<int>` | CS1503 |
| `(List<int>)list` | CS0030 |

Fix it in one of two ways:

- Change the receiving code to accept `IList<T>`, `IReadOnlyList<T>`, `ICollection<T>` or `IEnumerable<T>`. The list itself is passed, with its guarantee intact.
- Call `.ToList()` where a `List<T>` is really required.

```csharp
// Before (2.x)
void Save(List<Order> orders) { /* ... */ }
Save(nonEmptyOrders);

// After (3.0): widen the parameter
void Save(IReadOnlyList<Order> orders) { /* ... */ }
Save(nonEmptyOrders);

// After (3.0): or pass a copy
Save(nonEmptyOrders.ToList());
```

The package bundles a code fix for this migration. Wherever the compiler reports CS1503, CS0029 or CS0266 (and CS1662 in lambdas) because a non-empty list is used where a `List<T>` is required, the light bulb offers **Convert with ToList()**. Use *Fix all in solution* to apply it everywhere at once. See [Analyzers](./analyzers.md).

:::warning

`.ToList()` returns a copy. In 2.x the callee received the list itself, so its changes were visible to the caller. After the fix, changes made to the copy do not reach the original. Where the callee must change the list, widen its parameter to `IList<T>` instead.

:::

:::note

The code fix does not cover explicit casts such as `(List<int>)list`, which report CS0030. Replace those by hand. Runtime type checks such as `list is List<int>` now return `false`.

:::

## `Prepend` returns a new list

In 2.x, `Prepend(item)` returned `void` and inserted at index 0 in place. In 3.0, `Prepend(item)` returns a new `NonEmptyList<T>` and leaves the original unchanged, the same as LINQ's `Prepend` and `ImmutableNonEmptyList<T>.Prepend`. Use the new `AddFirst(item)` to insert in place.

Why: this matches LINQ's `Prepend` and `ImmutableNonEmptyList<T>.Prepend`, which both return a new sequence and leave the source alone.

```csharp
// Before (2.x)
list.Prepend(0);                         // list is now [0, 1, 2, 3]

// After (3.0): in place
list.AddFirst(0);                        // list is now [0, 1, 2, 3]

// After (3.0): new list
NonEmptyList<int> withZero = list.Prepend(0);   // list is unchanged
```

:::warning

A 2.x call written as a statement, `list.Prepend(0);`, still compiles in 3.0 but now does nothing: the new list is discarded. Search your code for `.Prepend(` and replace each statement call with `AddFirst`.

:::

## `Remove` returns `bool`

In 2.x, `Remove(item)` returned `void`. Its behaviour for an absent item depended on the count: with several elements it did nothing, with one element it threw `InvalidOperationException` even though nothing would be removed.

In 3.0, `Remove(item)` returns `bool`, like `List<T>.Remove`. It returns `false` for an absent item, whatever the count. It still throws `InvalidOperationException` when the item is present and is the only element, and `ArgumentNullException` for `null`.

Why: this matches `List<T>.Remove` and `ICollection<T>.Remove`, which report whether an element was removed. The 2.x signature could not.

```csharp
// Before (2.x)
if (list.Contains(item))
    list.Remove(item);

// After (3.0)
bool removed = list.Remove(item);
```

Existing calls written as statements (`list.Remove(item);`) compile unchanged. Code that caught `InvalidOperationException` to detect an absent item in a single-element list now gets `false` instead.

## `AsReadOnly` returns `ReadOnlyCollection<T>`

In 2.x, `AsReadOnly()` returned the list itself, typed as `IReadOnlyList<T>`. A caller could cast it back and change it. In 3.0, it returns a `ReadOnlyCollection<T>` wrapper, the same type `List<T>.AsReadOnly()` returns.

Why: the 2.x result was the mutable list itself under a read-only static type, so it was not a read-only view.

```csharp
// Before (2.x)
IReadOnlyList<int> view = list.AsReadOnly();
var back = (NonEmptyList<int>)view;      // worked: the view was the list

// After (3.0)
ReadOnlyCollection<int> view = list.AsReadOnly();
IReadOnlyList<int> sameAsBefore = list.AsReadOnly();   // still compiles
// Casting the view back to NonEmptyList<int> no longer works.
```

Code that only assigns the result to `IReadOnlyList<T>` or enumerates it is unaffected. The wrapper still reflects later changes to the list.

## `TryValidate` out parameter annotation

The signature changed from `TryValidate(Func<T, bool> predicate, out T? failingElement)` to `TryValidate(Func<T, bool> predicate, [MaybeNullWhen(true)] out T failingElement)`.

Why: the failing element is always set when the method returns `false`. The new annotation tells the compiler so, and removes the nullable warning at the most common call site.

```csharp
// Before (2.x)
if (!list.TryValidate(x => x.IsActive, out var bad))
    Console.WriteLine(bad!.Name);        // `!` needed to silence the warning

// After (3.0)
if (!list.TryValidate(x => x.IsActive, out var bad))
    Console.WriteLine(bad.Name);         // no warning
```

There is no runtime change. You can remove `!` operators added for this call.

## Constructor calls with `null` are ambiguous

3.0 adds `NonEmptyList<T>(IEnumerable<T> items)`, `NonEmptyList<T>(T firstItem, params ReadOnlySpan<T> otherItems)` and `ImmutableNonEmptyList<T>(IEnumerable<T> items)`. A bare `null` argument now matches more than one constructor when `T` is a reference type, and the call no longer compiles (CS0121).

```csharp
// Before (2.x): compiled and threw ArgumentNullException at run time
var list = new NonEmptyList<string>(null);

// After (3.0): CS0121. Cast the argument to say which constructor you mean.
var a = new NonEmptyList<string>((string)null!);                // first-item constructor
var b = new NonEmptyList<string>((IEnumerable<string>)null!);   // sequence constructor
```

Both forms still throw `ArgumentNullException`. This usually only appears in tests that check null handling.

:::note

A related change affects element types that a sequence also converts to, such as `object`. `new NonEmptyList<object>(someList)` with a `List<object>` argument created a one-element list containing `someList` in 2.x. In 3.0 it binds to the `IEnumerable<T>` constructor and copies the elements. Write `new NonEmptyList<object>((object)someList)` to keep the 2.x meaning.

:::

## EF Core: call `UseNonEmptyList()`

If you use `Masterly.NonEmptyList.EntityFrameworkCore`, register the package with `UseNonEmptyList()` on your `DbContextOptionsBuilder`.

Why: 3.0 adds type-preserving LINQ overloads on `NonEmptyList<T>`. Inside a query, `order.Items.Select(i => i.Name)` on a `NonEmptyList<OrderItem>` navigation now binds to `NonEmptyListLinqExtensions.Select` instead of `Enumerable.Select`, and EF Core cannot translate it. `UseNonEmptyList()` adds a query interceptor that rewrites these calls, and the `Count`, `Head`, `First` and `Last` properties, back to `Enumerable` before EF Core compiles the query. It also adds the SaveChanges validation interceptor that `UseNonEmptyListValidation()` added in 2.x.

```csharp
// Before (2.x)
services.AddDbContext<ShopContext>(options =>
    options.UseSqlServer(connectionString)
           .UseNonEmptyListValidation());

// After (3.0)
services.AddDbContext<ShopContext>(options =>
    options.UseSqlServer(connectionString)
           .UseNonEmptyList());        // validation + query translation
```

`UseNonEmptyListValidation()` still exists and still adds only the validation interceptor. `UseNonEmptyListQueryTranslation()` adds only the query interceptor. The rest of the EF Core package API (converters, comparers, relationship and query extensions) is unchanged. See [Entity Framework Core](./entity-framework-core.md).

## Other behaviour changes

These are not compile errors, but you may notice them.

| What changed | What to do |
|--------------|------------|
| LINQ operators that cannot produce an empty result (`Select`, `SelectMany`, `OrderBy`, `ThenBy`, `GroupBy`, `Distinct`, `Union`, `Chunk` and others) now return a `NonEmptyList<T>` (or `OrderedNonEmptyList<T>`) and run immediately. In 2.x they were the deferred `Enumerable` operators. `Where`, `Skip` and `Take` still return a deferred `IEnumerable<T>`. | Check queries that relied on deferred execution, for example a selector with side effects, or a query built before the list was changed. See [LINQ](./linq.md). |
| `Chunk(size)` returns `NonEmptyList<NonEmptyList<T>>`. In 2.x it was `Enumerable.Chunk`, whose chunks are `T[]`. | `foreach (int[] chunk in list.Chunk(2))` now reports CS0030. Declare the loop variable as `var` or `NonEmptyList<T>`, or call `.ToArray()` on each chunk. |
| `ConvertAll` returns `NonEmptyList<TOutput>`. In 2.x it was the inherited `List<T>.ConvertAll` and returned `List<TOutput>`. | Assignments to `List<TOutput>` report CS0029; the code fix offers `.ToList()`. |
| `Max()` and `Min()` on a `NonEmptyList<T>` return `T`, not `T?`. | Nothing; remove null checks you no longer need. |
| `Tail` and `Init` are no longer cached: every read returns a new list that reflects the current elements. In 2.x, `Tail` could return stale elements after a `Sort`. | Read `Tail` once into a variable in hot loops. |
| `MinBy` and `MaxBy` run in O(n) instead of sorting. The result is the same, including the first element on ties. | Nothing. |
| `Random()` uses `Random.Shared` instead of a new `Random` per call. | Nothing. |

For every addition in 3.0, see the [CHANGELOG](https://github.com/a7mdfre7at/Masterly.NonEmptyList/blob/master/CHANGELOG.md).

## Checklist

- [ ] Target .NET 8, .NET 9 or .NET 10.
- [ ] Upgrade `Masterly.NonEmptyList` and every `Masterly.NonEmptyList.*` package to 3.0 together, and rebuild every assembly that references them.
- [ ] Fix CS0029 and CS1503 errors: widen parameters to `IList<T>` or `IReadOnlyList<T>`, or apply the **Convert with ToList()** code fix. Check that no caller relied on the callee changing the original list.
- [ ] Replace explicit `(List<T>)` casts (CS0030) and `is List<T>` checks by hand.
- [ ] Replace statement calls `list.Prepend(x);` with `list.AddFirst(x);`.
- [ ] Review code around `Remove` that expected an exception for an absent item.
- [ ] Remove casts of `AsReadOnly()` back to `NonEmptyList<T>`.
- [ ] Cast `null` arguments to `new NonEmptyList<T>(...)` and `new ImmutableNonEmptyList<T>(...)`, and check `NonEmptyList<object>` constructor calls.
- [ ] Replace `UseNonEmptyListValidation()` with `UseNonEmptyList()` if you use the EF Core package.
- [ ] Check LINQ queries on `NonEmptyList<T>` that relied on deferred execution, and loops over `Chunk(...)` that declare the chunk as `T[]`.
- [ ] Run your tests.
