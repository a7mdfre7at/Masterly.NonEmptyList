// Ignore Spelling: Zipper

using System.Diagnostics;

namespace Masterly.NonEmptyList;

/// <summary>
/// An immutable list with a cursor that always points at an element. Moving the cursor past either end
/// returns <see langword="null"/> instead of throwing, so wizard steps, carousels, tab strips and playlists
/// never dereference a missing "current" item. Every move is O(1).
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
[DebuggerDisplay("Focus = {Focus}, Index = {Index}, Count = {Count}")]
public sealed class NonEmptyZipper<T> : IEquatable<NonEmptyZipper<T>>
{
    // _left is stored in reverse order so that moving left is O(1).
    private readonly ImmutableNonEmptyList<T>? _left;
    private readonly ImmutableNonEmptyList<T>? _right;

    private NonEmptyZipper(ImmutableNonEmptyList<T>? left, T focus, ImmutableNonEmptyList<T>? right)
    {
        _left = left;
        Focus = focus;
        _right = right;
    }

    /// <summary>Creates a zipper focused on the first element of the list.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the list is null.</exception>
    public static NonEmptyZipper<T> FromStart(NonEmptyList<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return new NonEmptyZipper<T>(null, list.Head, list.Tail?.ToImmutable());
    }

    /// <summary>Creates a zipper focused on the last element of the list.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the list is null.</exception>
    public static NonEmptyZipper<T> FromEnd(NonEmptyList<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        ImmutableNonEmptyList<T>? left = list.Init is { } init ? ImmutableNonEmptyList<T>.From(init.ReverseList()) : null;
        return new NonEmptyZipper<T>(left, list.Last, null);
    }

    /// <summary>Creates a zipper focused on the element at <paramref name="index"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the list.</exception>
    public static NonEmptyZipper<T> FromIndex(NonEmptyList<T> list, int index)
    {
        ArgumentNullException.ThrowIfNull(list);
        if (index < 0 || index >= list.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        ImmutableNonEmptyList<T>? left = index == 0 ? null : ImmutableNonEmptyList<T>.From(list.Slice(0, index).AsEnumerable().Reverse());
        ImmutableNonEmptyList<T>? right = index == list.Count - 1 ? null : ImmutableNonEmptyList<T>.From(list.Slice(index + 1, list.Count - index - 1));
        return new NonEmptyZipper<T>(left, list[index], right);
    }

    /// <summary>Creates a zipper over a single element.</summary>
    public static NonEmptyZipper<T> Singleton(T focus)
    {
        ArgumentNullException.ThrowIfNull(focus);
        return new NonEmptyZipper<T>(null, focus, null);
    }

    /// <summary>The element under the cursor. Always valid.</summary>
    public T Focus { get; }

    /// <summary>The zero-based position of the cursor.</summary>
    public int Index => _left?.Count ?? 0;

    /// <summary>The total number of elements.</summary>
    public int Count => (_left?.Count ?? 0) + 1 + (_right?.Count ?? 0);

    /// <summary><see langword="true"/> if the cursor is on the first element.</summary>
    public bool IsAtStart => _left is null;

    /// <summary><see langword="true"/> if the cursor is on the last element.</summary>
    public bool IsAtEnd => _right is null;

    /// <summary>The elements before the cursor, in list order. May be empty.</summary>
    public IEnumerable<T> Before => _left is null ? Enumerable.Empty<T>() : _left.Reverse();

    /// <summary>The elements after the cursor, in list order. May be empty.</summary>
    public IEnumerable<T> After => _right ?? Enumerable.Empty<T>();

    /// <summary>Moves the cursor one step left, or returns <see langword="null"/> when already at the start.</summary>
    public NonEmptyZipper<T>? MoveLeft()
        => _left is null ? null : new NonEmptyZipper<T>(_left.Tail, _left.Head, _right is null ? ImmutableNonEmptyList<T>.Singleton(Focus) : _right.Prepend(Focus));

    /// <summary>Moves the cursor one step right, or returns <see langword="null"/> when already at the end.</summary>
    public NonEmptyZipper<T>? MoveRight()
        => _right is null ? null : new NonEmptyZipper<T>(_left is null ? ImmutableNonEmptyList<T>.Singleton(Focus) : _left.Prepend(Focus), _right.Head, _right.Tail);

    /// <summary>Moves the cursor one step left, staying put at the start.</summary>
    public NonEmptyZipper<T> MoveLeftOrStay() => MoveLeft() ?? this;

    /// <summary>Moves the cursor one step right, staying put at the end.</summary>
    public NonEmptyZipper<T> MoveRightOrStay() => MoveRight() ?? this;

    /// <summary>Moves the cursor one step left, wrapping to the end when at the start.</summary>
    public NonEmptyZipper<T> MoveLeftCyclic() => MoveLeft() ?? ToEnd();

    /// <summary>Moves the cursor one step right, wrapping to the start when at the end.</summary>
    public NonEmptyZipper<T> MoveRightCyclic() => MoveRight() ?? ToStart();

    /// <summary>Moves the cursor to the first element.</summary>
    public NonEmptyZipper<T> ToStart() => IsAtStart ? this : FromStart(ToNonEmptyList());

    /// <summary>Moves the cursor to the last element.</summary>
    public NonEmptyZipper<T> ToEnd() => IsAtEnd ? this : FromEnd(ToNonEmptyList());

    /// <summary>Moves the cursor to <paramref name="index"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is outside the list.</exception>
    public NonEmptyZipper<T> MoveTo(int index) => index == Index ? this : FromIndex(ToNonEmptyList(), index);

    /// <summary>Moves the cursor to the first element matching <paramref name="predicate"/>, or returns <see langword="null"/>.</summary>
    public NonEmptyZipper<T>? Find(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        NonEmptyList<T> list = ToNonEmptyList();
        int index = list.FindIndex(x => predicate(x));
        return index < 0 ? null : FromIndex(list, index);
    }

    /// <summary>Replaces the focused element.</summary>
    public NonEmptyZipper<T> Replace(T focus)
    {
        ArgumentNullException.ThrowIfNull(focus);
        return new NonEmptyZipper<T>(_left, focus, _right);
    }

    /// <summary>Applies a function to the focused element.</summary>
    public NonEmptyZipper<T> Update(Func<T, T> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return Replace(update(Focus));
    }

    /// <summary>Inserts an element immediately after the cursor without moving it.</summary>
    public NonEmptyZipper<T> InsertAfter(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new NonEmptyZipper<T>(_left, Focus, _right is null ? ImmutableNonEmptyList<T>.Singleton(item) : _right.Prepend(item));
    }

    /// <summary>Inserts an element immediately before the cursor without moving it.</summary>
    public NonEmptyZipper<T> InsertBefore(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new NonEmptyZipper<T>(_left is null ? ImmutableNonEmptyList<T>.Singleton(item) : _left.Prepend(item), Focus, _right);
    }

    /// <summary>
    /// Removes the focused element. The cursor moves to the next element, or to the previous one when at the end.
    /// Returns <see langword="null"/> if this was the only element, because a zipper cannot be empty.
    /// </summary>
    public NonEmptyZipper<T>? Delete()
    {
        if (_right is not null)
            return new NonEmptyZipper<T>(_left, _right.Head, _right.Tail);

        if (_left is not null)
            return new NonEmptyZipper<T>(_left.Tail, _left.Head, null);

        return null;
    }

    /// <summary>Applies a function to every element, keeping the cursor position.</summary>
    public NonEmptyZipper<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new NonEmptyZipper<TResult>(_left?.Map(selector), selector(Focus), _right?.Map(selector));
    }

    /// <summary>Returns the elements in list order.</summary>
    public NonEmptyList<T> ToNonEmptyList()
    {
        List<T> items = new(Count);
        if (_left is not null)
        {
            for (int i = _left.Count - 1; i >= 0; i--)
                items.Add(_left[i]);
        }

        items.Add(Focus);
        if (_right is not null)
            items.AddRange(_right);

        return NonEmptyList<T>.FromTrustedList(items);
    }

    /// <summary>Deconstructs into the elements before, the focus and the elements after.</summary>
    public void Deconstruct(out IEnumerable<T> before, out T focus, out IEnumerable<T> after)
    {
        before = Before;
        focus = Focus;
        after = After;
    }

    /// <inheritdoc />
    public bool Equals(NonEmptyZipper<T>? other)
        => other is not null && Index == other.Index && ToNonEmptyList().Equals(other.ToNonEmptyList());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NonEmptyZipper<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Index, ToNonEmptyList().GetHashCode());

    /// <inheritdoc />
    public override string ToString()
        => $"NonEmptyZipper: [{string.Join(", ", Before)}] >{Focus}< [{string.Join(", ", After)}]";
}

/// <summary>Extension methods that create zippers.</summary>
public static class NonEmptyZipperExtensions
{
    /// <summary>Creates a zipper focused on the first element.</summary>
    public static NonEmptyZipper<T> ToZipper<T>(this NonEmptyList<T> list) => NonEmptyZipper<T>.FromStart(list);

    /// <summary>Creates a zipper focused on the element at <paramref name="index"/>.</summary>
    public static NonEmptyZipper<T> ToZipper<T>(this NonEmptyList<T> list, int index) => NonEmptyZipper<T>.FromIndex(list, index);

    /// <summary>Creates a zipper focused on the first element.</summary>
    public static NonEmptyZipper<T> ToZipper<T>(this ImmutableNonEmptyList<T> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return NonEmptyZipper<T>.FromStart(list.ToMutable());
    }
}

/// <summary>
/// An undo/redo history that always has a current state. <see cref="Undo"/> on the first state and <see cref="Redo"/>
/// with nothing to redo return the same history instead of throwing. Pushing a new state discards the redo branch,
/// matching every editor users know.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
[DebuggerDisplay("Current = {Current}, Undo = {UndoCount}, Redo = {RedoCount}")]
public sealed class History<TState> : IEquatable<History<TState>>
{
    private readonly NonEmptyZipper<TState> _zipper;

    private History(NonEmptyZipper<TState> zipper) => _zipper = zipper;

    /// <summary>Starts a history with an initial state.</summary>
    public static History<TState> Start(TState initial) => new(NonEmptyZipper<TState>.Singleton(initial));

    /// <summary>The current state. Always valid.</summary>
    public TState Current => _zipper.Focus;

    /// <summary>The number of states that can be undone.</summary>
    public int UndoCount => _zipper.Index;

    /// <summary>The number of states that can be redone.</summary>
    public int RedoCount => _zipper.Count - _zipper.Index - 1;

    /// <summary><see langword="true"/> if <see cref="Undo"/> would change the current state.</summary>
    public bool CanUndo => !_zipper.IsAtStart;

    /// <summary><see langword="true"/> if <see cref="Redo"/> would change the current state.</summary>
    public bool CanRedo => !_zipper.IsAtEnd;

    /// <summary>Records a new state, discarding any states that could have been redone.</summary>
    public History<TState> Push(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        NonEmptyList<TState> kept = NonEmptyList<TState>.From(_zipper.Before.Append(_zipper.Focus));
        kept.Add(state);
        return new History<TState>(NonEmptyZipper<TState>.FromEnd(kept));
    }

    /// <summary>Records the state produced by applying <paramref name="update"/> to the current state.</summary>
    public History<TState> Push(Func<TState, TState> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return Push(update(Current));
    }

    /// <summary>Moves back one state, or returns this history when there is nothing to undo.</summary>
    public History<TState> Undo() => _zipper.MoveLeft() is { } moved ? new History<TState>(moved) : this;

    /// <summary>Moves forward one state, or returns this history when there is nothing to redo.</summary>
    public History<TState> Redo() => _zipper.MoveRight() is { } moved ? new History<TState>(moved) : this;

    /// <summary>Returns every recorded state from oldest to newest, including states that could be redone.</summary>
    public NonEmptyList<TState> States => _zipper.ToNonEmptyList();

    /// <inheritdoc />
    public bool Equals(History<TState>? other) => other is not null && _zipper.Equals(other._zipper);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is History<TState> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _zipper.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"History: {UndoCount} back, current = {Current}, {RedoCount} forward";
}

/// <summary>Factory for <see cref="History{TState}"/>.</summary>
public static class History
{
    /// <summary>Starts a history with an initial state.</summary>
    public static History<TState> Start<TState>(TState initial) => History<TState>.Start(initial);
}
