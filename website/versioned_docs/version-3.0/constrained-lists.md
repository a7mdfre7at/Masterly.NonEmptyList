---
id: constrained-lists
title: "Constrained Lists"
description: "ConstrainedList<T, TConstraint>: immutable lists whose size, uniqueness or order is part of the type, checked once at construction, with Second() and Third() only where the constraint proves them."
---

"At least one element" is the most common rule about a collection, but not the only one. A route needs at least two waypoints, a podium exactly three places, a recipient list distinct addresses, a schedule sorted dates. `ConstrainedList<T, TConstraint>` handles the whole family. The constraint is a type argument, it is checked once when the list is built, and the list never changes afterwards, so the constraint stays true for the lifetime of the value.

```csharp
using Masterly.NonEmptyList;

// Waypoint is your own record; waypoints is an IEnumerable<Waypoint>
ConstrainedList<Waypoint, AtLeast<Two>> route = ConstrainedList<Waypoint, AtLeast<Two>>.From(waypoints);

Waypoint start = route.Head;
Waypoint next = route.Second();   // compiles only because AtLeast<Two> is in the type
```

A method that takes `ConstrainedList<Waypoint, AtLeast<Two>>` can read the second waypoint without a check. The caller has already proven it exists.

## Constraints

| Constraint | Holds when | Description |
|------------|-----------|-------------|
| `NonEmpty` | the list has at least one element (always true) | `at least 1 element` |
| `AtLeast<N>` | `Count >= N` | `at least 2 elements` |
| `AtMost<N>` | `Count <= N` | `at most 10 elements` |
| `Exactly<N>` | `Count == N` | `exactly 3 elements` |
| `Distinct` | no two elements are equal, by `EqualityComparer<T>.Default` | `distinct elements` |
| `Sorted` | elements are in non-decreasing order, by `Comparer<T>.Default` | `elements in ascending order` |
| `SortedDescending` | elements are in non-increasing order, by `Comparer<T>.Default` | `elements in descending order` |
| `And<A, B>` | both `A` and `B` hold | `{A} and {B}` |
| `Or<A, B>` | `A`, `B` or both hold | `{A} or {B}` |

Every constraint is checked on top of the non-empty guarantee, and `null` elements are always rejected. `Sorted` accepts equal neighbours: `[1, 1, 2]` is sorted.

The description column is what `ConstrainedList<T, TConstraint>.Description` returns, and what error messages use. `And` and `Or` join their operands' descriptions:

```csharp
Console.WriteLine(ConstrainedList<int, And<AtLeast<Two>, Distinct>>.Description);
// at least 2 elements and distinct elements
```

## Cardinalities

`AtLeast`, `AtMost` and `Exactly` take a cardinality type, a type that stands for a number:

| Types | Values |
|-------|--------|
| `One`, `Two`, `Three`, `Four`, `Five`, `Six`, `Seven`, `Eight`, `Nine`, `Ten` | 1 to 10 |
| `Sixteen`, `ThirtyTwo`, `SixtyFour` | 16, 32, 64 |
| `Hundred`, `Thousand` | 100, 1000 |

Each one implements `ICardinality`, so `Two.Value` is `2`. Every cardinality from `Two` up also implements the marker interface `IAtLeastTwo`, and every one from `Three` up implements `IAtLeastThree`. The markers are what make `Second()` and `Third()` possible.

## Creating a constrained list

| Way | Example | On violation |
|-----|---------|--------------|
| Collection expression | `ConstrainedList<string, AtLeast<Three>> podium = ["gold", "silver", "bronze"];` | `ArgumentException` |
| From a sequence | `ConstrainedList<T, C>.From(source)` or `ConstrainedList.From<T, C>(source)` | `ArgumentException` |
| From a span | `ConstrainedList<T, C>.FromSpan(span)` or `ConstrainedList.Create<T, C>(span)` | `ArgumentException` |
| Extension on a sequence | `source.ToConstrainedList<T, C>()` | `ArgumentException` |
| Extension on a non-empty list | `list.Constrain<T, C>()` | `ArgumentException` |
| Without throwing | `ConstrainedList<T, C>.TryFrom(source, out var list)` or `ConstrainedList.TryFrom<T, C>(source, out var list)` | returns `false` |
| Just checking | `ConstrainedList<T, C>.Satisfies(source)` | returns `false` |
| As a validation result | `ConstrainedList<T, C>.Validate(source, path)` | an invalid `Validation` |

- An empty sequence and a constraint violation throw `ArgumentException`. The message names the constraint: `Sequence does not satisfy the constraint: expected at least 2 elements`.
- A `null` sequence or a `null` element throws `ArgumentNullException`.
- `TryFrom` and `Satisfies` return `false` for `null`, empty, `null` elements and violations.
- An empty collection expression `[]` is also reported at compile time by [NEL001](./analyzers.md).
- The list always copies the input, so changing the source afterwards has no effect.

```csharp
// ranked is a NonEmptyList<Candidate> and emails an IEnumerable<string> from your application
ConstrainedList<Candidate, AtLeast<Two>> finalists = ranked.Constrain<Candidate, AtLeast<Two>>();

ConstrainedList<DateOnly, Sorted> schedule = [new(2026, 1, 5), new(2026, 2, 2), new(2026, 3, 2)];

bool ok = ConstrainedList<string, Distinct>.TryFrom(emails, out ConstrainedList<string, Distinct>? unique);
```

**Validate instead of throwing.** `Validate` returns a `Validation<ConstrainedList<T, C>, ValidationError>` with a single error, so it fits straight into [Validation](./validation.md) and error-accumulating request handling. The optional `path` is recorded on the error.

```csharp
// input is an IEnumerable<int>? from a request
Validation<ConstrainedList<int, AtLeast<Two>>, ValidationError> ids =
    ConstrainedList<int, AtLeast<Two>>.Validate(input, path: "ids");
```

| Input | Error code | Message |
|-------|------------|---------|
| `null` | `required` | `A value is required` |
| empty | `nonEmpty` | `At least one element is required` |
| contains `null` | `noNulls` | `Elements cannot be null` |
| violates the constraint | `constraint` | `Expected at least 2 elements` (the description) |

## Second() and Third()

`Second()` and `Third()` compile only when the constraint proves the element exists. They are extension methods whose receiver type spells the constraint out, with a generic constraint on the cardinality:

```csharp
public static T Second<T, TCount>(this ConstrainedList<T, AtLeast<TCount>> list)
    where TCount : ICardinality, IAtLeastTwo
    => list[1];
```

`AtLeast<Two>` matches, because `Two` implements `IAtLeastTwo`. `AtLeast<One>` does not, so `Second()` is a compile error rather than a run-time exception. The same pattern gives you `Third()` (with `IAtLeastThree`) and `RestAfterSecond()`.

| Receiver | `Second()` | `Third()` | `RestAfterSecond()` |
|----------|-----------|-----------|---------------------|
| `ConstrainedList<T, AtLeast<N>>` | N is 2 or more | N is 3 or more | N is 2 or more |
| `ConstrainedList<T, Exactly<N>>` | N is 2 or more | N is 3 or more | no |
| `ConstrainedList<T, And<AtLeast<N>, TOther>>` | N is 2 or more | N is 3 or more | no |
| anything else | no | no | no |

```csharp
ConstrainedList<string, AtLeast<Three>> podium = ["gold", "silver", "bronze"];
string silver = podium.Second();
string bronze = podium.Third();

ConstrainedList<string, And<AtLeast<Two>, Distinct>> recipients = ["ada@example.com", "grace@example.com"];
string cc = recipients.Second();

List<string> others = podium.RestAfterSecond();   // ["bronze"]; may be empty
```

:::note

In an `And`, the `AtLeast` must be the first operand. `ConstrainedList<T, And<AtLeast<Two>, Distinct>>` has `Second()`. `ConstrainedList<T, And<Distinct, AtLeast<Two>>>` holds the same lists but does not, because no extension method matches that shape. `Or` never proves an element exists. `Exactly<N>` inside an `And` is not matched either.

:::

## Immutability

A constrained list derives from `NonEmptyList<T>`, so it has the full read API: `Head`, `Last`, indexers, list patterns, the type-preserving LINQ in [LINQ](./linq.md) and generic math. You can pass it to any method that takes a `NonEmptyList<T>`, `IReadOnlyList<T>` or `IEnumerable<T>`.

It cannot change. Every mutating member throws `NotSupportedException`, including `Add`, `AddRange`, `AddFirst`, `Insert`, `InsertRange`, `Remove`, `RemoveAt`, `RemoveRange`, `RemoveAll`, `Clear`, the indexer setter, `SetRange`, `Sort` and the in-place `Reverse()`, and the calls through `IList` and `IList<T>`. `ICollection<T>.IsReadOnly` and `IList.IsReadOnly` report `true`.

To change one, use the `With` methods. Each returns a new constrained list with the constraint checked again; the original is unchanged.

| Method | Returns | Throws |
|--------|---------|--------|
| `WithAppended(item)` | the list with `item` at the end | `ArgumentException` on violation, `ArgumentNullException` for `null` |
| `WithPrepended(item)` | the list with `item` at the start | same |
| `WithItem(index, item)` | the list with one element replaced | same, and `ArgumentOutOfRangeException` for a bad index |
| `WithoutAt(index)` | the list without one element | `ArgumentException` when the result is empty or violates the constraint, `ArgumentOutOfRangeException` for a bad index |

```csharp
ConstrainedList<int, And<AtLeast<Two>, AtMost<Three>>> pair = [1, 2];

var three = pair.WithAppended(3);    // [1, 2, 3]
var four = three.WithAppended(4);    // ArgumentException: at most 3 elements
var one = pair.WithoutAt(0);         // ArgumentException: at least 2 elements
```

**Leaving the constraint.**

- `Relax()` returns a mutable `NonEmptyList<T>` copy without the constraint.
- `As<TOther>()` checks the same elements against another constraint and throws `ArgumentException` on violation. `TryAs<TOther>(out var other)` returns `false` instead.
- LINQ operators return ordinary non-empty results: `list.Select(f)` is a `NonEmptyList<TResult>`, without the constraint, because the new elements may not satisfy it.

```csharp
ConstrainedList<int, Sorted> sorted = [1, 2, 2];

NonEmptyList<int> editable = sorted.Relax();
editable.Add(0);                                  // fine, the copy has no constraint

ConstrainedList<int, AtLeast<Two>> big = sorted.As<AtLeast<Two>>();
bool unique = sorted.TryAs<Distinct>(out _);      // false: 2 appears twice
```

## JSON

System.Text.Json support is built in. A constrained list is written as a JSON array, and reading checks the constraint: an array that is empty, contains `null` or violates the constraint throws `JsonException`. A JSON `null` reads as a `null` reference. This works for properties of your DTOs too.

```csharp
using System.Text.Json;

// Waypoint is your own record with Lat and Lng properties
public sealed record RouteDto(ConstrainedList<Waypoint, AtLeast<Two>> Waypoints);

// Throws JsonException: expected at least 2 elements
JsonSerializer.Deserialize<RouteDto>("""{ "Waypoints": [ { "Lat": 0, "Lng": 0 } ] }""");
```

## Writing your own constraint

A constraint is a type that implements `IListConstraint`, an interface with two static abstract members:

```csharp
public interface IListConstraint
{
    static abstract bool Holds<T>(ReadOnlySpan<T> items);
    static abstract string Describe();
}
```

- `Holds` receives the elements, already checked to be non-empty and free of `null`. Return `true` if they satisfy your rule.
- `Describe` returns the short text used in `Description`, exception messages and `Validate` errors. Write it so it reads after "expected": "at least 2 elements", "unique SKUs".
- `Holds` is generic over every element type, so you cannot add a generic constraint such as `where T : INumber<T>`. Test the type at run time instead.
- The built-in constraints are `readonly struct` types. A struct with no fields is the natural choice.

```csharp title="NoBlankStrings.cs"
public readonly struct NoBlankStrings : IListConstraint
{
    public static bool Holds<T>(ReadOnlySpan<T> items)
    {
        foreach (T item in items)
        {
            if (item is string text && string.IsNullOrWhiteSpace(text))
                return false;
        }

        return true;
    }

    public static string Describe() => "no blank strings";
}

// Compose it like any built-in constraint
ConstrainedList<string, And<AtLeast<Two>, NoBlankStrings>> names = ["Ada", "Grace"];
```

You can add a cardinality the same way. Implement `ICardinality` with a static `Value`, and add the markers when they are true, so `Second()` and `Third()` work for it:

```csharp title="Twelve.cs"
public readonly struct Twelve : ICardinality, IAtLeastTwo, IAtLeastThree
{
    public static int Value => 12;
}

// Month is your own type; months is an IEnumerable<Month>
ConstrainedList<Month, Exactly<Twelve>> year = ConstrainedList<Month, Exactly<Twelve>>.From(months);
```

:::warning

The markers are a promise. If you put `IAtLeastTwo` on a cardinality whose `Value` is 1, `Second()` compiles and then fails at run time with an index out of range.

:::
