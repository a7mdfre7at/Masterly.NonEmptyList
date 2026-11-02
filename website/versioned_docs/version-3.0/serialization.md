---
id: serialization
title: "Serialization"
description: "System.Text.Json support built into the core package, plus Newtonsoft.Json, MessagePack and Dapper packages that reject empty input while reading."
---

Every serializer integration follows the same rule: writing produces the natural shape (an array, an object or a string), and reading rejects input that would produce an empty value. An empty array, a `null` element or a blank string fails during deserialization with that serializer's own exception type, so the invalid value never reaches your code.

System.Text.Json support is built into the core package. The other serializers each have a small package.

| Type | System.Text.Json | Newtonsoft.Json | MessagePack | Dapper |
|------|------------------|-----------------|-------------|--------|
| `NonEmptyList<T>` | Built in | Yes | Yes | Yes |
| `ImmutableNonEmptyList<T>` | Built in | Yes | Yes | Yes |
| `NonEmptySet<T>` | Built in | Yes | Yes | Yes |
| `ConstrainedList<T, TConstraint>` | Built in | Yes | Yes | Yes |
| `NonEmptyDictionary<TKey, TValue>` | Built in | Yes | Yes | Yes |
| `NonEmptyString` | Built in | Yes | Yes | Yes |
| `[NonEmptyCollection]` classes | Generated converter | Yes | Yes | Yes |
| Error when reading bad input | `JsonException` | `JsonSerializationException` | `MessagePackSerializationException` | `DataException` |

Other types, such as `NonEmptyZipper<T>`, `NonEmptyTree<T>` and `EventStream<TEvent>`, have no dedicated converter in any of these packages.

## System.Text.Json

There is nothing to register. Each type carries a `[JsonConverter]` attribute that points at its converter, so `JsonSerializer`, ASP.NET Core and any other code that uses System.Text.Json pick it up automatically.

| Type | Converter | JSON shape |
|------|-----------|------------|
| `NonEmptyList<T>` | `NonEmptyListJsonConverterFactory`, creating `NonEmptyListJsonConverter<T>` | array |
| `ImmutableNonEmptyList<T>` | `ImmutableNonEmptyListJsonConverterFactory`, creating `ImmutableNonEmptyListJsonConverter<T>` | array |
| `NonEmptySet<T>` | `NonEmptySetJsonConverterFactory`, creating `NonEmptySetJsonConverter<T>` | array; duplicates collapse when reading |
| `ConstrainedList<T, TConstraint>` | `ConstrainedListJsonConverterFactory`, creating `ConstrainedListJsonConverter<T, TConstraint>` | array; the constraint is checked when reading |
| `NonEmptyDictionary<TKey, TValue>` | `NonEmptyDictionaryJsonConverterFactory`, creating `NonEmptyDictionaryJsonConverter<TKey, TValue>` | object; keys are handled exactly like `Dictionary<TKey, TValue>` |
| `NonEmptyString` | `NonEmptyStringJsonConverter` | string; also works as a dictionary key (property name) |
| `[NonEmptyCollection]` classes | A nested `JsonConverter` class that the source generator adds | array |

```csharp
using System.Text.Json;
using Masterly.NonEmptyList;

string json = JsonSerializer.Serialize(NonEmptyList.Of(1, 2, 3));                 // [1,2,3]
NonEmptyList<int>? back = JsonSerializer.Deserialize<NonEmptyList<int>>(json);

JsonSerializer.Deserialize<NonEmptyList<int>>("[]");
// JsonException: NonEmptyList cannot be empty
```

Reading throws `JsonException` when:

- the array (or object, for a dictionary) is empty;
- an element, or a dictionary value, is `null`;
- the token is not an array (`Expected start of array`), or for `NonEmptyString`, not a string;
- a `NonEmptyString` is empty or whitespace;
- a `ConstrainedList<T, TConstraint>` does not satisfy its constraint. The message names the expected constraint.

Elements are read and written with the same `JsonSerializerOptions`, so your naming policy and custom converters apply to them.

:::note

A JSON `null` in place of the whole collection deserializes to `null`, not to an exception. Declare such properties nullable, or check them, if your input can contain `null`.

:::

## Newtonsoft.Json

```bash
dotnet add package Masterly.NonEmptyList.NewtonsoftJson
```

One converter, `NonEmptyJsonConverter`, handles every non-empty type. Add it to your settings with `AddNonEmptyConverters()`:

```csharp
using Masterly.NonEmptyList;
using Masterly.NonEmptyList.NewtonsoftJson;
using Newtonsoft.Json;

// Order is your own class with non-empty properties; order is an instance of it
JsonSerializerSettings settings = new JsonSerializerSettings().AddNonEmptyConverters();

string json = JsonConvert.SerializeObject(order, settings);
Order? back = JsonConvert.DeserializeObject<Order>(json, settings);

JsonConvert.DeserializeObject<NonEmptyList<int>>("[]", settings);
// JsonSerializationException: NonEmptyList cannot be empty. Path '', line 1, position 2.
```

| Member | What it does |
|--------|--------------|
| `settings.AddNonEmptyConverters()` | Adds the converter to a `JsonSerializerSettings` and returns it. Calling it twice adds it once. |
| `serializer.AddNonEmptyConverters()` | The same for a `JsonSerializer`. |
| `NonEmptyJsonConverterExtensions.SerializeNonEmpty(value, settings)` | `JsonConvert.SerializeObject` with the converter added. `settings` is optional. |
| `NonEmptyJsonConverterExtensions.DeserializeNonEmpty<T>(json, settings)` | `JsonConvert.DeserializeObject<T>` with the converter added. |
| `token.ToNonEmpty<T>()` | Converts a `JToken` to a non-empty type. |

With ASP.NET Core MVC and `Microsoft.AspNetCore.Mvc.NewtonsoftJson`:

```csharp
builder.Services.AddControllers()
    .AddNewtonsoftJson(options => options.SerializerSettings.AddNonEmptyConverters());
```

Lists, sets and constrained lists are written as arrays, dictionaries as objects and `NonEmptyString` as a string. Reading throws `JsonSerializationException` for an empty array or object, a `null` element, a blank string, or a value the type's factory rejects, such as a list that breaks a `ConstrainedList` constraint. The exception's `Path`, `LineNumber` and `LinePosition` point at the offending value, and the message ends with the same location text Json.NET uses for its own errors.

The converter also covers classes that derive from these types and expose a static `From(IEnumerable<T>)` factory returning the derived type, which includes `[NonEmptyCollection]` classes. The list is rebuilt through that factory, so the class's own rules are checked again.

| Input | Target type | Result |
|-------|-------------|--------|
| `null` | `NonEmptyList<T>` and the other collection types | `null` |
| `null` | `NonEmptyString?` | `null` |
| `null` | `NonEmptyString` | `JsonSerializationException` |

:::note

Without the converter, Json.NET still rejects an empty array for `NonEmptyList<T>`, because it calls the `IEnumerable<T>` constructor, but you get the constructor's raw exception. It cannot create `NonEmptyString` or `NonEmptyDictionary<TKey, TValue>` at all. Register the converter.

:::

## MessagePack

```bash
dotnet add package Masterly.NonEmptyList.MessagePack
```

The package targets MessagePack-CSharp 3. Add `NonEmptyResolver` in front of your existing resolver with `WithNonEmptyResolver()`:

```csharp
using MessagePack;
using Masterly.NonEmptyList.MessagePack;

// Order is your own [MessagePackObject] class; order is an instance of it
MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithNonEmptyResolver();
// or the ready-made NonEmptyMessagePackExtensions.NonEmptyStandard

byte[] bytes = MessagePackSerializer.Serialize(order, options);
Order back = MessagePackSerializer.Deserialize<Order>(bytes, options);
```

`WithNonEmptyResolver()` builds a composite resolver that tries `NonEmptyResolver.Instance` first and then the resolver the options already had, so your other formatters keep working. `NonEmptyMessagePackExtensions.NonEmptyStandard` is `MessagePackSerializerOptions.Standard.WithNonEmptyResolver()`.

| Type | Formatter | MessagePack shape |
|------|-----------|-------------------|
| `NonEmptyList<T>` | `NonEmptyListFormatter<T>` | array |
| `ImmutableNonEmptyList<T>` | `ImmutableNonEmptyListFormatter<T>` | array |
| `NonEmptySet<T>` | `NonEmptySetFormatter<T>` | array |
| `ConstrainedList<T, TConstraint>` | `ConstrainedListFormatter<T, TConstraint>` | array; the constraint is checked when reading |
| `NonEmptyDictionary<TKey, TValue>` | `NonEmptyDictionaryFormatter<TKey, TValue>` | map |
| `NonEmptyString`, `NonEmptyString?` | `NonEmptyStringFormatter` | string |
| Classes that derive from `NonEmptyList<T>`, including `[NonEmptyCollection]` classes | `DerivedNonEmptyListFormatter<TList, T>` | array; rebuilt through the class's static `From(IEnumerable<T>)` |

Reading throws `MessagePackSerializationException` for an empty array or map, a `null` element or dictionary value, a blank string, or a constraint violation. A `nil` in place of a collection reads as `null`.

### MessagePack source generation

:::warning

MessagePack-CSharp's own source generator does not work for `[MessagePackObject]` types that have members of the non-empty generic types. Mark those types with `SuppressSourceGeneration = true` so they are serialized at runtime, with this package's formatters:

```csharp
[MessagePackObject(SuppressSourceGeneration = true)]
public sealed class OrderMessage
{
    [Key(0)] public NonEmptyList<int> Numbers { get; set; } = NonEmptyList.Of(0);
    [Key(1)] public NonEmptyString Title { get; set; } = new("untitled");
    [Key(2)] public NonEmptyList<int>? Optional { get; set; }
}
```

This is the only workaround. Registering the formatters with `MessagePackKnownFormatter` does not help.

:::

## Dapper

```bash
dotnet add package Masterly.NonEmptyList.Dapper
```

The Dapper handlers store a non-empty collection as JSON text and a `NonEmptyString` as plain text. Register them once at startup:

```csharp
using Masterly.NonEmptyList;
using Masterly.NonEmptyList.Dapper;

NonEmptyDapper.AddTypeHandlers<string>();                   // NonEmptyList<string>, ImmutableNonEmptyList<string>, NonEmptySet<string>
NonEmptyDapper.AddDictionaryHandler<string, int>();         // NonEmptyDictionary<string, int>
NonEmptyDapper.AddConstrainedHandler<int, AtLeast<Two>>();  // ConstrainedList<int, AtLeast<Two>>
NonEmptyDapper.AddNonEmptyStringHandler();                  // NonEmptyString and NonEmptyString?
NonEmptyDapper.AddTypeHandler<OrderLines>();                // any non-empty class, such as a [NonEmptyCollection] class
NonEmptyDapper.AddTypeHandlersFor<ProductRow>();            // everything the properties of ProductRow need
```

`AddTypeHandlersFor<TEntity>()` scans the public properties of a type and registers a handler for each non-empty collection type and for `NonEmptyString`. Every registration is idempotent, so calling the same method twice is harmless. Each collection method takes an optional `JsonSerializerOptions` that controls how elements are written.

Then use Dapper as usual:

```csharp
public sealed class ProductRow
{
    public long Id { get; set; }
    public NonEmptyString Name { get; set; }
    public NonEmptyList<string>? Tags { get; set; }
}

// connection is an open IDbConnection; row and id come from your code
connection.Execute("INSERT INTO Products (Id, Name, Tags) VALUES (@Id, @Name, @Tags)", row);
ProductRow back = connection.QuerySingle<ProductRow>("SELECT * FROM Products WHERE Id = @id", new { id });
NonEmptyList<string> tags = connection.QuerySingle<NonEmptyList<string>>("SELECT Tags FROM Products WHERE Id = @id", new { id });
```

A column value is plain JSON, such as `["a","b"]`, so it stays readable and you can query it with your database's JSON functions. Writing sets the parameter to `DbType.String`, or to `DBNull` for a `null` collection.

| Column value | Result when reading |
|--------------|---------------------|
| `NULL` in a collection column | `null`, so declare the property nullable |
| `[]`, empty or whitespace text in a collection column | `DataException` naming the type |
| Invalid JSON, or a constraint violation, in a collection column | `DataException` naming the type, wrapping the `JsonException` |
| Empty or whitespace text in a `NonEmptyString` column | `DataException` |

The collection handler is `NonEmptyJsonTypeHandler<TCollection>` and uses System.Text.Json, so it supports exactly the types listed in the System.Text.Json section. The text handler is `NonEmptyStringTypeHandler`. You can register either with `SqlMapper.AddTypeHandler` yourself.
