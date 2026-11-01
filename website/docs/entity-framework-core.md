---
id: entity-framework-core
title: "Entity Framework Core"
description: "Store non-empty lists as JSON columns or relationships, translate NonEmptyList LINQ inside queries, and validate on SaveChanges."
---

`Masterly.NonEmptyList.EntityFrameworkCore` lets EF Core store and query `NonEmptyList<T>` and `ImmutableNonEmptyList<T>`. A list of values can live in one JSON column. A list of entities is an ordinary one-to-many or many-to-many relationship. An interceptor makes the type-preserving LINQ operators of `NonEmptyList<T>` translate inside queries, and another one checks the lists before `SaveChanges`.

```bash
dotnet add package Masterly.NonEmptyList.EntityFrameworkCore
```

```csharp
using Masterly.NonEmptyList;
using Masterly.NonEmptyList.EntityFrameworkCore;
```

## Register the interceptors

Call `UseNonEmptyList()` on the options builder. It adds both interceptors.

```csharp title="Program.cs"
builder.Services.AddDbContext<ShopContext>(options => options
    .UseSqlServer(connectionString)
    .UseNonEmptyList());
```

| Method | Adds |
|--------|------|
| `UseNonEmptyList()` | `NonEmptyListSaveChangesInterceptor` and `NonEmptyListQueryExpressionInterceptor`. The recommended single call. |
| `UseNonEmptyListValidation()` | Only the SaveChanges interceptor. |
| `UseNonEmptyListQueryTranslation()` | Only the query interceptor. |

Each method has an overload for `DbContextOptionsBuilder` and for `DbContextOptionsBuilder<TContext>`, and returns the same builder for chaining.

## Lists of values as a JSON column

Use `HasNonEmptyListConversion()` for a list of simple values, or of small objects such as addresses, that should live in one column.

```csharp title="ShopContext.cs"
// Product is your entity: public NonEmptyList<string> Tags { get; set; } = new("default");
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .Property(p => p.Tags)
        .HasNonEmptyListConversion();
}
```

The column holds a JSON array as a string, such as `["red","large"]`. The conversion uses System.Text.Json. Reading a column that is empty, whitespace or `[]` throws `InvalidOperationException`, so an empty list can never be materialized.

The conversion also sets a value comparer. It compares lists element by element, in order, and takes a snapshot copy when the entity is loaded. Changes made in place, such as `product.Tags.Add("new")`, are therefore detected and saved.

| Overload | Use it to |
|----------|-----------|
| `HasNonEmptyListConversion()` | Use the default JSON options and `EqualityComparer<T>.Default`. |
| `HasNonEmptyListConversion(JsonSerializerOptions jsonSerializerOptions)` | Control how elements are written, for example camel-case property names. |
| `HasNonEmptyListConversion(IEqualityComparer<T> elementComparer)` | Decide when two elements count as equal for change tracking, for example case-insensitive strings. |
| `HasNonEmptyListConversion(JsonSerializerOptions jsonSerializerOptions, IEqualityComparer<T> elementComparer)` | Both. |

For `ImmutableNonEmptyList<T>` properties, use `HasImmutableNonEmptyListConversion()`. It has the first three overloads.

```csharp
// Product also has: public ImmutableNonEmptyList<int> Sizes { get; set; } = ImmutableNonEmptyList.Of(1);
modelBuilder.Entity<Product>(entity =>
{
    entity.Property(p => p.Tags).HasNonEmptyListConversion(StringComparer.OrdinalIgnoreCase);
    entity.Property(p => p.Sizes).HasImmutableNonEmptyListConversion();
});
```

The converters and comparers are public classes, so you can also use them with the standard EF Core API: `NonEmptyListValueConverter<T>`, `NonEmptyListValueComparer<T>`, `ImmutableNonEmptyListValueConverter<T>` and `ImmutableNonEmptyListValueComparer<T>`.

### Configure every property at once

`ApplyNonEmptyListConventions()` finds every `NonEmptyList<T>` and `ImmutableNonEmptyList<T>` property in the model and applies the JSON conversion and comparer with default settings. Call it after the entity types are configured.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>();
    modelBuilder.Entity<Warehouse>();

    modelBuilder.ApplyNonEmptyListConventions();
}
```

Properties whose element type is an entity type are skipped, because they are relationships. `ApplyNonEmptyListConventionsOnly()` handles only `NonEmptyList<T>` properties, and `ApplyImmutableNonEmptyListConventionsOnly()` only `ImmutableNonEmptyList<T>` properties.

## Lists of entities as relationships

A `NonEmptyList<TRelated>` navigation can be the collection side of a relationship. The helpers are thin wrappers over `HasMany`, `WithOne` and `WithMany` that accept the `NonEmptyList<T>` navigation.

```csharp title="ShopContext.cs"
// Order:     public NonEmptyList<OrderItem> Items { get; set; } = null!;
// OrderItem: public int OrderId { get; set; }  public Order Order { get; set; } = null!;
modelBuilder.Entity<Order>()
    .HasNonEmptyManyWithOne(o => o.Items, i => i.Order, i => i.OrderId);
```

| Method | Configures | Returns |
|--------|------------|---------|
| `HasNonEmptyMany(o => o.Items)` or `HasNonEmptyMany<TEntity, TRelated>("Items")` | The collection side only | `CollectionNavigationBuilder<TEntity, TRelated>` |
| `HasNonEmptyMany<TEntity, TRelated>("Items", "OrderId")` | No inverse navigation, foreign key by name | `ReferenceCollectionBuilder<TEntity, TRelated>` |
| `HasNonEmptyManyWithOne(o => o.Items, i => i.Order)` | With the inverse navigation | `ReferenceCollectionBuilder<TEntity, TRelated>` |
| `HasNonEmptyManyWithOne(o => o.Items, i => i.Order, i => i.OrderId)` | With the inverse navigation and foreign key | `ReferenceCollectionBuilder<TEntity, TRelated>` |
| `HasNonEmptyManyWithOne<TEntity, TRelated>("Items", i => i.Order)`, and with a foreign key expression | The same, navigation by name | `ReferenceCollectionBuilder<TEntity, TRelated>` |
| `HasNonEmptyManyWithOne<TEntity, TRelated>("Items", "Order", "OrderId")` | Everything by name | `ReferenceCollectionBuilder<TEntity, TRelated>` |
| `HasNonEmptyManyWithDeleteBehavior(o => o.Items, i => i.Order, DeleteBehavior.Cascade)`, optionally with a foreign key expression | Adds `OnDelete` | `ReferenceCollectionBuilder<TEntity, TRelated>` |
| `HasRequiredNonEmptyMany(o => o.Items, i => i.Order)`, optionally with a foreign key expression | Adds `IsRequired()`, so the foreign key is not nullable | `ReferenceCollectionBuilder<TEntity, TRelated>` |

For many-to-many relationships:

| Method | Configures | Returns |
|--------|------------|---------|
| `HasNonEmptyManyToMany(s => s.Courses)` | The left side only | `CollectionNavigationBuilder<TEntity, TRelated>` |
| `HasNonEmptyManyToMany(s => s.Courses, c => c.Students)` | Both sides are `NonEmptyList<T>` | `CollectionCollectionBuilder<TRight, TLeft>` |
| `HasNonEmptyManyToMany(s => s.Courses, "Students")` | The right side is any collection, by name | `CollectionCollectionBuilder<TRight, TLeft>` |
| `HasNonEmptyManyToMany<TLeft, TRight>("Courses", "Students")` | Both sides by name | `CollectionCollectionBuilder<TRight, TLeft>` |
| `HasNonEmptyManyToMany<TLeft, TRight, TJoin>(s => s.Courses, c => c.Students, join => ...)` | A join entity type you configure | `EntityTypeBuilder<TLeft>` |
| `HasNonEmptyManyToManyWithJoinTable(s => s.Courses, c => c.Students, "StudentCourses", "StudentId", "CourseId")` | A named join table and foreign key columns | `EntityTypeBuilder<TLeft>` |

The expression overloads accept only a plain property access, such as `o => o.Items`. Anything else throws `ArgumentException`.

### Loading related entities

EF Core fills a collection navigation by creating an empty collection and adding to it. A `NonEmptyList<T>` cannot be empty, so `Include` on a `NonEmptyList<T>` navigation throws `InvalidOperationException` when EF Core has to create the list itself. The repository's tests show this for a many-to-many navigation loaded into a fresh context.

Saving works: create the entity with its list filled in, add it, and call `SaveChanges`. To read, project what you need. With the query interceptor registered, the navigation can be used directly inside `Select`:

```csharp
var summaries = await context.Orders
    .Select(o => new
    {
        o.Id,
        ItemCount = o.Items.Count,
        FirstProduct = o.Items.Head.ProductName,
        Total = o.Items.Sum(i => i.Price),
        Names = o.Items.OrderBy(i => i.Price).Select(i => i.ProductName).ToList(),
    })
    .ToListAsync();
```

## Query translation

`NonEmptyList<T>` has its own LINQ operators, such as a `Select` that returns `NonEmptyList<TResult>` and a `Sum` built on generic math (see [LINQ](./linq.md)). C# picks them for a `NonEmptyList<T>` navigation, but EF Core does not know them and cannot translate the query. Without the query interceptor, a projection like `o.Items.Select(i => i.ProductName).ToList()` throws `InvalidOperationException`.

`NonEmptyListQueryExpressionInterceptor` runs before EF Core compiles each query and rewrites the expression tree:

| You write | EF Core receives |
|-----------|------------------|
| `o.Items.Count` | `o.Items.Count()` |
| `o.Items.Head`, `o.Items.First` | `o.Items.First()` |
| `o.Items.Last` | `o.Items.Last()` |
| `o.Items.Select(...)`, `OrderBy`, `ThenBy`, `Sum`, `Max`, `Min` and other operators from `NonEmptyListLinqExtensions` and `NonEmptyListMathExtensions` | The `Enumerable` method of the same name |

The rewrite applies only to an operator that has an `Enumerable` method with the same name and compatible parameters. Anything else, such as the instance methods `Map` and `Reduce`, reaches EF Core unchanged and fails to translate as usual. Use the LINQ names inside queries.

When a rewritten expression is assigned to a member or constructor parameter of type `NonEmptyList<T>`, the interceptor wraps it in `ToNonEmptyList()`. That conversion runs on the client after the subquery is evaluated, so you can project straight into your own types:

```csharp
public sealed record OrderSummary(string OrderNumber, NonEmptyList<string> ProductNames);

OrderSummary summary = context.Orders
    .Where(o => o.Id == id)
    .Select(o => new OrderSummary(o.OrderNumber, o.Items.Select(i => i.ProductName)))
    .Single();
```

`NonEmptyListQueryExpressionInterceptor.Rewrite(expression)` exposes the same rewrite for tests and custom query pipelines. It returns the original instance when nothing changed.

## Materializing query results

These extension methods on `IQueryable<T>` turn a query result into a non-empty list:

| Method | Empty result |
|--------|--------------|
| `ToNonEmptyList()`, `ToNonEmptyListAsync(cancellationToken)` | Throws `InvalidOperationException` |
| `ToNonEmptyListOrNull()`, `ToNonEmptyListOrNullAsync(cancellationToken)` | Returns `null` |
| `TryToNonEmptyList(out NonEmptyList<T>? result)` | Returns `false` |
| `TryToNonEmptyListAsync(cancellationToken)` | Returns `(Success: false, Result: null)` |
| `ToImmutableNonEmptyList()`, `ToImmutableNonEmptyListAsync(cancellationToken)` | Throws `InvalidOperationException` |
| `ToImmutableNonEmptyListOrNull()`, `ToImmutableNonEmptyListOrNullAsync(cancellationToken)` | Returns `null` |

```csharp
NonEmptyList<Product>? active = await context.Products
    .Where(p => p.IsActive)
    .ToNonEmptyListOrNullAsync(cancellationToken);
```

## SaveChanges validation

`NonEmptyListSaveChangesInterceptor` runs before `SaveChanges` and `SaveChangesAsync`. For every entity in the `Added` or `Modified` state, it reads each public property whose type is `NonEmptyList<T>` or derives from it (`ConstrainedList<T, TConstraint>`, `OrderedNonEmptyList<T>` and `[NonEmptyCollection]` classes), `ImmutableNonEmptyList<T>`, `NonEmptySet<T>` or `NonEmptyDictionary<TKey, TValue>`. It throws `InvalidOperationException` before anything is written if one is:

- `null` while the property is not declared nullable: "Non-empty property 'Items' on entity 'Order' cannot be null. Declare the property nullable if null is allowed; otherwise it must contain at least one element."
- empty: "Non-empty property 'Items' on entity 'Order' is empty. It must always contain at least one element."

This catches the case the type system cannot, such as a navigation initialized with `= null!` and never assigned.

A property declared nullable, such as `NonEmptyList<string>? Tags`, may be `null`: the interceptor reads the nullable annotation and skips it. Nullable reference types must be enabled in the entity's project for the annotation to exist; without them every such property counts as required.

## Provider support

The package depends only on `Microsoft.EntityFrameworkCore` (8.0.11 for .NET 8, 9.0.0 for .NET 9, 10.0.0 for .NET 10) and is not tied to a provider. The JSON column is a plain string column, and the relationship helpers produce ordinary relationships.

:::note

The package's own tests run against the EF Core InMemory provider. Query translation on a relational provider still goes through that provider's own rules for `First()`, `Last()` and the other operators. For example, a `Last()` over an unordered navigation may need an `OrderBy` first. Test your queries against the provider you ship with.

:::

For a complete order example that combines a relationship, validation and an API endpoint, see [Examples](./examples.md).
