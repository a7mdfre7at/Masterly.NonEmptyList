#pragma warning disable CA2260 // derived collections are a legitimate use of NonEmptyList<T> as a base

namespace Masterly.NonEmptyList.Integrations.Tests;

/// <summary>
/// A collection that derives from <see cref="NonEmptyList{T}"/> but has no <c>From(IEnumerable&lt;T&gt;)</c> factory of
/// its own, so no integration can rebuild it. Every integration must say so instead of producing a base-class instance.
/// </summary>
public sealed class NoFactoryList : NonEmptyList<int>
{
    public NoFactoryList() : base(NonEmptyList.Of(1))
    {
    }
}

/// <summary>A source-generated collection: distinct, with its own System.Text.Json converter.</summary>
[NonEmptyCollection(typeof(int), Distinct = true)]
public sealed partial class Readings;

/// <summary>A derived collection whose factory fails with an exception that is not a validation failure.</summary>
public sealed class Unlucky : NonEmptyList<int>
{
    private Unlucky(NonEmptyList<int> source) : base(source)
    {
        if (Contains(13))
            throw new NotSupportedException("13 is unlucky");
    }

    public static new Unlucky From(IEnumerable<int> items) => new(NonEmptyList<int>.From(items));
}

/// <summary>
/// An element with a broken <see cref="object.Equals(object)"/>: negative values are not equal even to themselves.
/// Used to show that the published laws detect an unlawful element type.
/// </summary>
public sealed class Shaky(int value) : IComparable<Shaky>
{
    public int Value { get; } = value;

    public override bool Equals(object? obj) => Value >= 0 && obj is Shaky other && other.Value == Value;

    public override int GetHashCode() => Value;

    public int CompareTo(Shaky? other) => Value.CompareTo(other?.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>An element with an inconsistent ordering: every value claims to be greater than every other, itself included.</summary>
public sealed record Contrary(int Value) : IComparable<Contrary>
{
    public int CompareTo(Contrary? other) => 1;
}
