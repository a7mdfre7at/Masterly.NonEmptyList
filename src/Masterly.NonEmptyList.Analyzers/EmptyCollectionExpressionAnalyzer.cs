using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>
/// NEL001: <c>NonEmptyList&lt;int&gt; xs = [];</c> compiles because the collection builder accepts any span, but it always
/// throws. Report it at compile time instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyCollectionExpressionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.EmptyCollectionExpression);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CollectionExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var collection = (CollectionExpressionSyntax)context.Node;
        if (collection.Elements.Count != 0)
            return;

        ITypeSymbol? target = context.SemanticModel.GetTypeInfo(collection, context.CancellationToken).ConvertedType;
        if (!WellKnown.IsNonEmptyType(target))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.EmptyCollectionExpression,
            collection.GetLocation(),
            target!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
