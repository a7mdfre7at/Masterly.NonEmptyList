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
- **EF Core Integration**: Entity Framework Core support via separate package
- **Equality Support**: Implements `IEquatable<T>` with `==` and `!=` operators
- **Multi-targeting**: Supports .NET 6.0 and .NET 8.0

## Installation

```bash
dotnet add package Masterly.NonEmptyList
```

Or via Package Manager Console:
```
Install-Package Masterly.NonEmptyList
```

## Quick Start

```csharp
using Masterly.NonEmptyList;

// Create a NonEmptyList - guaranteed to have at least one element
NonEmptyList<int> numbers = new(1, 2, 3, 4, 5);

Console.WriteLine(numbers.Head);  // 1
Console.WriteLine(numbers.Last);  // 5
Console.WriteLine(numbers.Tail);  // [2, 3, 4, 5]

// Functional operations
NonEmptyList<string> strings = numbers.Map(x => x.ToString());
int sum = numbers.Reduce((a, b) => a + b);

// Pattern matching
string result = numbers.Match(
    single: x => $"Just {x}",
    multiple: (head, tail) => $"Head: {head}, Count: {tail.Count + 1}"
);

// Safe from empty collection errors
int first = numbers.Head;  // Always safe - no exceptions!
```

## Documentation

For comprehensive documentation, see the **[Wiki](../../wiki)**.

### Quick Links

| Topic | Description |
|-------|-------------|
| [Getting Started](../../wiki/Getting-Started) | Installation and basic usage |
| [Core Concepts](../../wiki/Core-Concepts) | Head, Tail, and properties |
| [Creating NonEmptyList](../../wiki/Creating-NonEmptyList) | Constructors and factory methods |
| [Functional Operations](../../wiki/Functional-Operations) | Map, FlatMap, Reduce, Fold, Zip |
| [Pattern Matching](../../wiki/Pattern-Matching) | Match and deconstruction |
| [Collection Operations](../../wiki/Collection-Operations) | Concat, Reverse, Sliding, Chunks |
| [Async Operations](../../wiki/Async-Operations) | MapAsync, FilterAsync, FoldAsync |
| [ImmutableNonEmptyList](../../wiki/ImmutableNonEmptyList) | Thread-safe immutable variant |
| [JSON Serialization](../../wiki/JSON-Serialization) | System.Text.Json support |
| [EF Core Integration](../../wiki/EF-Core-Integration) | Entity Framework Core support |

## EF Core Integration

For Entity Framework Core support, install the separate package:

```bash
dotnet add package Masterly.NonEmptyList.EntityFrameworkCore
```

```csharp
// Store NonEmptyList as JSON
modelBuilder.Entity<Product>()
    .Property(p => p.Tags)
    .HasNonEmptyListConversion();

// Configure relationships
modelBuilder.Entity<Order>()
    .HasNonEmptyManyWithOne(o => o.Items, i => i.Order);
```

See the [EF Core Integration wiki page](../../wiki/EF-Core-Integration) for full documentation.

## License

MIT

**Free Software, Hell Yeah!**
