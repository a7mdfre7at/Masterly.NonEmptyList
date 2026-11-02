---
id: domain-collections
title: "Domain Collections"
description: "The [NonEmptyCollection] source generator: one attribute on a partial class gives you a named non-empty collection with factories, validation, JSON support and invariants enforced on every mutation."
---

`NonEmptyList<OrderLine>` says "at least one line". A domain often says more: no duplicate lines, at most 500 of them, and the type should be called `OrderLines`. The `[NonEmptyCollection]` source generator writes that class for you. You declare an empty partial class, and the generator completes it: it derives from `NonEmptyList<T>`, adds constructors and factories, and overrides the list's invariant hooks so the rules hold after every `Add`, `Insert` and indexer write, not only at construction.

```csharp title="OrderLines.cs"
using Masterly.NonEmptyList;

public sealed record OrderLine(string Sku, int Quantity);

[NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 500)]
public sealed partial class OrderLines;
```

```csharp
// line1, line2 are OrderLine values; input is an IEnumerable<OrderLine>
OrderLines lines = [line1, line2];                    // throws on duplicates or more than 500
OrderLines fromInput = OrderLines.From(input);        // throws or returns, never empty
Validation<OrderLines, ValidationError> checkedLines = OrderLines.Validate(input, "lines");

lines.Add(line1);                                     // ArgumentException: duplicate
```

The generator ships inside the `Masterly.NonEmptyList` package. There is nothing else to install.

## The attribute

```csharp
[NonEmptyCollection(typeof(TElement), Distinct = ..., MinCount = ..., MaxCount = ..., Immutable = ...)]
```

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `ElementType` | `Type` | required, the constructor argument | the element type `T`; the class derives from `NonEmptyList<T>` |
| `Distinct` | `bool` | `false` | reject duplicate elements, compared with `EqualityComparer<T>.Default` |
| `MinCount` | `int` | `1` | the minimum number of elements; cannot be lower than 1 |
| `MaxCount` | `int` | `0` | the maximum number of elements; `0` means no limit |
| `Immutable` | `bool` | `false` | reject every mutation, so the invariants are checked once at construction; use it for value objects |

```csharp
// A value object: at least two stops, never changed after construction
[NonEmptyCollection(typeof(string), Immutable = true, MinCount = 2)]
public sealed partial class Route;

// A plain named collection with no extra rules
[NonEmptyCollection(typeof(int))]
public sealed partial class Scores;
```

## Requirements

The class must meet four rules. When one is broken, the generator reports an error and generates nothing for that class.

| Diagnostic | Rule |
|------------|------|
| NEL101 | the class must be `partial` |
| NEL102 | the class must not be nested inside another type; declare it at namespace scope |
| NEL103 | `MinCount` must be at least 1, and `MaxCount` must be `0` or at least `MinCount` |
| NEL104 | the class must not declare a base class; the generator supplies `NonEmptyList<T>` (writing `: NonEmptyList<T>` yourself is accepted) |

The generated part keeps your declaration's accessibility and namespace, so `internal` classes and classes in the global namespace work.

## What is generated

For `[NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 500)] partial class OrderLines`, the generator adds:

| Member | Purpose |
|--------|---------|
| `: NonEmptyList<OrderLine>` | the full non-empty API, type-preserving LINQ, generic math and list patterns |
| `OrderLines(OrderLine first, params OrderLine[] rest)` | constructor that checks every invariant |
| `OrderLines(IEnumerable<OrderLine> items)` | constructor from a sequence; checks every invariant |
| `static OrderLines From(IEnumerable<OrderLine> items)` | factory; same checks as the constructor |
| `static OrderLines Of(OrderLine first, params OrderLine[] rest)` | factory |
| `static OrderLines Create(ReadOnlySpan<OrderLine> items)` | the collection-expression builder |
| `static bool TryFrom(IEnumerable<OrderLine>? items, out OrderLines? result)` | returns `false` instead of throwing; `[NotNullWhen(true)]` on `result` |
| `static bool Satisfies(IEnumerable<OrderLine>? items)` | `true` when `TryFrom` would succeed |
| `static Validation<OrderLines, ValidationError> Validate(IEnumerable<OrderLine>? items, string? path = null)` | validation result instead of an exception, see [below](#validate) |
| `const int MinCount`, `const int MaxCount` | the bounds; `MaxCount` is `int.MaxValue` when unbounded |
| `const bool IsDistinct`, `const bool IsImmutable` | the flags |
| `static string Description` | a readable summary, such as `at least 1 element, at most 500, distinct` |
| `[CollectionBuilder]` | `OrderLines lines = [a, b];` |
| nested `OrderLines.JsonConverter` and `[JsonConverter]` | System.Text.Json support with every invariant checked on read |
| `[DebuggerDisplay("Count = {Count}, Head = {Head}")]`, `ToString()` | `OrderLines: [a, b]` |
| invariant hook overrides | the rules on every mutation, see [Invariants on every mutation](#invariants-on-every-mutation) |

The generated file is named `<Namespace>.<Name>.NonEmptyCollection.g.cs`. You can add your own members to the partial class: domain methods, computed properties, extra factories.

```csharp
public sealed partial class OrderLines
{
    public int TotalQuantity => this.Sum(l => l.Quantity);

    public bool Contains(string sku) => Exists(l => l.Sku == sku);
}
```

## Creating instances

All the creation paths check the same rules. On failure they throw `ArgumentException` (or its subclass `ArgumentNullException` for `null` elements):

```csharp
OrderLines a = OrderLines.Of(line1, line2);
OrderLines b = new(input);
OrderLines c = [line1, line2];

OrderLines.Of(line1, line1);           // ArgumentException: duplicate
OrderLines.From([]);                   // ArgumentException: empty
Route.Of("Amman");                     // ArgumentException: at least 2 elements

if (OrderLines.TryFrom(input, out OrderLines? parsed))
{
    Console.WriteLine(parsed.Head);
}
```

`TryFrom` and `Satisfies` return `false` for `null`, empty input and any broken rule.

## Validate

`Validate` returns a `Validation<TCollection, ValidationError>` instead of throwing, so it fits into the error-accumulating flow on [Validation](./validation.md). The optional `path` is recorded on every error.

| Problem | Code | Message |
|---------|------|---------|
| `items` is `null` | `required` | `A value is required` |
| `items` is empty | `nonEmpty` | `At least one element is required` |
| an element is `null` (only checked when the element type can be `null`) | `noNulls` | `Elements cannot be null` |
| fewer than `MinCount` elements | `minCount` | `At least {MinCount} elements are required` |
| more than `MaxCount` elements | `maxCount` | `At most {MaxCount} elements are allowed` |
| duplicates, when `Distinct = true` | `distinct` | `Elements must be distinct` |

The first three stop validation with a single error. The last three are checked together, so one call can report several:

```csharp
// OrderLines has Distinct = true and, in this example, MaxCount = 3; a, b and c are different lines
Validation<OrderLines, ValidationError> result = OrderLines.Validate([a, a, b, c], "lines");

// result.Errors: maxCount, distinct
```

## Invariants on every mutation

A generated collection is a mutable `NonEmptyList<T>` (unless `Immutable = true`), so the rules have to hold after it changes, too. The generator overrides the protected hooks that `NonEmptyList<T>` calls before every change:

| Hook | Generated when | Throws |
|------|----------------|--------|
| `ValidateItem(item)` | `Distinct = true` | `ArgumentException` when the item is already in the collection |
| `ValidateRange(items)` | `Distinct = true` | `ArgumentException` when a batch repeats an existing element or contains duplicates itself |
| `ValidateReplacement(index, item)` | `Distinct = true` | `ArgumentException` when an indexer write would duplicate another element |
| `ValidateCount(proposedCount)` | `MinCount > 1` or `MaxCount > 0` | `InvalidOperationException` when the change would go below `MinCount` or above `MaxCount` |
| `EnsureMutable()` and `IsReadOnly` | `Immutable = true` | `NotSupportedException` for every mutation; `IsReadOnly` reports `true` |

```csharp
// OrderLines has Distinct = true and MaxCount = 3; a, b, c and d are different OrderLine values
OrderLines lines = OrderLines.Of(a, b);

lines.Add(c);              // fine
lines.Add(a);              // ArgumentException: duplicate
lines.Add(d);              // InvalidOperationException: more than 3
lines[0] = b;              // ArgumentException: b is already at index 1
lines[0] = a;              // fine: replacing an element with itself

Route route = Route.Of("Amman", "Petra");
route.Add("Aqaba");        // NotSupportedException: Route is immutable
```

Removing the last element is blocked by `NonEmptyList<T>` itself, as for every non-empty list. With a `MinCount` above 1, removals stop at `MinCount`.

:::note

The hooks run before the list changes. A call that throws leaves the collection exactly as it was.

:::

You can use the same hooks in a hand-written class that derives from `NonEmptyList<T>`. The generator is a convenience over them, not a separate mechanism.

## JSON and the other packages

The generated `JsonConverter` reads a JSON array and builds the collection through its constructor, so every invariant is checked. An empty array, a `null` element or a broken rule throws `JsonException`; a JSON `null` reads as `null`. The collection is written as a plain array.

```csharp
using System.Text.Json;

public sealed record OrderDto(int Id, OrderLines Lines);

// lines holds OrderLine("A", 1) and OrderLine("B", 2)
string json = JsonSerializer.Serialize(new OrderDto(42, lines));
// {"Id":42,"Lines":[{"Sku":"A","Quantity":1},{"Sku":"B","Quantity":2}]}
```

The integration packages recognize generated collections:

- The ASP.NET Core OpenAPI transformer reads `MinCount`, `MaxCount` and `IsDistinct` into the schema. See [ASP.NET Core](./aspnetcore.md).
- Configuration binding, the Newtonsoft.Json converter, the MessagePack formatters and the AutoFixture customization create instances through the generated `From` factory, so the rules apply there too. See [Configuration](./configuration.md), [Serialization](./serialization.md) and [Testing](./testing.md).

The analyzers treat a generated collection as a non-empty list, so `OrderLines lines = [];` is reported as NEL001. See [Analyzers and code fixes](./analyzers.md).
