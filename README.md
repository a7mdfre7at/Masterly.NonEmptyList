# Masterly.NonEmptyList
A simple and lightweight implementation of a non-empty list in C#, inspired by Scala's `List`, that ensures a collection always has at least one item. This helps in reducing null-related bugs and ensures safe operations on collections with at least one element.

<img src="https://raw.githubusercontent.com/a7mdfre7at/Masterly.NonEmptyList/master/repo_image.png" width="200" height="180">

[![Nuget](https://img.shields.io/nuget/v/Masterly.NonEmptyList?style=flat-square)](https://www.nuget.org/packages/Masterly.NonEmptyList) ![Nuget](https://img.shields.io/nuget/dt/Masterly.NonEmptyList?style=flat-square) ![GitHub last commit](https://img.shields.io/github/last-commit/a7mdfre7at/Masterly.NonEmptyList?style=flat-square) ![GitHub](https://img.shields.io/github/license/a7mdfre7at/Masterly.NonEmptyList?style=flat-square) [![Build](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/build.yml) [![CodeQL Analysis](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/codeql-analysis.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/codeql.yml) [![Publish to NuGet](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/publish.yml/badge.svg?branch=master)](https://github.com/a7mdfre7at/Masterly.NonEmptyList/actions/workflows/publish.yml)


## Give a Star! :star:

If you like or are using this project please give it a star. Thanks!

## Features

- Always contains at least one element.
- Provides `Head` and `Tail` access for list traversal.
- Supports indexing for element access.
- Basic list operations like `Add`, `Contains`, `Count`, and more.

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


## Usage

````csharp
// Create a NonEmptyList with a single element
var nonEmptyList = new NonEmptyList<int>(1);
Console.WriteLine(nonEmptyList.Head); // Output: 1

// Create a NonEmptyList with multiple elements
var nonEmptyListMultiple = new NonEmptyList<int>(1, 2, 3);
Console.WriteLine(nonEmptyListMultiple.Head);  // Output: 1
Console.WriteLine(nonEmptyListMultiple.Tail);  // Output: NonEmptyList: [2, 3]

// Add an element to the list
nonEmptyListMultiple.Add(4);
Console.WriteLine(nonEmptyListMultiple);  // Output: NonEmptyList: [1, 2, 3, 4]

// Access elements by index
Console.WriteLine(nonEmptyListMultiple[1]);  // Output: 2

// Check if the list contains a specific element
Console.WriteLine(nonEmptyListMultiple.Contains(3));  // Output: True
````
