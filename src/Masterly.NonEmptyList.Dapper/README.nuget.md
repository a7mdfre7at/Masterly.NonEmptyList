# Masterly.NonEmptyList.Dapper

Dapper type handlers for [Masterly.NonEmptyList](https://www.nuget.org/packages/Masterly.NonEmptyList). Non-empty collections are stored as JSON text columns and `NonEmptyString` as text; reading an empty or blank column fails instead of producing an empty value.

## Installation

```bash
dotnet add package Masterly.NonEmptyList.Dapper
```

## Usage

Register once at startup:

```csharp
using Masterly.NonEmptyList.Dapper;

NonEmptyDapper.AddTypeHandlers<string>();                  // NonEmptyList<string>, ImmutableNonEmptyList<string>, NonEmptySet<string>
NonEmptyDapper.AddDictionaryHandler<string, int>();        // NonEmptyDictionary<string, int>
NonEmptyDapper.AddConstrainedHandler<int, AtLeast<Two>>(); // ConstrainedList<int, AtLeast<Two>>
NonEmptyDapper.AddNonEmptyStringHandler();                 // NonEmptyString and NonEmptyString?
NonEmptyDapper.AddTypeHandlersFor<ProductRow>();           // scans a DTO and registers everything it needs
NonEmptyDapper.AddTypeHandler<OrderLines>();               // any [NonEmptyCollection]-generated class
```

Then use Dapper as usual:

```csharp
public sealed class ProductRow
{
    public long Id { get; set; }
    public NonEmptyString Name { get; set; }
    public NonEmptyList<string>? Tags { get; set; }
}

connection.Execute("INSERT INTO Products (Id, Name, Tags) VALUES (@Id, @Name, @Tags)", row);
ProductRow back = connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = @id", new { id });
```

Column values are plain JSON (`["a","b"]`), so they remain queryable with your database's JSON functions. Reading `[]`, whitespace or invalid JSON raises `DataException` naming the type. Nullable properties read `NULL` as `null`.

All registrations are idempotent. Pass `JsonSerializerOptions` to any `Add...` method to control element serialization.

## Documentation

Full guide: <https://a7mdfre7at.github.io/Masterly.NonEmptyList/docs/serialization>.

## License

MIT
