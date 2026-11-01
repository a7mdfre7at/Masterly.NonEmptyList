# Changelog

## 3.0.0

### Breaking changes
- `NonEmptyList<T>` no longer inherits from `List<T>`. It wraps its storage and implements `IList<T>`, `IReadOnlyList<T>` and `IList`, so the non-empty guarantee now holds through every interface. In v2, `((IList<T>)list).Clear()` emptied the list.
- `Prepend(item)` returns a new list (LINQ semantics). Use `AddFirst(item)` to insert in place. A bare `list.Prepend(x);` statement still compiles but no longer changes `list`.
- `Remove(item)` returns `bool` like `List<T>`: `true` when the item was removed, `false` when it is absent. In 2.x it returned nothing, and removing an absent item from a one-element list threw.
- `AsReadOnly()` returns `ReadOnlyCollection<T>`.
- `TryValidate` annotates its out parameter with `[MaybeNullWhen(true)]`.
- LINQ operators that cannot produce an empty result (`Select`, `SelectMany`, `OrderBy`, `GroupBy`, `Distinct`, `Union`, `Chunk`, ...) now bind to `NonEmptyListLinqExtensions`, return `NonEmptyList<T>` and run immediately. In 2.x they were the deferred `System.Linq` operators.
- `Chunk(size)` yields `NonEmptyList<T>` chunks instead of `T[]`, so `foreach (int[] chunk in xs.Chunk(2))` no longer compiles.
- `ConvertAll` returns `NonEmptyList<TOutput>`.
- `new NonEmptyList<object>(someList)` now copies the elements of `someList` through the new `IEnumerable<T>` constructor. In 2.x it created a one-element list containing `someList`.
- Passing a non-empty list where a `List<T>` is required now needs `.ToList()`, which is a copy: a method that mutated the list it received no longer changes the caller's list.
- Dropped the .NET 6 target. Targets are .NET 8, .NET 9 and .NET 10.
- EF Core package: call `.UseNonEmptyList()` on your `DbContextOptionsBuilder` to translate NonEmptyList LINQ inside queries.
- EF Core package: SaveChanges validation now also checks types derived from `NonEmptyList<T>` (`ConstrainedList`, `[NonEmptyCollection]` classes), `NonEmptySet<T>` and `NonEmptyDictionary<TKey, TValue>`, allows `null` on properties declared nullable, and words its messages as "Non-empty property ...".

### Added
- Collection expressions for `NonEmptyList<T>` and `ImmutableNonEmptyList<T>`: `NonEmptyList<int> xs = [1, 2, 3];`. An empty expression throws `ArgumentException`.
- List patterns and range indexing: `xs is [var head, .. var rest]`, `xs[1..]`, `xs[^1]`, `Slice`.
- Non-generic `NonEmptyList` factories with type inference: `Of`, `Create`, `From`, `TryFrom`, `Singleton`, `Repeat`, `Range`, `Iterate`, `Unfold`, `UnfoldValue`. The non-generic `ImmutableNonEmptyList` class has `Of`, `Create`, `From` and `Singleton`.
- `params ReadOnlySpan<T>` constructors and `FromSpan` on `NonEmptyList<T>`.
- Type-preserving LINQ (`NonEmptyListLinqExtensions`): `Select`, `SelectMany`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Order`, `OrderDescending`, `GroupBy` (returns `NonEmptyGrouping<TKey, T>`), `Distinct`, `Union` (with either or both operands non-empty), `Concat`, `Chunk`, `Flatten`, `Unzip`, `Interleave`, `Cycle`, `Max`, `Min`, `MinMax`, `ToNonEmptyList`, `ToNonEmptyListOrNull`, `TryToNonEmptyList`. Query syntax is supported. `OrderedNonEmptyList<T>` implements `IOrderedEnumerable<T>`.
- Generic math (`NonEmptyListMathExtensions`): `Sum`, `Product`, `Average`, `Median`, `Mode`, with LINQ-compatible result types for integers.
- `ISpanParsable<NonEmptyList<T>>` and `NonEmptyListTypeConverter`: bind from query strings and route values in ASP.NET Core; comma-separated parsing with culture support for any `ISpanParsable`, `IParsable`, enum or `TypeConverter`-backed element type.
- `Validation<T, TError>` with `Combine` (2 to 8 inputs), `Sequence`, `Traverse`, `Map`, `MapErrors`, `Bind`, `Ensure`, `Match`, `NotNull`, `HasValue`, `NotBlank`, `NonEmpty`, plus the `ValidationError` record and `ToErrorDictionary()` for problem details.
- New operators: `Scan`, `Tails`, `Inits`, `Append`, `AddFirst`, `ConvertAll`, `Capacity`, `Find`, `FindAll`, `FindIndex`, `Exists`, `TrueForAll`, `BinarySearch`, `GetRange`, `Sort`, `Reverse` (in place), `TrimExcess`, `EnsureCapacity`.
- `[NotNullWhen(true)]` on `NonEmptyList<T>.TryFrom` and `ImmutableNonEmptyList<T>.TryFrom`, `DebuggerDisplay` and a debugger type proxy, `MinBy`/`MaxBy` comparer overloads.
- EF Core: `NonEmptyListQueryExpressionInterceptor`, `UseNonEmptyList()`, `UseNonEmptyListQueryTranslation()`. `Count`, `Head`, `First` and `Last` on NonEmptyList navigations translate to `Count()`, `First()` and `Last()`.
- Constraint algebra: `ConstrainedList<T, TConstraint>` with `NonEmpty`, `AtLeast<N>`, `AtMost<N>`, `Exactly<N>`, `Distinct`, `Sorted`, `SortedDescending`, `And<A, B>`, `Or<A, B>` and cardinalities `One` to `Ten`, `Sixteen`, `ThirtyTwo`, `SixtyFour`, `Hundred`, `Thousand`. `Second()` and `Third()` exist only when the constraint proves them. `Validate` produces a `Validation` with the failing rule. Collection expressions and System.Text.Json supported.
- `NonEmptySet<T>` (guarded `ISet<T>` with `Parse`/`TryParse`), `NonEmptyDictionary<TKey, TValue>` (guarded `IDictionary<TKey, TValue>`), `NonEmptyString` (never blank; implicit to `string`, `ISpanParsable`, `TypeConverter`, JSON property-name support), `NonEmptySpan<T>` (stack-only view with `Head`, `Max`, `Sum`).
- `NonEmptyZipper<T>` (cursor that always points at an element) and `History<T>` (undo/redo that never loses the current state).
- `EventStream<TEvent>` with a guaranteed origin: `Replay`, `ReplayHistory`, `Since`, `UpTo`, `Version`.
- `NonEmptyTree<T>` rose tree: `Flatten`, `FlattenPostOrder`, `FlattenBreadthFirst`, `Leaves`, `PathsToLeaves`, `Levels`, `Fold`, `Map`, `Find`, `PathTo`, `PruneChildren`, `Unfold`, `FromParentKeys`.
- `NonEmptyEnumerable<T>`: lazy, possibly infinite sequence with a known head. `NonEmptyList.Iterate(seed, next)` and `Cycle()` return it; `Take(n)`, `TakeWhile`, `TakeUntil` return a `NonEmptyList<T>`.
- Async batching: `BatchNonEmpty(size)` for sequences and async streams, `BatchNonEmpty(size, maxWait)` with a timer that only starts once an element is waiting, `ReadAllBatchesAsync` for channels, `ToNonEmptyAsync` returning `NonEmptyAsyncEnumerable<T>` (head in memory, rest streaming). Every operator checks its cancellation token between elements, so it stops even when the source ignores the token.
- `NonEmptyList(IEnumerable<T>)` and `ImmutableNonEmptyList(IEnumerable<T>)` constructors.
- Protected invariant hooks on `NonEmptyList<T>`: `IsReadOnly`, `EnsureMutable`, `ValidateItem`, `ValidateRange`, `ValidateReplacement`, `ValidateCount`, and a protected copy constructor, so derived types can add invariants without reimplementing the list.
- Roslyn analyzers bundled in the main package: NEL001 empty collection expression, NEL002 provably empty factory argument, NEL003 throwing LINQ on a non-empty list (fix to `Head`/`Last`/`Reduce`/`Count`), NEL004 redundant emptiness check (fix to constant), NEL005 opt-in unproven sequence (fix wraps in `ToNonEmptyList()`), plus a migration fix offering `.ToList()` for CS1503/CS0029/CS0266/CS1662. Explicit casts (CS0030) are not covered. See `docs/analyzers.md`.
- `[NonEmptyCollection]` source generator for domain collections: constructors, `From`/`Of`/`Create`/`TryFrom`/`Satisfies`/`Validate`, collection expressions, JSON converter, `DebuggerDisplay`, and `Distinct`/`MinCount`/`MaxCount`/`Immutable` enforced on every mutation.

### New packages
- `Masterly.NonEmptyList.Configuration`: `GetNonEmptyList` and friends on `IConfiguration`, `BindNonEmptyConfiguration` / `AddNonEmptyOptions` for options classes; required non-empty properties fail at startup naming the key. Every element and entry of a non-empty collection is converted strictly and never skipped (unlike Microsoft's binder, which drops values it cannot convert), including collections nested inside it; duplicate converted dictionary keys and values that convert to null throw. Objects with non-empty members are bound member by member with their required checks. Everything Microsoft's binder can create on its own, including the plain members of those objects, is still bound by it with its usual semantics.
- `Masterly.NonEmptyList.AspNetCore`: OpenAPI schema transformer (`minItems`, `maxItems`, `uniqueItems`, `minLength`, `minProperties`, array/items shape), `Validation` to `IResult`/`ActionResult`/problem details, `ModelState.AddErrors`, `NonEmptyShape`.
- `Masterly.NonEmptyList.NewtonsoftJson`: one converter for every non-empty type.
- `Masterly.NonEmptyList.MessagePack`: formatters and `NonEmptyResolver`.
- `Masterly.NonEmptyList.Dapper`: JSON type handlers and `NonEmptyString` handler.
- `Masterly.NonEmptyList.Testing`: CsCheck generators (`NonEmptyGen`), AutoFixture customization, `NonEmptyListLaws`.

### Changed
- `Tail`, `Init`, `Map`, `Reduce`, `Fold`, `Zip`, `Rotate*`, `Sliding`, `ChunkNonEmpty` no longer go through LINQ and allocate less.
- `MinBy` and `MaxBy` run in O(n) instead of sorting.
- `Random()` uses `Random.Shared`.
- `Tail` and `Init` are no longer cached: every read returns a new list built from the current elements, so changing the result never leaks into the original list or a later read. In 2.x, `Tail` could return stale elements after `Sort`.

## 2.0.0

## Added

### New Types
- `ImmutableNonEmptyList<T>` - An immutable variant that returns new instances on modification, with methods like `Append`, `Prepend`, `RemoveFirst`, `RemoveLast`, `SetItem`, and `Insert`

### Factory Methods
- `TryFrom` - Safe creation that doesn't throw, returns bool with out parameter
- `Singleton` - Creates a single-element list
- `Repeat` - Creates a list with repeated elements

### Properties
- `Init` - Returns all elements except the last (opposite of `Tail`)
- Indexer null validation - Prevents setting null values via indexer

### Functional Programming Operations
- `Map<TResult>` - Projects each element into a new form, preserving non-empty guarantee
- `MapWithIndex<TResult>` - Map with element index
- `FlatMap<TResult>` - Projects each element to a NonEmptyList and flattens
- `Reduce` - Combines all elements without requiring a seed
- `ReduceRight` - Reduce from right to left
- `Fold<TResult>` - Accumulates with a seed value
- `FoldRight<TResult>` - Fold from right to left
- `Zip<TOther>` - Merges two lists into tuples
- `ZipWith<TOther, TResult>` - Zip with a selector function
- `ZipWithIndex` - Pairs elements with their indices
- `Partition` - Splits list by predicate into (matching, notMatching)
- `GroupByNonEmpty<TKey>` - Groups elements with NonEmptyList values

### Pattern Matching
- `Match<TResult>` - Pattern match on single vs multiple elements
- `Match` (void) - Action-based pattern matching
- Three-element deconstruction - `(first, second, rest)`

### Collection Operations
- `Concat(NonEmptyList<T>)` - Concatenate two NonEmptyLists
- `Concat(IEnumerable<T>)` - Concatenate with enumerable
- `Prepend` - Add element at the beginning
- `ReverseList` - Returns a new reversed NonEmptyList
- `DistinctList` - Returns a new list with distinct elements
- `DistinctList(IEqualityComparer<T>)` - Distinct with comparer
- `DistinctBy<TKey>` - Distinct by key selector
- `TakeNonEmpty` - Take first n elements, returns null if count < 1
- `SkipNonEmpty` - Skip first n elements, returns null if nothing remains
- `TakeAtLeastOne` - Ensures at least one element
- `Sliding` - Returns sliding windows of specified size
- `ChunkNonEmpty` - Splits into chunks
- `Intersperse` - Inserts separator between elements
- `RotateLeft` - Rotates elements to the left
- `RotateRight` - Rotates elements to the right

### Validation & Safety
- `RemoveAll` override - Prevents emptying via predicate removal
- `SetRange` - Bulk update with null validation
- `Validate` - Throws if any element fails predicate
- `TryValidate` - Returns bool and failing element

### Conversion Methods
- `ToHashSet()` / `ToHashSet(IEqualityComparer<T>)`
- `ToDictionary<TKey>` / `ToDictionary<TKey, TValue>`
- `ToQueue()`
- `ToStack()`
- `ToLinkedList()`
- `AsReadOnly()` - Returns IReadOnlyList<T>
- `AsSpan()` - Returns ReadOnlySpan<T> (.NET 6+)
- `AsMemory()` - Returns ReadOnlyMemory<T> (.NET 6+)
- `ToImmutable()` - Extension method to convert to ImmutableNonEmptyList

### Utility Methods
- `Random()` / `Random(Random)` - Get random element (safe since list is never empty)
- `MinBy<TKey>` - Element with minimum key
- `MaxBy<TKey>` - Element with maximum key
- `ForEachWithIndex` - Iterate with indices

### Async Extensions (NonEmptyListAsyncExtensions)
- `MapAsync<TResult>` - Async map operation
- `MapWithIndexAsync<TResult>` - Async map with index
- `MapParallelAsync<TResult>` - Parallel async map
- `MapParallelAsync<TResult>(maxDegreeOfParallelism)` - Throttled parallel map
- `ForEachAsync` - Async foreach
- `ForEachWithIndexAsync` - Async foreach with index
- `ForEachParallelAsync` - Parallel async foreach
- `FilterAsync` - Async filter
- `FoldAsync<TResult>` - Async fold
- `AllAsync` - Async all check
- `AnyAsync` - Async any check
- `FirstOrDefaultAsync` - Async first or default
- `ToNonEmptyListAsync` - Create from IAsyncEnumerable
- `ToAsyncEnumerable` - Convert to IAsyncEnumerable

### JSON Serialization
- `NonEmptyListJsonConverterFactory` - System.Text.Json support for NonEmptyList<T>
- `ImmutableNonEmptyListJsonConverterFactory` - System.Text.Json support for ImmutableNonEmptyList<T>

### Equality & Comparison
- Implements `IEquatable<NonEmptyList<T>>` and `IEquatable<ImmutableNonEmptyList<T>>`
- `Equals(NonEmptyList<T>)` - Value-based equality
- `SequenceEqual(NonEmptyList<T>, IEqualityComparer<T>)` - Equality with comparer
- `GetHashCode()` - Proper hash code implementation
- `==` and `!=` operators

## Changed
- Multi-targeting: Now targets both .NET 6.0 and .NET 8.0
- Implements `IReadOnlyList<T>` for better interoperability
- Centralized cache invalidation with `InvalidateCaches()` method

## Fixed
- `RemoveRange` bug - Changed condition from `if (index is 0 && Count == count)` to `if (Count - count < 1)` to properly prevent emptying the list from any starting index
- Fixed multiple enumeration performance issue in `Constructor`, `AddRange`, `InsertRange`, and `From` methods - Now materializes the enumerable once to avoid multiple iterations
- `Remove` null check - Added `ArgumentNullException.ThrowIfNull(item)` to prevent removing null values
- `Insert` and `InsertRange` overrides - New methods that validate against null values and invalidate the tail cache