# Masterly.NonEmptyList

Proof-carrying collections for .NET. `NonEmptyList<T>` is a list that the type system guarantees has at least one element, so `Head`, `Last`, `Max`, `Average` and `Reduce` can never fail for lack of data. Version 3 turns that guarantee into something the compiler and the whole platform understand: collection expressions, list patterns, type-preserving LINQ, generic math, a constraint algebra, a Roslyn analyzer and source generator, and integration packages for ASP.NET Core, configuration, EF Core, Newtonsoft.Json, MessagePack, Dapper and testing.

<img src="https://raw.githubusercontent.com/a7mdfre7at/Masterly.NonEmptyList/master/repo_image.png" width="200" height="180">

[![Nuget](https://img.shields.io/nuget/v/Masterly.NonEmptyList?style=flat-square)](https://www.nuget.org/packages/Masterly.NonEmptyList) ![Nuget](https://img.shields.io/nuget/dt/Masterly.NonEmptyList?style=flat-square) ![GitHub last commit](https://img.shields.io/github/last-commit/a7mdfre7at/Masterly.NonEmptyList?style=flat-square) ![GitHub](https://img.shields.io/github/license/a7mdfre7at/Masterly.NonEmptyList) [![Build](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/build.yml) [![CodeQL Analysis](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/codeql.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/codeql.yml) [![Publish to NuGet](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/publish.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/publish.yml)

## Give a Star! :star:

If you like or are using this project please give it a star. Thanks!

## Installation

```bash
dotnet add package Masterly.NonEmptyList
```

Targets .NET 8, .NET 9 and .NET 10. The analyzer and source generator are bundled; nothing else to install.

| Package | Adds |
|---------|------|
| `Masterly.NonEmptyList.EntityFrameworkCore` | value converters, relationships, SaveChanges validation, query translation |
| `Masterly.NonEmptyList.AspNetCore` | OpenAPI `minItems`/`minLength`, `Validation` to problem details |
| `Masterly.NonEmptyList.Configuration` | options binding that fails at startup when a required collection is empty |
| `Masterly.NonEmptyList.NewtonsoftJson` | Json.NET converter |
| `Masterly.NonEmptyList.MessagePack` | MessagePack formatters and resolver |
| `Masterly.NonEmptyList.Dapper` | JSON type handlers |
| `Masterly.NonEmptyList.Testing` | CsCheck generators, AutoFixture customization, law suite |

## Sixty-second tour

```csharp
using Masterly.NonEmptyList;

// Collection expressions and inference
NonEmptyList<int> numbers = [3, 1, 2];
var names = NonEmptyList.Of("Ada", "Grace", "Linus");

// Head, Last, Tail never throw
int first = numbers.Head;
int last  = numbers.Last;

// List patterns and slicing
string shape = numbers switch
{
    [var only]              => $"just {only}",
    [var head, .. var rest] => $"{head} then {rest.Count} more",
};
List<int> tail = numbers[1..];

// LINQ stays inside the type when the result is provably non-empty
NonEmptyList<string> sorted =
    from n in names
    orderby n.Length, n
    select n.ToUpperInvariant();

NonEmptyList<int> doubled = numbers.Select(n => n * 2);   // not IEnumerable<int>
IEnumerable<int> big = numbers.Where(n => n > 10);        // Where may be empty, so it is not promoted

// Aggregations with no empty-sequence exception path
int    max     = numbers.Max();        // int, not int?
double average = numbers.Average();
int    sum     = numbers.Sum();        // checked, no seed needed
double median  = numbers.Median();

// Cardinality in the type
ConstrainedList<Waypoint, AtLeast<Two>> route = ConstrainedList<Waypoint, AtLeast<Two>>.From(points);
Waypoint start  = route.Head;
Waypoint second = route.Second();      // exists only because AtLeast<Two> is in the type

// Bind straight from a query string: GET /products?ids=1,2,3
app.MapGet("/products", (NonEmptyList<int> ids) => repo.GetByIds(ids));
// GET /products?ids=   -> 400 before your handler runs

// The analyzer catches what the compiler cannot
NonEmptyList<int> oops = [];           // NEL001 error: always throws
var top = orders.Max(o => o.Total);    // NEL005 (opt in): 'orders' is not proven non-empty
```

## Why a non-empty list

Half of the methods on `IEnumerable<T>` throw `InvalidOperationException: Sequence contains no elements`. Every one of them is total on a `NonEmptyList<T>`. Encoding "at least one" in the type removes a whole class of defensive checks, and makes domain rules such as "an order has at least one line" or "a route has at least one waypoint" visible in signatures instead of comments.

## Feature guide

### The guarantee is unbreakable

`NonEmptyList<T>` wraps its storage rather than inheriting `List<T>`. Removing the last element through any interface, `IList<T>`, `ICollection<T>` or the non-generic `IList`, throws instead of emptying the list. It still exposes the full `List<T>` surface: `Add`, `Insert`, `Sort`, `Find`, `BinarySearch`, `GetRange`, `Capacity` and friends.

```csharp
NonEmptyList<int> xs = [1];
IList<int> asInterface = xs;
asInterface.Clear();        // NotSupportedException
asInterface.RemoveAt(0);    // InvalidOperationException
```

Derived types can add invariants through protected hooks (`ValidateItem`, `ValidateRange`, `ValidateReplacement`, `ValidateCount`, `EnsureMutable`) without reimplementing the list. The source generator uses exactly these.

### Type-preserving LINQ

Operators that cannot produce an empty result return `NonEmptyList<T>`: `Select`, `SelectMany` (with a non-empty selector), `OrderBy`/`ThenBy`, `Order`, `GroupBy`, `Distinct`, `Union`, `Concat`, `Chunk`, `Max`, `Min`. Query syntax works, including `orderby a, b descending` and `group x by k into g`. Operators that may be empty (`Where`, `Skip`, `Take`, `OfType`) fall back to `IEnumerable<T>` so the type never lies.

```csharp
NonEmptyList<(string Country, int Count)> perCountry =
    from c in customers
    group c by c.Country into g
    select (g.Key, g.Count);          // each group is itself a NonEmptyList
```

### Generic math

Works for any type implementing the `System.Numerics` interfaces, including your own.

```csharp
Money total   = invoice.Lines.Select(l => l.Amount).Sum();   // Money has +, no Money.Zero needed
Vector3 mean  = points.Average();
decimal med   = prices.Median();
string common = tags.Mode();
(int lo, int hi) = numbers.MinMax();
```

### Constraint algebra

"At least one" is the most common cardinality invariant, but "at least two", "distinct", "sorted" and "bounded" are the same problem. `ConstrainedList<T, TConstraint>` solves the family: constraints are types, checked once at construction, and composable.

```csharp
ConstrainedList<Email,    And<AtLeast<Two>, Distinct>> recipients = [a, b, c];
ConstrainedList<DateOnly, Sorted>                      schedule   = ConstrainedList<DateOnly, Sorted>.From(dates);
ConstrainedList<Candidate, AtLeast<Two>>               finalists  = ranked.Constrain<Candidate, AtLeast<Two>>();

Candidate winner   = finalists.Head;
Candidate runnerUp = finalists.Second();                 // compiles only for AtLeast<Two> and up

Validation<ConstrainedList<int, AtLeast<Two>>, ValidationError> checked =
    ConstrainedList<int, AtLeast<Two>>.Validate(input, path: "ids");   // "Expected at least 2 elements"
```

Constraints: `NonEmpty`, `AtLeast<N>`, `AtMost<N>`, `Exactly<N>`, `Distinct`, `Sorted`, `SortedDescending`, `And<A, B>`, `Or<A, B>`; cardinalities `One` to `Ten`, `Sixteen`, `ThirtyTwo`, `SixtyFour`, `Hundred`, `Thousand`. Constrained lists are immutable; `WithAppended`, `WithItem`, `WithoutAt` return re-checked copies and `Relax()` gives a mutable `NonEmptyList<T>`.

### Validation that accumulates errors

`Validation<T, TError>` is either a value or a `NonEmptyList<TError>`. An invalid result with zero errors cannot exist.

```csharp
Validation<User, ValidationError> user = Validation.Combine(
    Validation.NotBlank(input.Name,  new ValidationError("required", "Name is required", "name")),
    Validation.NotBlank(input.Email, new ValidationError("required", "Email is required", "email"))
              .Ensure(e => e.Contains('@'), new ValidationError("format", "Email must contain @", "email")),
    Validation.Ensure(input.Age, a => a >= 18, new ValidationError("range", "Must be an adult", "age")),
    (name, email, age) => new User(name, email, age));

return user.ToResult();   // Masterly.NonEmptyList.AspNetCore: 200 with the user, or 400 problem details
```

`Sequence` and `Traverse` validate whole collections and report every failure at once.

### Sibling types

| Type | Guarantee |
|------|-----------|
| `ImmutableNonEmptyList<T>` | same API, returns new instances |
| `NonEmptySet<T>` | a `HashSet<T>` that cannot become empty; `IntersectWith` a disjoint set throws |
| `NonEmptyDictionary<TKey, TValue>` | a `Dictionary` with at least one entry |
| `NonEmptyString` | never null, empty or whitespace; implicit to `string`; binds from query strings, config and JSON |
| `NonEmptySpan<T>` | stack-only view with `Head`, `Max`, `Sum`, no allocation |
| `NonEmptyEnumerable<T>` | lazy, possibly infinite, head already known: `NonEmptyList.Iterate(1, x => x * 2).Take(10)` |

### Modeling types

```csharp
// A cursor that always points at something: wizards, carousels, playlists
NonEmptyZipper<Step> wizard = steps.ToZipper();
wizard = wizard.MoveRight() ?? wizard;        // null at the end, never an exception
Step current = wizard.Focus;

// Undo/redo that never loses the current state
History<Document> history = History.Start(doc).Push(edited).Undo();

// Event sourcing: the creation event is always there
EventStream<OrderEvent> stream = EventStream.Start<OrderEvent>(new OrderCreated(id))
                                            .Append(new LineAdded(sku, 2));
Order order = stream.Replay(Order.Create, (state, e) => state.Apply(e));

// Rose trees with a guaranteed root
NonEmptyTree<Department> org = NonEmptyTree.Create(ceo, divisions);
int depth = org.Fold<int>((_, childDepths) => 1 + (childDepths.Count == 0 ? 0 : childDepths.Max()));
NonEmptyList<NonEmptyList<Department>> paths = org.PathsToLeaves();
```

### Non-empty batching for streams and channels

Every batch consumer starts with `if (batch.Count == 0) continue;`. These operators delete that line.

```csharp
await foreach (NonEmptyList<Event> batch in events.BatchNonEmpty(size: 100, maxWait: TimeSpan.FromSeconds(1), ct))
    await bulkWriter.WriteAsync(batch);          // batch.Head is always valid

await foreach (NonEmptyList<Job> batch in channel.Reader.ReadAllBatchesAsync(maxSize: 50, ct))
    await Process(batch);

NonEmptyAsyncEnumerable<Row>? rows = await reader.ReadRowsAsync().ToNonEmptyAsync(ct);
if (rows is null) return Results.NoContent();
Row first = rows.Head;                          // already in memory
await foreach (Row row in rows.Rest) ...         // the rest streams lazily
```

### Analyzer and source generator

Bundled in the main package. Full reference in [docs/analyzers.md](docs/analyzers.md).

| Rule | Catches | Fix |
|------|---------|-----|
| NEL001 (error) | `NonEmptyList<int> xs = [];` | |
| NEL002 (error) | `NonEmptyList.From(Array.Empty<int>())`, `new List<int>().ToNonEmptyList()` | |
| NEL003 (info) | `xs.First()`, `xs.Last()`, `xs.Aggregate(f)`, `xs.Count()` on a non-empty list | `Head`, `Last`, `Reduce(f)`, `Count` |
| NEL004 (info) | `xs.Any()`, `xs.Count == 0`, `xs.Count > 0` on a non-empty list | `true` / `false` |
| NEL005 (opt in) | `First()`, `Max()`, `Single()`, `Average()`, ... on an `IEnumerable<T>` not proven non-empty | `xs.ToNonEmptyList().Max()` |
| CS1503 / CS0029 | v3 migration: non-empty list passed where `List<T>` is required | `.ToList()` |

One attribute on a partial class generates a domain collection with constructors, factories, `Validate`, collection expressions, a JSON converter and invariants enforced on every mutation:

```csharp
[NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 500)]
public sealed partial class OrderLines;

OrderLines lines = [line1, line2];                     // throws on duplicates or more than 500
Order order = new(customer, OrderLines.From(input));   // throws or returns, never empty
Validation<OrderLines, ValidationError> checked = OrderLines.Validate(input, "lines");
```

### Framework integration

- **ASP.NET Core**: `NonEmptyList<T>`, `NonEmptySet<T>` and `NonEmptyString` bind from query strings and route values with no extra package; an empty value is a 400. `Masterly.NonEmptyList.AspNetCore` adds `AddOpenApi(o => o.AddNonEmptySchemas())` and `validation.ToResult()`.
- **Configuration**: `services.AddNonEmptyOptions<SmtpOptions>("Smtp")` binds array or comma-separated values and refuses to start the host when a required collection is empty, naming the key.
- **EF Core**: store lists as JSON columns, configure non-empty relationships, validate on `SaveChanges`, and use NonEmptyList LINQ inside projections. See below.
- **Serialization**: System.Text.Json is built in; Newtonsoft.Json and MessagePack are separate packages; Dapper type handlers store collections as JSON text.
- **Testing**: `NonEmptyGen.List(Gen.Int).Sample(xs => NonEmptyListLaws.CheckAll(xs).Count == 0)`.

## EF Core integration

```csharp
services.AddDbContext<ShopContext>(options =>
    options.UseSqlServer(connectionString)
           .UseNonEmptyList());        // SaveChanges validation + query translation

modelBuilder.Entity<Product>()
    .Property(p => p.Tags)
    .HasNonEmptyListConversion();        // JSON column

modelBuilder.Entity<Order>()
    .HasNonEmptyManyWithOne(o => o.Items, i => i.Order);

var summaries = await context.Orders
    .Select(o => new { o.Id, Total = o.Items.Sum(i => i.Price), First = o.Items.Head.Sku })
    .ToListAsync();                      // translated like List<T>
```

`UseNonEmptyList()` registers a query interceptor that rewrites NonEmptyList operators back to `Enumerable` before EF compiles the query. Without it, NonEmptyList LINQ inside a projection fails to translate.

## Benchmarks

`benchmarks/Masterly.NonEmptyList.Benchmarks` compares creation, access, LINQ and mutation against `List<T>`, `ImmutableList<T>` and `ImmutableArray<T>` with BenchmarkDotNet:

```bash
dotnet run -c Release --project benchmarks/Masterly.NonEmptyList.Benchmarks -- --filter '*'
```

## Migrating from 2.x

- `NonEmptyList<T>` no longer derives from `List<T>`. Code that passed it where a `List<T>` was required should accept `IList<T>` or `IReadOnlyList<T>` instead, or call `.ToList()`. The bundled code fix offers this at every affected site.
- `Prepend(item)` now returns a new list, matching LINQ. Use `AddFirst(item)` to insert in place.
- `Remove(item)` returns `bool`; `AsReadOnly()` returns `ReadOnlyCollection<T>`.
- `new NonEmptyList<string>(null)` is ambiguous now that an `IEnumerable<T>` constructor exists; cast the argument.
- .NET 6 is no longer targeted.
- If you use the EF Core package, add `.UseNonEmptyList()` to your options.

## Documentation

- [Analyzer rules and the source generator](docs/analyzers.md)
- [Changelog](CHANGELOG.md)
- [llms.txt](llms.txt) for AI assistants
- The **[Wiki](../../wiki)**

## License

MIT

**Free Software, Hell Yeah!**
