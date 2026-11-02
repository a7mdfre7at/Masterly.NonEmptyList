---
id: quick-start
title: "Quick Start"
description: "Create a non-empty list, read its first and last elements, transform it with LINQ and handle empty input. Your first working list in five minutes."
---

This example creates a list of scores, reads from it without any emptiness checks, transforms it with LINQ, and then shows what happens when the input is empty.

Install the package first:

```bash
dotnet add package Masterly.NonEmptyList
```

**1. Create a list.** Assign a collection expression to a `NonEmptyList<T>`, or call `NonEmptyList.Of` and let the compiler infer the element type. Both give you a list that holds at least one element.

```csharp title="Program.cs"
using Masterly.NonEmptyList;

NonEmptyList<int> scores = [72, 95, 88];

var names = NonEmptyList.Of("Ada", "Grace", "Linus"); // NonEmptyList<string>
```

**2. Read the first and last elements.** `Head` and `Last` are plain properties. They cannot throw, because the list is never empty, so you need no `Count` check and no `FirstOrDefault`. `Tail`, the elements after the head, is `null` when the list has a single element, so the compiler makes you handle that case.

```csharp title="Read safely"
int first = scores.Head; // 72
int last = scores.Last;  // 88

Console.WriteLine($"First: {first}, last: {last}");

if (scores.Tail is NonEmptyList<int> rest)
{
    Console.WriteLine($"After the head: {string.Join(", ", rest)}");
}
```

**3. Transform with LINQ.** Operators that can never produce an empty result, such as `Select`, `OrderBy` and `Distinct`, return a `NonEmptyList<T>` again. Aggregates such as `Max` and `Average` return a plain value, with no "sequence contains no elements" path. Operators that may produce an empty result, such as `Where`, return an ordinary `IEnumerable<T>`.

```csharp title="Type-preserving LINQ"
NonEmptyList<int> curved = scores.Select(s => s + 5); // still non-empty

int best = curved.Max();            // int, not int?
double average = curved.Average();  // never divides by zero

IEnumerable<int> passing = curved.Where(s => s >= 90); // may be empty

Console.WriteLine($"Curved: {string.Join(", ", curved)}");
Console.WriteLine($"Best: {best}, average: {average}");
Console.WriteLine($"Passing: {string.Join(", ", passing)}");
```

**4. Handle empty input.** Data from outside your code (a database, a request, a file) can be empty. Use `TryFrom` when empty input is a normal case you want to branch on. Use `ToNonEmptyList()` when empty input is a bug. It throws an `ArgumentException`.

```csharp title="Empty input"
// Pretend this came from a query that returned no rows.
List<int> fromDatabase = new();

if (NonEmptyList.TryFrom(fromDatabase, out NonEmptyList<int>? loaded))
{
    Console.WriteLine($"Loaded {loaded.Count} scores"); // loaded is not null here
}
else
{
    Console.WriteLine("No scores yet");
}

try
{
    NonEmptyList<int> required = fromDatabase.ToNonEmptyList();
    Console.WriteLine(required.Head);
}
catch (ArgumentException exception)
{
    Console.WriteLine(exception.Message);
}
```

Run the program. It prints:

```text title="Console output"
First: 72, last: 88
After the head: 95, 88
Curved: 77, 100, 93
Best: 100, average: 90
Passing: 100, 93
No scores yet
Cannot create a NonEmptyList from null or empty (Parameter 'enumerable')
```

:::tip

The bundled analyzer catches empty input you write in code. `NonEmptyList<int> none = [];` is reported as error `NEL001` at compile time, so it never reaches run time. See [Analyzers and source generator](./analyzers.md).

:::

**Next steps**

- For every way to build a list, and which ones throw or return `null`, see [Creating Lists](./creating-lists.md).
- For `Head`, `Last`, `Tail`, indexers, ranges and list patterns, see [Reading Elements](./reading-elements.md).
- For the LINQ operators that keep the non-empty type, see [Type-preserving LINQ](./linq.md).
- For what `Add`, `Remove` and `Clear` do on a list that must never be empty, see [Safe Mutation](./safe-mutation.md).
