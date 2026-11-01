# Masterly.NonEmptyList.Configuration

Configuration and Options binding for [Masterly.NonEmptyList](https://www.nuget.org/packages/Masterly.NonEmptyList). A config array that must have at least one entry is checked once, when the host starts, and the failure names the key.

## Installation

```bash
dotnet add package Masterly.NonEmptyList.Configuration
```

## Why

The default binder cannot construct `NonEmptyList<T>` because the type has no parameterless constructor, and it would happily bind an empty array anyway. This package binds non-empty properties itself and delegates everything else to the default binder.

## Usage

```csharp
public sealed class SmtpOptions
{
    public required NonEmptyList<string> Hosts { get; init; }   // required: empty or missing fails at startup
    public NonEmptySet<string>? Tags { get; set; }               // optional: null when absent
    public NonEmptyString Sender { get; set; }                   // required: blank fails at startup
    public int Port { get; init; } = 25;                         // bound by the default binder
}

builder.Services.AddNonEmptyOptions<SmtpOptions>("Smtp");
// same as: AddOptions<SmtpOptions>().BindNonEmptyConfiguration("Smtp").ValidateOnStart()
```

```json
{ "Smtp": { "Hosts": ["smtp1", "smtp2"], "Sender": "noreply@example.com" } }
```

An empty `Hosts` array stops the host with:

```
Configuration key 'Smtp:Hosts' must contain at least one element.
```

Both the array form (`Hosts:0`, `Hosts:1`) and a comma-separated scalar (`Hosts=smtp1,smtp2`, handy for environment variables) are accepted.

### Direct reads

```csharp
NonEmptyList<int> ports = configuration.GetNonEmptyList<int>("Smtp:Ports");
NonEmptyList<int>? optional = configuration.GetNonEmptyListOrNull<int>("Smtp:Ports");
NonEmptySet<string> tags = configuration.GetNonEmptySet<string>("Tags");
NonEmptyDictionary<string, int> limits = configuration.GetNonEmptyDictionary<string, int>("Limits");
ConstrainedList<int, AtLeast<Two>> pair = configuration.GetConstrainedList<int, AtLeast<Two>>("Pair");
NonEmptyString sender = configuration.GetNonEmptyString("Smtp:Sender");
SmtpOptions options = configuration.GetSection("Smtp").GetNonEmpty<SmtpOptions>();
```

Supported property types: `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, `NonEmptySet<T>`, `NonEmptyDictionary<TKey, TValue>`, `ConstrainedList<T, TConstraint>`, `NonEmptyString`, `[NonEmptyCollection]`-generated classes, and nested classes containing any of them. A property is treated as optional when its type is nullable (`NonEmptyList<string>?`) or it already has a value.

## License

MIT
