using Microsoft.CodeAnalysis;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>Diagnostic ids and descriptors for every rule in the package.</summary>
public static class Descriptors
{
    private const string Category = "Masterly.NonEmptyList";
    private const string HelpBase = "https://github.com/a7mdfre7at/Masterly.NonEmptyList/blob/master/docs/analyzers.md#";

    /// <summary>NEL001: an empty collection expression is assigned to a non-empty type.</summary>
    public const string EmptyCollectionExpressionId = "NEL001";

    /// <summary>NEL002: a non-empty factory is called with a provably empty argument.</summary>
    public const string EmptyFactoryArgumentId = "NEL002";

    /// <summary>NEL003: a throwing LINQ operator is used where a total member exists.</summary>
    public const string ThrowingLinqOnNonEmptyId = "NEL003";

    /// <summary>NEL004: an emptiness check on a non-empty list is redundant.</summary>
    public const string RedundantEmptinessCheckId = "NEL004";

    /// <summary>NEL005: a throwing LINQ operator is used on a sequence not proven non-empty.</summary>
    public const string UnprovenSequenceId = "NEL005";

    /// <summary>NEL101: a [NonEmptyCollection] class must be partial.</summary>
    public const string CollectionMustBePartialId = "NEL101";

    /// <summary>NEL102: a [NonEmptyCollection] class must not be nested.</summary>
    public const string CollectionMustNotBeNestedId = "NEL102";

    /// <summary>NEL103: invalid MinCount/MaxCount on [NonEmptyCollection].</summary>
    public const string CollectionInvalidBoundsId = "NEL103";

    /// <summary>NEL104: a [NonEmptyCollection] class must not declare a base type.</summary>
    public const string CollectionMustNotHaveBaseTypeId = "NEL104";

    public static readonly DiagnosticDescriptor EmptyCollectionExpression = new(
        EmptyCollectionExpressionId,
        "Empty collection expression assigned to a non-empty type",
        "An empty collection expression can never produce a '{0}'; this throws at run time",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Non-empty collection types reject empty input in their collection builder. The compiler cannot stop '[]' from being written, but this analyzer can.",
        helpLinkUri: HelpBase + "nel001");

    public static readonly DiagnosticDescriptor EmptyFactoryArgument = new(
        EmptyFactoryArgumentId,
        "Non-empty factory called with an empty argument",
        "'{0}' is called with an argument that is always empty; this throws at run time",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Array.Empty<T>(), Enumerable.Empty<T>(), new T[0], new List<T>() and empty literals can never satisfy a non-empty factory.",
        helpLinkUri: HelpBase + "nel002");

    public static readonly DiagnosticDescriptor ThrowingLinqOnNonEmpty = new(
        ThrowingLinqOnNonEmptyId,
        "Use the total member instead of a throwing LINQ operator",
        "'{0}' on a non-empty list is total; use '{1}' instead, which the type guarantees cannot throw",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "First(), Last(), FirstOrDefault(), LastOrDefault(), Single() and Aggregate() carry an empty-sequence exception path that the non-empty type has already ruled out. Head, Last and Reduce make that visible.",
        helpLinkUri: HelpBase + "nel003");

    public static readonly DiagnosticDescriptor RedundantEmptinessCheck = new(
        RedundantEmptinessCheckId,
        "Redundant emptiness check on a non-empty list",
        "'{0}' is always {1} for a non-empty list",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Any(), Count == 0, Count > 0 and similar checks are decided by the type. Remove the check or widen the variable's type if emptiness is really possible.",
        helpLinkUri: HelpBase + "nel004");

    public static readonly DiagnosticDescriptor UnprovenSequence = new(
        UnprovenSequenceId,
        "Sequence is not proven non-empty",
        "'{0}' may throw because '{1}' is not proven non-empty; convert it with ToNonEmptyList() or handle the empty case",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Opt-in rule. Treats First(), Last(), Single(), Max(), Min(), Average() and Aggregate() on an IEnumerable<T> as unproven unless the receiver is a non-empty type or a construction the analyzer recognizes as non-empty. Enable with dotnet_diagnostic.NEL005.severity = warning.",
        helpLinkUri: HelpBase + "nel005");

    public static readonly DiagnosticDescriptor CollectionMustBePartial = new(
        CollectionMustBePartialId,
        "[NonEmptyCollection] class must be partial",
        "'{0}' is marked [NonEmptyCollection] but is not partial; add the 'partial' modifier so the generator can complete it",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpBase + "nel101");

    public static readonly DiagnosticDescriptor CollectionMustNotBeNested = new(
        CollectionMustNotBeNestedId,
        "[NonEmptyCollection] class must not be nested",
        "'{0}' is marked [NonEmptyCollection] but is nested in another type; move it to namespace scope",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpBase + "nel102");

    public static readonly DiagnosticDescriptor CollectionInvalidBounds = new(
        CollectionInvalidBoundsId,
        "[NonEmptyCollection] has invalid bounds",
        "'{0}': {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpBase + "nel103");

    public static readonly DiagnosticDescriptor CollectionMustNotHaveBaseType = new(
        CollectionMustNotHaveBaseTypeId,
        "[NonEmptyCollection] class must not declare a base type",
        "'{0}' declares a base type; the generator supplies NonEmptyList<{1}> as the base",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpBase + "nel104");
}
