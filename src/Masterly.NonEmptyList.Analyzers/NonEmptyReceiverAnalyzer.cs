using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>
/// NEL003: a LINQ operator (<c>First()</c>, <c>Last()</c>, <c>Aggregate(f)</c>, <c>Count()</c>, ...) is called on a
/// non-empty collection that offers a total member (<c>Head</c>, <c>Last</c>, <c>Reduce</c>, <c>Count</c>).
/// NEL004: an emptiness check (<c>Any()</c>, <c>Count == 0</c>, ...) on a non-empty list is decided by the type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonEmptyReceiverAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Property names that make the diagnostics' replacement text. Used by the code fix too.</summary>
    public const string ReplacementProperty = "Replacement";

    /// <summary>Set to "Member" when the replacement is a property access, "Method" when it is a method call with the same arguments.</summary>
    public const string ReplacementKindProperty = "ReplacementKind";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.ThrowingLinqOnNonEmpty, Descriptors.RedundantEmptinessCheck);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeComparison,
            SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression,
            SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.LessThanExpression, SyntaxKind.LessThanOrEqualExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
            return;

        if (method.ContainingType.ToDisplayString() != "System.Linq.Enumerable")
            return;

        ITypeSymbol? receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if (!WellKnown.IsNonEmptyType(receiverType))
            return;

        int argumentCount = invocation.ArgumentList.Arguments.Count;
        bool receiverIsList = IsListLike(receiverType);

        switch (method.Name)
        {
            case "First" when argumentCount == 0 && receiverIsList:
            case "FirstOrDefault" when argumentCount == 0 && receiverIsList:
                ReportThrowing(context, invocation, method.Name, "Head", "Member");
                return;

            case "Last" when argumentCount == 0 && receiverIsList:
            case "LastOrDefault" when argumentCount == 0 && receiverIsList:
                ReportThrowing(context, invocation, method.Name, "Last", "Member");
                return;

            case "Aggregate" when argumentCount == 1 && receiverIsList:
                ReportThrowing(context, invocation, method.Name, "Reduce", "Method");
                return;

            // Only where the receiver has a Count property to switch to; a lazy NonEmptyEnumerable has none.
            case "Count" when argumentCount == 0 && HasCountProperty(receiverType):
            case "LongCount" when argumentCount == 0 && HasCountProperty(receiverType):
                ReportThrowing(context, invocation, method.Name, "Count", "Member");
                return;

            case "Any" when argumentCount == 0:
                ReportRedundant(context, invocation, "Any()", "true");
                return;

            case "DefaultIfEmpty" when argumentCount <= 1:
                ReportRedundant(context, invocation, "DefaultIfEmpty()", "the list itself");
                return;
        }
    }

    private static void AnalyzeComparison(SyntaxNodeAnalysisContext context)
    {
        var binary = (BinaryExpressionSyntax)context.Node;

        if (TryGetCountAndLiteral(binary, context.SemanticModel, context.CancellationToken, out bool countOnLeft, out int literal))
        {
            // Normalise to "Count <op> literal".
            SyntaxKind kind = binary.Kind();
            if (!countOnLeft)
                kind = Mirror(kind);

            string? verdict = kind switch
            {
                SyntaxKind.EqualsExpression when literal == 0 => "false",
                SyntaxKind.NotEqualsExpression when literal == 0 => "true",
                SyntaxKind.GreaterThanExpression when literal <= 0 => "true",
                SyntaxKind.GreaterThanOrEqualExpression when literal <= 1 => "true",
                SyntaxKind.LessThanExpression when literal <= 1 => "false",
                SyntaxKind.LessThanOrEqualExpression when literal <= 0 => "false",
                _ => null,
            };

            if (verdict is not null)
                ReportRedundant(context, binary, binary.ToString(), verdict);
        }
    }

    private static bool TryGetCountAndLiteral(BinaryExpressionSyntax binary, SemanticModel model, System.Threading.CancellationToken ct, out bool countOnLeft, out int literal)
    {
        countOnLeft = true;
        literal = 0;

        if (IsNonEmptyCount(binary.Left, model, ct) && TryGetInt(binary.Right, model, ct, out literal))
            return true;

        if (IsNonEmptyCount(binary.Right, model, ct) && TryGetInt(binary.Left, model, ct, out literal))
        {
            countOnLeft = false;
            return true;
        }

        return false;
    }

    private static bool IsNonEmptyCount(ExpressionSyntax expression, SemanticModel model, System.Threading.CancellationToken ct)
    {
        ExpressionSyntax? receiver = expression switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" or "Length" } member => member.Expression,
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" or "LongCount" } member, ArgumentList.Arguments.Count: 0 } => member.Expression,
            _ => null,
        };

        return receiver is not null && WellKnown.IsNonEmptyType(model.GetTypeInfo(receiver, ct).Type);
    }

    private static bool TryGetInt(ExpressionSyntax expression, SemanticModel model, System.Threading.CancellationToken ct, out int value)
    {
        Optional<object?> constant = model.GetConstantValue(expression, ct);
        if (constant.HasValue && constant.Value is int i)
        {
            value = i;
            return true;
        }

        if (constant.HasValue && constant.Value is long l && l >= int.MinValue && l <= int.MaxValue)
        {
            value = (int)l;
            return true;
        }

        value = 0;
        return false;
    }

    private static SyntaxKind Mirror(SyntaxKind kind) => kind switch
    {
        SyntaxKind.GreaterThanExpression => SyntaxKind.LessThanExpression,
        SyntaxKind.GreaterThanOrEqualExpression => SyntaxKind.LessThanOrEqualExpression,
        SyntaxKind.LessThanExpression => SyntaxKind.GreaterThanExpression,
        SyntaxKind.LessThanOrEqualExpression => SyntaxKind.GreaterThanOrEqualExpression,
        _ => kind,
    };

    private static bool IsListLike(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current is INamedTypeSymbol named && WellKnown.IsInLibrary(named) &&
                named.Name is "NonEmptyList" or "ImmutableNonEmptyList" or "ConstrainedList" or "OrderedNonEmptyList" or "NonEmptyGrouping")
                return true;
        }

        return false;
    }

    private static bool HasCountProperty(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            foreach (ISymbol member in current.GetMembers("Count"))
            {
                if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public })
                    return true;
            }
        }

        return false;
    }

    private static void ReportThrowing(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string methodName, string replacement, string kind)
    {
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
            .Add(ReplacementProperty, replacement)
            .Add(ReplacementKindProperty, kind);

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.ThrowingLinqOnNonEmpty,
            invocation.GetLocation(),
            properties,
            methodName + "()",
            replacement));
    }

    private static void ReportRedundant(SyntaxNodeAnalysisContext context, SyntaxNode node, string text, string verdict)
    {
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add(ReplacementProperty, verdict);
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.RedundantEmptinessCheck, node.GetLocation(), properties, text, verdict));
    }
}
