using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>
/// NEL005 (opt-in): treats <c>First()</c>, <c>Last()</c>, <c>Single()</c>, <c>Max()</c>, <c>Min()</c>, <c>Average()</c> and
/// <c>Aggregate(f)</c> on an <c>IEnumerable&lt;T&gt;</c> as unproven unless the receiver is a non-empty type or a
/// construction the analyzer recognizes as non-empty. Teams drive the count to zero the way they drove nullable warnings to zero.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnprovenSequenceAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> ThrowingOperators = ImmutableHashSet.Create(
        "First", "Last", "Single", "Max", "Min", "Average", "Aggregate", "MaxBy", "MinBy", "ElementAt");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.UnprovenSequence);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (!ThrowingOperators.Contains(memberAccess.Name.Identifier.ValueText))
            return;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
            return;

        if (method.ContainingType.ToDisplayString() != "System.Linq.Enumerable")
            return;

        // Aggregate with a seed is total. Max/Min/Average over nullable element types return null for empty input.
        if (method.Name == "Aggregate" && invocation.ArgumentList.Arguments.Count != 1)
            return;

        // Max/Min/MaxBy/MinBy return null instead of throwing when the result type is a reference type or a Nullable<T>;
        // Average over Nullable<T> elements returns null too. Decide by type, not by nullable annotation, so this works in
        // projects with nullable reference types disabled as well.
        if (method.Name is "Max" or "Min" or "Average" or "MaxBy" or "MinBy" &&
            (method.ReturnType.IsReferenceType || method.ReturnType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }))
            return;

        // Extension syntax: the receiver is the sequence. Static syntax (Enumerable.Max(xs)): the first argument is.
        ExpressionSyntax? receiver = method.ReducedFrom is not null
            ? memberAccess.Expression
            : invocation.ArgumentList.Arguments.Count > 0 ? invocation.ArgumentList.Arguments[0].Expression : null;

        if (receiver is null || WellKnown.IsProvenNonEmpty(receiver, context.SemanticModel))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.UnprovenSequence,
            invocation.GetLocation(),
            method.Name + "()",
            receiver.ToString()));
    }
}
