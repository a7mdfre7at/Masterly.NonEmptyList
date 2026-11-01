---
id: safe-mutation
title: "Safe Mutation"
description: "Add, remove and reorder elements without ever emptying the list, through the class or any interface, and add your own invariants in a derived type."
---

`NonEmptyList<T>` is mutable. You can add, insert, replace, remove, sort and reverse elements, much like a `List<T>`. The one thing you cannot do is leave it empty. Every operation that would remove the last element throws, and it throws before the list changes, so the list is still intact afterwards.

## The guarantee

`NonEmptyList<T>` wraps a private `List<T>` instead of inheriting from it. It implements `IList<T>`, `IReadOnlyList<T>` and the non-generic `IList` itself, and every one of those routes through the same guarded members.

This matters because of how interfaces work in C#. In version 2, the class inherited `List<T>` and hid `Clear` and `Remove` with `new` methods. A call through `IList<T>` skipped the hiding method and went straight to `List<T>`, so `((IList<T>)list).Clear()` emptied the list. In version 3 there is no inherited `List<T>` to fall through to.

```csharp
NonEmptyList<int> numbers = [1];
IList<int> asInterface = numbers;

asInterface.Clear();      // NotSupportedException
asInterface.RemoveAt(0);  // InvalidOperationException

Console.WriteLine(numbers.Count); // 1
```

A `NonEmptyList<T>` is therefore not a `List<T>` any more: `numbers is List<int>` is `false`. Where an API asks for a `List<T>`, change it to accept `IList<T>` or `IReadOnlyList<T>`, or pass `numbers.ToList()`. See [Upgrading from 2.x](./upgrading-from-2x.md).

## Adding and replacing elements

| Member | What it does | Throws |
|--------|--------------|--------|
| `Add(T item)` | Appends one element. | `ArgumentNullException` when `item` is `null` |
| `AddFirst(T item)` | Inserts one element at index 0. | `ArgumentNullException` when `item` is `null` |
| `Insert(int index, T item)` | Inserts one element at `index`. | `ArgumentNullException` when `item` is `null` |
| `AddRange(IEnumerable<T> collection)` | Appends a batch. | `ArgumentNullException` when `collection` is `null` or contains `null`; `ArgumentException` when `collection` is empty |
| `InsertRange(int index, IEnumerable<T> collection)` | Inserts a batch at `index`. | Same as `AddRange` |
| `this[int index]` (set) | Replaces one element. | `ArgumentNullException` when the value is `null` |
| `SetRange(int startIndex, IEnumerable<T> items)` | Replaces consecutive elements starting at `startIndex`. | `ArgumentNullException` when `items` is `null` or contains `null`; `ArgumentOutOfRangeException` when the range does not fit inside the list |

:::note

`AddRange` and `InsertRange` reject an empty batch with `ArgumentException`. `List<T>.AddRange` accepts one and does nothing. If the batch may be empty, check it first, or add the elements one at a time.

:::

The `Index` indexer, `xs[^1]`, is read-only. To replace the last element, write `xs[xs.Count - 1] = value`.

## Removing elements

| Member | Returns | Throws `InvalidOperationException` when |
|--------|---------|------------------------------------------|
| `Clear()` | | Never: it always throws `NotSupportedException` instead |
| `Remove(T item)` | `true` if the item was removed, `false` if it was not in the list | the item is in the list and is its only element |
| `RemoveAt(int index)` | | the list has one element |
| `RemoveRange(int index, int count)` | | `count` is greater than or equal to `Count` |
| `RemoveAll(Predicate<T> match)` | the number of elements removed | every element matches |

Some details are easy to miss:

- `Remove` returns `false` for an element that is not in the list, even when the list has a single element. It throws only when the removal would really empty the list.
- `Remove(null)` throws `ArgumentNullException`.
- `RemoveAll` counts the matches first. If every element matches, it throws and removes nothing. It does not remove "all but one".
- The usual index checks still apply. `RemoveAt` with an index outside the list throws `ArgumentOutOfRangeException`, as on `List<T>`.

```csharp
NonEmptyList<string> names = ["Ada", "Grace"];

bool removed = names.Remove("Linus"); // false: not in the list
names.Remove("Ada");                  // true: "Grace" is left

names.Remove("Grace");                // InvalidOperationException: it is the only element
names.RemoveAll(n => n.Length > 0);   // InvalidOperationException: every element matches
```

To pick out the elements you want without mutating, use the non-mutating operators, which return `null` or a possibly empty `List<T>` instead of throwing. See [Functional operations](./functional-operations.md).

## Reordering and capacity

These members never change the number of elements, so they never throw for emptiness:

- `Sort()`, `Sort(Comparison<T> comparison)`, `Sort(IComparer<T>? comparer)` and `Sort(int index, int count, IComparer<T>? comparer)` sort in place.
- `Reverse()` and `Reverse(int index, int count)` reverse in place.
- `Capacity`, `TrimExcess()` and `EnsureCapacity(int capacity)` manage the storage, as on `List<T>`.

:::warning

`Reverse()` on a `NonEmptyList<T>` is the in-place `void` method, not LINQ's `Reverse`. `var reversed = list.Reverse();` does not compile. Use `ReverseList()` for a new, reversed copy.

:::

## Mutating or returning a new list

Several operations come in two forms. The mutating form changes the list. The other form leaves the list alone and returns a new `NonEmptyList<T>`.

| Changes the list | Returns a new list |
|------------------|--------------------|
| `Add(item)` | `Append(item)` |
| `AddFirst(item)` | `Prepend(item)` |
| `AddRange(items)` | `Concat(items)` |
| `Reverse()` | `ReverseList()` |

:::warning

In version 2, `Prepend(item)` inserted the item in place. In version 3 it returns a new list and leaves the original unchanged, like LINQ's `Prepend`. If you relied on the old behaviour, call `AddFirst(item)`.

:::

## Through interfaces

The guarantee holds whichever type you hold the list as.

| Through | Behaviour |
|---------|-----------|
| `IList<T>`, `ICollection<T>` | `Add`, `Insert`, `Remove`, `RemoveAt`, the indexer and `Clear` run the same code as on the class, with the same exceptions. `IsReadOnly` is `false`. |
| `IList` (non-generic) | `Add`, `Insert` and the indexer setter throw `ArgumentNullException` for `null` and `ArgumentException` for a value that is not a `T`. `Remove` ignores a value that is not a `T`. `Clear` throws `NotSupportedException`. `IsReadOnly` and `IsFixedSize` are `false`. |
| `IReadOnlyList<T>` | Read-only access. |

```csharp
NonEmptyList<int> numbers = [1, 2];
ICollection<int> asCollection = numbers;

asCollection.Remove(1); // true
asCollection.Remove(2); // InvalidOperationException: 2 is the only element left
```

## Views and copies

Some members hand out a view of the list, others a copy. The difference matters when the list changes later.

| Member | Returns | Sees later changes |
|--------|---------|--------------------|
| `AsReadOnly()` | `ReadOnlyCollection<T>` wrapping the list | Yes |
| `AsSpan()` | `ReadOnlySpan<T>` over the storage | Do not add or remove elements while you use the span |
| `Tail`, `Init` | A new `NonEmptyList<T>?` on every read | No |
| `ToArray()`, `GetRange`, `Slice`, `xs[1..]` | A copy | No |

`Tail` and `Init` build a new list on every read, so you can change the result freely: `list.Tail!.Add(x)` changes neither `list` nor the next `list.Tail`.

## Adding your own invariants

You can derive from `NonEmptyList<T>` to add rules on top of "at least one element", such as "no duplicates" or "at most ten entries". You do not reimplement the list. You override protected hooks, and every mutating member calls them, including calls through the interfaces.

| Hook | Called | Default |
|------|--------|---------|
| `protected virtual bool IsReadOnly` | Read by `ICollection<T>.IsReadOnly` and `IList.IsReadOnly`. | `false` |
| `protected virtual void EnsureMutable()` | Before every operation that changes the contents or the order: the add, insert, remove, replace, sort and reverse members, and `Clear`. | Does nothing |
| `protected virtual void ValidateItem(T item)` | For each element added by `Add`, `AddFirst` and `Insert`, before the list changes. | Does nothing |
| `protected virtual void ValidateRange(IReadOnlyList<T> items)` | Once for the batch passed to `AddRange` or `InsertRange`, before the list changes. | Calls `ValidateItem` for each element |
| `protected virtual void ValidateReplacement(int index, T item)` | Before an element is replaced through the indexer, or for each element written by `SetRange`. | Does nothing |
| `protected virtual void ValidateCount(int proposedCount)` | Before any add or remove changes the number of elements, with the count the list would have afterwards. | Does nothing |

The hooks always receive non-`null` elements, and `proposedCount` is always at least 1, because the null checks and the non-empty checks run first. A hook rejects an operation by throwing. Since every hook runs before the list changes, a rejected operation leaves the list as it was.

To create the derived list, chain to the protected copy constructor, `protected NonEmptyList(NonEmptyList<T> source)`. It copies the elements and throws `ArgumentNullException` when `source` is `null`.

```csharp title="TagList.cs"
using Masterly.NonEmptyList;

#pragma warning disable CA2260 // NonEmptyList<T> is a collection base class, not a self-referencing generic

/// <summary>A non-empty list of unique tags with at most ten entries.</summary>
public sealed class TagList : NonEmptyList<string>
{
    public const int MaxTags = 10;

    public TagList(IEnumerable<string> tags) : base(NonEmptyList<string>.From(tags))
    {
        // The hooks do not run during construction, so check the initial contents here.
        if (Count > MaxTags)
            throw new ArgumentException($"A tag list holds at most {MaxTags} tags.", nameof(tags));

        if (DistinctList().Count != Count)
            throw new ArgumentException("Tags must be unique.", nameof(tags));
    }

    protected override void ValidateItem(string item)
    {
        if (Contains(item))
            throw new ArgumentException($"The tag '{item}' is already in the list.", nameof(item));
    }

    protected override void ValidateRange(IReadOnlyList<string> items)
    {
        base.ValidateRange(items); // runs ValidateItem for each new tag

        if (items.Distinct().Count() != items.Count)
            throw new ArgumentException("The new tags contain duplicates.", nameof(items));
    }

    protected override void ValidateReplacement(int index, string item)
    {
        int existing = IndexOf(item);
        if (existing >= 0 && existing != index)
            throw new ArgumentException($"The tag '{item}' is already in the list.", nameof(item));
    }

    protected override void ValidateCount(int proposedCount)
    {
        if (proposedCount > MaxTags)
            throw new InvalidOperationException($"A tag list holds at most {MaxTags} tags.");
    }
}
```

```csharp title="Using TagList"
TagList tags = new(["dotnet", "csharp"]);

tags.Add("linq");                 // fine
tags.Add("dotnet");               // ArgumentException: already in the list
tags[0] = "csharp";               // ArgumentException: "csharp" is already at index 1
tags.AddRange(["efcore", "efcore"]); // ArgumentException: duplicates in the batch
```

To make a derived type read-only, override `IsReadOnly` and `EnsureMutable` together:

```csharp title="FrozenList.cs"
#pragma warning disable CA2260

public sealed class FrozenList<T> : NonEmptyList<T>
{
    public FrozenList(NonEmptyList<T> source) : base(source)
    {
    }

    protected override bool IsReadOnly => true;

    protected override void EnsureMutable() =>
        throw new NotSupportedException("This list is read-only.");
}
```

Every mutating member of a `FrozenList<T>`, including `Sort`, `Reverse` and `Clear`, now throws `NotSupportedException`, and `((ICollection<T>)frozen).IsReadOnly` is `true`.

What else you should know:

- The hooks guard mutation only. The constructors do not call them, so validate the initial contents in your own constructor, as `TagList` does.
- Operators that return a new list, such as `Append`, `Map`, `Select` or `ReverseList`, return a plain `NonEmptyList<T>`, not your derived type, and do not run your hooks.
- The code analysis rule CA2260 fires on classes that derive from `NonEmptyList<T>`, because the base class implements `ISpanParsable<NonEmptyList<T>>`. The warning does not apply to a collection base class. Suppress it as shown.

:::tip

You rarely need to write these classes by hand. The `[NonEmptyCollection]` source generator emits a derived collection with `Distinct`, `MinCount`, `MaxCount` and `Immutable` rules wired to exactly these hooks. See [Domain collections](./domain-collections.md). For element-count and ordering rules carried in the type itself, see [Constrained lists](./constrained-lists.md).

:::
