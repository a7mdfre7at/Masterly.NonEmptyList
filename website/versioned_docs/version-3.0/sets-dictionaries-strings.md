---
id: sets-dictionaries-strings
title: "Sets, Dictionaries and Strings"
description: "NonEmptySet<T>, NonEmptyDictionary<TKey, TValue> and NonEmptyString: the non-empty guarantee for hash sets, lookup tables and text."
---

The non-empty guarantee is not only for lists. This page covers three sibling types: a hash set, a dictionary and a string that can never be empty. Each one wraps the familiar .NET type, keeps its API, and throws (or returns `null`) where the familiar type would have become empty.

## NonEmptySet&lt;T&gt;

`NonEmptySet<T>` wraps a `HashSet<T>` and implements `ISet<T>` and `IReadOnlySet<T>`. Duplicates collapse as in any set, and nothing you call can leave it empty.

```csharp
using Masterly.NonEmptyList;

NonEmptySet<string> tags = ["dotnet", "csharp", "dotnet"];
Console.WriteLine(tags.Count);   // 2

tags.Add("linq");                // true
tags.Remove("csharp");           // true
```

**Creating a set.**

| Way | Example |
|-----|---------|
| Collection expression | `NonEmptySet<int> ids = [1, 2, 3];` |
| Type-inferred factory | `NonEmptySet.Of("a", "b")` |
| Constructor | `new NonEmptySet<string>("a", "b")` or `new NonEmptySet<string>(first, rest, StringComparer.OrdinalIgnoreCase)` |
| From a sequence | `NonEmptySet<T>.From(source, comparer)`, `NonEmptySet.From(source)` or `source.ToNonEmptySet()` |
| Without throwing | `NonEmptySet<T>.TryFrom(source, out var set)` or `source.TryToNonEmptySet(out var set)` |
| One element | `NonEmptySet<T>.Singleton(item)` |
| From text | `NonEmptySet<int>.Parse("1, 2, 3")` or `TryParse(text, provider, out var set)` |

- An empty sequence or collection expression throws `ArgumentException`. A `null` element throws `ArgumentNullException`. `TryFrom` and `TryParse` return `false` instead.
- The `comparer` parameter is optional. Pass one for case-insensitive strings and similar. `FromSpan` and collection expressions use the default comparer.
- `Parse` reads comma-separated text, trims each element and collapses duplicates: `"1, 2,2 ,3"` becomes `{1, 2, 3}`. Empty or unparseable text throws `FormatException`. Elements are parsed the same way as for `NonEmptyList<T>.Parse`.
- Because the set has a static `TryParse`, ASP.NET Core can bind it from a query-string value. See [ASP.NET Core](./aspnetcore.md).

**Operations that could empty the set.** The set keeps the full `ISet<T>` surface. The operations that could leave it empty check first:

| Member | What happens when the result would be empty |
|--------|---------------------------------------------|
| `Remove(item)` | `InvalidOperationException` when `item` is the last element; returns `false` when `item` is absent |
| `RemoveWhere(match)` | `InvalidOperationException` when every element matches; otherwise returns the number removed |
| `Clear()` | always `NotSupportedException` |
| `IntersectWith(other)` | `InvalidOperationException` when the sets are disjoint |
| `ExceptWith(other)` | `InvalidOperationException` when every element is in `other` |
| `SymmetricExceptWith(other)` | `InvalidOperationException` when the sets are equal |

The check runs before the set changes, so a failed call leaves the set as it was.

```csharp
NonEmptySet<string> roles = ["admin", "editor"];

roles.IntersectWith(["editor", "viewer"]);   // roles is now { editor }
roles.IntersectWith(["viewer"]);             // InvalidOperationException: disjoint
```

**Operations that return a new set.** When you want the result without changing the set, use the non-mutating versions. The ones that can be empty return `null`:

| Method | Returns |
|--------|---------|
| `Union(other)` | `NonEmptySet<T>` |
| `Intersect(other)` | `NonEmptySet<T>?`, `null` when the sets are disjoint |
| `Except(other)` | `NonEmptySet<T>?`, `null` when nothing remains |
| `Map(selector)` | `NonEmptySet<TResult>`; duplicates collapse, a `null` result throws `InvalidOperationException` |

```csharp
NonEmptySet<string> required = ["read", "write"];
NonEmptySet<string> granted = ["read"];

NonEmptySet<string>? missing = required.Except(granted);
if (missing is not null)
{
    Console.WriteLine($"Missing: {string.Join(", ", missing)}");   // Missing: write
}
```

**Reading and converting.**

- `Any()` returns an arbitrary element. It never throws because the set is never empty.
- `ToNonEmptyList()` returns the elements in enumeration order. `ToSortedNonEmptyList(comparer)` sorts them.
- `ToHashSet()` copies to a plain `HashSet<T>` with the same comparer. `Comparer` returns that comparer.
- `Contains`, `IsSubsetOf`, `IsSupersetOf`, `Overlaps`, `SetEquals` and the other queries behave as on `HashSet<T>`.
- Two sets are equal when they contain the same elements, in any order.

:::warning

`set.Any()` is an instance method that returns an element, so it hides LINQ's `Any()`. On a `NonEmptySet<T>`, `if (tags.Any())` does not compile. You do not need the check anyway: the set is never empty.

:::

**JSON.** The set reads and writes a JSON array. An empty array or a `null` element throws `JsonException`. A deserialized set uses the default comparer. See [Serialization](./serialization.md).

## NonEmptyDictionary&lt;TKey, TValue&gt;

`NonEmptyDictionary<TKey, TValue>` wraps a `Dictionary<TKey, TValue>` and implements `IDictionary<TKey, TValue>` and `IReadOnlyDictionary<TKey, TValue>`. Use it for lookups where an empty table is a configuration bug, such as exchange rates per currency or a handler per message type.

```csharp
NonEmptyDictionary<string, decimal> rates = NonEmptyDictionary.OfPairs(
    ("USD", 1.00m),
    ("EUR", 0.92m),
    ("GBP", 0.79m));

decimal eur = rates["EUR"];
decimal jpy = rates.GetValueOrDefault("JPY", 0m);
```

**Creating a dictionary.**

| Way | Example |
|-----|---------|
| One entry | `new NonEmptyDictionary<string, int>("a", 1)`, `NonEmptyDictionary.Of("a", 1)` or `NonEmptyDictionary<string, int>.Singleton("a", 1)` |
| Several entries | `NonEmptyDictionary.OfPairs(("a", 1), ("b", 2))` or `new NonEmptyDictionary<string, int>(pair1, pair2)` |
| From pairs | `NonEmptyDictionary<TKey, TValue>.From(pairs, comparer)` or `pairs.ToNonEmptyDictionary()` |
| By projection | `customers.ToNonEmptyDictionary(c => c.Id, c => c.Name)` |
| From a non-empty list | `customers.ToNonEmptyDictionary(c => c.Id)`, where `customers` is a `NonEmptyList<Customer>` |
| Without throwing | `NonEmptyDictionary<TKey, TValue>.TryFrom(pairs, out var dict)` or `pairs.TryToNonEmptyDictionary(out var dict)` |

- An empty sequence throws `ArgumentException`. A duplicate key throws `ArgumentException`. A `null` key or value throws `ArgumentNullException`.
- `TryFrom` returns `false` for a `null` or empty sequence, a duplicate key and a `null` value.
- Values can never be `null`. The indexer setter, `Add` and `TryAdd` all reject a `null` value.
- There is no collection-expression support for dictionaries.

**Removing entries.**

| Member | Behaviour |
|--------|-----------|
| `Remove(key)`, `Remove(key, out value)` | returns `false` when the key is absent; throws `InvalidOperationException` when it is the last entry |
| `Clear()` | always `NotSupportedException` |
| `dict[key] = value` | adds or replaces, so it can never empty the dictionary |

**Reading and transforming.**

| Member | Returns |
|--------|---------|
| `dict[key]` | the value; `KeyNotFoundException` for a missing key |
| `TryGetValue(key, out value)`, `GetValueOrDefault(key, fallback)`, `ContainsKey`, `ContainsValue` | as on `Dictionary` |
| `Any()` | an arbitrary entry; never throws |
| `KeyList()`, `ValueList()`, `ToNonEmptyList()` | the keys, values or entries as a `NonEmptyList` |
| `Keys`, `Values` | the dictionary's key and value collections; never empty |
| `MapValues(v => ...)`, `Map((k, v) => ...)` | a `NonEmptyDictionary<TKey, TResult>` with the same keys; a `null` result throws `InvalidOperationException` |
| `Where((k, v) => ...)` | `NonEmptyDictionary<TKey, TValue>?`, `null` when nothing matches |
| `ToDictionary()` | a plain `Dictionary<TKey, TValue>` copy with the same comparer |

```csharp
// rates is the dictionary from above
NonEmptyDictionary<string, decimal> inCents = rates.MapValues(r => r * 100m);

NonEmptyDictionary<string, decimal>? cheap = rates.Where((_, rate) => rate < 1m);
NonEmptyList<string> currencies = rates.KeyList();
```

Two dictionaries are equal when they have the same keys mapped to equal values.

**JSON.** The dictionary reads and writes a JSON object, using the built-in dictionary converter, so keys work exactly as for `Dictionary<TKey, TValue>`. An empty object `{}` or a `null` value throws `JsonException`.

## NonEmptyString

`NonEmptyString` is a `readonly struct` that holds a string which is never `null`, empty or whitespace-only. It converts implicitly to `string`, so it drops into existing APIs, and it rejects blank input at every boundary: constructors, parsing, JSON, model binding and configuration.

```csharp
public sealed record Customer(NonEmptyString Name, NonEmptyString Email);

NonEmptyString name = new("Ada Lovelace");
string plain = name;                 // implicit conversion
int length = name.Length;            // always at least 1
char initial = name.Head;            // 'A'
```

**Creating a value.**

| Way | Blank input |
|-----|-------------|
| `new NonEmptyString(value)` | `ArgumentException` |
| `(NonEmptyString)value` | `ArgumentException` |
| `value.ToNonEmptyString()` | `ArgumentException` |
| `NonEmptyString.Parse(string, provider)` | `ArgumentException` |
| `NonEmptyString.Parse(ReadOnlySpan<char>, provider)` | `FormatException` |
| `NonEmptyString.TryCreate(value, out var result)` | returns `false` |
| `NonEmptyString.TryParse(value, provider, out var result)` | returns `false` (string and span overloads) |
| `NonEmptyString.FromNullable(value)`, `value.ToNonEmptyStringOrNull()` | returns `null` (a `NonEmptyString?`) |

The value is stored as given, without trimming. `" Ada "` is valid and keeps its spaces. Call `Trim()` to get a trimmed copy.

:::warning

`default(NonEmptyString)` holds no value. This happens for uninitialized fields, array elements and `default` parameters. Its `HasValue` is `false`, reading `Value` (or converting it to `string`) throws `InvalidOperationException`, and `ToString()` returns an empty string. The constructor, `Parse` and `TryCreate` never produce such a value.

:::

**Members.**

| Member | Returns |
|--------|---------|
| `Value` | the underlying `string` |
| `HasValue` | `false` only for `default(NonEmptyString)` |
| `Length`, `Head`, `Last`, `this[index]` | the length, first character, last character, a character |
| `Trim()`, `ToUpperInvariant()`, `ToLowerInvariant()` | a new `NonEmptyString` |
| `Concat(string?)`, `+` | a new `NonEmptyString`; appending `null` or `""` returns the same value |
| `Truncate(maxLength)` | at most `maxLength` characters, always at least one; `maxLength` below 1 throws `ArgumentOutOfRangeException` |
| `Split(separator)` | `NonEmptyList<NonEmptyString>`; parts are trimmed and blank parts dropped |
| `ToCharList()` | `NonEmptyList<char>` |
| `AsSpan()` | `ReadOnlySpan<char>`; there is also an implicit conversion to `ReadOnlySpan<char>` |

```csharp
NonEmptyString csv = new("red, green, , blue");
NonEmptyList<NonEmptyString> colors = csv.Split(',');   // red, green, blue

NonEmptyString joined = colors.Join(" | ");             // "red | green | blue"
NonEmptyString shortName = new NonEmptyString("Masterly.NonEmptyList").Truncate(8);   // "Masterly"
```

`Join(separator)` is an extension on `NonEmptyList<NonEmptyString>`. The result is never blank because every part has content.

**Comparisons.** Equality, hashing and ordering are ordinal.

- `==`, `!=`, `Equals(NonEmptyString)` and `GetHashCode()` are ordinal and case-sensitive.
- `Equals(other, StringComparison.OrdinalIgnoreCase)` compares with any `StringComparison`.
- `Equals(object)` also returns `true` for a plain `string` with the same characters.
- `CompareTo` is ordinal, so `OrderBy(s => s)` sorts by code unit. There are no `<` or `>` operators.

**Parsing, binding and JSON.**

- `NonEmptyString` implements `ISpanParsable<NonEmptyString>` and `ISpanFormattable`, so generic code constrained on those interfaces, and minimal API parameter binding, accept it.
- Its `TypeConverter` (`NonEmptyStringTypeConverter`) converts from and to `string`. MVC model binding and configuration binding use it; a blank value throws `FormatException`.
- The System.Text.Json converter reads and writes a JSON string. A blank string or a non-string token throws `JsonException`.
- It also works as a dictionary key in JSON: a `Dictionary<NonEmptyString, int>` serializes as an object, and a blank property name throws `JsonException`.

See [ASP.NET Core](./aspnetcore.md), [Configuration](./configuration.md) and [Serialization](./serialization.md) for the framework side.
