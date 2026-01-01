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
- **Validation Interceptor**: Ensure non-empty constraints on save
- **Query Extensions**: Convert query results to `NonEmptyList<T>`
- **Auto-Configuration**: Convention-based setup for all properties

## Quick Start

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

## Documentation

For full documentation, see the [Wiki](https://github.com/a7mdfre7at/Masterly.NonEmptyList/wiki/EF-Core-Integration).

## License

MIT
