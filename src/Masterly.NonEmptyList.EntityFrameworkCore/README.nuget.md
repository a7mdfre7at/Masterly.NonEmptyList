# Masterly.NonEmptyList.EntityFrameworkCore

Entity Framework Core integration for [Masterly.NonEmptyList](https://www.nuget.org/packages/Masterly.NonEmptyList). Provides value converters, comparers, and extension methods to seamlessly store and retrieve `NonEmptyList<T>` and `ImmutableNonEmptyList<T>` collections in your database.

## Installation

```bash
dotnet add package Masterly.NonEmptyList.EntityFrameworkCore
```

## Features

- **JSON Value Conversion**: Store `NonEmptyList<T>` as JSON strings in the database
- **Change Tracking**: Proper change detection with custom value comparers
- **Relationship Support**: Configure one-to-many and many-to-many relationships
- **Validation Interceptor**: Reject a null or empty non-empty property on save, including derived types such as `ConstrainedList`; properties declared nullable may be null
- **Query Translation**: Use NonEmptyList LINQ (`Select`, `OrderBy`, `Sum`, `Count`, `Head`, ...) inside EF Core projections
- **Query Extensions**: Convert query results to `NonEmptyList<T>`
- **Auto-Configuration**: Convention-based setup for all properties

## Quick Start

### Register once

```csharp
services.AddDbContext<ShopContext>(options =>
    options
        .UseSqlServer(connectionString)
        .UseNonEmptyList());   // SaveChanges validation + query translation
```

`UseNonEmptyList()` is required in v3 if you use LINQ on `NonEmptyList<T>` navigation properties inside queries. It rewrites the type-preserving operators back to `Enumerable` before EF Core compiles the query, so they translate exactly like `List<T>`.

```csharp
var summaries = await context.Orders
    .Select(o => new
    {
        o.Id,
        Total = o.Items.Sum(i => i.Price),
        Count = o.Items.Count,
        FirstSku = o.Items.Head.Sku,
        Names = o.Items.OrderBy(i => i.Price).Select(i => i.Name).ToList(),
    })
    .ToListAsync();
```

### JSON Value Conversion

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public NonEmptyList<string> Tags { get; set; } = new("default");
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .Property(p => p.Tags)
        .HasNonEmptyListConversion();
}
```

### One-to-Many Relationships

```csharp
modelBuilder.Entity<Order>()
    .HasNonEmptyManyWithOne(
        o => o.Items,
        i => i.Order,
        i => i.OrderId);
```

### Query Extensions

```csharp
NonEmptyList<Product> products = await context.Products
    .Where(p => p.IsActive)
    .ToNonEmptyListAsync();
```

## Caveat: Include on many-to-many navigations

`Include` works on one-to-many navigations configured with `HasNonEmptyManyWithOne`. On a many-to-many `NonEmptyList<T>` navigation it throws `InvalidOperationException`, because EF Core materializes the collection by creating it empty first. Query many-to-many data through a projection (`Select`) instead.

## Documentation

Full guide: [Entity Framework Core](https://a7mdfre7at.github.io/Masterly.NonEmptyList/docs/entity-framework-core).

## License

MIT
