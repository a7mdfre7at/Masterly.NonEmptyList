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