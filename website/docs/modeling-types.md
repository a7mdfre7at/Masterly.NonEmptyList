---
id: modeling-types
title: "Modeling Types"
description: "NonEmptyZipper<T>, History<T>, EventStream<TEvent> and NonEmptyTree<T>: cursors, undo stacks, event streams and trees that always have a current item, a current state, an origin event or a root."
---

Some domain shapes are non-empty by nature. A wizard always has a current step. An editor always has a current document. An event-sourced aggregate always has the event that created it. An org chart always has a root. The four types on this page encode that, so the "nothing there" case disappears from your code. All four are immutable: every operation returns a new value.

## NonEmptyZipper&lt;T&gt;

A zipper is a list with a cursor that always points at an element, the `Focus`. Moving past either end returns `null` instead of throwing, so a wizard, carousel, tab strip or playlist never dereferences a missing "current" item.

```csharp title="Checkout wizard"
using Masterly.NonEmptyList;

public sealed record Step(string Name, bool IsComplete = false);

NonEmptyList<Step> steps = [new("Cart"), new("Address"), new("Payment"), new("Review")];
NonEmptyZipper<Step> wizard = steps.ToZipper();

// "Next" button: mark the step complete and move on, or stay on the last step
wizard = wizard.Update(s => s with { IsComplete = true });
wizard = wizard.MoveRight() ?? wizard;

Console.WriteLine(wizard.Focus.Name);                       // Address
Console.WriteLine($"Step {wizard.Index + 1} of {wizard.Count}");
bool showBack = !wizard.IsAtStart;
bool showFinish = wizard.IsAtEnd;
```

**Creating a zipper.**

| Call | Focus |
|------|-------|
| `list.ToZipper()` or `NonEmptyZipper<T>.FromStart(list)` | the first element |
| `list.ToZipper(index)` or `NonEmptyZipper<T>.FromIndex(list, index)` | the element at `index`; `ArgumentOutOfRangeException` outside the list |
| `NonEmptyZipper<T>.FromEnd(list)` | the last element |
| `NonEmptyZipper<T>.Singleton(item)` | the only element |

`ToZipper()` works on a `NonEmptyList<T>` and on an `ImmutableNonEmptyList<T>`.

**Moving the cursor.**

| Method | At the edge |
|--------|-------------|
| `MoveLeft()`, `MoveRight()` | returns `null` |
| `MoveLeftOrStay()`, `MoveRightOrStay()` | returns the same zipper |
| `MoveLeftCyclic()`, `MoveRightCyclic()` | wraps to the other end |
| `ToStart()`, `ToEnd()` | jumps to the first or last element |
| `MoveTo(index)` | jumps to `index`; `ArgumentOutOfRangeException` outside the list |
| `Find(predicate)` | jumps to the first matching element, or returns `null` |

**Reading and editing.**

| Member | Purpose |
|--------|---------|
| `Focus`, `Index`, `Count` | the current element, its position and the total |
| `IsAtStart`, `IsAtEnd` | whether the cursor is on the first or last element |
| `Before`, `After` | the elements on each side, in list order; either may be empty |
| `Replace(item)`, `Update(f)` | change the focused element |
| `InsertBefore(item)`, `InsertAfter(item)` | add an element next to the cursor; the focus stays on the same element |
| `Delete()` | removes the focus; the cursor moves to the next element, or to the previous one at the end; returns `null` when it was the only element |
| `Map(f)` | transforms every element and keeps the cursor position |
| `ToNonEmptyList()` | all elements in list order |

You can deconstruct a zipper into its three parts: `var (before, focus, after) = wizard;`. Two zippers are equal when they hold equal elements and the cursor is at the same index.

## History&lt;T&gt;

`History<T>` is an undo/redo stack that always has a current state. `Undo()` at the first state and `Redo()` with nothing to redo return the same history instead of throwing. Pushing a new state discards everything that could have been redone, like every editor users know.

```csharp title="Document editor"
public sealed record Document(string Text);

History<Document> history = History.Start(new Document(""));

history = history.Push(d => d with { Text = "Hello" });
history = history.Push(d => d with { Text = "Hello, world" });
history = history.Undo();

Console.WriteLine(history.Current.Text);   // Hello
Console.WriteLine(history.CanRedo);        // True

history = history.Push(d => d with { Text = "Hello there" });
Console.WriteLine(history.CanRedo);        // False: the redo branch is gone
```

| Member | Purpose |
|--------|---------|
| `History.Start(initial)` or `History<T>.Start(initial)` | creates a history with one state |
| `Current` | the current state; always valid |
| `Push(state)`, `Push(current => next)` | records a new state and discards the redo branch |
| `Undo()`, `Redo()` | moves one step; returns the same history when there is nothing to move to |
| `CanUndo`, `CanRedo`, `UndoCount`, `RedoCount` | what the undo and redo buttons need |
| `States` | every recorded state from oldest to newest, including the ones that could be redone, as a `NonEmptyList<T>` |

`Start` and `Push` throw `ArgumentNullException` for a `null` state.

## EventStream&lt;TEvent&gt;

`EventStream<TEvent>` is an append-only sequence of events that always contains the event that created the aggregate. Because the origin is guaranteed, rebuilding state needs no "uninitialized" placeholder and `Origin` needs no null check.

```csharp title="Order event sourcing"
public abstract record OrderEvent;
public sealed record OrderPlaced(Guid OrderId, string CustomerId) : OrderEvent;
public sealed record LineAdded(string Sku, int Quantity) : OrderEvent;
public sealed record OrderShipped(DateTimeOffset At) : OrderEvent;

public sealed record Order(Guid Id, string CustomerId, int Lines, bool Shipped)
{
    public static Order Create(OrderEvent origin) => origin switch
    {
        OrderPlaced placed => new Order(placed.OrderId, placed.CustomerId, 0, false),
        _ => throw new InvalidOperationException($"An order cannot start with {origin.GetType().Name}"),
    };

    public Order Apply(OrderEvent e) => e switch
    {
        LineAdded => this with { Lines = Lines + 1 },
        OrderShipped => this with { Shipped = true },
        _ => this,
    };
}
```

```csharp
EventStream<OrderEvent> stream = EventStream.Start<OrderEvent>(new OrderPlaced(Guid.NewGuid(), "c-42"))
    .Append(new LineAdded("SKU-1", 2))
    .Append(new LineAdded("SKU-2", 1));

Order order = stream.Replay(Order.Create, (state, e) => state.Apply(e));
Console.WriteLine(order.Lines);          // 2
Console.WriteLine(stream.Version);       // 3

OrderPlaced placed = stream.OriginAs<OrderPlaced>();
```

Pass the type argument to `EventStream.Start<OrderEvent>(...)`. Without it, the compiler infers the stream type from the origin, here `EventStream<OrderPlaced>`, and you cannot append other events.

**Building and reading a stream.**

| Member | Purpose |
|--------|---------|
| `EventStream.Start<TEvent>(origin)` | a new stream with its creation event; `ArgumentNullException` for `null` |
| `EventStream.From(events)`, `EventStream<TEvent>.From(events)`, `events.ToEventStream()` | rebuild from stored events; the first event is the origin; an empty sequence throws `ArgumentException` |
| `Append(e)`, `AppendRange(events)` | a new stream with more events; an empty range returns the same stream |
| `Origin`, `Latest` | the first and the most recent event; equal for a fresh stream |
| `Version`, `Count` | the number of events; starts at 1 |
| `Events` | all events, oldest first, as an `ImmutableNonEmptyList<TEvent>` |
| `stream[i]`, enumeration | the stream is an `IReadOnlyList<TEvent>` |
| `OfType<TDerived>()` | the events of one type, oldest first; may be empty |
| `OriginAs<TOrigin>()` | the origin as a derived type; `InvalidCastException` when it is not one |

**Replaying.**

| Method | Behaviour |
|--------|-----------|
| `Replay(create, apply)` | `create` builds the first state from the origin; `apply` folds every later event in order |
| `Replay(seed, apply)` | folds every event, including the origin, starting from `seed` |
| `ReplayHistory(create, apply)` | every intermediate state as a `NonEmptyList<TState>` with exactly `Version` entries; index `i` holds the state after `stream[i]` |

**Versions and concurrency.** `Version` doubles as an optimistic-concurrency token.

| Method | Returns | Throws `ArgumentOutOfRangeException` when |
|--------|---------|-------------------------------------------|
| `Since(version)` | the events recorded after `version`, as an `IReadOnlyList<TEvent>`; may be empty | `version` is negative or greater than `Version` |
| `UpTo(version)` | the stream truncated to its first `version` events | `version` is less than 1 or greater than `Version` |

```csharp
// loadedVersion is the version you read from storage
int loadedVersion = 1;
IReadOnlyList<OrderEvent> toPersist = stream.Since(loadedVersion);   // the two LineAdded events

EventStream<OrderEvent> asPlaced = stream.UpTo(1);                   // just the origin
```

Two streams are equal when they hold equal events in the same order.

## NonEmptyTree&lt;T&gt;

`NonEmptyTree<T>` is an immutable rose tree: a value and any number of child trees. The root always exists, so flattening, the leaves and the root-to-leaf paths are never empty, and a bottom-up fold can call `Max` on its children's results.

```csharp title="Org chart"
public sealed record Employee(int Id, string Name, int? ManagerId);

// rows come from a self-referencing table
Employee[] rows =
[
    new(1, "Grace", null),
    new(2, "Ada", 1),
    new(3, "Linus", 1),
    new(4, "Barbara", 2),
    new(5, "Ken", 2),
];

NonEmptyList<NonEmptyTree<Employee>>? roots = NonEmptyTree.FromParentKeys(rows, e => e.Id, e => e.ManagerId);
NonEmptyTree<Employee> org = roots!.Head;

int headcount = org.Count;                                        // 5
int levels = org.Depth;                                           // 3
NonEmptyList<Employee> individualContributors = org.Leaves();     // Barbara, Ken, Linus
NonEmptyList<Employee>? chain = org.PathTo(e => e.Name == "Ken"); // Grace, Ada, Ken

// The largest number of direct reports anywhere in the tree
int largestTeam = org.Fold<int>((_, below) => below.Count == 0 ? 0 : Math.Max(below.Count, below.Max()));
Console.WriteLine(org.Map(e => e.Name).ToOutline());
```

`ToOutline()` prints one node per line, indented by depth:

```text title="Console output"
Grace
  Ada
    Barbara
    Ken
  Linus
```

**Building a tree.**

| Call | Builds |
|------|--------|
| `new NonEmptyTree<T>(value)` or `NonEmptyTree.Leaf(value)` | a leaf |
| `NonEmptyTree.Create(value, child1, child2)` or `NonEmptyTree.Create(value, children)` | a node with child trees |
| `NonEmptyTree.Create(value, leafValues)` | a node whose children are leaves, from an `IEnumerable<T>` |
| `NonEmptyTree.Unfold(root, node => children)` | a tree grown from a root and a function that returns each node's children |
| `NonEmptyTree.FromParentKeys(rows, keySelector, parentKeySelector)` | a `NonEmptyList<NonEmptyTree<T>>?` from flat rows; rows with a `null` parent key become roots; returns `null` when no row is a root; works with class keys and nullable value-type keys |

A `null` value or child throws `ArgumentNullException`.

**Querying.**

| Member | Returns |
|--------|---------|
| `Value`, `Children`, `IsLeaf` | the node's value, its child trees (may be empty), whether it has none |
| `Count`, `Depth` | the number of nodes; the number of levels, where a leaf has depth 1 |
| `Flatten()` | every value in pre-order (root first) as a `NonEmptyList<T>` |
| `FlattenPostOrder()` | every value in post-order (root last) |
| `FlattenBreadthFirst()` | every value level by level |
| `Leaves()` | the leaf values, left to right; a single node is its own leaf |
| `PathsToLeaves()` | every root-to-leaf path, as a `NonEmptyList<NonEmptyList<T>>` |
| `Levels()` | the values at each depth, root level first, as a `NonEmptyList<NonEmptyList<T>>` |
| `Find(predicate)` | the first matching subtree in pre-order, or `null` |
| `PathTo(predicate)` | the values from the root to the first match, or `null` |
| `Any(predicate)`, `All(predicate)` | whether some or every node matches |

**Transforming.** Every method returns a new tree.

| Method | Result |
|--------|--------|
| `Map(f)` | the same shape with every value transformed |
| `MapWithDepth((value, depth) => ...)` | like `Map`, with the root at depth 0 |
| `Fold<TResult>((value, childResults) => ...)` | a bottom-up fold; `childResults` is empty for leaves |
| `PruneChildren(keep)` | removes every subtree whose root fails `keep`; the root itself always stays |
| `AddChild(tree)`, `AddChild(value)` | appends a child |
| `WithValue(value)` | replaces the root value |

`Fold` needs its type argument, as in `org.Fold<int>(...)`, because the compiler cannot infer it from the lambda. Two trees are equal when their values and their children are equal, in order. You can deconstruct a node with `var (value, children) = org;`.
