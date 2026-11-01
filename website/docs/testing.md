---
id: testing
title: "Testing"
description: "CsCheck generators that never produce empty values, an AutoFixture customization, and the NonEmptyListLaws suite."
---

`Masterly.NonEmptyList.Testing` helps you test code that takes non-empty types. It has three parts: [CsCheck](https://github.com/AnthonyLloyd/CsCheck) generators for property-based tests, an [AutoFixture](https://github.com/AutoFixture/AutoFixture) customization for example-based tests, and `NonEmptyListLaws`, the laws every `NonEmptyList<T>` obeys.

```bash
dotnet add package Masterly.NonEmptyList.Testing
```

```csharp
using CsCheck;
using Masterly.NonEmptyList;
using Masterly.NonEmptyList.Testing;
```

The package references CsCheck 4.9.1 and AutoFixture 4.18.1, so you do not need to add them yourself. The examples on this page use xUnit, but nothing in the package depends on a test framework.

## CsCheck generators

`NonEmptyGen` produces CsCheck `Gen<T>` instances. Every generator yields at least one element, so your properties never need an "if empty" guard.

```csharp title="OrderTotalTests.cs"
public class OrderTotalTests
{
    [Fact]
    public void Max_IsNeverBelowAnyElement()
    {
        NonEmptyGen.List(Gen.Int[-1000, 1000]).Sample(xs => xs.All(x => x <= xs.Max()));
    }

    [Fact]
    public void Sorted_HeadIsTheSmallest()
    {
        NonEmptyGen.Sorted(Gen.Int).Sample(xs => xs.Head <= xs.Last);
    }

    [Fact]
    public void Pairs_AlwaysHaveASecondElement()
    {
        NonEmptyGen.Constrained<int, AtLeast<Two>>(Gen.Int, minCount: 2).Sample(xs => xs.Second() == xs[1]);
    }
}
```

`Sample` runs the property many times with random input and throws when it returns `false`, reporting the smallest failing input it can find. Combine generators with CsCheck's own tools, for example `Gen.Select(NonEmptyGen.List(Gen.Int), NonEmptyGen.String())`.

### Every generator

All counts and lengths have a floor of 1: a value below 1 is raised to 1.

| Generator | Produces | Parameters |
|-----------|----------|------------|
| `List<T>(Gen<T> element, int maxCount = 20)` | `NonEmptyList<T>` with 1 to `maxCount` elements | |
| `Immutable<T>(Gen<T> element, int maxCount = 20)` | `ImmutableNonEmptyList<T>` with 1 to `maxCount` elements | |
| `Set<T>(Gen<T> element, int maxCount = 20)` | `NonEmptySet<T>` | Duplicates collapse, so the count is between 1 and `maxCount`. |
| `Dictionary<TKey, TValue>(Gen<TKey> key, Gen<TValue> value, int maxCount = 20)` | `NonEmptyDictionary<TKey, TValue>` | Duplicate keys collapse to the last value. |
| `Constrained<T, TConstraint>(Gen<T> element, int minCount = 1, int maxCount = 20)` | `ConstrainedList<T, TConstraint>` | Filters candidate lists with the constraint. Set `minCount` to the constraint's minimum, and use an element generator whose lists usually satisfy it, or generation gets slow. |
| `Sorted<T>(Gen<T> element, int maxCount = 20)` | `ConstrainedList<T, Sorted>` | Sorts the candidate list, so nothing is filtered. |
| `Distinct<T>(Gen<T> element, int maxCount = 20)` | `ConstrainedList<T, Distinct>` | De-duplicates the candidate list. |
| `String(int maxLength = 20)` | `NonEmptyString` of 1 to `maxLength` characters | Always has at least one non-whitespace character. |
| `String(string alphabet, int maxLength = 20)` | `NonEmptyString` built only from `alphabet` | Same rule. |
| `EventStream<TEvent>(Gen<TEvent> @event, int maxCount = 20)` | `EventStream<TEvent>` with 1 to `maxCount` events | |
| `Zipper<T>(Gen<T> element, int maxCount = 20)` | `NonEmptyZipper<T>` | The cursor is at a random position. |
| `Tree<T>(Gen<T> value, int maxDepth = 3, int maxChildren = 3)` | `NonEmptyTree<T>` | Each node has 0 to `maxChildren` children, down to `maxDepth` levels. |
| `Validation<T, TError>(Gen<T> value, Gen<TError> error, int maxErrors = 5)` | `Validation<T, TError>` | Valid or invalid with roughly equal probability. An invalid one has 1 to `maxErrors` errors. |

Every generator throws `ArgumentNullException` for a `null` generator or alphabet.

```csharp
NonEmptyGen.Set(Gen.Int[0, 3]).Sample(set => set.Count is >= 1 and <= 4);
NonEmptyGen.String("abc").Sample(s => s.Value.All(c => c is 'a' or 'b' or 'c'));
NonEmptyGen.Zipper(Gen.Int).Sample(z => z.Index >= 0 && z.Index < z.Count);
NonEmptyGen.Validation(Gen.Int, Gen.String).Sample(v => v.IsValid || v.Errors.Count >= 1);
```

## AutoFixture

`NonEmptyCustomization` teaches AutoFixture to create the non-empty types, including when they appear as properties or constructor parameters of your own types.

```csharp title="InvoiceTests.cs"
// Invoice is your own record:
// record Invoice(Guid Id, NonEmptyList<string> Lines, NonEmptyString Customer, NonEmptySet<int> Codes);
public class InvoiceTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new NonEmptyCustomization());

    [Fact]
    public void Fixture_FillsEveryNonEmptyMember()
    {
        Invoice invoice = _fixture.Create<Invoice>();

        Assert.Equal(3, invoice.Lines.Count);
        Assert.True(invoice.Customer.HasValue);
    }
}
```

It handles `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, `NonEmptySet<T>`, `NonEmptyDictionary<TKey, TValue>`, `ConstrainedList<T, TConstraint>`, `NonEmptyString`, nullable versions of these, and classes derived from them that expose a static `From` factory, such as `[NonEmptyCollection]` classes.

- Lists get the fixture's `RepeatCount` elements by default, which is 3 unless you change it. Set `Count` to override it: `new NonEmptyCustomization { Count = 1 }`. Sets and dictionaries can end up smaller, because duplicate values or keys collapse.
- When a constraint or a generated class rejects the elements (for example `Distinct`, `Sorted` or a maximum count), the last element is dropped and creation is retried, so `ConstrainedList<int, AtMost<Two>>` comes out with one or two elements.
- A nullable property such as `NonEmptyList<int>?` is filled in, not left `null`.
- A `NonEmptyString` uses the string AutoFixture creates, or a new GUID string if that one is blank.

The customization inserts a `NonEmptySpecimenBuilder` at the front of `fixture.Customizations`. You can add that builder yourself: `fixture.Customizations.Add(new NonEmptySpecimenBuilder(count: 5))`. For any other request it returns AutoFixture's `NoSpecimen`, so the rest of the fixture works as before.

## Laws

`NonEmptyListLaws` states the laws of `NonEmptyList<T>` as plain functions that return `bool`: `true` means the law holds for the given input. Use them with CsCheck, another property-testing library or a simple loop, to check a fork of the library or a type you derived from `NonEmptyList<T>`.

```csharp title="LawTests.cs"
public class LawTests
{
    private static readonly Gen<NonEmptyList<int>> Ints = NonEmptyGen.List(Gen.Int[-100, 100]);

    [Fact]
    public void AllParameterlessLawsHold()
    {
        Ints.Sample(xs => NonEmptyListLaws.CheckAll(xs).Count == 0);
    }

    [Fact]
    public void MapComposes()
    {
        Ints.Sample(xs => NonEmptyListLaws.MapComposition(xs, x => x + 1, x => x * 2));
    }
}
```

### CheckAll

`CheckAll<T>(NonEmptyList<T> xs)` runs every law that needs nothing but the list itself, and returns an `IReadOnlyList<string>` with the names of the laws that failed. An empty list means they all hold. It throws `ArgumentNullException` for a `null` list.

It runs these 16 laws: `MapIdentity`, `FlatMapRightIdentity`, `HeadTailRoundTrip`, `InitLastRoundTrip`, `ReverseTwiceIsIdentity`, `TailsShape`, `InitsShape`, `DistinctKeepsFirstOccurrences`, `EqualityIsStructural`, `ConcatPreservesElements` and `ZipUnzipRoundTrip` (both with the list paired with itself), `SelectMatchesMap` (with the identity function), `ReduceMatchesAggregate`, `FoldMatchesAggregate`, `ScanLastEqualsFold` (with fixed functions) and `JsonRoundTrip`.

To report which law failed, assert on the result instead of the count:

```csharp
Ints.Sample(xs => Assert.Empty(NonEmptyListLaws.CheckAll(xs)));
```

### Individual laws

| Law | Holds when |
|-----|------------|
| `MapIdentity(xs)` | `xs.Map(x => x)` equals `xs`. |
| `MapComposition(xs, f, g)` | Mapping `f` and then `g` equals mapping their composition. |
| `FlatMapLeftIdentity(x, f)` | `Singleton(x).FlatMap(f)` equals `f(x)`. |
| `FlatMapRightIdentity(xs)` | `xs.FlatMap(Singleton)` equals `xs`. |
| `FlatMapAssociativity(xs, f, g)` | Flat-mapping `f` then `g` equals flat-mapping `x => f(x).FlatMap(g)`. |
| `SelectMatchesMap(xs, f)` | `Select` and `Map` give the same list. |
| `ReduceMatchesAggregate(xs, op)` | `Reduce` agrees with LINQ `Aggregate`. |
| `FoldMatchesAggregate(xs, seed, folder)` | `Fold` agrees with seeded `Aggregate`. |
| `ScanLastEqualsFold(xs, seed, folder)` | The last element of `Scan` equals `Fold` with the same seed. |
| `HeadTailRoundTrip(xs)` | `Head` in front of `Tail` rebuilds the list. |
| `InitLastRoundTrip(xs)` | `Init` followed by `Last` rebuilds the list. |
| `ReverseTwiceIsIdentity(xs)` | Reversing twice gives the original list. |
| `ConcatPreservesElements(xs, ys)` | `Concat` keeps every element, the counts and the order. |
| `ZipUnzipRoundTrip(xs, ys)` | `Zip` then `Unzip` gives both lists back, cut to the shorter length. |
| `TailsShape(xs)` | `Tails()` has `Count` entries, from the whole list down to the singleton of `Last`. |
| `InitsShape(xs)` | `Inits()` has `Count` entries, from the singleton of `Head` up to the whole list. |
| `OrderByIsSortedPermutation(xs)` | `Order()` keeps every element and is sorted. Not in `CheckAll`, because it needs comparable elements. |
| `DistinctKeepsFirstOccurrences(xs)` | `Distinct()` keeps first occurrences in order. |
| `EqualityIsStructural(xs)` | A copy is equal and has the same hash code. |
| `JsonRoundTrip(xs)` | System.Text.Json serialization and back gives an equal list. |

```csharp
Gen.Int.Sample(x => NonEmptyListLaws.FlatMapLeftIdentity(x, v => NonEmptyList.Of(v, v)));
Gen.Select(Ints, Ints).Sample(t => NonEmptyListLaws.ConcatPreservesElements(t.Item1, t.Item2));
Ints.Sample(xs => NonEmptyListLaws.FoldMatchesAggregate(xs, "", (acc, x) => acc + x));
```
