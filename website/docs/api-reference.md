---
id: api-reference
title: "API Reference"
description: "Every public type and member of Masterly.NonEmptyList 3.0 and its integration packages, package by package."
---

This reference lists every public type of version 3.0, one package at a time. Each package uses a single namespace that matches its package name. Signatures are shortened: `Name(parameters) : ReturnType`. Members marked `static` are static; everything else is an instance member. For explanations and examples, follow the guide links.

Every package targets .NET 8, .NET 9 and .NET 10.

## Masterly.NonEmptyList

The core package. Namespace: `Masterly.NonEmptyList`. It has no package dependencies and bundles the analyzers, code fixes and source generator (see [Analyzer diagnostics](#analyzer-diagnostics)).

### `NonEmptyList<T>`

A mutable list that always contains at least one element and never contains `null`. It wraps a `List<T>` and implements `IList<T>`, `IReadOnlyList<T>`, `IList`, `IEquatable<NonEmptyList<T>>` and `ISpanParsable<NonEmptyList<T>>`. The class is not sealed. It carries `[JsonConverter]`, `[TypeConverter]` and `[CollectionBuilder]`, so System.Text.Json, model binding and collection expressions work without setup.

Guides: [Creating lists](./creating-lists.md), [Reading elements](./reading-elements.md), [Safe mutation](./safe-mutation.md), [Functional operations](./functional-operations.md).

**Construction**

- `NonEmptyList(T firstItem, params T[] otherItems)`
- `NonEmptyList(T firstItem, params ReadOnlySpan<T> otherItems)`
- `NonEmptyList(T firstItem, IEnumerable<T> otherItems)`
- `NonEmptyList(IEnumerable<T> items)`: copies the sequence; throws `ArgumentException` if it is empty.
- `protected NonEmptyList(NonEmptyList<T> source)`: copy constructor for derived types.
- `static From(IEnumerable<T> enumerable) : NonEmptyList<T>`
- `static FromSpan(ReadOnlySpan<T> items) : NonEmptyList<T>`
- `static TryFrom(IEnumerable<T>? enumerable, [NotNullWhen(true)] out NonEmptyList<T>? result) : bool`
- `static Singleton(T item) : NonEmptyList<T>`
- `static Repeat(T item, int count) : NonEmptyList<T>`
- `static Parse(string s, IFormatProvider? provider = null) : NonEmptyList<T>`
- `static Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) : NonEmptyList<T>`
- `static TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonEmptyList<T> result) : bool`
- `static TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out NonEmptyList<T> result) : bool`
- `static TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonEmptyList<T> result) : bool`

**Properties and indexers**

- `Head : T`, `First : T` (same as `Head`), `Last : T`
- `Tail : NonEmptyList<T>?`: a new list of every element after the first; `null` for a single-element list.
- `Init : NonEmptyList<T>?`: a new list of every element before the last; `null` for a single-element list.
- `Count : int`, `Capacity : int` (get and set)
- `this[int index] : T` (get and set; setting `null` throws)
- `this[Index index] : T` (get)
- `this[Range range] : List<T>` (get, returns a copy; enables `xs[1..]` and list patterns)

**Adding**

- `Add(T item) : void`
- `AddRange(IEnumerable<T> collection) : void`: throws `ArgumentException` for an empty collection.
- `AddFirst(T item) : void`: inserts at index 0, in place.
- `Insert(int index, T item) : void`
- `InsertRange(int index, IEnumerable<T> collection) : void`: throws `ArgumentException` for an empty collection.

**Removing** (each throws instead of emptying the list)

- `Remove(T item) : bool`: `false` when the item is absent; throws `InvalidOperationException` when it would remove the last element.
- `RemoveAt(int index) : void`
- `RemoveRange(int index, int count) : void`
- `RemoveAll(Predicate<T> match) : int`
- `Clear() : void`: always throws `NotSupportedException`.

**Search and query** (same behaviour as `List<T>`)

- `Contains(T item) : bool`
- `IndexOf(T item) : int`, `IndexOf(T item, int index) : int`, `IndexOf(T item, int index, int count) : int`
- `LastIndexOf(T item) : int`, `LastIndexOf(T item, int index) : int`, `LastIndexOf(T item, int index, int count) : int`
- `Exists(Predicate<T> match) : bool`, `TrueForAll(Predicate<T> match) : bool`
- `Find(Predicate<T> match) : T?`, `FindLast(Predicate<T> match) : T?`, `FindAll(Predicate<T> match) : List<T>`
- `FindIndex(Predicate<T> match) : int`, `FindIndex(int startIndex, Predicate<T> match) : int`, `FindIndex(int startIndex, int count, Predicate<T> match) : int`
- `FindLastIndex(Predicate<T> match) : int`, `FindLastIndex(int startIndex, Predicate<T> match) : int`, `FindLastIndex(int startIndex, int count, Predicate<T> match) : int`
- `BinarySearch(T item) : int`, `BinarySearch(T item, IComparer<T>? comparer) : int`, `BinarySearch(int index, int count, T item, IComparer<T>? comparer) : int`
- `GetRange(int index, int count) : List<T>`, `Slice(int start, int length) : List<T>`
- `MinBy<TKey>(Func<T, TKey> keySelector) : T`, `MinBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer) : T`
- `MaxBy<TKey>(Func<T, TKey> keySelector) : T`, `MaxBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer) : T`
- `Random() : T` (uses `Random.Shared`), `Random(Random random) : T`

**In-place reordering and storage**

- `Reverse() : void`, `Reverse(int index, int count) : void`
- `Sort() : void`, `Sort(Comparison<T> comparison) : void`, `Sort(IComparer<T>? comparer) : void`, `Sort(int index, int count, IComparer<T>? comparer) : void`
- `SetRange(int startIndex, IEnumerable<T> items) : void`: overwrites elements starting at `startIndex`.
- `TrimExcess() : void`, `EnsureCapacity(int capacity) : int`

**Iteration**

- `GetEnumerator() : List<T>.Enumerator` (allocation-free)
- `ForEach(Action<T> action) : void`, `ForEachWithIndex(Action<T, int> action) : void`

**Functional operations**

- `Map<TResult>(Func<T, TResult> selector) : NonEmptyList<TResult>`
- `MapWithIndex<TResult>(Func<T, int, TResult> selector) : NonEmptyList<TResult>`
- `FlatMap<TResult>(Func<T, NonEmptyList<TResult>> selector) : NonEmptyList<TResult>`
- `ConvertAll<TOutput>(Converter<T, TOutput> converter) : NonEmptyList<TOutput>`
- `Reduce(Func<T, T, T> reducer) : T`, `ReduceRight(Func<T, T, T> reducer) : T`
- `Fold<TResult>(TResult seed, Func<TResult, T, TResult> folder) : TResult`
- `FoldRight<TResult>(TResult seed, Func<T, TResult, TResult> folder) : TResult`
- `Scan<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> folder) : NonEmptyList<TAccumulate>`
- `Scan(Func<T, T, T> reducer) : NonEmptyList<T>`
- `Zip<TOther>(NonEmptyList<TOther> other) : NonEmptyList<(T First, TOther Second)>`
- `ZipWith<TOther, TResult>(NonEmptyList<TOther> other, Func<T, TOther, TResult> selector) : NonEmptyList<TResult>`
- `ZipWithIndex() : NonEmptyList<(T Item, int Index)>`
- `Partition(Func<T, bool> predicate) : (List<T> Matching, List<T> NotMatching)`
- `GroupByNonEmpty<TKey>(Func<T, TKey> keySelector) : Dictionary<TKey, NonEmptyList<T>>`
- `GroupByNonEmpty<TKey, TElement>(Func<T, TKey> keySelector, Func<T, TElement> elementSelector) : Dictionary<TKey, NonEmptyList<TElement>>`
- `Match<TResult>(Func<T, TResult> single, Func<T, NonEmptyList<T>, TResult> multiple) : TResult`
- `Match(Action<T> single, Action<T, NonEmptyList<T>> multiple) : void`
- `Deconstruct(out T head, out NonEmptyList<T>? tail) : void`
- `Deconstruct(out T first, out T? second, out IEnumerable<T> rest) : void`

**Operations that return a new list**

- `Concat(NonEmptyList<T> other) : NonEmptyList<T>`, `Concat(IEnumerable<T> other) : NonEmptyList<T>`
- `Prepend(T item) : NonEmptyList<T>`, `Append(T item) : NonEmptyList<T>`
- `ReverseList() : NonEmptyList<T>`
- `DistinctList() : NonEmptyList<T>`, `DistinctList(IEqualityComparer<T>? comparer) : NonEmptyList<T>`
- `DistinctBy<TKey>(Func<T, TKey> keySelector) : NonEmptyList<T>`
- `TakeNonEmpty(int count) : NonEmptyList<T>?`, `SkipNonEmpty(int count) : NonEmptyList<T>?`
- `TakeAtLeastOne(int count) : NonEmptyList<T>`
- `Sliding(int size, int step = 1) : IEnumerable<NonEmptyList<T>>`
- `ChunkNonEmpty(int size) : IEnumerable<NonEmptyList<T>>`
- `Tails() : NonEmptyList<NonEmptyList<T>>`, `Inits() : NonEmptyList<NonEmptyList<T>>`
- `Intersperse(T separator) : NonEmptyList<T>`
- `RotateLeft(int positions) : NonEmptyList<T>`, `RotateRight(int positions) : NonEmptyList<T>`

**Validation of elements**

- `Validate(Func<T, bool> predicate, string errorMessage) : NonEmptyList<T>`: throws if any element fails.
- `TryValidate(Func<T, bool> predicate, [MaybeNullWhen(true)] out T failingElement) : bool`

**Conversion**

- `ToArray() : T[]`, `ToArrayNonEmpty() : T[]`
- `ToHashSet() : HashSet<T>`, `ToHashSet(IEqualityComparer<T>? comparer) : HashSet<T>`
- `ToDictionary<TKey>(Func<T, TKey> keySelector) : Dictionary<TKey, T>`
- `ToDictionary<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector) : Dictionary<TKey, TValue>`
- `ToQueue() : Queue<T>`, `ToStack() : Stack<T>`, `ToLinkedList() : LinkedList<T>`
- `AsReadOnly() : ReadOnlyCollection<T>`
- `AsSpan() : ReadOnlySpan<T>`, `AsMemory() : ReadOnlyMemory<T>` (copies to an array)
- `CopyTo(T[] array) : void`, `CopyTo(T[] array, int arrayIndex) : void`, `CopyTo(int index, T[] array, int arrayIndex, int count) : void`

**Equality**

- `Equals(NonEmptyList<T>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`: element-by-element.
- `SequenceEqual(NonEmptyList<T> other, IEqualityComparer<T>? comparer) : bool`
- `operator ==`, `operator !=`
- `ToString() : string`: `NonEmptyList: [1, 2, 3]`.

**Extension points for derived types** (all `protected virtual`)

- `IsReadOnly : bool` (default `false`)
- `EnsureMutable() : void`
- `ValidateItem(T item) : void`
- `ValidateRange(IReadOnlyList<T> items) : void`
- `ValidateReplacement(int index, T item) : void`
- `ValidateCount(int proposedCount) : void`

:::note

The explicit `IList<T>`, `ICollection<T>` and `IList` implementations route through the members above, so removing the last element through an interface throws as well.

:::

### `NonEmptyList` (static)

Non-generic factories with type inference, and the target of collection expressions (`NonEmptyList<int> xs = [1, 2, 3];`). Namespace: `Masterly.NonEmptyList`. Guide: [Creating lists](./creating-lists.md).

- `static Create<T>(ReadOnlySpan<T> items) : NonEmptyList<T>`: the collection builder; an empty span throws `ArgumentException`.
- `static Of<T>(T first, params T[] rest) : NonEmptyList<T>`, `static Of<T>(T first, params ReadOnlySpan<T> rest) : NonEmptyList<T>`
- `static From<T>(IEnumerable<T> source) : NonEmptyList<T>`
- `static TryFrom<T>(IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptyList<T>? result) : bool`
- `static Singleton<T>(T item) : NonEmptyList<T>`
- `static Repeat<T>(T item, int count) : NonEmptyList<T>`
- `static Range(int start, int count) : NonEmptyList<int>`
- `static Iterate<T>(T seed, Func<T, T> next, int count) : NonEmptyList<T>`
- `static Iterate<T>(T seed, Func<T, T> next) : NonEmptyEnumerable<T>`: lazy and infinite.
- `static Unfold<T>(T seed, Func<T, T?> next) : NonEmptyList<T>` where `T : class`
- `static UnfoldValue<T>(T seed, Func<T, T?> next) : NonEmptyList<T>` where `T : struct`

### `OrderedNonEmptyList<T>`

The result of `OrderBy`, `OrderByDescending`, `Order`, `OrderDescending`, `ThenBy` and `ThenByDescending`. Namespace: `Masterly.NonEmptyList`. Derives from `NonEmptyList<T>` and implements `IOrderedEnumerable<T>`, so `ThenBy` and `orderby a, b` stay in the non-empty type. It has no public constructor. Guide: [LINQ](./linq.md).

### `NonEmptyGrouping<TKey, TElement>`

One group produced by `GroupBy` on a `NonEmptyList<T>`. Namespace: `Masterly.NonEmptyList`. Derives from `NonEmptyList<TElement>` and implements `IGrouping<TKey, TElement>`. Guide: [LINQ](./linq.md).

- `Key : TKey`
- `ToString() : string`

### `NonEmptyListLinqExtensions`

LINQ operators that cannot produce an empty result, overloaded on a `NonEmptyList<T>` receiver so they win over `Enumerable` and keep query syntax in the non-empty type. They run eagerly and return a new list. Operators that may be empty (`Where`, `Skip`, `Take`, `OfType`) are not overloaded and return `IEnumerable<T>`. Namespace: `Masterly.NonEmptyList`. Guide: [LINQ](./linq.md).

**Projection**

- `Select<T, TResult>(this NonEmptyList<T>, Func<T, TResult>) : NonEmptyList<TResult>`
- `Select<T, TResult>(this NonEmptyList<T>, Func<T, int, TResult>) : NonEmptyList<TResult>`
- `SelectMany<T, TResult>(this NonEmptyList<T>, Func<T, NonEmptyList<TResult>>) : NonEmptyList<TResult>`
- `SelectMany<T, TResult>(this NonEmptyList<T>, Func<T, ImmutableNonEmptyList<TResult>>) : NonEmptyList<TResult>`
- `SelectMany<T, TCollection, TResult>(this NonEmptyList<T>, Func<T, NonEmptyList<TCollection>> collectionSelector, Func<T, TCollection, TResult> resultSelector) : NonEmptyList<TResult>`
- `Flatten<T>(this NonEmptyList<NonEmptyList<T>>) : NonEmptyList<T>`

**Ordering** (stable; each returns `OrderedNonEmptyList<T>`)

- `OrderBy<T, TKey>(this NonEmptyList<T>, Func<T, TKey> keySelector)`, plus an overload with `IComparer<TKey>? comparer`
- `OrderByDescending<T, TKey>(this NonEmptyList<T>, Func<T, TKey> keySelector)`, plus an overload with `IComparer<TKey>? comparer`
- `Order<T>(this NonEmptyList<T>)`, `Order<T>(this NonEmptyList<T>, IComparer<T>? comparer)`
- `OrderDescending<T>(this NonEmptyList<T>)`, `OrderDescending<T>(this NonEmptyList<T>, IComparer<T>? comparer)`
- `ThenBy<T, TKey>(this OrderedNonEmptyList<T>, Func<T, TKey> keySelector)`, plus an overload with `IComparer<TKey>? comparer`
- `ThenByDescending<T, TKey>(this OrderedNonEmptyList<T>, Func<T, TKey> keySelector)`, plus an overload with `IComparer<TKey>? comparer`

**Set and combination**

- `Distinct<T>(this NonEmptyList<T>) : NonEmptyList<T>`, `Distinct<T>(this NonEmptyList<T>, IEqualityComparer<T>? comparer) : NonEmptyList<T>`
- `Concat<T>(this IEnumerable<T> first, NonEmptyList<T> second) : NonEmptyList<T>`
- `Union<T>(this NonEmptyList<T> first, IEnumerable<T> second) : NonEmptyList<T>`, plus an overload with `IEqualityComparer<T>? comparer`
- `Union<T>(this IEnumerable<T> first, NonEmptyList<T> second) : NonEmptyList<T>`, plus an overload with `IEqualityComparer<T>? comparer`
- `Union<T>(this NonEmptyList<T> first, NonEmptyList<T> second) : NonEmptyList<T>`, plus an overload with `IEqualityComparer<T>? comparer`; makes `a.Union(b)` unambiguous when both are non-empty lists
- `Interleave<T>(this NonEmptyList<T> first, NonEmptyList<T> second) : NonEmptyList<T>`

**Grouping and chunking**

- `GroupBy<T, TKey>(this NonEmptyList<T>, Func<T, TKey> keySelector) : NonEmptyList<NonEmptyGrouping<TKey, T>>`, plus an overload with `IEqualityComparer<TKey>? comparer`
- `GroupBy<T, TKey, TElement>(this NonEmptyList<T>, Func<T, TKey> keySelector, Func<T, TElement> elementSelector) : NonEmptyList<NonEmptyGrouping<TKey, TElement>>`, plus an overload with `IEqualityComparer<TKey>? comparer`
- `Chunk<T>(this NonEmptyList<T>, int size) : NonEmptyList<NonEmptyList<T>>`
- `Unzip<TFirst, TSecond>(this NonEmptyList<(TFirst First, TSecond Second)>) : (NonEmptyList<TFirst> Firsts, NonEmptyList<TSecond> Seconds)`
- `Cycle<T>(this NonEmptyList<T>) : NonEmptyEnumerable<T>` (infinite), `Cycle<T>(this NonEmptyList<T>, int times) : NonEmptyList<T>`

**Aggregation** (non-nullable results)

- `Max<T>(this NonEmptyList<T>) : T`, `Max<T>(this NonEmptyList<T>, IComparer<T>? comparer) : T`, `Max<T, TResult>(this NonEmptyList<T>, Func<T, TResult> selector) : TResult`
- `Min<T>(this NonEmptyList<T>) : T`, `Min<T>(this NonEmptyList<T>, IComparer<T>? comparer) : T`, `Min<T, TResult>(this NonEmptyList<T>, Func<T, TResult> selector) : TResult`
- `MinMax<T>(this NonEmptyList<T>) : (T Min, T Max)`, `MinMax<T>(this NonEmptyList<T>, IComparer<T>? comparer) : (T Min, T Max)`

**Conversion from other sequences**

- `ToNonEmptyList<T>(this IEnumerable<T> source) : NonEmptyList<T>`: throws for an empty sequence.
- `ToNonEmptyListOrNull<T>(this IEnumerable<T>? source) : NonEmptyList<T>?`
- `TryToNonEmptyList<T>(this IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptyList<T>? result) : bool`
- `ToNonEmptyList<T>(this ReadOnlySpan<T> source) : NonEmptyList<T>`

### `NonEmptyListMathExtensions`

Numeric aggregations built on generic math (`System.Numerics`), with no empty-sequence exception path. `Sum` and `Product` use checked arithmetic and need no zero or one value. Namespace: `Masterly.NonEmptyList`. Guide: [Generic math](./generic-math.md).

- `Sum<T>(this NonEmptyList<T>) : T` where `T : IAdditionOperators<T, T, T>`
- `Sum<T, TResult>(this NonEmptyList<T>, Func<T, TResult> selector) : TResult` where `TResult : IAdditionOperators<TResult, TResult, TResult>`
- `Product<T>(this NonEmptyList<T>) : T` where `T : IMultiplyOperators<T, T, T>`
- `Average<T>(this NonEmptyList<T>) : T` where `T : INumberBase<T>`
- `Average<T, TResult>(this NonEmptyList<T>, Func<T, TResult> selector) : TResult` where `TResult : INumberBase<TResult>`
- `Average(this NonEmptyList<int>) : double`, `Average(this NonEmptyList<long>) : double`
- `Average<T>(this NonEmptyList<T>, Func<T, int> selector) : double`, `Average<T>(this NonEmptyList<T>, Func<T, long> selector) : double`
- `Median<T>(this NonEmptyList<T>) : T` where `T : INumber<T>`
- `Median(this NonEmptyList<int>) : double`, `Median(this NonEmptyList<long>) : double`
- `Mode<T>(this NonEmptyList<T>) : T`, `Mode<T>(this NonEmptyList<T>, IEqualityComparer<T>? comparer) : T` where `T : notnull`

### `NonEmptyListAsyncExtensions`

Async versions of the functional operations. Namespace: `Masterly.NonEmptyList`. Guide: [Async operations](./async-operations.md).

- `MapAsync<T, TResult>(this NonEmptyList<T>, Func<T, Task<TResult>> selector) : Task<NonEmptyList<TResult>>`
- `MapWithIndexAsync<T, TResult>(this NonEmptyList<T>, Func<T, int, Task<TResult>> selector) : Task<NonEmptyList<TResult>>`
- `MapParallelAsync<T, TResult>(this NonEmptyList<T>, Func<T, Task<TResult>> selector) : Task<NonEmptyList<TResult>>`
- `MapParallelAsync<T, TResult>(this NonEmptyList<T>, Func<T, Task<TResult>> selector, int maxDegreeOfParallelism) : Task<NonEmptyList<TResult>>`
- `ForEachAsync<T>(this NonEmptyList<T>, Func<T, Task> action) : Task`
- `ForEachWithIndexAsync<T>(this NonEmptyList<T>, Func<T, int, Task> action) : Task`
- `ForEachParallelAsync<T>(this NonEmptyList<T>, Func<T, Task> action) : Task`
- `FilterAsync<T>(this NonEmptyList<T>, Func<T, Task<bool>> predicate) : Task<NonEmptyList<T>?>`
- `FoldAsync<T, TResult>(this NonEmptyList<T>, TResult seed, Func<TResult, T, Task<TResult>> folder) : Task<TResult>`
- `AllAsync<T>(this NonEmptyList<T>, Func<T, Task<bool>> predicate) : Task<bool>`
- `AnyAsync<T>(this NonEmptyList<T>, Func<T, Task<bool>> predicate) : Task<bool>`
- `FirstOrDefaultAsync<T>(this NonEmptyList<T>, Func<T, Task<bool>> predicate) : Task<T?>`
- `ToNonEmptyListAsync<T>(this IAsyncEnumerable<T> source) : Task<NonEmptyList<T>?>`
- `ToAsyncEnumerable<T>(this NonEmptyList<T> source) : IAsyncEnumerable<T>`

### `NonEmptyListBatchingExtensions`

Batching operators that only ever emit non-empty batches. Namespace: `Masterly.NonEmptyList`. Guide: [Batching streams](./batching-streams.md).

- `BatchNonEmpty<T>(this IEnumerable<T> source, int size) : IEnumerable<NonEmptyList<T>>`
- `BatchNonEmpty<T>(this IAsyncEnumerable<T> source, int size, CancellationToken cancellationToken = default) : IAsyncEnumerable<NonEmptyList<T>>`
- `BatchNonEmpty<T>(this IAsyncEnumerable<T> source, int size, TimeSpan maxWait, CancellationToken cancellationToken = default) : IAsyncEnumerable<NonEmptyList<T>>`: the timer starts only once an element is waiting.
- `ReadAllBatchesAsync<T>(this ChannelReader<T> reader, int maxSize = int.MaxValue, CancellationToken cancellationToken = default) : IAsyncEnumerable<NonEmptyList<T>>`
- `ToNonEmptyAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default) : ValueTask<NonEmptyAsyncEnumerable<T>?>`: `null` for an empty stream.

### `NonEmptyAsyncEnumerable<T>`

An async stream whose first element has already been read. Implements `IAsyncEnumerable<T>` and `IAsyncDisposable`. `Rest` can be consumed once. Namespace: `Masterly.NonEmptyList`. Guide: [Batching streams](./batching-streams.md).

- `NonEmptyAsyncEnumerable(T head, IAsyncEnumerable<T> rest)`
- `Head : T`, `Rest : IAsyncEnumerable<T>`
- `GetAsyncEnumerator(CancellationToken cancellationToken = default) : IAsyncEnumerator<T>`
- `ToNonEmptyListAsync(CancellationToken cancellationToken = default) : ValueTask<NonEmptyList<T>>`
- `Select<TResult>(Func<T, TResult> selector) : NonEmptyAsyncEnumerable<TResult>`
- `BatchRest(int size, CancellationToken cancellationToken = default) : IAsyncEnumerable<NonEmptyList<T>>`
- `DisposeAsync() : ValueTask`

### `ImmutableNonEmptyList<T>`

An immutable list with at least one element. Every modification returns a new instance. Implements `IReadOnlyList<T>` and `IEquatable<ImmutableNonEmptyList<T>>`, and supports collection expressions and System.Text.Json. Namespace: `Masterly.NonEmptyList`. Guide: [Immutable lists](./immutable-lists.md).

**Construction**

- `ImmutableNonEmptyList(T firstItem, params T[] otherItems)`
- `ImmutableNonEmptyList(IEnumerable<T> items)`
- `ImmutableNonEmptyList(T firstItem, IEnumerable<T> otherItems)`
- `static From(IEnumerable<T> enumerable) : ImmutableNonEmptyList<T>`
- `static TryFrom(IEnumerable<T>? enumerable, out ImmutableNonEmptyList<T>? result) : bool`
- `static Singleton(T item) : ImmutableNonEmptyList<T>`
- `static Repeat(T item, int count) : ImmutableNonEmptyList<T>`

**Reading**

- `Count : int`, `Head : T`, `First : T`, `Last : T`
- `Tail : ImmutableNonEmptyList<T>?`, `Init : ImmutableNonEmptyList<T>?`
- `this[int index] : T`, `this[Index index] : T`
- `Slice(int start, int length) : T[]`, `AsSpan() : ReadOnlySpan<T>`
- `GetEnumerator() : IEnumerator<T>`

**Modification** (each returns a new list)

- `Append(T item)`, `AppendRange(IEnumerable<T> items)`, `Prepend(T item)` : `ImmutableNonEmptyList<T>`
- `SetItem(int index, T item)`, `Insert(int index, T item)` : `ImmutableNonEmptyList<T>`
- `RemoveFirst()`, `RemoveLast()`, `RemoveAt(int index)`, `Remove(T item)` : `ImmutableNonEmptyList<T>?` (`null` when nothing would remain)

**Functional operations**

- `Map<TResult>(Func<T, TResult> selector) : ImmutableNonEmptyList<TResult>`
- `MapWithIndex<TResult>(Func<T, int, TResult> selector) : ImmutableNonEmptyList<TResult>`
- `FlatMap<TResult>(Func<T, ImmutableNonEmptyList<TResult>> selector) : ImmutableNonEmptyList<TResult>`
- `Reduce(Func<T, T, T> reducer) : T`
- `Fold<TResult>(TResult seed, Func<TResult, T, TResult> folder) : TResult`
- `Zip<TOther>(ImmutableNonEmptyList<TOther> other) : ImmutableNonEmptyList<(T First, TOther Second)>`
- `Match<TResult>(Func<T, TResult> single, Func<T, ImmutableNonEmptyList<T>, TResult> multiple) : TResult`
- `Concat(ImmutableNonEmptyList<T> other) : ImmutableNonEmptyList<T>`
- `Reverse() : ImmutableNonEmptyList<T>`, `Distinct() : ImmutableNonEmptyList<T>`
- `Take(int count) : ImmutableNonEmptyList<T>?`, `Skip(int count) : ImmutableNonEmptyList<T>?`
- `Intersperse(T separator) : ImmutableNonEmptyList<T>`
- `Deconstruct(out T head, out ImmutableNonEmptyList<T>? tail) : void`

**Conversion and equality**

- `ToMutable() : NonEmptyList<T>`, `ToArray() : T[]`, `ToList() : List<T>`
- `Equals(ImmutableNonEmptyList<T>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `operator ==`, `operator !=`, `ToString() : string`

### `ImmutableNonEmptyList` (static)

Non-generic factories and the collection-expression builder. Namespace: `Masterly.NonEmptyList`. Guide: [Immutable lists](./immutable-lists.md).

- `static Create<T>(ReadOnlySpan<T> items) : ImmutableNonEmptyList<T>`
- `static Of<T>(T first, params T[] rest) : ImmutableNonEmptyList<T>`
- `static From<T>(IEnumerable<T> source) : ImmutableNonEmptyList<T>`
- `static Singleton<T>(T item) : ImmutableNonEmptyList<T>`

### `ImmutableNonEmptyListExtensions`

Namespace: `Masterly.NonEmptyList`. Guide: [Immutable lists](./immutable-lists.md).

- `ToImmutable<T>(this NonEmptyList<T> source) : ImmutableNonEmptyList<T>`

### `NonEmptySet<T>`

A hash set that always contains at least one element. Wraps a `HashSet<T>` and implements `ISet<T>`, `IReadOnlySet<T>`, `IReadOnlyCollection<T>` and `IEquatable<NonEmptySet<T>>`. Supports collection expressions and System.Text.Json. Every operation that would empty the set throws. Namespace: `Masterly.NonEmptyList`. Guide: [Sets, dictionaries and strings](./sets-dictionaries-strings.md).

**Construction**

- `NonEmptySet(T first, params T[] rest)`
- `NonEmptySet(T first, IEnumerable<T> rest, IEqualityComparer<T>? comparer)`
- `static From(IEnumerable<T> source, IEqualityComparer<T>? comparer = null) : NonEmptySet<T>`
- `static FromSpan(ReadOnlySpan<T> items) : NonEmptySet<T>`
- `static TryFrom(IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptySet<T>? result, IEqualityComparer<T>? comparer = null) : bool`
- `static Singleton(T item) : NonEmptySet<T>`
- `static Parse(string s, IFormatProvider? provider = null) : NonEmptySet<T>`
- `static TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out NonEmptySet<T> result) : bool`

**Reading**

- `Count : int`, `Comparer : IEqualityComparer<T>`
- `Any() : T`: an arbitrary element.
- `Contains(T item) : bool`, `CopyTo(T[] array) : void`, `CopyTo(T[] array, int arrayIndex) : void`
- `IsSubsetOf`, `IsSupersetOf`, `IsProperSubsetOf`, `IsProperSupersetOf`, `Overlaps`, `SetEquals` (each `(IEnumerable<T> other) : bool`)
- `GetEnumerator() : HashSet<T>.Enumerator`

**Mutation**

- `Add(T item) : bool`
- `Remove(T item) : bool`: `false` when absent; throws when it would empty the set.
- `RemoveWhere(Predicate<T> match) : int`
- `UnionWith`, `IntersectWith`, `ExceptWith`, `SymmetricExceptWith` (each `(IEnumerable<T> other) : void`; throw instead of emptying the set)
- `Clear() : void`: always throws `NotSupportedException`.

**Operations that return a new set or list**

- `Union(IEnumerable<T> other) : NonEmptySet<T>`
- `Intersect(IEnumerable<T> other) : NonEmptySet<T>?`, `Except(IEnumerable<T> other) : NonEmptySet<T>?`
- `Map<TResult>(Func<T, TResult> selector) : NonEmptySet<TResult>`
- `ToNonEmptyList() : NonEmptyList<T>`, `ToSortedNonEmptyList(IComparer<T>? comparer = null) : NonEmptyList<T>`, `ToHashSet() : HashSet<T>`

**Equality**

- `Equals(NonEmptySet<T>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `ToString() : string`

### `NonEmptySet` (static)

Factories and conversion extensions. Namespace: `Masterly.NonEmptyList`. Guide: [Sets, dictionaries and strings](./sets-dictionaries-strings.md).

- `static Create<T>(ReadOnlySpan<T> items) : NonEmptySet<T>`
- `static Of<T>(T first, params T[] rest) : NonEmptySet<T>`
- `static From<T>(IEnumerable<T> source, IEqualityComparer<T>? comparer = null) : NonEmptySet<T>`
- `ToNonEmptySet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) : NonEmptySet<T>`
- `ToNonEmptySet<T>(this NonEmptyList<T> source, IEqualityComparer<T>? comparer = null) : NonEmptySet<T>`
- `TryToNonEmptySet<T>(this IEnumerable<T>? source, [NotNullWhen(true)] out NonEmptySet<T>? result) : bool`

### `NonEmptyDictionary<TKey, TValue>`

A dictionary that always contains at least one entry (`TKey : notnull`). Wraps a `Dictionary<TKey, TValue>` and implements `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>` and `IEquatable<NonEmptyDictionary<TKey, TValue>>`. Supports System.Text.Json. Namespace: `Masterly.NonEmptyList`. Guide: [Sets, dictionaries and strings](./sets-dictionaries-strings.md).

**Construction**

- `NonEmptyDictionary(TKey key, TValue value, IEqualityComparer<TKey>? comparer = null)`
- `NonEmptyDictionary(KeyValuePair<TKey, TValue> first, params KeyValuePair<TKey, TValue>[] rest)`
- `static From(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null) : NonEmptyDictionary<TKey, TValue>`
- `static From<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer = null) : NonEmptyDictionary<TKey, TValue>`
- `static TryFrom(IEnumerable<KeyValuePair<TKey, TValue>>? source, [NotNullWhen(true)] out NonEmptyDictionary<TKey, TValue>? result, IEqualityComparer<TKey>? comparer = null) : bool`
- `static Singleton(TKey key, TValue value) : NonEmptyDictionary<TKey, TValue>`

**Reading**

- `Count : int`, `Comparer : IEqualityComparer<TKey>`
- `this[TKey key] : TValue` (get and set; setting adds or replaces)
- `Keys : Dictionary<TKey, TValue>.KeyCollection`, `Values : Dictionary<TKey, TValue>.ValueCollection`
- `Any() : KeyValuePair<TKey, TValue>`
- `KeyList() : NonEmptyList<TKey>`, `ValueList() : NonEmptyList<TValue>`, `ToNonEmptyList() : NonEmptyList<KeyValuePair<TKey, TValue>>`
- `ContainsKey(TKey key) : bool`, `ContainsValue(TValue value) : bool`
- `TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) : bool`
- `GetValueOrDefault(TKey key, TValue fallback) : TValue`
- `GetEnumerator() : Dictionary<TKey, TValue>.Enumerator`

**Mutation**

- `Add(TKey key, TValue value) : void`, `TryAdd(TKey key, TValue value) : bool`
- `Remove(TKey key) : bool`, `Remove(TKey key, [MaybeNullWhen(false)] out TValue value) : bool`: throw when they would remove the last entry.
- `Clear() : void`: always throws `NotSupportedException`.

**Operations that return a new dictionary**

- `MapValues<TResult>(Func<TValue, TResult> selector) : NonEmptyDictionary<TKey, TResult>`
- `Map<TResult>(Func<TKey, TValue, TResult> selector) : NonEmptyDictionary<TKey, TResult>`
- `Where(Func<TKey, TValue, bool> predicate) : NonEmptyDictionary<TKey, TValue>?`
- `ToDictionary() : Dictionary<TKey, TValue>`

**Equality**

- `Equals(NonEmptyDictionary<TKey, TValue>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `ToString() : string`

### `NonEmptyDictionary` (static)

Factories and conversion extensions (all with `TKey : notnull`). Namespace: `Masterly.NonEmptyList`. Guide: [Sets, dictionaries and strings](./sets-dictionaries-strings.md).

- `static Of<TKey, TValue>(TKey key, TValue value) : NonEmptyDictionary<TKey, TValue>`
- `static OfPairs<TKey, TValue>((TKey Key, TValue Value) first, params (TKey Key, TValue Value)[] rest) : NonEmptyDictionary<TKey, TValue>`
- `static From<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null) : NonEmptyDictionary<TKey, TValue>`
- `ToNonEmptyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer = null) : NonEmptyDictionary<TKey, TValue>`
- `ToNonEmptyDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer = null) : NonEmptyDictionary<TKey, TValue>`
- `ToNonEmptyDictionary<T, TKey>(this NonEmptyList<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) : NonEmptyDictionary<TKey, T>`
- `TryToNonEmptyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>? source, [NotNullWhen(true)] out NonEmptyDictionary<TKey, TValue>? result) : bool`

### `NonEmptyString`

A `readonly struct` holding a string that is never null, empty or whitespace. Implements `IEquatable<NonEmptyString>`, `IComparable<NonEmptyString>`, `IComparable`, `ISpanParsable<NonEmptyString>` and `ISpanFormattable`, and carries a JSON converter and a type converter. Namespace: `Masterly.NonEmptyList`. Guide: [Sets, dictionaries and strings](./sets-dictionaries-strings.md).

**Construction**

- `NonEmptyString(string value)`: throws `ArgumentException` for blank input.
- `static TryCreate([NotNullWhen(true)] string? value, out NonEmptyString result) : bool`
- `static FromNullable(string? value) : NonEmptyString?`
- `static Parse(string s, IFormatProvider? provider) : NonEmptyString`, `static Parse(ReadOnlySpan<char> s, IFormatProvider? provider) : NonEmptyString`
- `static TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NonEmptyString result) : bool`
- `static TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out NonEmptyString result) : bool`

**Reading**

- `Value : string`: throws `InvalidOperationException` for a `default` instance.
- `HasValue : bool`: `false` only for a `default` instance.
- `Length : int`, `Head : char`, `Last : char`, `this[int index] : char`
- `AsSpan() : ReadOnlySpan<char>`

**Operations** (each returns a new value)

- `Trim() : NonEmptyString`, `ToUpperInvariant() : NonEmptyString`, `ToLowerInvariant() : NonEmptyString`
- `Concat(string? other) : NonEmptyString`, `Truncate(int maxLength) : NonEmptyString`
- `Split(char separator) : NonEmptyList<NonEmptyString>`, `ToCharList() : NonEmptyList<char>`

**Conversion, equality and formatting**

- `implicit operator string(NonEmptyString value)`, `explicit operator NonEmptyString(string value)`, `implicit operator ReadOnlySpan<char>(NonEmptyString value)`
- `Equals(NonEmptyString other) : bool`, `Equals(NonEmptyString other, StringComparison comparison) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `CompareTo(NonEmptyString other) : int`
- `operator ==`, `operator !=`, `operator +(NonEmptyString, NonEmptyString)`, `operator +(NonEmptyString, string?)`
- `ToString() : string`, `ToString(string? format, IFormatProvider? formatProvider) : string`, `TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) : bool`

### `NonEmptyStringExtensions`

Namespace: `Masterly.NonEmptyList`. Guide: [Sets, dictionaries and strings](./sets-dictionaries-strings.md).

- `ToNonEmptyString(this string value) : NonEmptyString`
- `ToNonEmptyStringOrNull(this string? value) : NonEmptyString?`
- `Join(this NonEmptyList<NonEmptyString> parts, string separator) : NonEmptyString`

### `NonEmptySpan<T>`

A `readonly ref struct`: a stack-only, allocation-free view over at least one element. It has no public constructor; create one with `NonEmptySpan.From` or `AsNonEmptySpan`. Namespace: `Masterly.NonEmptyList`. Guide: [Spans and lazy sequences](./spans-and-lazy-sequences.md).

- `Head : T`, `Last : T`, `Length : int`, `this[int index] : T`
- `Tail : ReadOnlySpan<T>`, `Init : ReadOnlySpan<T>` (may be empty), `Span : ReadOnlySpan<T>`
- `implicit operator ReadOnlySpan<T>(NonEmptySpan<T> span)`
- `Reduce(Func<T, T, T> reducer) : T`, `Fold<TResult>(TResult seed, Func<TResult, T, TResult> folder) : TResult`
- `Max() : T`, `Min() : T`
- `ToNonEmptyList() : NonEmptyList<T>`, `ToArray() : T[]`
- `GetEnumerator() : ReadOnlySpan<T>.Enumerator`, `ToString() : string`

### `NonEmptySpan` (static)

Namespace: `Masterly.NonEmptyList`. Guide: [Spans and lazy sequences](./spans-and-lazy-sequences.md).

- `static From<T>(ReadOnlySpan<T> span) : NonEmptySpan<T>`, `static From<T>(Span<T> span) : NonEmptySpan<T>`, `static From<T>(T[] array) : NonEmptySpan<T>`
- `static TryFrom<T>(ReadOnlySpan<T> span, out NonEmptySpan<T> result) : bool`
- `AsNonEmptySpan<T>(this NonEmptyList<T> list) : NonEmptySpan<T>`, `AsNonEmptySpan<T>(this ImmutableNonEmptyList<T> list) : NonEmptySpan<T>`
- `Sum<T>(this NonEmptySpan<T> span) : T` where `T : IAdditionOperators<T, T, T>`
- `Average<T>(this NonEmptySpan<T> span) : T` where `T : INumberBase<T>`

### `NonEmptyEnumerable<T>`

A lazy, possibly infinite sequence whose first element is already known. Implements `IEnumerable<T>`. Namespace: `Masterly.NonEmptyList`. Guide: [Spans and lazy sequences](./spans-and-lazy-sequences.md).

- `NonEmptyEnumerable(T head, IEnumerable<T> rest)`
- `Head : T`, `Rest : IEnumerable<T>`
- `Take(int count) : NonEmptyList<T>`, `TakeWhile(Func<T, bool> predicate) : NonEmptyList<T>`, `TakeUntil(Func<T, bool> predicate) : NonEmptyList<T>`
- `Select<TResult>(Func<T, TResult> selector) : NonEmptyEnumerable<TResult>`
- `Index() : NonEmptyEnumerable<(T Item, int Index)>`
- `Zip<TOther>(NonEmptyEnumerable<TOther> other) : NonEmptyEnumerable<(T First, TOther Second)>`
- `Prepend(T item) : NonEmptyEnumerable<T>`
- `Scan<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> folder) : NonEmptyEnumerable<TAccumulate>`
- `ElementAt(int index) : T`
- `ToNonEmptyList() : NonEmptyList<T>`: only for finite sequences.
- `GetEnumerator() : IEnumerator<T>`

### `NonEmptyEnumerable` (static)

Namespace: `Masterly.NonEmptyList`. Guide: [Spans and lazy sequences](./spans-and-lazy-sequences.md).

- `static Create<T>(T head, IEnumerable<T> rest) : NonEmptyEnumerable<T>`, `static Create<T>(T head, params T[] rest) : NonEmptyEnumerable<T>`
- `static FromEnumerable<T>(IEnumerable<T> source) : NonEmptyEnumerable<T>?`
- `static Iterate<T>(T seed, Func<T, T> next) : NonEmptyEnumerable<T>`
- `static Repeat<T>(T value) : NonEmptyEnumerable<T>`
- `static From(int start) : NonEmptyEnumerable<int>`
- `static Generate<T>(Func<int, T> generator) : NonEmptyEnumerable<T>`

### `NonEmptyZipper<T>`

An immutable list with a cursor that always points at an element. Moves past either end return `null`. Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

- `static FromStart(NonEmptyList<T> list)`, `static FromEnd(NonEmptyList<T> list)`, `static FromIndex(NonEmptyList<T> list, int index)`, `static Singleton(T focus)` : `NonEmptyZipper<T>`
- `Focus : T`, `Index : int`, `Count : int`, `IsAtStart : bool`, `IsAtEnd : bool`
- `Before : IEnumerable<T>`, `After : IEnumerable<T>`
- `MoveLeft() : NonEmptyZipper<T>?`, `MoveRight() : NonEmptyZipper<T>?`
- `MoveLeftOrStay()`, `MoveRightOrStay()`, `MoveLeftCyclic()`, `MoveRightCyclic()`, `ToStart()`, `ToEnd()`, `MoveTo(int index)` : `NonEmptyZipper<T>`
- `Find(Func<T, bool> predicate) : NonEmptyZipper<T>?`
- `Replace(T focus)`, `Update(Func<T, T> update)`, `InsertAfter(T item)`, `InsertBefore(T item)` : `NonEmptyZipper<T>`
- `Delete() : NonEmptyZipper<T>?`: `null` when the focus is the only element.
- `Map<TResult>(Func<T, TResult> selector) : NonEmptyZipper<TResult>`
- `ToNonEmptyList() : NonEmptyList<T>`
- `Deconstruct(out IEnumerable<T> before, out T focus, out IEnumerable<T> after) : void`
- `Equals(NonEmptyZipper<T>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `ToString() : string`

### `NonEmptyZipperExtensions`

Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

- `ToZipper<T>(this NonEmptyList<T> list) : NonEmptyZipper<T>`
- `ToZipper<T>(this NonEmptyList<T> list, int index) : NonEmptyZipper<T>`
- `ToZipper<T>(this ImmutableNonEmptyList<T> list) : NonEmptyZipper<T>`

### `History<TState>`

An immutable undo/redo history that always has a current state. `Undo` on the first state and `Redo` with nothing to redo return the same history. Pushing discards the redo branch. Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

- `static Start(TState initial) : History<TState>`
- `Current : TState`, `UndoCount : int`, `RedoCount : int`, `CanUndo : bool`, `CanRedo : bool`
- `States : NonEmptyList<TState>`
- `Push(TState state) : History<TState>`, `Push(Func<TState, TState> update) : History<TState>`
- `Undo() : History<TState>`, `Redo() : History<TState>`
- `Equals(History<TState>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `ToString() : string`

### `History` (static)

Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

- `static Start<TState>(TState initial) : History<TState>`

### `EventStream<TEvent>`

An immutable, append-only event sequence that always contains its origin event. Implements `IReadOnlyList<TEvent>` and `IEquatable<EventStream<TEvent>>`. Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

- `static Start(TEvent origin)`, `static From(IEnumerable<TEvent> events)`, `static From(NonEmptyList<TEvent> events)` : `EventStream<TEvent>`
- `Origin : TEvent`, `Latest : TEvent`, `Version : int`, `Count : int`, `this[int index] : TEvent`
- `Events : ImmutableNonEmptyList<TEvent>`
- `Append(TEvent @event) : EventStream<TEvent>`, `AppendRange(IEnumerable<TEvent> events) : EventStream<TEvent>`
- `Replay<TState>(Func<TEvent, TState> create, Func<TState, TEvent, TState> apply) : TState`
- `Replay<TState>(TState seed, Func<TState, TEvent, TState> apply) : TState`
- `ReplayHistory<TState>(Func<TEvent, TState> create, Func<TState, TEvent, TState> apply) : NonEmptyList<TState>`
- `Since(int version) : IReadOnlyList<TEvent>`, `UpTo(int version) : EventStream<TEvent>`
- `OfType<TDerived>() : IEnumerable<TDerived>` where `TDerived : TEvent`
- `OriginAs<TOrigin>() : TOrigin` where `TOrigin : TEvent`
- `GetEnumerator() : IEnumerator<TEvent>`
- `Equals(EventStream<TEvent>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `ToString() : string`

### `EventStream` (static)

Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

- `static Start<TEvent>(TEvent origin) : EventStream<TEvent>`
- `static From<TEvent>(IEnumerable<TEvent> events) : EventStream<TEvent>`
- `ToEventStream<TEvent>(this NonEmptyList<TEvent> events) : EventStream<TEvent>`

### `NonEmptyTree<T>`

An immutable rose tree with a guaranteed root. Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

**Construction and shape**

- `NonEmptyTree(T value)`, `NonEmptyTree(T value, IEnumerable<NonEmptyTree<T>> children)`, `NonEmptyTree(T value, params NonEmptyTree<T>[] children)`
- `Value : T`, `Children : IReadOnlyList<NonEmptyTree<T>>`, `IsLeaf : bool`, `Count : int`, `Depth : int`

**Traversal**

- `Flatten()`, `FlattenPostOrder()`, `FlattenBreadthFirst()`, `Leaves()` : `NonEmptyList<T>`
- `PathsToLeaves() : NonEmptyList<NonEmptyList<T>>`, `Levels() : NonEmptyList<NonEmptyList<T>>`
- `Fold<TResult>(Func<T, IReadOnlyList<TResult>, TResult> folder) : TResult`
- `Find(Func<T, bool> predicate) : NonEmptyTree<T>?`, `PathTo(Func<T, bool> predicate) : NonEmptyList<T>?`
- `Any(Func<T, bool> predicate) : bool`, `All(Func<T, bool> predicate) : bool`

**Transformation** (each returns a new tree)

- `Map<TResult>(Func<T, TResult> selector) : NonEmptyTree<TResult>`
- `MapWithDepth<TResult>(Func<T, int, TResult> selector) : NonEmptyTree<TResult>`
- `PruneChildren(Func<T, bool> keep) : NonEmptyTree<T>`
- `AddChild(NonEmptyTree<T> child) : NonEmptyTree<T>`, `AddChild(T value) : NonEmptyTree<T>`
- `WithValue(T value) : NonEmptyTree<T>`

**Other**

- `Deconstruct(out T value, out IReadOnlyList<NonEmptyTree<T>> children) : void`
- `ToOutline(string indent = "  ") : string`
- `Equals(NonEmptyTree<T>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `ToString() : string`

### `NonEmptyTree` (static)

Namespace: `Masterly.NonEmptyList`. Guide: [Modeling types](./modeling-types.md).

- `static Leaf<T>(T value) : NonEmptyTree<T>`
- `static Create<T>(T value, params NonEmptyTree<T>[] children) : NonEmptyTree<T>`
- `static Create<T>(T value, IEnumerable<NonEmptyTree<T>> children) : NonEmptyTree<T>`
- `static Create<T>(T value, IEnumerable<T> leafChildren) : NonEmptyTree<T>`
- `static Unfold<T>(T root, Func<T, IEnumerable<T>> children) : NonEmptyTree<T>`
- `static FromParentKeys<T, TKey>(IEnumerable<T> rows, Func<T, TKey> keySelector, Func<T, TKey?> parentKeySelector) : NonEmptyList<NonEmptyTree<T>>?`: two overloads, one for `TKey : class` and one for `TKey : struct`. Rows whose parent key is `null` become roots. Returns the roots, or `null` when there are none.

### `ConstrainedList<T, TConstraint>`

An immutable `NonEmptyList<T>` whose contents also satisfy `TConstraint : IListConstraint`, checked once at construction. Every mutating member inherited from `NonEmptyList<T>` throws `NotSupportedException`. Supports collection expressions and System.Text.Json. Namespace: `Masterly.NonEmptyList`. Guide: [Constrained lists](./constrained-lists.md).

- `static Description : string`: the constraint text, such as "at least 2 elements".
- `static Satisfies(IEnumerable<T>? source) : bool`
- `static new From(IEnumerable<T> source) : ConstrainedList<T, TConstraint>`
- `static new FromSpan(ReadOnlySpan<T> items) : ConstrainedList<T, TConstraint>`
- `static TryFrom(IEnumerable<T>? source, [NotNullWhen(true)] out ConstrainedList<T, TConstraint>? result) : bool`
- `static Validate(IEnumerable<T>? source, string? path = null) : Validation<ConstrainedList<T, TConstraint>, ValidationError>`
- `Relax() : NonEmptyList<T>`: a mutable copy.
- `As<TOther>() : ConstrainedList<T, TOther>`, `TryAs<TOther>([NotNullWhen(true)] out ConstrainedList<T, TOther>? result) : bool` where `TOther : IListConstraint`
- `WithAppended(T item)`, `WithPrepended(T item)`, `WithItem(int index, T item)`, `WithoutAt(int index)` : `ConstrainedList<T, TConstraint>` (re-checked copies)
- `ToString() : string`

### `ConstrainedList` (static)

Namespace: `Masterly.NonEmptyList`. Guide: [Constrained lists](./constrained-lists.md). All methods have `TConstraint : IListConstraint`.

- `static Create<T, TConstraint>(ReadOnlySpan<T> items) : ConstrainedList<T, TConstraint>`: the collection builder.
- `static From<T, TConstraint>(IEnumerable<T> source) : ConstrainedList<T, TConstraint>`
- `static TryFrom<T, TConstraint>(IEnumerable<T>? source, [NotNullWhen(true)] out ConstrainedList<T, TConstraint>? result) : bool`

### `ConstrainedListExtensions`

Namespace: `Masterly.NonEmptyList`. Guide: [Constrained lists](./constrained-lists.md).

- `ToConstrainedList<T, TConstraint>(this IEnumerable<T> source) : ConstrainedList<T, TConstraint>`
- `Constrain<T, TConstraint>(this NonEmptyList<T> source) : ConstrainedList<T, TConstraint>`
- `Second<T, TCount>(this ConstrainedList<T, AtLeast<TCount>> list) : T` where `TCount : ICardinality, IAtLeastTwo`
- `Third<T, TCount>(this ConstrainedList<T, AtLeast<TCount>> list) : T` where `TCount : ICardinality, IAtLeastThree`
- `Second<T, TCount, TOther>(this ConstrainedList<T, And<AtLeast<TCount>, TOther>> list) : T` where `TCount : ICardinality, IAtLeastTwo`
- `Third<T, TCount, TOther>(this ConstrainedList<T, And<AtLeast<TCount>, TOther>> list) : T` where `TCount : ICardinality, IAtLeastThree`
- `Second<T, TCount>(this ConstrainedList<T, Exactly<TCount>> list) : T` where `TCount : ICardinality, IAtLeastTwo`
- `Third<T, TCount>(this ConstrainedList<T, Exactly<TCount>> list) : T` where `TCount : ICardinality, IAtLeastThree`
- `RestAfterSecond<T, TCount>(this ConstrainedList<T, AtLeast<TCount>> list) : List<T>` where `TCount : ICardinality, IAtLeastTwo`

### Constraints and cardinalities

The type-level vocabulary for `ConstrainedList<T, TConstraint>`. All are `readonly struct` types except the interfaces. Namespace: `Masterly.NonEmptyList`. Guide: [Constrained lists](./constrained-lists.md).

| Type | Purpose |
|------|---------|
| `IListConstraint` | A constraint: `static abstract Holds<T>(ReadOnlySpan<T> items) : bool` and `static abstract Describe() : string`. Implement it to write your own. |
| `ICardinality` | A type-level number: `static abstract Value : int`. |
| `IAtLeastTwo` / `IAtLeastThree` | Markers on cardinalities of two or more / three or more. They enable `Second()` / `Third()`. |
| `NonEmpty` | The trivial constraint: an immutable non-empty list. |
| `AtLeast<TCount>` / `AtMost<TCount>` / `Exactly<TCount>` | Count bounds, with `TCount : ICardinality`. |
| `Distinct` | No duplicates, by `EqualityComparer<T>.Default`. |
| `Sorted` / `SortedDescending` | Non-decreasing / non-increasing order, by `Comparer<T>.Default`. |
| `And<TFirst, TSecond>` / `Or<TFirst, TSecond>` | Both constraints / at least one of them. |
| `One`, `Two`, `Three`, `Four`, `Five`, `Six`, `Seven`, `Eight`, `Nine`, `Ten`, `Sixteen`, `ThirtyTwo`, `SixtyFour`, `Hundred`, `Thousand` | Cardinalities. `Two` and up implement `IAtLeastTwo`; `Three` and up implement `IAtLeastThree`. |

Each constraint struct exposes `static Holds<T>(ReadOnlySpan<T> items) : bool` and `static Describe() : string`; each cardinality exposes `static Value : int`.

### `Validation<T, TError>`

Either a valid `T` or a `NonEmptyList<TError>` of errors. An invalid result with no errors cannot exist. Implements `IEquatable<Validation<T, TError>>`. Namespace: `Masterly.NonEmptyList`. Guide: [Validation](./validation.md).

**Construction**

- `static Valid(T value) : Validation<T, TError>`
- `static Invalid(TError error)`, `static Invalid(TError first, params TError[] rest)`, `static Invalid(NonEmptyList<TError> errors)` : `Validation<T, TError>`
- `implicit operator Validation<T, TError>(T value)`

**State**

- `IsValid : bool`, `IsInvalid : bool`
- `Value : T`: throws `InvalidOperationException` when invalid.
- `Errors : NonEmptyList<TError>`: throws `InvalidOperationException` when valid.
- `TryGetValue([MaybeNullWhen(false)] out T value) : bool`
- `TryGetErrors([MaybeNullWhen(false)] out NonEmptyList<TError> errors) : bool`
- `GetValueOrDefault(T fallback) : T`, `GetValueOr(Func<NonEmptyList<TError>, T> fallback) : T`
- `GetValueOrThrow(Func<NonEmptyList<TError>, Exception> exceptionFactory) : T`

**Composition**

- `Match<TResult>(Func<T, TResult> valid, Func<NonEmptyList<TError>, TResult> invalid) : TResult`
- `Match(Action<T> valid, Action<NonEmptyList<TError>> invalid) : void`
- `Map<TResult>(Func<T, TResult> selector) : Validation<TResult, TError>`
- `MapErrors<TNewError>(Func<TError, TNewError> selector) : Validation<T, TNewError>`
- `Bind<TResult>(Func<T, Validation<TResult, TError>> binder) : Validation<TResult, TError>`
- `Ensure(Func<T, bool> predicate, TError error) : Validation<T, TError>`, `Ensure(Func<T, bool> predicate, Func<T, TError> errorFactory) : Validation<T, TError>`
- `Tap(Action<T> action) : Validation<T, TError>`, `TapErrors(Action<NonEmptyList<TError>> action) : Validation<T, TError>`

**Equality**

- `Equals(Validation<T, TError>? other) : bool`, `Equals(object? obj) : bool`, `GetHashCode() : int`, `operator ==`, `operator !=`, `ToString() : string`

### `Validation` (static)

Factories and combinators. Namespace: `Masterly.NonEmptyList`. Guide: [Validation](./validation.md).

- `static Valid<T, TError>(T value)`, `static Invalid<T, TError>(TError error)`, `static Invalid<T, TError>(NonEmptyList<TError> errors)` : `Validation<T, TError>`
- `static Ensure<T, TError>(T value, Func<T, bool> predicate, TError error) : Validation<T, TError>`
- `static NotNull<T, TError>(T? value, TError error) : Validation<T, TError>` where `T : class`
- `static HasValue<T, TError>(T? value, TError error) : Validation<T, TError>` where `T : struct`
- `static NotBlank<TError>(string? value, TError error) : Validation<string, TError>`
- `static NonEmpty<T, TError>(IEnumerable<T>? source, TError error) : Validation<NonEmptyList<T>, TError>`
- `static Combine<T1, T2, TResult, TError>(Validation<T1, TError> v1, Validation<T2, TError> v2, Func<T1, T2, TResult> selector) : Validation<TResult, TError>`, with overloads for 3 to 8 inputs. Errors of every failed input are accumulated.
- `Sequence<T, TError>(this NonEmptyList<Validation<T, TError>> validations) : Validation<NonEmptyList<T>, TError>`
- `Sequence<T, TError>(this IEnumerable<Validation<T, TError>> validations) : Validation<List<T>, TError>`
- `Traverse<T, TResult, TError>(this NonEmptyList<T> source, Func<T, Validation<TResult, TError>> validator) : Validation<NonEmptyList<TResult>, TError>`
- `Traverse<T, TResult, TError>(this IEnumerable<T> source, Func<T, Validation<TResult, TError>> validator) : Validation<List<TResult>, TError>`

### `ValidationError`

`sealed record ValidationError(string Code, string Message, string? Path = null)`: a general-purpose error. Overrides `ToString()`. Namespace: `Masterly.NonEmptyList`. Guide: [Validation](./validation.md).

### `ValidationErrorExtensions`

Namespace: `Masterly.NonEmptyList`. Guide: [Validation](./validation.md).

- `ToErrorDictionary(this NonEmptyList<ValidationError> errors) : Dictionary<string, string[]>`: grouped by `Path`, the shape ASP.NET Core problem details use.

### `NonEmptyCollectionAttribute`

Marks a `partial` class for the bundled source generator, which turns it into a domain collection deriving from `NonEmptyList<T>`. Namespace: `Masterly.NonEmptyList`. Guide: [Domain collections](./domain-collections.md).

- `NonEmptyCollectionAttribute(Type elementType)`
- `ElementType : Type`
- `Distinct : bool` (reject duplicates), `MinCount : int` (default 1, cannot be lower), `MaxCount : int` (0 means no limit), `Immutable : bool` (reject every mutation)

The generated class gets two constructors, `From`, `Of`, `Create`, `TryFrom`, `Satisfies` and `Validate` factories, the constants `MinCount`, `MaxCount`, `IsDistinct` and `IsImmutable`, a static `Description`, a nested `JsonConverter`, `[CollectionBuilder]`, `[DebuggerDisplay]`, `ToString()`, and overrides of the protected validation hooks. See [Domain collections](./domain-collections.md).

### Converters

Converters that the core types apply through attributes. You rarely use them directly. Namespace: `Masterly.NonEmptyList`. Guide: [Serialization](./serialization.md).

| Type | Purpose |
|------|---------|
| `NonEmptyListJsonConverterFactory` / `NonEmptyListJsonConverter<T>` | System.Text.Json for `NonEmptyList<T>`. Reads a JSON array; an empty array fails. |
| `ImmutableNonEmptyListJsonConverterFactory` / `ImmutableNonEmptyListJsonConverter<T>` | System.Text.Json for `ImmutableNonEmptyList<T>`. |
| `NonEmptySetJsonConverterFactory` / `NonEmptySetJsonConverter<T>` | System.Text.Json for `NonEmptySet<T>`. |
| `NonEmptyDictionaryJsonConverterFactory` / `NonEmptyDictionaryJsonConverter<TKey, TValue>` | System.Text.Json for `NonEmptyDictionary<TKey, TValue>`. Key handling matches `Dictionary<TKey, TValue>`. |
| `ConstrainedListJsonConverterFactory` / `ConstrainedListJsonConverter<T, TConstraint>` | System.Text.Json for `ConstrainedList<T, TConstraint>`. Checks the constraint on read. |
| `NonEmptyStringJsonConverter` | System.Text.Json for `NonEmptyString`, including use as a dictionary key (`ReadAsPropertyName` / `WriteAsPropertyName`). |
| `NonEmptyListTypeConverter` | `TypeConverter` between a comma-separated string and `NonEmptyList<T>`. Used by MVC model binding. |
| `NonEmptyStringTypeConverter` | `TypeConverter` for `NonEmptyString`, used by MVC and configuration binding. |

Each factory overrides `CanConvert(Type)` and `CreateConverter(Type, JsonSerializerOptions)`; each converter overrides `Read` and `Write`.

## Masterly.NonEmptyList.EntityFrameworkCore

Namespace: `Masterly.NonEmptyList.EntityFrameworkCore`. Depends on Microsoft.EntityFrameworkCore 8, 9 or 10, matching the target framework. Guide: [Entity Framework Core](./entity-framework-core.md).

### `DbContextOptionsBuilderExtensions`

Registers the interceptors. Each method has a `DbContextOptionsBuilder` overload and a `DbContextOptionsBuilder<TContext>` overload (`TContext : DbContext`), and returns the same builder.

- `UseNonEmptyList()`: adds both interceptors below. The recommended call.
- `UseNonEmptyListValidation()`: adds only `NonEmptyListSaveChangesInterceptor`.
- `UseNonEmptyListQueryTranslation()`: adds only `NonEmptyListQueryExpressionInterceptor`.

### `NonEmptyListQueryExpressionInterceptor`

An `IQueryExpressionInterceptor` that rewrites NonEmptyList LINQ operators back to `Enumerable` before EF Core compiles a query, and rewrites the `Count`, `Head`, `First` and `Last` properties to `Count()`, `First()` and `Last()`.

- `QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData) : Expression`
- `static Rewrite(Expression expression) : Expression`: the rewrite on its own, for tests and custom pipelines.

### `NonEmptyListSaveChangesInterceptor`

A `SaveChangesInterceptor` that checks NonEmptyList navigations of added and modified entities before saving.

- `SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) : InterceptionResult<int>`
- `SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) : ValueTask<InterceptionResult<int>>`

### `NonEmptyListExtensions`

Property and relationship configuration on `PropertyBuilder` and `EntityTypeBuilder`. Relationship methods have `TEntity : class` and `TRelated : class` (or `TLeft`/`TRight : class`).

**JSON column conversion**

- `HasNonEmptyListConversion<T>(this PropertyBuilder<NonEmptyList<T>>)`, with overloads taking `JsonSerializerOptions`, `IEqualityComparer<T> elementComparer`, or both. Returns `PropertyBuilder<NonEmptyList<T>>`.
- `HasImmutableNonEmptyListConversion<T>(this PropertyBuilder<ImmutableNonEmptyList<T>>)`, with overloads taking `JsonSerializerOptions` or `IEqualityComparer<T> elementComparer`. Returns `PropertyBuilder<ImmutableNonEmptyList<T>>`.

**One-to-many**

- `HasNonEmptyMany<TEntity, TRelated>(string navigationPropertyName) : CollectionNavigationBuilder<TEntity, TRelated>`
- `HasNonEmptyMany<TEntity, TRelated>(Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression) : CollectionNavigationBuilder<TEntity, TRelated>`
- `HasNonEmptyMany<TEntity, TRelated>(string navigationPropertyName, string foreignKeyPropertyName) : ReferenceCollectionBuilder<TEntity, TRelated>`
- `HasNonEmptyManyWithOne<TEntity, TRelated>(...) : ReferenceCollectionBuilder<TEntity, TRelated>`: five overloads. The navigation is a property name or an `Expression<Func<TEntity, NonEmptyList<TRelated>?>>`; the inverse navigation is an `Expression<Func<TRelated, TEntity?>>`, with an optional foreign key `Expression<Func<TRelated, object?>>`. A fifth overload takes the navigation, inverse navigation and foreign key as three property names.
- `HasNonEmptyManyWithDeleteBehavior<TEntity, TRelated>(navigationExpression, inverseNavigationExpression, [foreignKeyExpression,] DeleteBehavior deleteBehavior) : ReferenceCollectionBuilder<TEntity, TRelated>`
- `HasRequiredNonEmptyMany<TEntity, TRelated>(navigationExpression, Expression<Func<TRelated, TEntity>> inverseNavigationExpression, [foreignKeyExpression]) : ReferenceCollectionBuilder<TEntity, TRelated>`

**Many-to-many**

- `HasNonEmptyManyToMany<TEntity, TRelated>(Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression) : CollectionNavigationBuilder<TEntity, TRelated>`
- `HasNonEmptyManyToMany<TLeft, TRight>(leftNavigationExpression, Expression<Func<TRight, NonEmptyList<TLeft>?>> rightNavigationExpression) : CollectionCollectionBuilder<TRight, TLeft>`
- `HasNonEmptyManyToMany<TLeft, TRight, TJoin>(leftNavigationExpression, rightNavigationExpression, Action<EntityTypeBuilder<TJoin>> configureJoinEntity) : EntityTypeBuilder<TLeft>`
- `HasNonEmptyManyToMany<TLeft, TRight>(leftNavigationExpression, string rightNavigationPropertyName) : CollectionCollectionBuilder<TRight, TLeft>`
- `HasNonEmptyManyToMany<TLeft, TRight>(string leftNavigationPropertyName, string rightNavigationPropertyName) : CollectionCollectionBuilder<TRight, TLeft>`
- `HasNonEmptyManyToManyWithJoinTable<TLeft, TRight>(leftNavigationExpression, rightNavigationExpression, string joinTableName, string leftForeignKeyName, string rightForeignKeyName) : EntityTypeBuilder<TLeft>`

### `NonEmptyListModelBuilderExtensions`

Model-wide conventions. Call after all entity types are configured. Each returns the `ModelBuilder`.

- `ApplyNonEmptyListConventions(this ModelBuilder)`: JSON conversion for every `NonEmptyList<T>` and `ImmutableNonEmptyList<T>` property.
- `ApplyNonEmptyListConventionsOnly(this ModelBuilder)`: only `NonEmptyList<T>` properties.
- `ApplyImmutableNonEmptyListConventionsOnly(this ModelBuilder)`: only `ImmutableNonEmptyList<T>` properties.

### `NonEmptyListQueryExtensions`

Materialize an `IQueryable<T>` into a non-empty list.

- `ToNonEmptyList<T>(this IQueryable<T>) : NonEmptyList<T>`, `ToNonEmptyListAsync<T>(this IQueryable<T>, CancellationToken cancellationToken = default) : Task<NonEmptyList<T>>`: throw for an empty result.
- `TryToNonEmptyList<T>(this IQueryable<T>, out NonEmptyList<T>? result) : bool`
- `TryToNonEmptyListAsync<T>(this IQueryable<T>, CancellationToken cancellationToken = default) : Task<(bool Success, NonEmptyList<T>? Result)>`
- `ToNonEmptyListOrNull<T>(this IQueryable<T>) : NonEmptyList<T>?`, `ToNonEmptyListOrNullAsync<T>(this IQueryable<T>, CancellationToken cancellationToken = default) : Task<NonEmptyList<T>?>`
- `ToImmutableNonEmptyList<T>(this IQueryable<T>) : ImmutableNonEmptyList<T>`, `ToImmutableNonEmptyListAsync<T>(...) : Task<ImmutableNonEmptyList<T>>`
- `ToImmutableNonEmptyListOrNull<T>(this IQueryable<T>) : ImmutableNonEmptyList<T>?`, `ToImmutableNonEmptyListOrNullAsync<T>(...) : Task<ImmutableNonEmptyList<T>?>`

### Value converters and comparers

| Type | Purpose |
|------|---------|
| `NonEmptyListValueConverter<T>` | `ValueConverter<NonEmptyList<T>, string>`: stores the list as JSON. Constructors: `()` and `(JsonSerializerOptions? jsonSerializerOptions)`. |
| `ImmutableNonEmptyListValueConverter<T>` | The same for `ImmutableNonEmptyList<T>`. |
| `NonEmptyListValueComparer<T>` | `ValueComparer<NonEmptyList<T>>` for change tracking. Constructors: `()` and `(IEqualityComparer<T> elementComparer)`. |
| `ImmutableNonEmptyListValueComparer<T>` | The same for `ImmutableNonEmptyList<T>`. |

## Masterly.NonEmptyList.AspNetCore

Namespace: `Masterly.NonEmptyList.AspNetCore`. References the ASP.NET Core shared framework. Guide: [ASP.NET Core](./aspnetcore.md).

:::note

Binding `NonEmptyList<T>`, `NonEmptySet<T>` and `NonEmptyString` from query strings and route values needs only the core package. This package adds OpenAPI documentation and HTTP results for `Validation`.

:::

### `NonEmptyOpenApiSchemaTransformer`

An `IOpenApiSchemaTransformer` that documents the guarantee in the OpenAPI document: `minItems`, `maxItems`, `uniqueItems`, `minProperties` for dictionaries and `minLength: 1` for `NonEmptyString`. Available on .NET 9 and .NET 10 only.

- `TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) : Task`
- `static Apply(OpenApiSchema schema, Type type) : void`

### `NonEmptyOpenApiExtensions`

Available on .NET 9 and .NET 10 only.

- `AddNonEmptySchemas(this OpenApiOptions options) : OpenApiOptions`: use as `builder.Services.AddOpenApi(o => o.AddNonEmptySchemas())`.

### `NonEmptyShape`

`sealed record NonEmptyShape(Type ElementType, bool IsDictionary, int MinItems, int? MaxItems, bool UniqueItems)`: what a non-empty type guarantees about its cardinality, derived from the type alone.

- `static Of(Type type) : NonEmptyShape?`: `null` for a type that is not a non-empty collection.

### `ValidationHttpExtensions`

Turn a `Validation<T, ValidationError>` into an HTTP response. Invalid results become 400 validation problem details grouped by `ValidationError.Path`.

**Minimal APIs**

- `ToProblemDetails(this NonEmptyList<ValidationError> errors, string? title = null, int status = StatusCodes.Status400BadRequest) : HttpValidationProblemDetails`
- `ToValidationProblem(this NonEmptyList<ValidationError> errors, string? title = null) : IResult`
- `ToResult<T>(this Validation<T, ValidationError>, Func<T, IResult> onValid) : IResult`
- `ToResult<T>(this Validation<T, ValidationError>) : IResult`: 200 OK with the value.
- `ToCreatedResult<T>(this Validation<T, ValidationError>, Func<T, string> location) : IResult`: 201 Created.
- `ToResultAsync<T>(this Task<Validation<T, ValidationError>>, Func<T, IResult> onValid) : Task<IResult>`
- `ToResultAsync<T>(this Task<Validation<T, ValidationError>>) : Task<IResult>`

**MVC**

- `ToActionResult<T>(this Validation<T, ValidationError>, Func<T, ActionResult> onValid) : ActionResult`
- `ToActionResult<T>(this Validation<T, ValidationError>) : ActionResult`: `OkObjectResult` or `BadRequestObjectResult`.
- `AddErrors(this ModelStateDictionary modelState, NonEmptyList<ValidationError> errors) : ModelStateDictionary`
- `TryGetValue<T>(this Validation<T, ValidationError>, ModelStateDictionary modelState, out T value) : bool`: adds the errors to the model state when invalid.

## Masterly.NonEmptyList.Configuration

Namespace: `Masterly.NonEmptyList.Configuration`. Depends on Microsoft.Extensions.Options.ConfigurationExtensions. Guide: [Configuration](./configuration.md).

### `NonEmptyConfigurationExtensions`

Read non-empty values from `IConfiguration` and bind options classes that contain them. Values can be arrays (`Hosts:0`, `Hosts:1`) or comma-separated strings.

- `GetNonEmptyList<T>(this IConfiguration configuration, string key) : NonEmptyList<T>`
- `GetNonEmptyListOrNull<T>(this IConfiguration configuration, string key) : NonEmptyList<T>?`: `null` when the key is absent.
- `GetNonEmptySet<T>(this IConfiguration configuration, string key) : NonEmptySet<T>`
- `GetNonEmptyDictionary<TKey, TValue>(this IConfiguration configuration, string key) : NonEmptyDictionary<TKey, TValue>` where `TKey : notnull`
- `GetConstrainedList<T, TConstraint>(this IConfiguration configuration, string key) : ConstrainedList<T, TConstraint>` where `TConstraint : IListConstraint`
- `GetNonEmptyString(this IConfiguration configuration, string key) : NonEmptyString`
- `GetNonEmpty<TOptions>(this IConfiguration configuration) : TOptions` where `TOptions : class`
- `BindNonEmpty(this IConfiguration configuration, object instance) : void`
- `BindNonEmptyConfiguration<TOptions>(this OptionsBuilder<TOptions> builder, string? sectionPath = null) : OptionsBuilder<TOptions>`: replaces `BindConfiguration`.
- `AddNonEmptyOptions<TOptions>(this IServiceCollection services, string sectionPath) : OptionsBuilder<TOptions>`: shorthand for `AddOptions<T>().BindNonEmptyConfiguration(path).ValidateOnStart()`.

### `NonEmptyConfigurationBinder`

The binder behind the extensions. It handles non-empty properties itself and delegates everything else to the default binder.

- `static Get<TOptions>(IConfiguration configuration) : TOptions` where `TOptions : class`
- `static Bind(IConfiguration configuration, object instance) : void`
- `static IsNonEmptyType(Type type) : bool`
- `static ReadNonEmpty(IConfigurationSection section, Type targetType) : object?`: `null` when the section has no value and no children.

## Masterly.NonEmptyList.NewtonsoftJson

Namespace: `Masterly.NonEmptyList.NewtonsoftJson`. Depends on Newtonsoft.Json. Guide: [Serialization](./serialization.md).

### `NonEmptyJsonConverter`

One Json.NET `JsonConverter` for `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, `NonEmptySet<T>`, `ConstrainedList<T, TConstraint>`, `NonEmptyDictionary<TKey, TValue>`, `NonEmptyString` and `[NonEmptyCollection]` classes.

- `CanConvert(Type objectType) : bool`
- `ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) : object?`
- `WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) : void`

### `NonEmptyJsonConverterExtensions`

- `AddNonEmptyConverters(this JsonSerializerSettings settings) : JsonSerializerSettings` (idempotent)
- `AddNonEmptyConverters(this JsonSerializer serializer) : JsonSerializer` (idempotent)
- `static SerializeNonEmpty(object? value, JsonSerializerSettings? settings = null) : string`
- `static DeserializeNonEmpty<T>(string json, JsonSerializerSettings? settings = null) : T?`
- `ToNonEmpty<T>(this JToken token) : T?`

## Masterly.NonEmptyList.MessagePack

Namespace: `Masterly.NonEmptyList.MessagePack`. Depends on MessagePack. Guide: [Serialization](./serialization.md).

### `NonEmptyResolver`

An `IFormatterResolver` for every non-empty type.

- `static Instance : NonEmptyResolver` (a static readonly field)
- `GetFormatter<T>() : IMessagePackFormatter<T>?`

### `NonEmptyMessagePackExtensions`

- `WithNonEmptyResolver(this MessagePackSerializerOptions options) : MessagePackSerializerOptions`: tries the non-empty formatters first, then the existing resolver.
- `static NonEmptyStandard : MessagePackSerializerOptions`: `MessagePackSerializerOptions.Standard` with the non-empty formatters.

### Formatters

Each formatter has `Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options) : void` and `Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) : T`, and rejects empty input.

| Type | Formats |
|------|---------|
| `NonEmptyListFormatter<T>` | `NonEmptyList<T>?` as an array. |
| `ImmutableNonEmptyListFormatter<T>` | `ImmutableNonEmptyList<T>?` as an array. |
| `NonEmptySetFormatter<T>` | `NonEmptySet<T>?` as an array. |
| `ConstrainedListFormatter<T, TConstraint>` | `ConstrainedList<T, TConstraint>?`; checks the constraint on read. |
| `DerivedNonEmptyListFormatter<TList, T>` | Classes deriving from `NonEmptyList<T>`, such as `[NonEmptyCollection]` classes; reads through the type's `From` factory. |
| `NonEmptyDictionaryFormatter<TKey, TValue>` | `NonEmptyDictionary<TKey, TValue>?` as a map. |
| `NonEmptyStringFormatter` | `NonEmptyString` as a string. |

## Masterly.NonEmptyList.Dapper

Namespace: `Masterly.NonEmptyList.Dapper`. Depends on Dapper. Guide: [Serialization](./serialization.md).

### `NonEmptyDapper`

Static registration helpers. Call once at startup; every method is idempotent.

- `static AddTypeHandlers<T>(JsonSerializerOptions? options = null) : void`: handlers for `NonEmptyList<T>`, `ImmutableNonEmptyList<T>` and `NonEmptySet<T>`.
- `static AddDictionaryHandler<TKey, TValue>(JsonSerializerOptions? options = null) : void` where `TKey : notnull`
- `static AddConstrainedHandler<T, TConstraint>(JsonSerializerOptions? options = null) : void` where `TConstraint : IListConstraint`
- `static AddTypeHandler<TCollection>(JsonSerializerOptions? options = null) : void` where `TCollection : class`: any non-empty collection type, including `[NonEmptyCollection]` classes.
- `static AddNonEmptyStringHandler() : void`
- `static AddTypeHandlersFor<TEntity>(JsonSerializerOptions? options = null) : void`: scans a type's properties and registers what it needs.

### `NonEmptyJsonTypeHandler<TCollection>`

A `SqlMapper.TypeHandler<TCollection>` (`TCollection : class`) that stores a collection as JSON text and reads it back through the type's `From` factory.

- `NonEmptyJsonTypeHandler(JsonSerializerOptions? options = null)`
- `SetValue(IDbDataParameter parameter, TCollection? value) : void`
- `Parse(object value) : TCollection?`

### `NonEmptyStringTypeHandler`

A `SqlMapper.TypeHandler<NonEmptyString>` that stores text and rejects blank values on read.

- `SetValue(IDbDataParameter parameter, NonEmptyString value) : void`
- `Parse(object value) : NonEmptyString`

## Masterly.NonEmptyList.Testing

Namespace: `Masterly.NonEmptyList.Testing`. Depends on CsCheck and AutoFixture. Guide: [Testing](./testing.md).

### `NonEmptyGen`

CsCheck generators. Every generator produces at least one element.

- `static List<T>(Gen<T> element, int maxCount = 20) : Gen<NonEmptyList<T>>`
- `static Immutable<T>(Gen<T> element, int maxCount = 20) : Gen<ImmutableNonEmptyList<T>>`
- `static Set<T>(Gen<T> element, int maxCount = 20) : Gen<NonEmptySet<T>>`
- `static Dictionary<TKey, TValue>(Gen<TKey> key, Gen<TValue> value, int maxCount = 20) : Gen<NonEmptyDictionary<TKey, TValue>>` where `TKey : notnull`
- `static Constrained<T, TConstraint>(Gen<T> element, int minCount = 1, int maxCount = 20) : Gen<ConstrainedList<T, TConstraint>>`: filters candidates with the constraint.
- `static Sorted<T>(Gen<T> element, int maxCount = 20) : Gen<ConstrainedList<T, Sorted>>`
- `static Distinct<T>(Gen<T> element, int maxCount = 20) : Gen<ConstrainedList<T, Distinct>>`
- `static String(int maxLength = 20) : Gen<NonEmptyString>`, `static String(string alphabet, int maxLength = 20) : Gen<NonEmptyString>`
- `static EventStream<TEvent>(Gen<TEvent> @event, int maxCount = 20) : Gen<EventStream<TEvent>>`
- `static Zipper<T>(Gen<T> element, int maxCount = 20) : Gen<NonEmptyZipper<T>>`
- `static Tree<T>(Gen<T> value, int maxDepth = 3, int maxChildren = 3) : Gen<NonEmptyTree<T>>`
- `static Validation<T, TError>(Gen<T> value, Gen<TError> error, int maxErrors = 5) : Gen<Validation<T, TError>>`

### `NonEmptyCustomization`

An AutoFixture `ICustomization`: `fixture.Customize(new NonEmptyCustomization())` lets the fixture create non-empty types, DTOs that contain them, `NonEmptyString` and `[NonEmptyCollection]` classes.

- `Count : int?` (init): elements per collection; defaults to the fixture's `RepeatCount`, at least 1.
- `Customize(IFixture fixture) : void`

### `NonEmptySpecimenBuilder`

The AutoFixture `ISpecimenBuilder` behind the customization.

- `NonEmptySpecimenBuilder(int? count = null)`
- `Create(object request, ISpecimenContext context) : object`

### `NonEmptyListLaws`

Static law checks for property-based tests. Each returns `true` when the law holds.

- Functor and monad laws: `MapIdentity<T>(xs)`, `MapComposition<T, TU, TV>(xs, f, g)`, `FlatMapLeftIdentity<T, TU>(x, f)`, `FlatMapRightIdentity<T>(xs)`, `FlatMapAssociativity<T, TU, TV>(xs, f, g)`
- Agreement with LINQ: `SelectMatchesMap<T, TU>(xs, f)`, `ReduceMatchesAggregate<T>(xs, op)`, `FoldMatchesAggregate<T, TAcc>(xs, seed, folder)`, `ScanLastEqualsFold<T, TAcc>(xs, seed, folder)`
- Structure: `HeadTailRoundTrip<T>(xs)`, `InitLastRoundTrip<T>(xs)`, `ReverseTwiceIsIdentity<T>(xs)`, `ConcatPreservesElements<T>(xs, ys)`, `ZipUnzipRoundTrip<T, TU>(xs, ys)`, `TailsShape<T>(xs)`, `InitsShape<T>(xs)`, `OrderByIsSortedPermutation<T>(xs)`, `DistinctKeepsFirstOccurrences<T>(xs)`, `EqualityIsStructural<T>(xs)`, `JsonRoundTrip<T>(xs)`
- `CheckAll<T>(NonEmptyList<T> xs) : IReadOnlyList<string>`: runs every law that needs no extra functions and returns the names of those that fail. An empty result means all hold.

Here `xs` and `ys` are `NonEmptyList<...>` and `f` / `g` are functions; every method returns `bool` except `CheckAll`.

## Analyzer diagnostics

The analyzers, code fixes and source generator ship inside the `Masterly.NonEmptyList` package. Their types are not part of the API you call. Change severities in `.editorconfig`, for example `dotnet_diagnostic.NEL005.severity = warning`. Guide: [Analyzers](./analyzers.md).

| Id | Title | Default severity | Code fix |
|----|-------|------------------|----------|
| NEL001 | Empty collection expression assigned to a non-empty type | Error | |
| NEL002 | Non-empty factory called with an empty argument | Error | |
| NEL003 | Use the total member instead of a throwing LINQ operator | Info | Use `Head`, `Last`, `Reduce` or `Count` |
| NEL004 | Redundant emptiness check on a non-empty list | Info | Replace with `true` or `false` |
| NEL005 | Sequence is not proven non-empty | Warning, disabled by default | Wrap the receiver in `ToNonEmptyList()` |
| NEL101 | `[NonEmptyCollection]` class must be partial | Error | |
| NEL102 | `[NonEmptyCollection]` class must not be nested | Error | |
| NEL103 | `[NonEmptyCollection]` has invalid bounds | Error | |
| NEL104 | `[NonEmptyCollection]` class must not declare a base type | Error | |

A migration code fix also attaches to the compiler errors CS1503, CS0029, CS0266 and CS1662 when a non-empty list is used where a `List<T>` is required. It offers `.ToList()`. See [Upgrading from 2.x](./upgrading-from-2x.md).
