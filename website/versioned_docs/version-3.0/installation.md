---
id: installation
title: "Installation"
description: "Install the core package and only the companion packages you need. Target frameworks, dependencies and namespaces."
---

Install the core package first. Then add only the companion packages you need. All eight packages ship together with the same version number, 3.0.1.

## Packages

| Package | What it is for | Target frameworks | Extra dependencies |
|---------|----------------|-------------------|--------------------|
| `Masterly.NonEmptyList` | The core: `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, type-preserving LINQ, generic math, `ConstrainedList<T, TConstraint>`, `Validation<T, TError>`, `NonEmptySet<T>`, `NonEmptyDictionary<TKey, TValue>`, `NonEmptyString`, batching, System.Text.Json converters, and the bundled analyzers and source generator | `net8.0`, `net9.0`, `net10.0` | None |
| `Masterly.NonEmptyList.EntityFrameworkCore` | Value converters, comparers, non-empty relationships, `SaveChanges` validation and query translation | `net8.0`, `net9.0`, `net10.0` | `Microsoft.EntityFrameworkCore` 8.0.11 on .NET 8, 9.0.0 on .NET 9, 10.0.0 on .NET 10 |
| `Masterly.NonEmptyList.AspNetCore` | OpenAPI schema transformer (`minItems`, `minLength`, `uniqueItems`), `Validation` to HTTP results and problem details, `ModelState` helpers | `net8.0`, `net9.0`, `net10.0` | The `Microsoft.AspNetCore.App` framework. `Microsoft.AspNetCore.OpenApi` 9.0.11 on .NET 9 and 10.0.2 on .NET 10; `Microsoft.OpenApi` 2.13.0 on .NET 10 |
| `Masterly.NonEmptyList.Configuration` | Binds non-empty collections from configuration and fails at startup, naming the key, when a required collection is empty or missing | `net8.0`, `net9.0`, `net10.0` | `Microsoft.Extensions.Options.ConfigurationExtensions` 8.0.0 on .NET 8, 9.0.0 on .NET 9, 10.0.0 on .NET 10 |
| `Masterly.NonEmptyList.NewtonsoftJson` | Json.NET converters for every non-empty type; empty arrays are rejected when reading | `net8.0`, `net9.0`, `net10.0` | `Newtonsoft.Json` 13.0.3 |
| `Masterly.NonEmptyList.MessagePack` | MessagePack-CSharp formatters and resolver; empty payloads are rejected when reading | `net8.0`, `net9.0`, `net10.0` | `MessagePack` 3.1.12 |
| `Masterly.NonEmptyList.Dapper` | Dapper type handlers that store collections as JSON text and `NonEmptyString` as text | `net8.0`, `net9.0`, `net10.0` | `Dapper` 2.1.66 |
| `Masterly.NonEmptyList.Testing` | CsCheck generators, an AutoFixture customization and a law suite for your test projects | `net8.0`, `net9.0`, `net10.0` | `CsCheck` 4.9.1, `AutoFixture` 4.18.1 |

Every companion package also depends on `Masterly.NonEmptyList` 3.0.1. The versions shown are the minimum versions each package references.

```bash
dotnet add package Masterly.NonEmptyList
dotnet add package Masterly.NonEmptyList.EntityFrameworkCore
dotnet add package Masterly.NonEmptyList.AspNetCore
dotnet add package Masterly.NonEmptyList.Configuration
dotnet add package Masterly.NonEmptyList.NewtonsoftJson
dotnet add package Masterly.NonEmptyList.MessagePack
dotnet add package Masterly.NonEmptyList.Dapper
dotnet add package Masterly.NonEmptyList.Testing
```

Or, in the Visual Studio Package Manager Console, run `Install-Package Masterly.NonEmptyList`, and the same for each companion package you need.

:::note

.NET 6 and .NET 7 are not supported. Version 3 targets .NET 8, .NET 9 and .NET 10 only. If you are still on .NET 6, stay on 2.x until you upgrade the runtime. See [Upgrading from 2.x](./upgrading-from-2x.md).

:::

:::note

The OpenAPI schema transformer in `Masterly.NonEmptyList.AspNetCore` is compiled only for .NET 9 and later, because it builds on `Microsoft.AspNetCore.OpenApi`. On .NET 8 the package still gives you the validation result and `ModelState` helpers. See [ASP.NET Core](./aspnetcore.md).

:::

## Analyzers and source generator

You do not install anything extra for the analyzers or the `[NonEmptyCollection]` source generator. Both ship inside the `Masterly.NonEmptyList` package and are active as soon as you reference it:

- The analyzers report diagnostics `NEL001` to `NEL005`, for example an empty collection expression assigned to a non-empty type, and offer code fixes.
- The source generator turns a `[NonEmptyCollection]` partial class into a domain collection that derives from `NonEmptyList<T>`.

The analyzer assemblies are packed for C# only. See [Analyzers and source generator](./analyzers.md) and [Domain collections](./domain-collections.md).

## Namespaces

Each package puts its public types in one namespace. Add the `using` directives for the packages you reference:

| Package | Namespace |
|---------|-----------|
| `Masterly.NonEmptyList` | `Masterly.NonEmptyList` |
| `Masterly.NonEmptyList.EntityFrameworkCore` | `Masterly.NonEmptyList.EntityFrameworkCore` |
| `Masterly.NonEmptyList.AspNetCore` | `Masterly.NonEmptyList.AspNetCore` |
| `Masterly.NonEmptyList.Configuration` | `Masterly.NonEmptyList.Configuration` |
| `Masterly.NonEmptyList.NewtonsoftJson` | `Masterly.NonEmptyList.NewtonsoftJson` |
| `Masterly.NonEmptyList.MessagePack` | `Masterly.NonEmptyList.MessagePack` |
| `Masterly.NonEmptyList.Dapper` | `Masterly.NonEmptyList.Dapper` |
| `Masterly.NonEmptyList.Testing` | `Masterly.NonEmptyList.Testing` |

Everything in the core package, including the LINQ, math, async and batching extension methods, lives in `Masterly.NonEmptyList`. One directive is enough:

```csharp
using Masterly.NonEmptyList;

NonEmptyList<int> numbers = [1, 2, 3];
```

To make it available in every file, add a global using to your project file:

```xml title="YourProject.csproj"
<ItemGroup>
  <Using Include="Masterly.NonEmptyList" />
</ItemGroup>
```

:::tip

Collection expressions such as `[1, 2, 3]` need C# 12 or later. That is the default language version for .NET 8 and later, so you do not need to change `LangVersion`.

:::
