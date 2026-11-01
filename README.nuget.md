# Masterly.NonEmptyList

Proof-carrying collections for .NET. `NonEmptyList<T>` is a list the type system guarantees has at least one element, so `Head`, `Last`, `Max`, `Average` and `Reduce` can never fail for lack of data.

## Give a Star on Github! :star:

If you like or are using this project please give it a star. Thanks!

## Highlights

- **Unbreakable guarantee**: the list wraps its storage, so no interface cast can empty it.
- **Collection expressions and list patterns**: `NonEmptyList<int> xs = [1, 2, 3];`, `xs is [var head, .. var rest]`, `xs[1..]`.
- **Type-preserving LINQ**: `Select`, `OrderBy`/`ThenBy`, `GroupBy`, `Distinct`, `Union`, `Concat`, `Chunk`, `Max`, `Min` keep the non-empty type. Query syntax supported.
- **Generic math**: `Sum`, `Product`, `Average`, `Median`, `Mode`, `MinMax` with no empty-sequence exception path, for built-in and user-defined numeric types.
- **Constraint algebra**: `ConstrainedList<T, AtLeast<Two>>`, `Distinct`, `Sorted`, `AtMost`, `Exactly`, `And`, `Or`. `Second()` and `Third()` compile only when the type proves them.
- **Sibling types**: `NonEmptySet<T>`, `NonEmptyDictionary<TKey, TValue>`, `NonEmptyString`, `NonEmptySpan<T>`, `ImmutableNonEmptyList<T>`, lazy `NonEmptyEnumerable<T>`.
- **Modeling types**: `NonEmptyZipper<T>`, `History<T>`, `EventStream<TEvent>`, `NonEmptyTree<T>`.
- **Validation&lt;T, TError&gt;**: error-accumulating validation whose error list is non-empty by construction.
- **Async batching**: `BatchNonEmpty(size, maxWait)`, `ReadAllBatchesAsync` for channels, `ToNonEmptyAsync` with the head in memory and the rest streaming.
- **Query-string binding**: `ISpanParsable<T>` plus a `TypeConverter`, so ASP.NET Core binds `NonEmptyList<T>`, `NonEmptySet<T>` and `NonEmptyString` directly and rejects empty input with a 400.
- **Bundled analyzer and source generator**: NEL001 to NEL005 with code fixes, a v3 migration fix, and `[NonEmptyCollection]` for domain collections.
- **Integration packages**: EF Core, ASP.NET Core (OpenAPI, problem details), Configuration, Newtonsoft.Json, MessagePack, Dapper, Testing (CsCheck, AutoFixture, laws).

## Installation

```
dotnet add package Masterly.NonEmptyList
```

## Usage

```csharp
using Masterly.NonEmptyList;

NonEmptyList<int> numbers = [3, 1, 2];

int first = numbers.Head;              // never throws
int max = numbers.Max();               // int, not int?
double average = numbers.Average();    // never divides by zero

NonEmptyList<string> sorted =
    from n in numbers
    orderby n
    select n.ToString();               // still a NonEmptyList

ConstrainedList<Waypoint, AtLeast<Two>> route = ConstrainedList<Waypoint, AtLeast<Two>>.From(points);
Waypoint second = route.Second();      // safe only because AtLeast<Two> is in the type

Validation<User, ValidationError> user = Validation.Combine(
    ValidateName(input.Name),
    ValidateEmail(input.Email),
    (name, email) => new User(name, email));   // all errors reported together

app.MapGet("/products", (NonEmptyList<int> ids) => repo.GetByIds(ids));   // ?ids=1,2,3

[NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 500)]
public sealed partial class OrderLines;                                      // generated domain collection
```

## Migrating from 2.x

`NonEmptyList<T>` no longer derives from `List<T>` (a bundled code fix offers `.ToList()` for CS1503, CS0029, CS0266 and CS1662; explicit casts need a manual change); `Prepend` returns a new list (use `AddFirst` to mutate); `Remove` returns `bool`; .NET 6 is no longer targeted. EF Core users should call `.UseNonEmptyList()` on their options.

## License

MIT

**Free Software, Hell Yeah!**
