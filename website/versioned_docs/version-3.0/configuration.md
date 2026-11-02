---
id: configuration
title: "Configuration"
description: "Bind non-empty collections from appsettings.json and environment variables, and stop the host at startup when a required one is missing or empty."
---

`Masterly.NonEmptyList.Configuration` binds non-empty types from `Microsoft.Extensions.Configuration`. A list that must have at least one entry is checked once, when the options are first created, and the failure names the configuration key. Combined with `ValidateOnStart()`, a bad `appsettings.json` stops the host at startup instead of failing on the first request.

```bash
dotnet add package Masterly.NonEmptyList.Configuration
```

```csharp
using Masterly.NonEmptyList.Configuration;
```

## Why a separate binder

The default `ConfigurationBinder` cannot create a `NonEmptyList<T>` from an array section, because the type has no parameterless constructor. It would also accept an empty array if it could. This package binds the non-empty properties of your options class itself and hands every other property to the default binder.

## Options classes

```csharp title="SmtpOptions.cs"
using Masterly.NonEmptyList;

public sealed class SmtpOptions
{
    public required NonEmptyList<string> Hosts { get; init; }   // required: missing or empty fails
    public NonEmptyString Sender { get; set; }                   // required: missing or blank fails
    public NonEmptySet<string>? Tags { get; set; }               // optional: null when absent
    public int Port { get; init; } = 25;                         // bound by the default binder
}
```

```csharp title="Program.cs"
builder.Services.AddNonEmptyOptions<SmtpOptions>("Smtp");
```

```json title="appsettings.json"
{
  "Smtp": {
    "Hosts": ["smtp1.example.com", "smtp2.example.com"],
    "Sender": "noreply@example.com",
    "Port": 587
  }
}
```

`AddNonEmptyOptions<TOptions>(sectionPath)` is shorthand for:

```csharp
builder.Services.AddOptions<SmtpOptions>()
    .BindNonEmptyConfiguration("Smtp")
    .ValidateOnStart();
```

`BindNonEmptyConfiguration(sectionPath)` replaces the standard `BindConfiguration`. It binds the section with the non-empty aware binder and registers a change-token source, so `IOptionsMonitor<T>` sees configuration reloads. Pass no section path to bind the configuration root. Use the long form when you want to chain more configuration, such as your own `Validate` calls.

### Required and optional properties

The binder looks at each public property whose type is a non-empty type and that has a setter (`set` or `init`). When the configuration has no value for it:

| The property | Result |
|--------------|--------|
| Is declared nullable: `NonEmptyList<string>?` or `NonEmptyString?` | Left as it is (usually `null`). |
| Already has a value, for example from an initializer: `= NonEmptyList.Of("localhost")` | Keeps that value. |
| Is anything else | Required. Binding throws `InvalidOperationException`. |

A missing required property fails with a message such as:

```text
Configuration key 'Smtp:Hosts' is required and must contain at least one element.
```

A key that is present but empty, such as `"Hosts": []` in `appsettings.json`, fails with `Configuration key 'Smtp:Hosts' must contain at least one element.` instead. On .NET 8 the JSON provider drops an empty array altogether, so the key reads as missing and you get the first message.

A plain `NonEmptyString` property counts as having no value, because its default value is empty. Declare it `NonEmptyString?` to make it optional.

:::note

Nullability comes from your nullable reference type annotations. In a project with `<Nullable>disable</Nullable>`, there are no annotations, so every non-empty property without an initializer is required.

:::

A value that is present but empty or invalid fails, even for an optional property. For example, `"Tags": ""` throws. Some empty values never reach the binder, though: the JSON provider stores `{}` as a missing key on every target framework, and `[]` on .NET 8, so an optional property configured that way stays `null`. Properties without a setter are skipped.

### When the failure happens

The binder runs when the options are first created, which is the first time something reads `IOptions<SmtpOptions>.Value`. `ValidateOnStart()` makes the host create them while it starts, so the exception stops the host and its message names the key. Without `ValidateOnStart()`, the same exception surfaces on the first request that needs the options.

## Array and comma-separated values

A non-empty list can be written in either of two forms:

```json title="Array form (appsettings.json)"
{ "Smtp": { "Hosts": ["smtp1", "smtp2"] } }
```

```bash title="Comma-separated form (an environment variable)"
Smtp__Hosts=smtp1,smtp2
```

- The array form is any section with children (`Smtp:Hosts:0`, `Smtp:Hosts:1`). Each child is read with the rules in [How elements and entries are read](#how-elements-and-entries-are-read), so elements can be values, objects or nested collections.
- The comma-separated form is a single value. It is split on commas, each piece is trimmed, empty pieces are skipped (`"a, b ,,c"` gives `a`, `b`, `c`), and each piece is converted with the element type's `TypeConverter` using the invariant culture.

If the value has no elements after splitting, binding fails with `Configuration key 'Smtp:Hosts' must contain at least one element.`

:::warning

When a key has both children and a value, the children win. An environment variable `Smtp__Hosts=a,b` does not replace a `Hosts` array from `appsettings.json`; it is ignored. To override an array from the environment, use the indexed form: `Smtp__Hosts__0=a`, `Smtp__Hosts__1=b`. That replaces entries index by index, so any further entries from the file remain.

:::

An element that cannot be converted also fails, in both forms, with an `InvalidOperationException` that names the key and the bad value. The array form names the element, `Configuration key 'Smtp:Ports:1' contains 'eighty', which is not a valid Int32.`, and the comma-separated form names the key, `Configuration key 'Smtp:Ports' contains 'eighty', ...`. The converter's exception is its `InnerException`.

## How elements and entries are read

Microsoft's binder silently skips a collection element or dictionary entry it cannot convert, so `["80", "eighty"]` would bind as `[80]`. This package reads every element and entry itself, and none is ever skipped: each one becomes a value or an `InvalidOperationException` that names its full key.

| The element or entry is | It is read as |
|-------------------------|---------------|
| A single value of a type with a `TypeConverter`, such as `int`, `Uri` or an enum | Converted with the invariant culture. A value that cannot be converted throws, for example `Configuration key 'Smtp:Ports:1' contains 'eighty', which is not a valid Int32.` |
| A value that converts to `null`, such as `""` for `int?` or `Uri` | Rejected: a non-empty collection cannot hold `null`. |
| A JSON `null` | An empty string, on every target framework. The .NET 8 JSON provider already reads it that way; .NET 10 keeps `null`, and the binder treats both alike so the same file binds the same way. For a number this means an error; for a string, an empty string. |
| A non-empty type, such as a `NonEmptyList<int>` value in a dictionary | Read recursively with the same rules, as an array or a comma-separated value. A JSON `null` fails, as an empty value does. |
| A collection: an array, `List<T>`, `HashSet<T>`, `Dictionary<TKey, TValue>`, their interfaces, or a concrete collection class such as `SortedSet<T>`, `Collection<T>` or a class deriving from `List<T>` | Read recursively with the same strict conversion. It may be empty, and may hold `null`. |
| An object or struct with non-empty members anywhere inside it | Created with its parameterless constructor and bound member by member: its non-empty members by this package, with their required checks (`Configuration key 'P:0:Hosts' is required and must contain at least one element.`), and every other member by Microsoft's binder. A type without a parameterless constructor fails with a message that says so. |
| Any other object | Bound by Microsoft's binder, exactly as it would bind it anywhere else: constructor binding for records, `[ConfigurationKeyName]`, get-only collections, public setters only. An empty entry (`{}`) becomes a default object. A single value where an object is expected throws. |
| `object`, `IConfigurationSection` or a base64 `byte[]` | The string value, the section itself, or the decoded bytes. |
| A read-only collection type, such as `ImmutableArray<T>` | Rejected with a message that suggests an array, `List<T>`, `HashSet<T>` or `Dictionary<TKey, TValue>`. |
| A section where a single value is expected, such as `Ports:1:Value` for an `int` | Rejected: `Configuration key 'Ports:1' must be a single Int32 value, not a section.` A type that has both a converter and settable members, such as `System.Drawing.Point`, can be written either way. |

Dictionary keys are converted with the key type's `TypeConverter`. If two configuration keys convert to the same key, such as `1` and `01` for an `int` key, binding throws instead of letting the later one win. A dictionary with no entries fails with `Configuration key 'Smtp:Limits' must contain at least one entry.`

The same split applies to your option classes. This package binds only the members Microsoft's binder cannot create: non-empty types, and objects or collections that contain them. Everything else, including plain `List<int>` or `Dictionary<string, int>` properties and the plain members of objects inside a non-empty collection, is bound by Microsoft's binder with its own behaviour, which skips collection values it cannot convert.

A collection property that contains non-empty members follows the default binder's rules for an existing value: items are added to a collection that is already there, dictionary entries are merged, and an array is extended. A get-only property is filled in place, and fails with a message naming the key when it has no instance to fill. A setter or constructor that throws is reported with the key as well. Objects and collections may be nested up to 64 levels deep.

:::note

An explicit JSON `null` on a property, such as `"Ports": null`, cannot be told apart from a missing key on .NET 10, so an optional property stays `null`. On .NET 8 the JSON provider stores it as an empty string, which counts as a present but empty value and fails. Leave the key out instead of writing `null` when you mean "not configured".

:::


## Supported property types

| Type | Configuration shape |
|------|---------------------|
| `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, `NonEmptySet<T>` | Array or comma-separated value. Duplicates collapse in a set. |
| `ConstrainedList<T, TConstraint>` | Array or comma-separated value. The constraint is checked, and a violation throws `InvalidOperationException` naming the key. |
| `NonEmptyDictionary<TKey, TValue>` | An object section, such as `"Limits": { "gold": 10 }`. |
| `NonEmptyString`, `NonEmptyString?` | A string that is not blank. |
| Classes that derive from these and expose a static `From` factory, including `[NonEmptyCollection]` classes | Same as their base type. |

Nested option classes are bound recursively with the same rules, so a non-empty property inside `Smtp:Retry` is checked too. This happens only when the nested section exists: if `Smtp:Retry` is absent, the nested object is not bound and its required properties are not checked. The binder creates a nested object when the property is `null`, settable, and the class has a parameterless constructor. Collections whose elements contain non-empty members, such as a `List<Endpoint>` property where `Endpoint` has a `NonEmptyList<string>`, are read by this package too, with the rules above.

## Reading single keys

Extension methods on `IConfiguration` read one key at a time:

```csharp
// configuration is an IConfiguration
NonEmptyList<int> ports = configuration.GetNonEmptyList<int>("Smtp:Ports");
NonEmptyList<int>? optional = configuration.GetNonEmptyListOrNull<int>("Smtp:Ports");
NonEmptySet<string> tags = configuration.GetNonEmptySet<string>("Tags");
NonEmptyDictionary<string, int> limits = configuration.GetNonEmptyDictionary<string, int>("Limits");
ConstrainedList<int, AtLeast<Two>> pair = configuration.GetConstrainedList<int, AtLeast<Two>>("Pair");
NonEmptyString sender = configuration.GetNonEmptyString("Smtp:Sender");
```

| Method | Missing key | Present but empty or invalid |
|--------|-------------|------------------------------|
| `GetNonEmptyList<T>`, `GetNonEmptySet<T>`, `GetNonEmptyDictionary<TKey, TValue>`, `GetConstrainedList<T, TConstraint>`, `GetNonEmptyString` | Throws `InvalidOperationException` naming the key | Throws |
| `GetNonEmptyListOrNull<T>` | Returns `null` | Throws |

To bind a whole section without the options pattern, use `GetNonEmpty<TOptions>()` for a new instance or `BindNonEmpty(instance)` for an existing one:

```csharp
SmtpOptions options = configuration.GetSection("Smtp").GetNonEmpty<SmtpOptions>();
```

The same operations are available as static methods on `NonEmptyConfigurationBinder`: `Get<TOptions>`, `Bind`, `ReadNonEmpty(section, targetType)` and `IsNonEmptyType(type)`.

:::tip

The standard binder can still read a single comma-separated value, because `NonEmptyList<T>` has a `TypeConverter`: `configuration.GetValue<NonEmptyList<string>>("Hosts")` works for `Hosts=a,b`. It uses the stricter parsing of `NonEmptyList<T>.Parse`, which rejects empty pieces. Use this package for arrays and for options classes.

:::

See [Examples](./examples.md) for a complete alerting setup that fails fast when no recipients are configured.
