namespace Masterly.NonEmptyList;

/// <summary>
/// Marks a partial class as a domain-specific non-empty collection. The <c>Masterly.NonEmptyList.Analyzers</c>
/// package's source generator completes the class: it derives from <see cref="NonEmptyList{T}"/>, adds constructors,
/// <c>From</c>, <c>TryFrom</c>, <c>Of</c> and <c>Validate</c> factories, collection-expression support, a
/// System.Text.Json converter, a <c>DebuggerDisplay</c>, and enforces <see cref="Distinct"/>, <see cref="MinCount"/>
/// and <see cref="MaxCount"/> on construction and on every mutation.
/// </summary>
/// <example>
/// <code>
/// [NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 500)]
/// public sealed partial class OrderLines;
///
/// Order order = new(customer, OrderLines.From(lines));   // throws or returns, never empty
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NonEmptyCollectionAttribute : Attribute
{
    /// <summary>Marks a partial class as a non-empty collection of <paramref name="elementType"/>.</summary>
    public NonEmptyCollectionAttribute(Type elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ElementType = elementType;
    }

    /// <summary>The element type.</summary>
    public Type ElementType { get; }

    /// <summary>When <see langword="true"/>, duplicate elements (by <see cref="EqualityComparer{T}.Default"/>) are rejected.</summary>
    public bool Distinct { get; set; }

    /// <summary>The minimum number of elements. Defaults to 1 and cannot be lower.</summary>
    public int MinCount { get; set; } = 1;

    /// <summary>The maximum number of elements, or 0 for no limit.</summary>
    public int MaxCount { get; set; }

    /// <summary>
    /// When <see langword="true"/>, the generated collection rejects every mutation, so the invariants are checked once
    /// at construction. Use for value objects.
    /// </summary>
    public bool Immutable { get; set; }
}
