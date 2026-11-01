---
id: aspnetcore
title: "ASP.NET Core"
description: "Bind non-empty types from query strings, routes and JSON bodies, document the guarantee in OpenAPI, and turn Validation results into problem details."
---

ASP.NET Core support comes in two parts. Binding is built into the core package, so `NonEmptyList<T>`, `NonEmptySet<T>` and `NonEmptyString` work as endpoint parameters with nothing else installed. The `Masterly.NonEmptyList.AspNetCore` package adds an OpenAPI schema transformer and helpers that turn a `Validation<T, ValidationError>` into an HTTP response.

## Binding without the package

`NonEmptyList<T>` and `NonEmptyString` implement `ISpanParsable<TSelf>` and carry a `TypeConverter` (`NonEmptyListTypeConverter` and `NonEmptyStringTypeConverter`). `NonEmptySet<T>` has static `Parse` and `TryParse` methods but no `TypeConverter`. That is enough for minimal APIs to bind all three from query strings and route values, and for MVC to bind `NonEmptyList<T>` and `NonEmptyString`.

```csharp title="Program.cs"
using Masterly.NonEmptyList;

// repository is your own service
app.MapGet("/products", (NonEmptyList<int> ids) => repository.GetByIds(ids));      // ?ids=1,2,3
app.MapGet("/tags", (NonEmptySet<string> tags) => tags.Count);                     // ?tags=a,a,b gives 2
app.MapGet("/hello/{name}", (NonEmptyString name) => $"Hello {name}");
```

A request that cannot produce a non-empty value never reaches your handler. Each of these returns 400 Bad Request:

| Request | Why |
|---------|-----|
| `/products?ids=` | The value is empty. |
| `/products` | The parameter is missing, and a non-nullable parameter is required. |
| `/products?ids=1,x` | `x` is not an `int`. |
| `/hello/%20%20` | A `NonEmptyString` cannot be whitespace. |

### The comma-separated format

`NonEmptyList<T>.TryParse` splits the text on commas and parses each piece:

- Each piece is trimmed, so `1, 2 ,3` is the same as `1,2,3`.
- An empty piece fails the whole parse: `1,,2` and `1,2,` are rejected, and so is whitespace-only input.
- There is no escaping. An element can never contain a comma.
- `NonEmptySet<T>` parses the same way, then drops duplicates.

Each element is parsed with the first of these that the element type supports:

1. `string` elements are used as they are.
2. `ISpanParsable<T>`, then `IParsable<T>`, with the format provider that ASP.NET Core passes in.
3. Enums, by name, ignoring case.
4. The element type's `TypeConverter`, if it can convert from `string`. It uses the provider when it is a `CultureInfo`, otherwise the current culture.

If none of them applies, or any element fails, `TryParse` returns `false` and the request is a 400. Outside ASP.NET Core you can call the same methods yourself: `NonEmptyList<int>.Parse("1,2,3")` throws `FormatException` on bad input and `TryParse` returns `false`.

### JSON request bodies

Request bodies go through System.Text.Json, and the non-empty types carry their own converters. An empty array, a `null` element, or a blank string for a `NonEmptyString` makes the converter throw `JsonException`, and ASP.NET Core answers 400. See [Serialization](./serialization.md) for the details of each converter.

```csharp
public sealed record Order(NonEmptyList<string> Lines, NonEmptyString Customer);

app.MapPost("/echo", (Order order) => Results.Ok(order));
// {"lines":[],"customer":"ada"}    -> 400
// {"lines":["a"],"customer":" "}   -> 400
```

### MVC controllers

MVC binds `NonEmptyList<T>` and `NonEmptyString` from query strings and route values with the same parsing rules. With `[ApiController]`, a failed bind produces an automatic 400 response. Without it, the action still runs, so check `ModelState.IsValid` yourself.

```csharp title="ProductsController.cs"
[ApiController]
[Route("products")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    public ActionResult<int> Get([FromQuery] NonEmptyList<int> ids) => ids.Count;     // ?ids=4,5

    [HttpGet("name/{name}")]
    public ActionResult<string> Name([FromRoute] NonEmptyString name) => name.Value.ToUpperInvariant();
}
```

:::note

Query-string binding of `NonEmptySet<T>` is tested for minimal APIs only. In an MVC action, bind a `NonEmptyList<T>` and turn it into a set with `NonEmptySet.From(list)`.

:::

## The Masterly.NonEmptyList.AspNetCore package

```bash
dotnet add package Masterly.NonEmptyList.AspNetCore
```

```csharp
using Masterly.NonEmptyList.AspNetCore;
```

The package targets .NET 8, 9 and 10. The validation helpers work on all three. The OpenAPI transformer needs .NET 9 or later, because it builds on `Microsoft.AspNetCore.OpenApi`.

## OpenAPI schemas

Register the transformer when you add the OpenAPI document:

```csharp title="Program.cs"
builder.Services.AddOpenApi(options => options.AddNonEmptySchemas());

WebApplication app = builder.Build();
app.MapOpenApi();
```

`AddNonEmptySchemas()` adds `NonEmptyOpenApiSchemaTransformer`. For every schema whose type is a non-empty type (or a nullable one, such as `NonEmptyString?`), it adds the cardinality keywords below. The non-empty types serialize through custom JSON converters, so the generator does not know their JSON shape. The transformer fills in `type` when it is missing, and on .NET 10 it also fills in `items` (for lists and sets) or `additionalProperties` (for dictionaries) with the element schema. On .NET 9 it sets only `type`.

| Type | Keywords added | `type` |
|------|----------------|--------|
| `NonEmptyList<T>`, `ImmutableNonEmptyList<T>` | `minItems: 1` | `array` |
| `NonEmptySet<T>` | `minItems: 1`, `uniqueItems: true` | `array` |
| `ConstrainedList<T, TConstraint>` | `minItems`, `maxItems` and `uniqueItems` derived from the constraint (see below) | `array` |
| `[NonEmptyCollection]` classes | `minItems` from `MinCount`, `maxItems` from `MaxCount` when the class has an upper bound, `uniqueItems: true` when `Distinct = true` | `array` |
| `NonEmptyDictionary<TKey, TValue>` | `minProperties: 1` | `object` |
| `NonEmptyString` | `minLength: 1` | `string` |

For `ConstrainedList<T, TConstraint>`, the constraint maps to keywords like this:

| Constraint | Keywords |
|------------|----------|
| `AtLeast<N>` | `minItems: N` |
| `AtMost<N>` | `minItems: 1`, `maxItems: N` |
| `Exactly<N>` | `minItems: N`, `maxItems: N` |
| `Distinct` | `minItems: 1`, `uniqueItems: true` |
| `And<A, B>` | The larger minimum, the smaller maximum, and `uniqueItems` if either side is distinct |
| `Or<A, B>` | The smaller minimum, a maximum only when both sides have one (the larger), and `uniqueItems` only if both sides are distinct |
| Anything else, such as `Sorted` | `minItems: 1` |

For example, `ConstrainedList<int, And<AtLeast<Two>, And<AtMost<Five>, Distinct>>>` is documented with `minItems: 2`, `maxItems: 5` and `uniqueItems: true`.

The transformer never loosens a bound that is already on the schema. It keeps the larger `minItems` and the smaller `maxItems`. If you build schemas yourself, call the same logic directly with `NonEmptyOpenApiSchemaTransformer.Apply(schema, type)`.

## Validation results as HTTP responses

`Validation<T, ValidationError>` collects every error instead of stopping at the first one (see [Validation](./validation.md)). The extension methods in `ValidationHttpExtensions` turn it into a response. Because the error list is a `NonEmptyList<ValidationError>`, an invalid result always has at least one error in its body.

```csharp title="Program.cs"
using Masterly.NonEmptyList;
using Masterly.NonEmptyList.AspNetCore;

public sealed record CreateOrder(NonEmptyList<string> Lines, string Customer);
public sealed record Order(NonEmptyList<string> Lines, NonEmptyString Customer);

app.MapPost("/orders", (CreateOrder request) =>
    Validation.Combine(
        Validation.Ensure(request.Lines, lines => lines.All(x => x.Length > 0),
            new ValidationError("blank", "Lines cannot be blank", "lines")),
        Validation.NotBlank(request.Customer,
            new ValidationError("required", "Customer is required", "customer")),
        (lines, customer) => new Order(lines, new NonEmptyString(customer)))
    .ToResult());   // 200 with the order, or 400 problem details
```

An invalid request gets a 400 with content type `application/problem+json`. Errors are grouped by `ValidationError.Path`, and the distinct error codes are added as a `codes` extension. The body looks like this (the standard `type` field is left out):

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "lines": ["Lines cannot be blank"],
    "customer": ["Customer is required"]
  },
  "codes": ["blank", "required"]
}
```

Errors with a `null` path are grouped under the empty-string key.

### Minimal API helpers

| Method | Valid result | Invalid result |
|--------|--------------|----------------|
| `validation.ToResult()` | `Results.Ok(value)`, 200 | 400 validation problem |
| `validation.ToResult(onValid)` | Whatever `Func<T, IResult>` returns | 400 validation problem |
| `validation.ToCreatedResult(location)` | `Results.Created(location(value), value)`, 201 | 400 validation problem |
| `task.ToResultAsync()` | Awaits a `Task<Validation<T, ValidationError>>`, then 200 | 400 validation problem |
| `task.ToResultAsync(onValid)` | Awaits, then calls `onValid` | 400 validation problem |

The last two are called on the error list itself, a `NonEmptyList<ValidationError>`:

| Method | Returns |
|--------|---------|
| `errors.ToValidationProblem(title)` | A 400 validation problem `IResult` |
| `errors.ToProblemDetails(title, status)` | An `HttpValidationProblemDetails` object, with status 400 unless you pass another |

Both default the title to "One or more validation errors occurred." and add the `codes` extension.

```csharp
// Validate is your own method returning Validation<Order, ValidationError>,
// ValidateAsync the same wrapped in a Task.

// 201 Created with a Location header
app.MapPost("/orders/created", (CreateOrder request) =>
    Validate(request).ToCreatedResult(order => $"/orders/{order.Lines.Count}"));

// 202 Accepted after an async validation
app.MapPost("/orders/async", (CreateOrder request) =>
    ValidateAsync(request).ToResultAsync(order => Results.Accepted(null, order.Lines.Count)));
```

:::note

The `ToResult`, `ToCreatedResult` and `ToResultAsync` helpers always answer an invalid result with 400. For another status code, match the validation yourself and pass the errors to `ToProblemDetails`:

```csharp
app.MapPost("/orders/strict", (CreateOrder request) =>
    Validate(request).Match(
        order => Results.Ok(order),
        errors => Results.Problem(errors.ToProblemDetails(status: StatusCodes.Status422UnprocessableEntity))));
```

:::

### MVC helpers

```csharp title="OrdersController.cs"
// Validate(request) returns Validation<Order, ValidationError>, as in the minimal API example
[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpPost]
    public ActionResult<Order> Create([FromBody] CreateOrder request)
        => Validate(request).ToActionResult(order => new OkObjectResult(order));

    [HttpPost("modelstate")]
    public IActionResult CreateWithModelState([FromBody] CreateOrder request)
    {
        if (!Validate(request).TryGetValue(ModelState, out Order order))
            return ValidationProblem(ModelState);

        return Ok(order);
    }
}
```

| Method | What it does |
|--------|--------------|
| `validation.ToActionResult()` | `OkObjectResult` with the value, or `BadRequestObjectResult` with `ToProblemDetails()` |
| `validation.ToActionResult(onValid)` | Calls `Func<T, ActionResult>` for a valid result; otherwise the same `BadRequestObjectResult` |
| `ModelState.AddErrors(errors)` | Adds each error's `Message` under its `Path` (or the empty string) and returns the `ModelStateDictionary` |
| `validation.TryGetValue(ModelState, out value)` | Returns `true` with the value, or adds the errors to the model state and returns `false` |

## NonEmptyShape

`NonEmptyShape.Of(type)` reports what a type guarantees about its size. The OpenAPI transformer uses it, and you can use it in your own tooling. It returns `null` for any type that is not a non-empty collection, including `NonEmptyString`.

```csharp
NonEmptyShape? shape = NonEmptyShape.Of(typeof(ConstrainedList<int, Exactly<Four>>));
// NonEmptyShape { ElementType = int, IsDictionary = false, MinItems = 4, MaxItems = 4, UniqueItems = false }

NonEmptyShape? dictionary = NonEmptyShape.Of(typeof(NonEmptyDictionary<string, int>));
// ElementType = int (the value type), IsDictionary = true, MinItems = 1

NonEmptyShape? none = NonEmptyShape.Of(typeof(List<int>));   // null
```

`NonEmptyShape` is a record with the properties `ElementType`, `IsDictionary`, `MinItems`, `MaxItems` (`null` when unbounded) and `UniqueItems`.
