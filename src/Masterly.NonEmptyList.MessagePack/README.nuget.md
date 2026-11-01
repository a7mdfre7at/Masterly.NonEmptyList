# Masterly.NonEmptyList.MessagePack

MessagePack-CSharp formatters for [Masterly.NonEmptyList](https://www.nuget.org/packages/Masterly.NonEmptyList). Every non-empty type serializes to its natural MessagePack shape and an empty payload is rejected while deserializing.

## Installation

```bash
dotnet add package Masterly.NonEmptyList.MessagePack
```

## Usage

```csharp
using Masterly.NonEmptyList.MessagePack;

MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithNonEmptyResolver();
// or the ready-made: NonEmptyMessagePackExtensions.NonEmptyStandard

byte[] bytes = MessagePackSerializer.Serialize(order, options);
Order back = MessagePackSerializer.Deserialize<Order>(bytes, options);

MessagePackSerializer.Deserialize<NonEmptyList<int>>(emptyArrayBytes, options);
// MessagePackSerializationException: NonEmptyList cannot be empty
```

`WithNonEmptyResolver()` puts `NonEmptyResolver` in front of whatever resolver the options already use, so your existing formatters keep working.

## What is covered

| Type | MessagePack shape |
|------|-------------------|
| `NonEmptyList<T>`, `ImmutableNonEmptyList<T>`, `NonEmptySet<T>`, `ConstrainedList<T, C>` | array, at least one element |
| `NonEmptyDictionary<TKey, TValue>` | map, at least one entry |
| `NonEmptyString` (and `NonEmptyString?`) | string with content |
| classes deriving from `NonEmptyList<T>`, including `[NonEmptyCollection]`-generated ones | array, rebuilt through the type's `From` factory |

## Source generator note

MessagePack's own source generator tries to generate formatters for every member type of a `[MessagePackObject]` class. For members typed with the non-empty generics, mark the class with `[MessagePackObject(SuppressSourceGeneration = true)]` so the dynamic resolver (and this package's formatters) are used for it.

## License

MIT
