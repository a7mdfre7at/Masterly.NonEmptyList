# Masterly.NonEmptyList.NewtonsoftJson

Json.NET (Newtonsoft.Json) support for [Masterly.NonEmptyList](https://www.nuget.org/packages/Masterly.NonEmptyList). One converter handles every non-empty type and rejects empty input while deserializing.

## Installation

```bash
dotnet add package Masterly.NonEmptyList.NewtonsoftJson
```

## Usage

```csharp
using Masterly.NonEmptyList.NewtonsoftJson;

JsonSerializerSettings settings = new JsonSerializerSettings().AddNonEmptyConverters();

string json = JsonConvert.SerializeObject(order, settings);
Order back = JsonConvert.DeserializeObject<Order>(json, settings)!;

JsonConvert.DeserializeObject<NonEmptyList<int>>("[]", settings);   // JsonSerializationException: NonEmptyList cannot be empty
```

ASP.NET Core MVC with Newtonsoft:

```csharp
builder.Services.AddControllers().AddNewtonsoftJson(o => o.SerializerSettings.AddNonEmptyConverters());
```

## What is covered

| Type | JSON shape |
|------|------------|
| `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, `NonEmptySet<T>`, `ConstrainedList<T, C>` | array, at least one element |
| `NonEmptyDictionary<TKey, TValue>` | object, at least one property |
| `NonEmptyString` | string with content |
| `[NonEmptyCollection]`-generated classes | array, every invariant re-checked |

Null elements, empty arrays, empty objects, blank strings and constraint violations all raise `JsonSerializationException` with the JSON path.

## License

MIT
