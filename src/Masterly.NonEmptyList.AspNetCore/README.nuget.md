# Masterly.NonEmptyList.AspNetCore

ASP.NET Core integration for [Masterly.NonEmptyList](https://www.nuget.org/packages/Masterly.NonEmptyList): the non-empty guarantee shows up in your OpenAPI document, and `Validation<T, ValidationError>` turns into RFC 7807 problem details in one call.

## Installation

```bash
dotnet add package Masterly.NonEmptyList.AspNetCore
```

## Binding works without this package

`NonEmptyList<T>`, `NonEmptySet<T>` and `NonEmptyString` implement `TryParse` and ship a `TypeConverter`, so minimal APIs and MVC bind them from query strings and route values out of the box. An empty value is a 400 before your handler runs, and an empty JSON array in a request body is a 400 as well.

```csharp
app.MapGet("/products", (NonEmptyList<int> ids) => repo.GetByIds(ids));   // ?ids=1,2,3
app.MapGet("/hello/{name}", (NonEmptyString name) => $"Hello {name}");
```

## OpenAPI (.NET 9 and later)

```csharp
builder.Services.AddOpenApi(options => options.AddNonEmptySchemas());
```

| Type | Schema |
|------|--------|
| `NonEmptyList<T>`, `ImmutableNonEmptyList<T>` | `type: array, minItems: 1, items: T` |
| `NonEmptySet<T>` | `minItems: 1, uniqueItems: true` |
| `ConstrainedList<T, AtLeast<Two>>` and friends | `minItems` / `maxItems` / `uniqueItems` derived from the constraint |
| `NonEmptyDictionary<TKey, TValue>` | `type: object, minProperties: 1` |
| `NonEmptyString` | `type: string, minLength: 1` |
| `[NonEmptyCollection]` classes | bounds from `MinCount`, `MaxCount`, `IsDistinct` |

## Validation to HTTP

```csharp
app.MapPost("/orders", (CreateOrder request) =>
    Validation.Combine(
        Validation.NotBlank(request.Customer, new ValidationError("required", "Customer is required", "customer")),
        Validation.Ensure(request.Lines, l => l.All(x => x.Length > 0), new ValidationError("blank", "Lines cannot be blank", "lines")),
        (customer, lines) => new Order(customer, lines))
    .ToResult());                                   // 200 with the order, or 400 problem details
```

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "customer": ["Customer is required"], "lines": ["Lines cannot be blank"] },
  "codes": ["required", "blank"]
}
```

Also available: `ToResult(onValid)`, `ToCreatedResult(location)`, `ToResultAsync`, `ToValidationProblem()`, `ToProblemDetails()`, and for MVC `ToActionResult()`, `ModelState.AddErrors(errors)` and `validation.TryGetValue(ModelState, out value)`.

`NonEmptyShape.Of(type)` exposes the cardinality a type guarantees (minimum, maximum, uniqueness) for your own tooling.

## License

MIT
