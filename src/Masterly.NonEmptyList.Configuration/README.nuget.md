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

On .NET 8 the JSON provider drops an empty array altogether, so the key reads as missing and the message is `Configuration key 'Smtp:Hosts' is required and must contain at least one element.`

Both the array form (`Hosts:0`, `Hosts:1`) and a comma-separated scalar (`Hosts=smtp1,smtp2`, handy for environment variables) are accepted.

Every element and dictionary entry is read by this package, never skipped: Microsoft's binder would silently turn `["80", "eighty"]` into `[80]`. A value that cannot be converted, a value that converts to `null`, a section where a single value is expected, and two dictionary keys that convert to the same key all throw `InvalidOperationException` naming the full key, for example `Configuration key 'Smtp:Ports:1' contains 'eighty', which is not a valid Int32.` Nested collections inside a non-empty collection are read with the same rules, and an object's own required non-empty members are checked. A JSON `null` element reads as an empty string on every target framework. Everything Microsoft's binder can create on its own (plain properties, plain collections, objects without non-empty members) is still bound by it, with its usual behaviour: constructor binding, `[ConfigurationKeyName]`, get-only collections.

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

## Documentation

Full guide: <https://a7mdfre7at.github.io/Masterly.NonEmptyList/docs/configuration>.

## License

MIT
