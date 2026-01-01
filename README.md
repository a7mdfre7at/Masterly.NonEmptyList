# Masterly.NonEmptyList
A simple and lightweight implementation of a non-empty list in C#, inspired by Scala's `List`, that ensures a collection always has at least one item. This helps in reducing null-related bugs and ensures safe operations on collections with at least one element.

<img src="https://raw.githubusercontent.com/a7mdfre7at/Masterly.NonEmptyList/master/repo_image.png" width="200" height="180">

[![Nuget](https://img.shields.io/nuget/v/Masterly.NonEmptyList?style=flat-square)](https://www.nuget.org/packages/Masterly.NonEmptyList) ![Nuget](https://img.shields.io/nuget/dt/Masterly.NonEmptyList?style=flat-square) ![GitHub last commit](https://img.shields.io/github/last-commit/a7mdfre7at/Masterly.NonEmptyList?style=flat-square) ![GitHub](https://img.shields.io/github/license/a7mdfre7at/Masterly.NonEmptyList) [![Build](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/build.yml) [![CodeQL Analysis](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/codeql.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/codeql.yml) [![Publish to NuGet](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/publish.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/publish.yml)


## Give a Star! :star:

If you like or are using this project please give it a star. Thanks!

## Features

- **Non-Empty Guarantee**: Always contains at least one element
- **Head/Tail Access**: Convenient `Head`, `Tail`, `Init`, `First`, and `Last` properties
- **Functional Operations**: `Map`, `FlatMap`, `Reduce`, `Fold`, `Zip`, `Partition`, and more
- **Pattern Matching**: `Match` method and deconstruction support
- **Async Support**: Full async extension methods for all operations
- **Immutable Variant**: `ImmutableNonEmptyList<T>` for thread-safe scenarios
- **JSON Serialization**: Built-in System.Text.Json support
- **Equality Support**: Implements `IEquatable<T>` with `==` and `!=` operators
- **Multi-targeting**: Supports .NET 6.0 and .NET 8.0

## Installation

Install the [Masterly.NonEmptyList NuGet Package](https://www.nuget.org/packages/Masterly.NonEmptyList).

### Package Manager Console

```
Install-Package Masterly.NonEmptyList
```

### .NET Core CLI

```
dotnet add package Masterly.NonEmptyList
```

## Quick Start

```csharp
using Masterly.NonEmptyList;

// Create a NonEmptyList with a single element
NonEmptyList<int> list = new(1);
Console.WriteLine(list.Head); // Output: 1

// Create a NonEmptyList with multiple elements
NonEmptyList<int> numbers = new(1, 2, 3, 4, 5);
Console.WriteLine(numbers.Head);  // Output: 1
Console.WriteLine(numbers.Last);  // Output: 5
Console.WriteLine(numbers.Tail);  // Output: NonEmptyList: [2, 3, 4, 5]
```

## Core Concepts

### Properties

| Property | Description |
|----------|-------------|
| `Head` | Gets the first element (same as `First`) |
| `First` | Gets the first element (same as `Head`) |
| `Last` | Gets the last element |
| `Tail` | Gets all elements except the first (returns `null` if only one element) |
| `Init` | Gets all elements except the last (returns `null` if only one element) |
| `Count` | Gets the number of elements |

### Creating NonEmptyList

```csharp
// Constructor with first element and optional additional elements
NonEmptyList<int> list1 = new(1);
NonEmptyList<int> list2 = new(1, 2, 3);

// From an existing enumerable
NonEmptyList<int> list3 = NonEmptyList<int>.From(new[] { 1, 2, 3 });

// Safe creation with TryFrom
if (NonEmptyList<int>.TryFrom(someEnumerable, out NonEmptyList<int>? result))
{
    // Use result
}

// Singleton (single element)
NonEmptyList<int> single = NonEmptyList<int>.Singleton(42);

// Repeat an element
NonEmptyList<int> repeated = NonEmptyList<int>.Repeat(7, 5); // [7, 7, 7, 7, 7]
```

## Functional Operations

### Map

Transform each element while preserving the non-empty guarantee:

```csharp
NonEmptyList<int> numbers = new(1, 2, 3);
NonEmptyList<string> strings = numbers.Map(x => x.ToString());
// Result: ["1", "2", "3"]

// With index
NonEmptyList<string> indexed = numbers.MapWithIndex((x, i) => $"{i}:{x}");
// Result: ["0:1", "1:2", "2:3"]
```

### FlatMap

Project each element to a NonEmptyList and flatten:

```csharp
NonEmptyList<int> numbers = new(1, 2, 3);
NonEmptyList<int> result = numbers.FlatMap(x => new NonEmptyList<int>(x, x * 10));
// Result: [1, 10, 2, 20, 3, 30]
```

### Reduce and Fold

Aggregate elements without or with a seed value:

```csharp
NonEmptyList<int> numbers = new(1, 2, 3, 4);

// Reduce - no seed needed since list is never empty
int sum = numbers.Reduce((a, b) => a + b); // 10

// ReduceRight - from right to left
string result = new NonEmptyList<string>("a", "b", "c")
    .ReduceRight((a, b) => $"({a}+{b})"); // "(a+(b+c))"

// Fold - with seed value
int foldSum = numbers.Fold(10, (acc, x) => acc + x); // 20

// FoldRight - from right with seed
string foldResult = numbers.FoldRight("", (x, acc) => acc + x.ToString()); // "4321"
```

### Zip

Combine two lists element by element:

```csharp
NonEmptyList<int> numbers = new(1, 2, 3);
NonEmptyList<string> letters = new("a", "b", "c");

// Zip into tuples
NonEmptyList<(int, string)> zipped = numbers.Zip(letters);
// Result: [(1, "a"), (2, "b"), (3, "c")]

// ZipWith - custom selector
NonEmptyList<string> combined = numbers.ZipWith(letters, (n, l) => $"{n}{l}");
// Result: ["1a", "2b", "3c"]

// ZipWithIndex
NonEmptyList<(int Item, int Index)> withIndex = numbers.ZipWithIndex();
// Result: [(1, 0), (2, 1), (3, 2)]
```

### Partition

Split into two groups based on a predicate:

```csharp
NonEmptyList<int> numbers = new(1, 2, 3, 4, 5, 6);
(List<int> even, List<int> odd) = numbers.Partition(x => x % 2 == 0);
// even: [2, 4, 6], odd: [1, 3, 5]
```

### GroupByNonEmpty

Group elements with NonEmptyList values:

```csharp
NonEmptyList<string> words = new("apple", "banana", "apricot", "blueberry");
Dictionary<char, NonEmptyList<string>> groups = words.GroupByNonEmpty(w => w[0]);
// 'a' -> ["apple", "apricot"], 'b' -> ["banana", "blueberry"]
```

## Pattern Matching

### Match Method

```csharp
NonEmptyList<int> list = new(1, 2, 3);

string result = list.Match(
    single: x => $"Single element: {x}",
    multiple: (head, tail) => $"Head: {head}, Tail count: {tail.Count}"
);
// Result: "Head: 1, Tail count: 2"
```

### Deconstruction

```csharp
NonEmptyList<int> list = new(1, 2, 3, 4, 5);

// Two-element deconstruction
(int head, NonEmptyList<int>? tail) = list;
// head: 1, tail: [2, 3, 4, 5]

// Three-element deconstruction
(int first, int second, IEnumerable<int> rest) = list;
// first: 1, second: 2, rest: [3, 4, 5]
```

## Collection Operations

### Concatenation

```csharp
NonEmptyList<int> list1 = new(1, 2);
NonEmptyList<int> list2 = new(3, 4);

NonEmptyList<int> combined = list1.Concat(list2);
// Result: [1, 2, 3, 4]
```

### Prepend

```csharp
NonEmptyList<int> list = new(2, 3);
list.Prepend(1);
// Result: [1, 2, 3]
```

### Reverse, Distinct, Take, Skip

```csharp
NonEmptyList<int> numbers = new(3, 1, 2, 1, 3);

NonEmptyList<int> reversed = numbers.ReverseList();     // [3, 1, 2, 1, 3]
NonEmptyList<int> distinct = numbers.DistinctList();    // [3, 1, 2]

NonEmptyList<int>? first2 = numbers.TakeNonEmpty(2);    // [3, 1]
NonEmptyList<int>? skip2 = numbers.SkipNonEmpty(2);     // [2, 1, 3]
NonEmptyList<int> atLeast = numbers.TakeAtLeastOne(0);  // [3] (ensures at least 1)
```

### DistinctBy

```csharp
NonEmptyList<string> words = new("apple", "apricot", "banana", "blueberry");
NonEmptyList<string> distinctByFirst = words.DistinctBy(w => w[0]);
// Result: ["apple", "banana"]
```

### Sliding Windows and Chunks

```csharp
NonEmptyList<int> numbers = new(1, 2, 3, 4, 5);

// Sliding windows
IEnumerable<NonEmptyList<int>> windows = numbers.Sliding(3);
// [[1,2,3], [2,3,4], [3,4,5]]

// With step
IEnumerable<NonEmptyList<int>> windowsStep2 = numbers.Sliding(2, step: 2);
// [[1,2], [3,4]]

// Chunks
IEnumerable<NonEmptyList<int>> chunks = numbers.ChunkNonEmpty(2);
// [[1,2], [3,4], [5]]
```

### Intersperse and Rotate

```csharp
NonEmptyList<int> numbers = new(1, 2, 3);

// Intersperse - insert separator between elements
NonEmptyList<int> interspersed = numbers.Intersperse(0);
// Result: [1, 0, 2, 0, 3]

// Rotate
NonEmptyList<int> rotatedLeft = numbers.RotateLeft(1);   // [2, 3, 1]
NonEmptyList<int> rotatedRight = numbers.RotateRight(1); // [3, 1, 2]
```

## Conversion Methods

```csharp
NonEmptyList<int> list = new(1, 2, 3);

int[] array = list.ToArrayNonEmpty();
HashSet<int> set = list.ToHashSet();
Queue<int> queue = list.ToQueue();
Stack<int> stack = list.ToStack();
LinkedList<int> linked = list.ToLinkedList();
IReadOnlyList<int> readOnly = list.AsReadOnly();

// ToDictionary
NonEmptyList<string> words = new("apple", "banana");
Dictionary<char, string> dict = words.ToDictionary(w => w[0]);
// {'a': "apple", 'b': "banana"}
```

## Utility Methods

```csharp
NonEmptyList<int> numbers = new(1, 2, 3, 4, 5);

// Random element
int random = numbers.Random();

// MinBy / MaxBy
NonEmptyList<string> words = new("hi", "hello", "hey");
string shortest = words.MinBy(w => w.Length); // "hi"
string longest = words.MaxBy(w => w.Length);  // "hello"

// ForEachWithIndex
numbers.ForEachWithIndex((item, index) => Console.WriteLine($"{index}: {item}"));
```

## Validation

```csharp
NonEmptyList<int> numbers = new(1, 2, 3, 4, 5);

// Validate - throws if any fail
NonEmptyList<int> validated = numbers.Validate(x => x > 0, "All numbers must be positive");

// TryValidate - returns false with failing element
if (!numbers.TryValidate(x => x < 5, out int? failingElement))
{
    Console.WriteLine($"Validation failed for: {failingElement}"); // 5
}
```

## Async Operations

All functional operations have async equivalents:

```csharp
NonEmptyList<int> numbers = new(1, 2, 3);

// MapAsync
NonEmptyList<string> results = await numbers.MapAsync(async x =>
{
    await Task.Delay(100);
    return x.ToString();
});

// MapParallelAsync - parallel execution
NonEmptyList<int> parallelResults = await numbers.MapParallelAsync(async x =>
{
    await Task.Delay(100);
    return x * 2;
});

// With degree of parallelism
NonEmptyList<int> throttled = await numbers.MapParallelAsync(
    async x => { await Task.Delay(100); return x * 2; },
    maxDegreeOfParallelism: 2
);

// FilterAsync
NonEmptyList<int>? filtered = await numbers.FilterAsync(async x =>
{
    await Task.Delay(10);
    return x % 2 == 0;
});

// FoldAsync, AllAsync, AnyAsync, FirstOrDefaultAsync...
bool allPositive = await numbers.AllAsync(async x => { await Task.Delay(1); return x > 0; });

// ToAsyncEnumerable
await foreach (int item in numbers.ToAsyncEnumerable())
{
    Console.WriteLine(item);
}
```

## ImmutableNonEmptyList

An immutable variant where all operations return new instances:

```csharp
ImmutableNonEmptyList<int> list = new(1, 2, 3);

// All modifications return new instances
ImmutableNonEmptyList<int> appended = list.Append(4);       // [1, 2, 3, 4]
ImmutableNonEmptyList<int> prepended = list.Prepend(0);     // [0, 1, 2, 3]
ImmutableNonEmptyList<int> updated = list.SetItem(1, 99);   // [1, 99, 3]
ImmutableNonEmptyList<int> inserted = list.Insert(1, 99);   // [1, 99, 2, 3]

// Remove operations return null if would become empty
ImmutableNonEmptyList<int>? removed = list.RemoveAt(0);     // [2, 3]
ImmutableNonEmptyList<int>? removedFirst = list.RemoveFirst(); // [2, 3]
ImmutableNonEmptyList<int>? removedLast = list.RemoveLast();   // [1, 2]

// Conversion
NonEmptyList<int> mutable = list.ToMutable();
ImmutableNonEmptyList<int> immutable = mutableList.ToImmutable();

// All functional operations available
ImmutableNonEmptyList<string> mapped = list.Map(x => x.ToString());
ImmutableNonEmptyList<int> reversed = list.Reverse();
ImmutableNonEmptyList<int> distinct = list.Distinct();
```

## JSON Serialization

Built-in support for System.Text.Json:

```csharp
using System.Text.Json;

NonEmptyList<int> list = new(1, 2, 3);

// Serialize
string json = JsonSerializer.Serialize(list);
// Output: [1,2,3]

// Deserialize
NonEmptyList<int> deserialized = JsonSerializer.Deserialize<NonEmptyList<int>>(json);

// Works with ImmutableNonEmptyList too
ImmutableNonEmptyList<int> immutable = new(1, 2, 3);
string immutableJson = JsonSerializer.Serialize(immutable);
```

## Equality

```csharp
NonEmptyList<int> list1 = new(1, 2, 3);
NonEmptyList<int> list2 = new(1, 2, 3);
NonEmptyList<int> list3 = new(1, 2, 4);

bool equal = list1.Equals(list2);     // true
bool notEqual = list1.Equals(list3);  // false

// Operators
bool op1 = list1 == list2;  // true
bool op2 = list1 != list3;  // true

// With custom comparer
bool caseInsensitive = new NonEmptyList<string>("A", "B")
    .SequenceEqual(new NonEmptyList<string>("a", "b"), StringComparer.OrdinalIgnoreCase);
// true
```

## Error Handling

The library throws appropriate exceptions to maintain the non-empty invariant:

```csharp
// Cannot create from empty
NonEmptyList<int>.From(Array.Empty<int>()); // throws ArgumentException

// Cannot clear
list.Clear(); // throws NotSupportedException

// Cannot remove last element
NonEmptyList<int> single = new(1);
single.Remove(1); // throws InvalidOperationException

// Cannot add null (for reference types)
NonEmptyList<string> strings = new("a");
strings.Add(null!); // throws ArgumentNullException
```

## Thread Safety

- `NonEmptyList<T>` is **not** thread-safe (same as `List<T>`)
- Use `ImmutableNonEmptyList<T>` for thread-safe scenarios
- Async operations are safe for concurrent reads but not concurrent modifications

## License

MIT

**Free Software, Hell Yeah!**
