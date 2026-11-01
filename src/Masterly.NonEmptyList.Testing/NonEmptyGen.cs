using CsCheck;

namespace Masterly.NonEmptyList.Testing;

/// <summary>
/// CsCheck generators for the non-empty types. Every generator produces at least one element, so properties never
/// need an "if empty" guard.
/// </summary>
/// <example>
/// <code>
/// NonEmptyGen.List(Gen.Int).Sample(xs => xs.Map(x => x) == xs);
/// </code>
/// </example>
public static class NonEmptyGen
{
    /// <summary>Generates a <see cref="NonEmptyList{T}"/> with between 1 and <paramref name="maxCount"/> elements.</summary>
    public static Gen<NonEmptyList<T>> List<T>(Gen<T> element, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.List[1, Math.Max(1, maxCount)].Select(static items => new NonEmptyList<T>(items));
    }

    /// <summary>Generates an <see cref="ImmutableNonEmptyList{T}"/> with between 1 and <paramref name="maxCount"/> elements.</summary>
    public static Gen<ImmutableNonEmptyList<T>> Immutable<T>(Gen<T> element, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.List[1, Math.Max(1, maxCount)].Select(static items => new ImmutableNonEmptyList<T>(items));
    }

    /// <summary>Generates a <see cref="NonEmptySet{T}"/>. Duplicates collapse, so the count is at least 1 and at most <paramref name="maxCount"/>.</summary>
    public static Gen<NonEmptySet<T>> Set<T>(Gen<T> element, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.List[1, Math.Max(1, maxCount)].Select(static items => NonEmptySet<T>.From(items));
    }

    /// <summary>Generates a <see cref="NonEmptyDictionary{TKey, TValue}"/>. Duplicate keys collapse to the last value.</summary>
    public static Gen<NonEmptyDictionary<TKey, TValue>> Dictionary<TKey, TValue>(Gen<TKey> key, Gen<TValue> value, int maxCount = 20)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        return Gen.Select(key, value).List[1, Math.Max(1, maxCount)].Select(static pairs =>
        {
            Dictionary<TKey, TValue> dictionary = new();
            foreach ((TKey k, TValue v) in pairs)
                dictionary[k] = v;
            return NonEmptyDictionary<TKey, TValue>.From(dictionary);
        });
    }

    /// <summary>
    /// Generates a <see cref="ConstrainedList{T, TConstraint}"/> by filtering candidate lists with the constraint.
    /// Use a generator whose output usually satisfies the constraint; a constraint that almost never holds will be slow.
    /// </summary>
    public static Gen<ConstrainedList<T, TConstraint>> Constrained<T, TConstraint>(Gen<T> element, int minCount = 1, int maxCount = 20)
        where TConstraint : IListConstraint
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.List[Math.Max(1, minCount), Math.Max(Math.Max(1, minCount), maxCount)]
            .Where(static items => ConstrainedList<T, TConstraint>.Satisfies(items))
            .Select(static items => ConstrainedList<T, TConstraint>.From(items));
    }

    /// <summary>Generates a <see cref="ConstrainedList{T, Sorted}"/> by sorting candidate lists, so no filtering is needed.</summary>
    public static Gen<ConstrainedList<T, Sorted>> Sorted<T>(Gen<T> element, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.List[1, Math.Max(1, maxCount)].Select(static items =>
        {
            items.Sort();
            return ConstrainedList<T, Sorted>.From(items);
        });
    }

    /// <summary>Generates a <see cref="ConstrainedList{T, Distinct}"/> by de-duplicating candidate lists.</summary>
    public static Gen<ConstrainedList<T, Distinct>> Distinct<T>(Gen<T> element, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.List[1, Math.Max(1, maxCount)].Select(static items => ConstrainedList<T, Distinct>.From(items.Distinct()));
    }

    /// <summary>Generates a <see cref="NonEmptyString"/> of 1 to <paramref name="maxLength"/> characters with at least one non-whitespace character.</summary>
    public static Gen<NonEmptyString> String(int maxLength = 20)
        => Gen.String[1, Math.Max(1, maxLength)].Where(static s => !string.IsNullOrWhiteSpace(s)).Select(static s => new NonEmptyString(s));

    /// <summary>Generates a <see cref="NonEmptyString"/> using only the given characters.</summary>
    public static Gen<NonEmptyString> String(string alphabet, int maxLength = 20)
    {
        ArgumentNullException.ThrowIfNull(alphabet);
        return Gen.String[Gen.Char[alphabet], 1, Math.Max(1, maxLength)].Where(static s => !string.IsNullOrWhiteSpace(s)).Select(static s => new NonEmptyString(s));
    }

    /// <summary>Generates an <see cref="EventStream{TEvent}"/> with 1 to <paramref name="maxCount"/> events.</summary>
    public static Gen<EventStream<TEvent>> EventStream<TEvent>(Gen<TEvent> @event, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return @event.List[1, Math.Max(1, maxCount)].Select(static events => global::Masterly.NonEmptyList.EventStream<TEvent>.From(events));
    }

    /// <summary>Generates a <see cref="NonEmptyZipper{T}"/> with the cursor at a random position.</summary>
    public static Gen<NonEmptyZipper<T>> Zipper<T>(Gen<T> element, int maxCount = 20)
    {
        ArgumentNullException.ThrowIfNull(element);
        return List(element, maxCount).SelectMany(list => Gen.Int[0, list.Count - 1].Select(index => list.ToZipper(index)));
    }

    /// <summary>Generates a <see cref="NonEmptyTree{T}"/> with the given maximum depth and branching factor.</summary>
    public static Gen<NonEmptyTree<T>> Tree<T>(Gen<T> value, int maxDepth = 3, int maxChildren = 3)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Build(Math.Max(1, maxDepth));

        Gen<NonEmptyTree<T>> Build(int depth)
        {
            if (depth <= 1)
                return value.Select(static v => new NonEmptyTree<T>(v));

            return Gen.Select(value, Build(depth - 1).List[0, Math.Max(0, maxChildren)])
                .Select(static t => new NonEmptyTree<T>(t.Item1, t.Item2));
        }
    }

    /// <summary>Generates a <see cref="Validation{T, TError}"/> that is valid or invalid with roughly equal probability.</summary>
    public static Gen<Validation<T, TError>> Validation<T, TError>(Gen<T> value, Gen<TError> error, int maxErrors = 5)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(error);
        return Gen.OneOf(
            value.Select(static v => global::Masterly.NonEmptyList.Validation<T, TError>.Valid(v)),
            List(error, maxErrors).Select(static errors => global::Masterly.NonEmptyList.Validation<T, TError>.Invalid(errors)));
    }
}
