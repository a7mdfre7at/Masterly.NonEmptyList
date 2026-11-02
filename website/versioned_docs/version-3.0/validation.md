---
id: validation
title: "Validation"
description: "Validation<T, TError>: a valid value or a non-empty list of errors, with Combine, Sequence and Traverse to report every failure at once."
---

`Validation<T, TError>` is the result of validating something: either a valid `T`, or a `NonEmptyList<TError>`. Because the error list is a non-empty list, an "invalid result with no errors" cannot exist. Unlike a fail-fast result type, `Validation.Combine` collects the errors of every failed input, so a form with three bad fields reports all three in one response.

```csharp
using Masterly.NonEmptyList;

// input comes from your application; input.Email is a string?
Validation<string, ValidationError> email =
    Validation.NotBlank(input.Email, new ValidationError("required", "Email is required", "email"))
        .Ensure(e => e.Contains('@'), new ValidationError("format", "Email must contain @", "email"));

if (email.IsValid)
{
    Console.WriteLine(email.Value);
}
else
{
    foreach (ValidationError error in email.Errors)
        Console.WriteLine(error);   // email format: Email must contain @
}
```

## Creating results

| Call | Result |
|------|--------|
| `Validation<T, TError>.Valid(value)` | valid; `ArgumentNullException` for `null` |
| `Validation<T, TError>.Invalid(error)` | invalid with one error |
| `Validation<T, TError>.Invalid(first, second, ...)` | invalid with several errors |
| `Validation<T, TError>.Invalid(errors)` | invalid with an existing `NonEmptyList<TError>` |
| `Validation<int, string> v = 5;` | valid, through the implicit conversion from `T` |
| `Validation.Valid<T, TError>(value)`, `Validation.Invalid<T, TError>(error)`, `Validation.Invalid<T, TError>(errors)` | the same, as non-generic helpers; pass both type arguments |

**Validation helpers.** These turn a raw value into a validation. The type arguments are inferred.

| Helper | Valid when | Valid value |
|--------|------------|-------------|
| `Validation.Ensure(value, predicate, error)` | `predicate(value)` is `true` | `value` |
| `Validation.NotNull(value, error)` | a reference is not `null` | the reference |
| `Validation.HasValue(value, error)` | a `Nullable<T>` has a value | the unwrapped `T` |
| `Validation.NotBlank(value, error)` | a `string?` is not `null`, empty or whitespace | the `string` |
| `Validation.NonEmpty(source, error)` | a sequence has at least one element (and no `null`) | a `NonEmptyList<T>` |

`NonEmpty` is how a request's `List<T>?` becomes a `NonEmptyList<T>` without a throwing factory. For size, uniqueness or ordering rules, use `ConstrainedList<T, TConstraint>.Validate` from [Constrained lists](./constrained-lists.md) or the generated `Validate` from [Domain collections](./domain-collections.md).

## Reading a result

| Member | Behaviour |
|--------|-----------|
| `IsValid`, `IsInvalid` | which case you have |
| `Value` | the value; `InvalidOperationException` when invalid, with the errors in the message |
| `Errors` | the `NonEmptyList<TError>`; `InvalidOperationException` when valid |
| `TryGetValue(out value)`, `TryGetErrors(out errors)` | the `Try` pattern |
| `GetValueOrDefault(fallback)` | the value, or `fallback` |
| `GetValueOr(errors => ...)` | the value, or one computed from the errors |
| `GetValueOrThrow(errors => exception)` | the value, or throws the exception you build |
| `Match(valid, invalid)` | handles both cases; returns a result, or takes two actions |

```csharp
// result is a Validation<Order, ValidationError>
string message = result.Match(
    valid: order => $"Order {order.Id} accepted",
    invalid: errors => $"{errors.Count} problem(s), first: {errors.Head.Message}");
```

`errors.Head` is always safe, because the errors are a `NonEmptyList<TError>`.

## Transforming

| Method | Behaviour |
|--------|-----------|
| `Map(value => ...)` | transforms a valid value; errors pass through |
| `MapErrors(error => ...)` | transforms every error; a valid value passes through |
| `Bind(value => otherValidation)` | chains a validation that needs the value; not called when invalid |
| `Ensure(predicate, error)`, `Ensure(predicate, value => error)` | adds an error when a valid value fails the predicate |
| `Tap(value => ...)`, `TapErrors(errors => ...)` | runs a side effect and returns the same result |

```csharp
// FindCustomer returns a Validation<Customer, ValidationError>
Validation<Customer, ValidationError> customer =
    Validation.NotBlank(input.CustomerId, new ValidationError("required", "Customer is required", "customerId"))
        .Bind(id => FindCustomer(id));

Validation<Customer, string> withMessages = customer.MapErrors(e => e.Message);
```

:::note

`Bind` and `Ensure` are fail-fast. Once a result is invalid, later `Ensure` calls are skipped and keep the existing errors; they do not add more. A chain such as `NotBlank(...).Ensure(a).Ensure(b)` reports the first rule that fails for that value. To collect errors from *different* values, use `Combine`.

:::

## Combining independent validations

`Validation.Combine` takes two to eight validations and a selector. If every input is valid, the selector builds the result. Otherwise the result is invalid with every error from every invalid input, in argument order.

```csharp
// Money is your own record; input.Amount is a decimal and input.Currency a string?
Validation<Money, ValidationError> price = Validation.Combine(
    Validation.Ensure(input.Amount, a => a >= 0, new ValidationError("range", "Amount cannot be negative", "amount")),
    Validation.NotBlank(input.Currency, new ValidationError("required", "Currency is required", "currency")),
    (amount, currency) => new Money(amount, currency));
```

All validations must share the same `TError`. Use `MapErrors` first if they do not.

## Validating collections

`Sequence` turns many validations into one, and `Traverse` validates every element of a collection. Both accumulate every error.

| Method | On | Returns |
|--------|----|---------|
| `validations.Sequence()` | `NonEmptyList<Validation<T, TError>>` | `Validation<NonEmptyList<T>, TError>` |
| `validations.Sequence()` | `IEnumerable<Validation<T, TError>>` | `Validation<List<T>, TError>` |
| `source.Traverse(validator)` | `NonEmptyList<T>` | `Validation<NonEmptyList<TResult>, TError>` |
| `source.Traverse(validator)` | `IEnumerable<T>` | `Validation<List<TResult>, TError>` |

A non-empty input gives a non-empty output, so the shape of your data survives validation.

```csharp
// lines is a NonEmptyList<OrderLineInput>; OrderLine is your own record
Validation<NonEmptyList<OrderLine>, ValidationError> validLines = lines.Traverse(line =>
    Validation.Combine(
        Validation.NotBlank(line.Sku, new ValidationError("required", "SKU is required", "lines.sku")),
        Validation.Ensure(line.Quantity, q => q > 0, new ValidationError("range", "Quantity must be positive", "lines.quantity")),
        (sku, quantity) => new OrderLine(sku, quantity)));
```

## ValidationError

`ValidationError` is a ready-made error type: a sealed record with a machine-readable `Code`, a human-readable `Message` and an optional `Path` that names the field.

```csharp
public sealed record ValidationError(string Code, string Message, string? Path = null);
```

- `ToString()` returns `"{Path} {Code}: {Message}"`, or `"{Code}: {Message}"` when there is no path.
- `errors.ToErrorDictionary()` groups a `NonEmptyList<ValidationError>` by path into a `Dictionary<string, string[]>` of messages, the shape `ValidationProblemDetails` expects. Errors without a path go under the empty string key.

You can use any type as `TError`: a `string`, an enum, or your own record.

## Example: user registration

This validator checks a registration request and reports every problem at once.

```csharp title="RegistrationValidator.cs"
using Masterly.NonEmptyList;

public sealed record RegisterRequest(string? UserName, string? Email, string? Password, int? Age, List<string>? Interests);

public sealed record Registration(NonEmptyString UserName, string Email, string Password, int Age, NonEmptyList<string> Interests);

public static class RegistrationValidator
{
    public static Validation<Registration, ValidationError> Validate(RegisterRequest request) =>
        Validation.Combine(
            Validation.NotBlank(request.UserName, new ValidationError("required", "User name is required", "userName"))
                .Map(name => new NonEmptyString(name)),
            Validation.NotBlank(request.Email, new ValidationError("required", "Email is required", "email"))
                .Ensure(e => e.Contains('@'), new ValidationError("format", "Email must contain @", "email")),
            Validation.NotBlank(request.Password, new ValidationError("required", "Password is required", "password"))
                .Ensure(p => p.Length >= 12, new ValidationError("length", "Password must have at least 12 characters", "password")),
            Validation.HasValue(request.Age, new ValidationError("required", "Age is required", "age"))
                .Ensure(a => a >= 18, a => new ValidationError("range", $"You must be 18 or older, not {a}", "age")),
            Validation.NonEmpty(request.Interests, new ValidationError("nonEmpty", "Pick at least one interest", "interests")),
            (userName, email, password, age, interests) => new Registration(userName, email, password, age, interests));
}
```

Run it with a request that is wrong in four places:

```csharp
RegisterRequest request = new("ada", "ada.example.com", "short", 17, []);
Validation<Registration, ValidationError> result = RegistrationValidator.Validate(request);

result.Match(
    valid: registration => Console.WriteLine($"Welcome, {registration.UserName}"),
    invalid: errors =>
    {
        foreach (ValidationError error in errors)
            Console.WriteLine(error);
    });
```

```text title="Console output"
email format: Email must contain @
password length: Password must have at least 12 characters
age range: You must be 18 or older, not 17
interests nonEmpty: Pick at least one interest
```

Each field reports its first failing rule, and `Combine` collects the fields. To send the errors to a client, group them by field:

```csharp
if (result.TryGetErrors(out var errors))
{
    Dictionary<string, string[]> byField = errors.ToErrorDictionary();
    // byField["email"] is ["Email must contain @"], byField["age"] is ["You must be 18 or older, not 17"], ...
}
```

## Returning a result from ASP.NET Core

The `Masterly.NonEmptyList.AspNetCore` package adds `ToResult()`, which turns a `Validation<T, ValidationError>` into an HTTP response: `200 OK` with the value, or `400` validation problem details with the errors grouped by `Path`.

```csharp
using Masterly.NonEmptyList.AspNetCore;

app.MapPost("/register", (RegisterRequest request) =>
    RegistrationValidator.Validate(request).ToResult());
```

There are also overloads that take your own success response, a `ToCreatedResult` for `201 Created`, and versions for MVC controllers. See [ASP.NET Core](./aspnetcore.md).
