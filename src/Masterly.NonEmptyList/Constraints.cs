// CA2260 is a heuristic for the curiously recurring template pattern; see OrderedNonEmptyList.cs.
#pragma warning disable CA2260

// Ignore Spelling: Cardinality

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// A compile-time constraint over the contents of a list, checked once when a
/// <see cref="ConstrainedList{T, TConstraint}"/> is constructed. Constraints are types, so they compose:
/// <c>ConstrainedList&lt;Email, And&lt;AtLeast&lt;Two&gt;, Distinct&gt;&gt;</c> is a list of at least two distinct emails.
/// Every constraint is checked on top of the non-empty guarantee.
/// </summary>
public interface IListConstraint
{
    /// <summary>Returns <see langword="true"/> if the given, already non-empty, contents satisfy the constraint.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The elements to check. Never empty.</param>
    static abstract bool Holds<T>(ReadOnlySpan<T> items);

    /// <summary>A short, human-readable description used in exception and validation messages, such as "at least 2 elements".</summary>
    static abstract string Describe();
}

/// <summary>A type-level natural number used by <see cref="AtLeast{TCount}"/>, <see cref="AtMost{TCount}"/> and <see cref="Exactly{TCount}"/>.</summary>
public interface ICardinality
{
    /// <summary>The number this type represents.</summary>
    static abstract int Value { get; }
}

/// <summary>Marker implemented by every cardinality of two or more. Enables <c>Second()</c> on constrained lists.</summary>
public interface IAtLeastTwo;

/// <summary>Marker implemented by every cardinality of three or more. Enables <c>Third()</c> on constrained lists.</summary>
public interface IAtLeastThree;

/// <summary>The number one.</summary>
public readonly struct One : ICardinality { /// <inheritdoc />
    public static int Value => 1; }

/// <summary>The number two.</summary>
public readonly struct Two : ICardinality, IAtLeastTwo { /// <inheritdoc />
    public static int Value => 2; }

/// <summary>The number three.</summary>
public readonly struct Three : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 3; }

/// <summary>The number four.</summary>
public readonly struct Four : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 4; }

/// <summary>The number five.</summary>
public readonly struct Five : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 5; }

/// <summary>The number six.</summary>
public readonly struct Six : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 6; }

/// <summary>The number seven.</summary>
public readonly struct Seven : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 7; }

/// <summary>The number eight.</summary>
public readonly struct Eight : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 8; }

/// <summary>The number nine.</summary>
public readonly struct Nine : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 9; }

/// <summary>The number ten.</summary>
public readonly struct Ten : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 10; }

/// <summary>The number sixteen.</summary>
public readonly struct Sixteen : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 16; }

/// <summary>The number thirty-two.</summary>
public readonly struct ThirtyTwo : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 32; }

/// <summary>The number sixty-four.</summary>
public readonly struct SixtyFour : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 64; }

/// <summary>The number one hundred.</summary>
public readonly struct Hundred : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 100; }

/// <summary>The number one thousand.</summary>
public readonly struct Thousand : ICardinality, IAtLeastTwo, IAtLeastThree { /// <inheritdoc />
    public static int Value => 1000; }

/// <summary>The trivial constraint. <c>ConstrainedList&lt;T, NonEmpty&gt;</c> is an immutable <see cref="NonEmptyList{T}"/>.</summary>
public readonly struct NonEmpty : IListConstraint
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items) => items.Length > 0;

    /// <inheritdoc />
    public static string Describe() => "at least 1 element";
}

/// <summary>Requires at least <typeparamref name="TCount"/> elements.</summary>
public readonly struct AtLeast<TCount> : IListConstraint where TCount : ICardinality
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items) => items.Length >= TCount.Value;

    /// <inheritdoc />
    public static string Describe() => $"at least {TCount.Value} element{(TCount.Value == 1 ? "" : "s")}";
}

/// <summary>Requires at most <typeparamref name="TCount"/> elements.</summary>
public readonly struct AtMost<TCount> : IListConstraint where TCount : ICardinality
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items) => items.Length <= TCount.Value;

    /// <inheritdoc />
    public static string Describe() => $"at most {TCount.Value} element{(TCount.Value == 1 ? "" : "s")}";
}

/// <summary>Requires exactly <typeparamref name="TCount"/> elements.</summary>
public readonly struct Exactly<TCount> : IListConstraint where TCount : ICardinality
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items) => items.Length == TCount.Value;

    /// <inheritdoc />
    public static string Describe() => $"exactly {TCount.Value} element{(TCount.Value == 1 ? "" : "s")}";
}

/// <summary>Requires every element to be distinct according to <see cref="EqualityComparer{T}.Default"/>.</summary>
public readonly struct Distinct : IListConstraint
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items)
    {
        HashSet<T> seen = new(items.Length);
        foreach (T item in items)
        {
            if (!seen.Add(item))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public static string Describe() => "distinct elements";
}

/// <summary>Requires the elements to be in non-decreasing order according to <see cref="Comparer{T}.Default"/>.</summary>
public readonly struct Sorted : IListConstraint
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items)
    {
        Comparer<T> comparer = Comparer<T>.Default;
        for (int i = 1; i < items.Length; i++)
        {
            if (comparer.Compare(items[i - 1], items[i]) > 0)
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public static string Describe() => "elements in ascending order";
}

/// <summary>Requires the elements to be in non-increasing order according to <see cref="Comparer{T}.Default"/>.</summary>
public readonly struct SortedDescending : IListConstraint
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items)
    {
        Comparer<T> comparer = Comparer<T>.Default;
        for (int i = 1; i < items.Length; i++)
        {
            if (comparer.Compare(items[i - 1], items[i]) < 0)
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public static string Describe() => "elements in descending order";
}

/// <summary>Requires both <typeparamref name="TFirst"/> and <typeparamref name="TSecond"/> to hold.</summary>
public readonly struct And<TFirst, TSecond> : IListConstraint
    where TFirst : IListConstraint
    where TSecond : IListConstraint
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items) => TFirst.Holds(items) && TSecond.Holds(items);

    /// <inheritdoc />
    public static string Describe() => $"{TFirst.Describe()} and {TSecond.Describe()}";
}

/// <summary>Requires at least one of <typeparamref name="TFirst"/> and <typeparamref name="TSecond"/> to hold.</summary>
public readonly struct Or<TFirst, TSecond> : IListConstraint
    where TFirst : IListConstraint
    where TSecond : IListConstraint
{
    /// <inheritdoc />
    public static bool Holds<T>(ReadOnlySpan<T> items) => TFirst.Holds(items) || TSecond.Holds(items);

    /// <inheritdoc />
    public static string Describe() => $"{TFirst.Describe()} or {TSecond.Describe()}";
}

/// <summary>
/// An immutable <see cref="NonEmptyList{T}"/> whose contents additionally satisfy <typeparamref name="TConstraint"/>,
/// checked once at construction. Because the list cannot change, the constraint is a property of the type:
/// a method that accepts <c>ConstrainedList&lt;Waypoint, AtLeast&lt;Two&gt;&gt;</c> can read the second waypoint without a check.
/// </summary>
/// <remarks>
/// The list still exposes the full non-empty API, type-preserving LINQ and generic math. Every mutating member
/// throws <see cref="NotSupportedException"/>; use <see cref="Relax"/> to obtain a mutable copy.
/// </remarks>
/// <typeparam name="T">The element type.</typeparam>
/// <typeparam name="TConstraint">The constraint the contents satisfy.</typeparam>
[JsonConverter(typeof(ConstrainedListJsonConverterFactory))]
[CollectionBuilder(typeof(ConstrainedList), nameof(ConstrainedList.Create))]
[DebuggerDisplay("Count = {Count}, Constraint = {Description}")]
public sealed class ConstrainedList<T, TConstraint> : NonEmptyList<T>
    where TConstraint : IListConstraint
{
    private ConstrainedList(List<T> trustedItems) : base(trustedItems)
    {
    }

    /// <summary>A human-readable description of <typeparamref name="TConstraint"/>.</summary>
    public static string Description => TConstraint.Describe();

    /// <inheritdoc />
    protected override bool IsReadOnly => true;

    /// <inheritdoc />
    protected override void EnsureMutable()
        => throw new NotSupportedException($"A ConstrainedList<{typeof(T).Name}, {Description}> is immutable. Call Relax() to obtain a mutable NonEmptyList copy.");

    /// <summary>Returns <see langword="true"/> if the sequence is non-empty, contains no nulls and satisfies the constraint.</summary>
    public static bool Satisfies(IEnumerable<T>? source)
    {
        if (source is null)
            return false;

        List<T> items = source as List<T> ?? source.ToList();
        return items.Count > 0 && !items.Exists(static x => x is null) && TConstraint.Holds(CollectionsMarshal.AsSpan(items));
    }

    /// <summary>
    /// Creates a constrained list from a sequence.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if the sequence is null or contains null.</exception>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty or violates the constraint.</exception>
    public static new ConstrainedList<T, TConstraint> From(IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<T> items = new(source);
        return FromOwnedList(items, nameof(source));
    }

    /// <summary>Creates a constrained list from a span.</summary>
    /// <exception cref="ArgumentException">Thrown if the span is empty or violates the constraint.</exception>
    public static new ConstrainedList<T, TConstraint> FromSpan(ReadOnlySpan<T> items)
    {
        List<T> list = new(items.Length);
        foreach (T item in items)
            list.Add(item);

        return FromOwnedList(list, nameof(items));
    }

    /// <summary>Attempts to create a constrained list, returning <see langword="false"/> instead of throwing.</summary>
    public static bool TryFrom(IEnumerable<T>? source, [NotNullWhen(true)] out ConstrainedList<T, TConstraint>? result)
    {
        result = null;
        if (source is null)
            return false;

        List<T> items = new(source);
        if (items.Count == 0 || items.Exists(static x => x is null) || !TConstraint.Holds(CollectionsMarshal.AsSpan(items)))
            return false;

        result = new ConstrainedList<T, TConstraint>(items);
        return true;
    }

    /// <summary>
    /// Validates a sequence against the constraint, producing a <see cref="Validation{T, TError}"/> instead of throwing.
    /// </summary>
    /// <param name="source">The sequence to validate.</param>
    /// <param name="path">An optional property path recorded on the error, for problem-details responses.</param>
    public static Validation<ConstrainedList<T, TConstraint>, ValidationError> Validate(IEnumerable<T>? source, string? path = null)
    {
        if (source is null)
            return Validation<ConstrainedList<T, TConstraint>, ValidationError>.Invalid(new ValidationError("required", "A value is required", path));

        List<T> items = new(source);
        if (items.Count == 0)
            return Validation<ConstrainedList<T, TConstraint>, ValidationError>.Invalid(new ValidationError("nonEmpty", "At least one element is required", path));

        if (items.Exists(static x => x is null))
            return Validation<ConstrainedList<T, TConstraint>, ValidationError>.Invalid(new ValidationError("noNulls", "Elements cannot be null", path));

        if (!TConstraint.Holds(CollectionsMarshal.AsSpan(items)))
            return Validation<ConstrainedList<T, TConstraint>, ValidationError>.Invalid(new ValidationError("constraint", $"Expected {Description}", path));

        return new ConstrainedList<T, TConstraint>(items);
    }

    /// <summary>Returns a mutable <see cref="NonEmptyList{T}"/> copy without the constraint.</summary>
    public NonEmptyList<T> Relax() => FromTrustedList(new List<T>(Items));

    /// <summary>Re-checks the contents against a different constraint.</summary>
    /// <exception cref="ArgumentException">Thrown if the contents violate <typeparamref name="TOther"/>.</exception>
    public ConstrainedList<T, TOther> As<TOther>() where TOther : IListConstraint
        => ConstrainedList<T, TOther>.From(Items);

    /// <summary>Attempts to re-check the contents against a different constraint.</summary>
    public bool TryAs<TOther>([NotNullWhen(true)] out ConstrainedList<T, TOther>? result) where TOther : IListConstraint
        => ConstrainedList<T, TOther>.TryFrom(Items, out result);

    /// <summary>Returns a new constrained list with <paramref name="item"/> appended, re-checking the constraint.</summary>
    /// <exception cref="ArgumentException">Thrown if the result would violate the constraint.</exception>
    public ConstrainedList<T, TConstraint> WithAppended(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        List<T> items = new(Count + 1);
        items.AddRange(Items);
        items.Add(item);
        return FromOwnedList(items, nameof(item));
    }

    /// <summary>Returns a new constrained list with <paramref name="item"/> prepended, re-checking the constraint.</summary>
    /// <exception cref="ArgumentException">Thrown if the result would violate the constraint.</exception>
    public ConstrainedList<T, TConstraint> WithPrepended(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        List<T> items = new(Count + 1) { item };
        items.AddRange(Items);
        return FromOwnedList(items, nameof(item));
    }

    /// <summary>Returns a new constrained list with the element at <paramref name="index"/> replaced, re-checking the constraint.</summary>
    /// <exception cref="ArgumentException">Thrown if the result would violate the constraint.</exception>
    public ConstrainedList<T, TConstraint> WithItem(int index, T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        List<T> items = new(Items) { [index] = item };
        return FromOwnedList(items, nameof(item));
    }

    /// <summary>Returns a new constrained list without the element at <paramref name="index"/>, re-checking the constraint.</summary>
    /// <exception cref="ArgumentException">Thrown if the result would be empty or violate the constraint.</exception>
    public ConstrainedList<T, TConstraint> WithoutAt(int index)
    {
        List<T> items = new(Items);
        items.RemoveAt(index);
        return FromOwnedList(items, nameof(index));
    }

    /// <inheritdoc />
    public override string ToString() => $"ConstrainedList<{Description}>: [{string.Join(", ", Items)}]";

    private static ConstrainedList<T, TConstraint> FromOwnedList(List<T> items, string paramName)
    {
        if (items.Count == 0)
            throw new ArgumentException("Sequence cannot be empty", paramName);

        if (items.Exists(static x => x is null))
            throw new ArgumentNullException(paramName, "Sequence cannot contain null values");

        if (!TConstraint.Holds(CollectionsMarshal.AsSpan(items)))
            throw new ArgumentException($"Sequence does not satisfy the constraint: expected {Description}", paramName);

        return new ConstrainedList<T, TConstraint>(items);
    }
}

/// <summary>Factories for <see cref="ConstrainedList{T, TConstraint}"/>.</summary>
public static class ConstrainedList
{
    /// <summary>Creates a constrained list from a span. Used by the compiler for collection expressions.</summary>
    /// <exception cref="ArgumentException">Thrown if the span is empty or violates the constraint.</exception>
    public static ConstrainedList<T, TConstraint> Create<T, TConstraint>(ReadOnlySpan<T> items) where TConstraint : IListConstraint
        => ConstrainedList<T, TConstraint>.FromSpan(items);

    /// <summary>Creates a constrained list from a sequence.</summary>
    public static ConstrainedList<T, TConstraint> From<T, TConstraint>(IEnumerable<T> source) where TConstraint : IListConstraint
        => ConstrainedList<T, TConstraint>.From(source);

    /// <summary>Attempts to create a constrained list from a sequence.</summary>
    public static bool TryFrom<T, TConstraint>(IEnumerable<T>? source, [NotNullWhen(true)] out ConstrainedList<T, TConstraint>? result)
        where TConstraint : IListConstraint
        => ConstrainedList<T, TConstraint>.TryFrom(source, out result);
}

/// <summary>Extension methods for <see cref="ConstrainedList{T, TConstraint}"/>.</summary>
public static class ConstrainedListExtensions
{
    /// <summary>Checks the sequence against <typeparamref name="TConstraint"/> and wraps it.</summary>
    /// <exception cref="ArgumentException">Thrown if the sequence is empty or violates the constraint.</exception>
    public static ConstrainedList<T, TConstraint> ToConstrainedList<T, TConstraint>(this IEnumerable<T> source)
        where TConstraint : IListConstraint
        => ConstrainedList<T, TConstraint>.From(source);

    /// <summary>Checks the list against <typeparamref name="TConstraint"/> and wraps a copy of it.</summary>
    /// <exception cref="ArgumentException">Thrown if the list violates the constraint.</exception>
    public static ConstrainedList<T, TConstraint> Constrain<T, TConstraint>(this NonEmptyList<T> source)
        where TConstraint : IListConstraint
        => ConstrainedList<T, TConstraint>.From(source);

    /// <summary>Returns the second element. Available only when the constraint guarantees at least two elements.</summary>
    public static T Second<T, TCount>(this ConstrainedList<T, AtLeast<TCount>> list)
        where TCount : ICardinality, IAtLeastTwo
        => list[1];

    /// <summary>Returns the third element. Available only when the constraint guarantees at least three elements.</summary>
    public static T Third<T, TCount>(this ConstrainedList<T, AtLeast<TCount>> list)
        where TCount : ICardinality, IAtLeastThree
        => list[2];

    /// <summary>Returns the second element. Available only when the first constraint guarantees at least two elements.</summary>
    public static T Second<T, TCount, TOther>(this ConstrainedList<T, And<AtLeast<TCount>, TOther>> list)
        where TCount : ICardinality, IAtLeastTwo
        where TOther : IListConstraint
        => list[1];

    /// <summary>Returns the third element. Available only when the first constraint guarantees at least three elements.</summary>
    public static T Third<T, TCount, TOther>(this ConstrainedList<T, And<AtLeast<TCount>, TOther>> list)
        where TCount : ICardinality, IAtLeastThree
        where TOther : IListConstraint
        => list[2];

    /// <summary>Returns the second element. Available only when the constraint guarantees exactly two or more elements.</summary>
    public static T Second<T, TCount>(this ConstrainedList<T, Exactly<TCount>> list)
        where TCount : ICardinality, IAtLeastTwo
        => list[1];

    /// <summary>Returns the third element. Available only when the constraint guarantees exactly three or more elements.</summary>
    public static T Third<T, TCount>(this ConstrainedList<T, Exactly<TCount>> list)
        where TCount : ICardinality, IAtLeastThree
        => list[2];

    /// <summary>
    /// Returns everything after the first two elements. Available only when the constraint guarantees at least two
    /// elements; the result may be empty.
    /// </summary>
    public static List<T> RestAfterSecond<T, TCount>(this ConstrainedList<T, AtLeast<TCount>> list)
        where TCount : ICardinality, IAtLeastTwo
        => list.Slice(2, list.Count - 2);
}

/// <summary>JSON converter factory for <see cref="ConstrainedList{T, TConstraint}"/>.</summary>
public sealed class ConstrainedListJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ConstrainedList<,>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type[] args = typeToConvert.GetGenericArguments();
        Type converterType = typeof(ConstrainedListJsonConverter<,>).MakeGenericType(args[0], args[1]);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>JSON converter for <see cref="ConstrainedList{T, TConstraint}"/>. Reads a JSON array and checks the constraint.</summary>
public sealed class ConstrainedListJsonConverter<T, TConstraint> : JsonConverter<ConstrainedList<T, TConstraint>>
    where TConstraint : IListConstraint
{
    /// <inheritdoc />
    public override ConstrainedList<T, TConstraint>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array");

        List<T> items = new();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            T? item = JsonSerializer.Deserialize<T>(ref reader, options);
            if (item is null)
                throw new JsonException("ConstrainedList cannot contain null elements");

            items.Add(item);
        }

        if (items.Count == 0)
            throw new JsonException("ConstrainedList cannot be empty");

        if (!TConstraint.Holds(CollectionsMarshal.AsSpan(items)))
            throw new JsonException($"ConstrainedList does not satisfy the constraint: expected {ConstrainedList<T, TConstraint>.Description}");

        return ConstrainedList<T, TConstraint>.From(items);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConstrainedList<T, TConstraint> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (T item in value)
            JsonSerializer.Serialize(writer, item, options);
        writer.WriteEndArray();
    }
}
