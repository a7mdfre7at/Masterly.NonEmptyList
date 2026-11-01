# Masterly.NonEmptyList.Testing

Test support for [Masterly.NonEmptyList](https://www.nuget.org/packages/Masterly.NonEmptyList): CsCheck generators, an AutoFixture customization and the published law suite.

## Installation

```bash
dotnet add package Masterly.NonEmptyList.Testing
```

## CsCheck generators

Every generator yields at least one element, so properties never need an empty guard.

```csharp
using CsCheck;
using Masterly.NonEmptyList.Testing;

NonEmptyGen.List(Gen.Int).Sample(xs => xs.Map(x => x) == xs);
NonEmptyGen.Sorted(Gen.Int).Sample(xs => xs.Head <= xs.Last);
NonEmptyGen.Constrained<int, AtLeast<Two>>(Gen.Int, minCount: 2).Sample(xs => xs.Second() == xs[1]);
NonEmptyGen.String().Sample(s => !string.IsNullOrWhiteSpace(s.Value));
```

Available: `List`, `Immutable`, `Set`, `Dictionary`, `Constrained`, `Sorted`, `Distinct`, `String`, `EventStream`, `Zipper`, `Tree`, `Validation`.

## AutoFixture

```csharp
IFixture fixture = new Fixture().Customize(new NonEmptyCustomization());

Order order = fixture.Create<Order>();   // NonEmptyList, NonEmptySet, NonEmptyString, ConstrainedList properties all populated
```

Generated `[NonEmptyCollection]` classes resolve too. Constrained types are shrunk until their constraint holds.

## Laws

`NonEmptyListLaws` exposes the functor, monad, fold and structural laws as boolean functions, plus `CheckAll(xs)` which returns the names of any laws that fail. Use them to verify your own extension methods or forks:

```csharp
NonEmptyGen.List(Gen.Int).Sample(xs => NonEmptyListLaws.CheckAll(xs).Count == 0);
NonEmptyGen.List(Gen.Int).Sample(xs => NonEmptyListLaws.MapComposition(xs, x => x + 1, x => x * 2));
```

## Documentation

Full guide: <https://a7mdfre7at.github.io/Masterly.NonEmptyList/docs/testing>.

## License

MIT
