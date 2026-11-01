using System.Diagnostics;

namespace Masterly.NonEmptyList;

/// <summary>
/// An append-only sequence of events that always contains the event that created the aggregate.
/// Because the origin is guaranteed, <see cref="Replay{TState}(Func{TEvent, TState}, Func{TState, TEvent, TState})"/> needs no "uninitialized" state and
/// <see cref="Origin"/> needs no null check.
/// </summary>
/// <typeparam name="TEvent">The event base type.</typeparam>
[DebuggerDisplay("Version = {Version}, Origin = {Origin}")]
public sealed class EventStream<TEvent> : IReadOnlyList<TEvent>, IEquatable<EventStream<TEvent>>
{
    private readonly ImmutableNonEmptyList<TEvent> _events;

    private EventStream(ImmutableNonEmptyList<TEvent> events) => _events = events;

    /// <summary>Starts a stream with its creation event.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the event is null.</exception>
    public static EventStream<TEvent> Start(TEvent origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        return new EventStream<TEvent>(ImmutableNonEmptyList<TEvent>.Singleton(origin));
    }

    /// <summary>Rebuilds a stream from stored events. The first event is the origin.</summary>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty.</exception>
    public static EventStream<TEvent> From(IEnumerable<TEvent> events)
        => new(ImmutableNonEmptyList<TEvent>.From(events));

    /// <summary>Rebuilds a stream from a non-empty list of stored events.</summary>
    public static EventStream<TEvent> From(NonEmptyList<TEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return new EventStream<TEvent>(events.ToImmutable());
    }

    /// <summary>The event that created the aggregate. Never null.</summary>
    public TEvent Origin => _events.Head;

    /// <summary>The most recent event. Equals <see cref="Origin"/> for a fresh stream.</summary>
    public TEvent Latest => _events.Last;

    /// <summary>The number of events. Starts at 1, so it doubles as an optimistic-concurrency version.</summary>
    public int Version => _events.Count;

    /// <inheritdoc />
    public int Count => _events.Count;

    /// <summary>All events, oldest first.</summary>
    public ImmutableNonEmptyList<TEvent> Events => _events;

    /// <inheritdoc />
    public TEvent this[int index] => _events[index];

    /// <summary>Returns a stream with one more event.</summary>
    /// <exception cref="ArgumentNullException">Thrown if the event is null.</exception>
    public EventStream<TEvent> Append(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return new EventStream<TEvent>(_events.Append(@event));
    }

    /// <summary>Returns a stream with the given events appended in order. An empty sequence returns this stream.</summary>
    public EventStream<TEvent> AppendRange(IEnumerable<TEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        List<TEvent> list = events.ToList();
        return list.Count == 0 ? this : new EventStream<TEvent>(_events.AppendRange(list));
    }

    /// <summary>
    /// Folds the stream into a state. The origin produces the initial state, every later event is applied in order.
    /// </summary>
    /// <typeparam name="TState">The aggregate state type.</typeparam>
    /// <param name="create">Builds the initial state from the origin event.</param>
    /// <param name="apply">Applies a subsequent event to the state.</param>
    public TState Replay<TState>(Func<TEvent, TState> create, Func<TState, TEvent, TState> apply)
    {
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(apply);

        TState state = create(Origin);
        for (int i = 1; i < _events.Count; i++)
            state = apply(state, _events[i]);

        return state;
    }

    /// <summary>Folds the stream into a state when the origin and later events share one apply function.</summary>
    public TState Replay<TState>(TState seed, Func<TState, TEvent, TState> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        return _events.Fold(seed, apply);
    }

    /// <summary>
    /// Returns every intermediate state, one per event. The result has exactly <see cref="Version"/> entries,
    /// so the state after event <c>n</c> is at index <c>n</c>.
    /// </summary>
    public NonEmptyList<TState> ReplayHistory<TState>(Func<TEvent, TState> create, Func<TState, TEvent, TState> apply)
    {
        ArgumentNullException.ThrowIfNull(create);
        ArgumentNullException.ThrowIfNull(apply);

        List<TState> states = new(_events.Count) { create(Origin) };
        for (int i = 1; i < _events.Count; i++)
            states.Add(apply(states[i - 1], _events[i]));

        return NonEmptyList<TState>.FromTrustedList(states);
    }

    /// <summary>Returns the events recorded after <paramref name="version"/>. May be empty.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the version is negative or beyond the stream.</exception>
    public IReadOnlyList<TEvent> Since(int version)
    {
        if (version < 0 || version > Version)
            throw new ArgumentOutOfRangeException(nameof(version));

        return _events.Slice(version, Version - version);
    }

    /// <summary>Returns the stream truncated to its first <paramref name="version"/> events.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the version is less than 1 or beyond the stream.</exception>
    public EventStream<TEvent> UpTo(int version)
    {
        if (version < 1 || version > Version)
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be between 1 and the current version");

        return version == Version ? this : new EventStream<TEvent>(ImmutableNonEmptyList<TEvent>.From(_events.Slice(0, version)));
    }

    /// <summary>Returns the events of the given derived type, oldest first. May be empty.</summary>
    public IEnumerable<TDerived> OfType<TDerived>() where TDerived : TEvent => _events.OfType<TDerived>();

    /// <summary>Returns the origin as <typeparamref name="TOrigin"/>.</summary>
    /// <exception cref="InvalidCastException">Thrown if the origin is not a <typeparamref name="TOrigin"/>.</exception>
    public TOrigin OriginAs<TOrigin>() where TOrigin : TEvent
        => Origin is TOrigin typed ? typed : throw new InvalidCastException($"The origin event is a {Origin!.GetType().Name}, not a {typeof(TOrigin).Name}");

    /// <inheritdoc />
    public IEnumerator<TEvent> GetEnumerator() => _events.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Equals(EventStream<TEvent>? other) => other is not null && _events.Equals(other._events);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EventStream<TEvent> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _events.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"EventStream: v{Version}, [{string.Join(", ", _events)}]";
}

/// <summary>Factories for <see cref="EventStream{TEvent}"/>.</summary>
public static class EventStream
{
    /// <summary>Starts a stream with its creation event.</summary>
    public static EventStream<TEvent> Start<TEvent>(TEvent origin) => EventStream<TEvent>.Start(origin);

    /// <summary>Rebuilds a stream from stored events.</summary>
    public static EventStream<TEvent> From<TEvent>(IEnumerable<TEvent> events) => EventStream<TEvent>.From(events);

    /// <summary>Rebuilds a stream from stored events.</summary>
    public static EventStream<TEvent> ToEventStream<TEvent>(this NonEmptyList<TEvent> events) => EventStream<TEvent>.From(events);
}
