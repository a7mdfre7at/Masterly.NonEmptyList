# Analyzers and source generator

The `Masterly.NonEmptyList` package ships Roslyn analyzers, code fixes and a source generator. They are active in every project that references the package; nothing to install.

Severity can be changed per rule in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.NEL003.severity = warning
dotnet_diagnostic.NEL005.severity = warning   # opt in to the unproven-sequence rule
```

## Rules

### NEL001: Empty collection expression assigned to a non-empty type

**Error.** `NonEmptyList<int> xs = [];` compiles because the collection builder accepts any span, but it always throws at run time. Applies to `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, `ConstrainedList<T, C>`, `NonEmptySet<T>` and arguments passed to parameters of those types.

```csharp
NonEmptyList<int> xs = [];        // NEL001
Take([]);                          // NEL001 when Take accepts a NonEmptyList<int>
NonEmptyList<int> ok = [1, 2];    // fine
```

### NEL002: Non-empty factory called with an empty argument

**Error.** A factory or constructor receives a sequence the analyzer can prove is always empty: `Array.Empty<T>()`, `Enumerable.Empty<T>()`, `ImmutableArray<T>.Empty`, `new T[0]`, `new T[] { }`, `new List<T>()`, `[]`, `""`.

```csharp
NonEmptyList.From(Array.Empty<int>());     // NEL002
new List<int>().ToNonEmptyList();          // NEL002
new NonEmptyList<int>(new List<int>());    // NEL002
NonEmptyList.From(source);                 // fine: not provably empty
```

### NEL003: Use the total member instead of a throwing LINQ operator

**Info, with code fix.** On a non-empty list, `First()`, `FirstOrDefault()`, `Last()`, `LastOrDefault()`, `Aggregate(f)`, `Count()` and `LongCount()` carry an empty-sequence exception path the type has already ruled out. The fix rewrites to `Head`, `Last`, `Reduce(f)` or `Count`.

```csharp
int first = xs.First();            // NEL003 -> xs.Head
int total = xs.Aggregate(Add);     // NEL003 -> xs.Reduce(Add)
```

### NEL004: Redundant emptiness check on a non-empty list

**Info, with code fix.** `xs.Any()`, `xs.Count == 0`, `xs.Count > 0`, `xs.Count >= 1`, `xs.Count < 1`, `0 < xs.Count`, `xs.DefaultIfEmpty()` and similar are decided by the type. The fix replaces the expression with `true` or `false`. If emptiness is really possible, widen the variable's type instead.

### NEL005: Sequence is not proven non-empty

**Warning, disabled by default, with code fix.** Treats `First()`, `Last()`, `Single()`, `Max()`, `Min()`, `Average()`, `Aggregate(f)`, `MaxBy()`, `MinBy()` and `ElementAt()` on an `IEnumerable<T>` as unproven unless the receiver is:

- a non-empty type (`NonEmptyList<T>` and friends, or anything deriving from them);
- a collection literal with elements (`new[] { 1 }`, `[1, 2]`, `new List<int> { 1 }`);
- `Enumerable.Range` or `Enumerable.Repeat` with a positive literal count;
- a `Prepend`, `Append` or `DefaultIfEmpty` result;
- an order-preserving operator (`Select`, `OrderBy`, `Reverse`, `Distinct`, `ToList`, `Concat`, `Union`, ...) applied to a proven receiver.

Overloads that cannot throw are skipped: `Aggregate` with a seed, and `Max`/`Min`/`MaxBy`/`MinBy`/`Average` whose result type is a reference type or `Nullable<T>`.

The fix wraps the receiver: `xs.Max()` becomes `xs.ToNonEmptyList().Max()`, moving the check to the one place it belongs and adding the `using`. Enable the rule, then drive the count to zero the way nullable warnings were driven to zero.

### Migration fix for CS1503, CS0029, CS0266

Version 3 stopped deriving `NonEmptyList<T>` from `List<T>`. Wherever the compiler reports a failed conversion from a non-empty list to `List<T>`, a code fix offers `.ToList()`. Use *Fix all in solution* to migrate a codebase in one step.

## Source generator: `[NonEmptyCollection]`

Turns a partial class into a domain-specific non-empty collection.

```csharp
[NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 500)]
public sealed partial class OrderLines;
```

Generated members:

| Member | Purpose |
|--------|---------|
| `: NonEmptyList<OrderLine>` | full non-empty API, type-preserving LINQ, generic math, list patterns |
| `OrderLines(first, params rest)`, `OrderLines(IEnumerable<T>)` | constructors that validate every invariant |
| `From`, `Of`, `Create`, `TryFrom`, `Satisfies`, `Validate` | factories; `Validate` returns `Validation<OrderLines, ValidationError>` with one error per violated rule |
| `MinCount`, `MaxCount`, `IsDistinct`, `IsImmutable`, `Description` | metadata, also read by the OpenAPI transformer |
| `ValidateItem`, `ValidateRange`, `ValidateReplacement`, `ValidateCount`, `EnsureMutable` overrides | invariants enforced on every mutation, not only at construction |
| `[CollectionBuilder]` | `OrderLines lines = [a, b];` |
| nested `JsonConverter` + `[JsonConverter]` | System.Text.Json round trip with every invariant re-checked |
| `[DebuggerDisplay]`, `ToString()` | diagnostics |

Attribute properties: `Distinct` (reject duplicates), `MinCount` (default 1), `MaxCount` (0 means unbounded), `Immutable` (reject every mutation; use for value objects).

Generator diagnostics: NEL101 the class must be `partial`; NEL102 the class must not be nested; NEL103 invalid `MinCount`/`MaxCount`; NEL104 the class must not declare another base type.

The other packages recognize generated classes automatically: Newtonsoft.Json, MessagePack, Dapper, Configuration binding, AutoFixture and the OpenAPI transformer all go through the generated `From` factory.
