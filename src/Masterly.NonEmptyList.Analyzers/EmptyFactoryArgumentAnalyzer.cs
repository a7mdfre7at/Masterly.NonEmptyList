using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>
/// NEL002: <c>NonEmptyList.From(Array.Empty&lt;int&gt;())</c>, <c>new List&lt;int&gt;().ToNonEmptyList()</c> and friends
/// always throw. Report them at compile time.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyFactoryArgumentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.EmptyFactoryArgument);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeCreation, SyntaxKind.ObjectCreationExpression, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
            return;

        if (!WellKnown.IsNonEmptyFactory(method))
            return;

        // The sequence argument is the receiver for extension methods, otherwise the first argument.
        ExpressionSyntax? sequence = null;
        if (method.IsExtensionMethod && invocation.Expression is MemberAccessExpressionSyntax memberAccess && method.ReducedFrom is not null)
            sequence = memberAccess.Expression;
        else if (invocation.ArgumentList.Arguments.Count > 0)
            sequence = invocation.ArgumentList.Arguments[0].Expression;

        if (sequence is null || !WellKnown.IsProvablyEmpty(sequence, context.SemanticModel))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.EmptyFactoryArgument,
            invocation.GetLocation(),
            $"{method.ContainingType.Name}.{method.Name}"));
    }

    private static void AnalyzeCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (BaseObjectCreationExpressionSyntax)context.Node;
        if (creation.ArgumentList is null || creation.ArgumentList.Arguments.Count != 1)
            return;

        ITypeSymbol? type = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken).Type;
        if (!WellKnown.IsNonEmptyType(type))
            return;

        if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol ctor)
            return;

        // Only the IEnumerable<T>-taking constructors can receive an empty sequence.
        if (ctor.Parameters.Length != 1 || ctor.Parameters[0].Type is not INamedTypeSymbol { IsGenericType: true } paramType ||
            paramType.OriginalDefinition.ToDisplayString() != "System.Collections.Generic.IEnumerable<T>")
            return;

        ExpressionSyntax argument = creation.ArgumentList.Arguments[0].Expression;
        if (!WellKnown.IsProvablyEmpty(argument, context.SemanticModel))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.EmptyFactoryArgument,
            creation.GetLocation(),
            $"new {type!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"));
    }
}
