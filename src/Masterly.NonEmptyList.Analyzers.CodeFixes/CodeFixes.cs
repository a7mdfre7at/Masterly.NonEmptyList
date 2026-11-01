using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>Replaces <c>xs.First()</c> with <c>xs.Head</c>, <c>xs.Aggregate(f)</c> with <c>xs.Reduce(f)</c>, and so on (NEL003).</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TotalMemberCodeFixProvider))]
[Shared]
public sealed class TotalMemberCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Descriptors.ThrowingLinqOnNonEmptyId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InvocationExpressionSyntax invocation)
                continue;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            if (!diagnostic.Properties.TryGetValue(NonEmptyReceiverAnalyzer.ReplacementProperty, out string? replacement) || replacement is null)
                continue;

            diagnostic.Properties.TryGetValue(NonEmptyReceiverAnalyzer.ReplacementKindProperty, out string? kind);

            ExpressionSyntax newExpression = kind == "Method"
                ? invocation.WithExpression(memberAccess.WithName(SyntaxFactory.IdentifierName(replacement)))
                : memberAccess.WithName(SyntaxFactory.IdentifierName(replacement)).WithTriviaFrom(invocation);

            string title = $"Use '{replacement}'";
            context.RegisterCodeFix(
                CodeAction.Create(title, ct => ReplaceAsync(context.Document, invocation, newExpression, ct), nameof(TotalMemberCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceAsync(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken ct)
    {
        SyntaxNode root = (await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))!;
        return document.WithSyntaxRoot(root.ReplaceNode(oldNode, newNode.WithTriviaFrom(oldNode)));
    }
}

/// <summary>Replaces a redundant emptiness check on a non-empty list with its constant value (NEL004).</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RedundantCheckCodeFixProvider))]
[Shared]
public sealed class RedundantCheckCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Descriptors.RedundantEmptinessCheckId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(NonEmptyReceiverAnalyzer.ReplacementProperty, out string? verdict) || verdict is not ("true" or "false"))
                continue;

            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not ExpressionSyntax expression)
                continue;

            LiteralExpressionSyntax literal = SyntaxFactory.LiteralExpression(verdict == "true" ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
            string title = $"Replace with '{verdict}'";
            context.RegisterCodeFix(
                CodeAction.Create(title, ct => ReplaceAsync(context.Document, expression, literal, ct), nameof(RedundantCheckCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceAsync(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken ct)
    {
        SyntaxNode root = (await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))!;
        return document.WithSyntaxRoot(root.ReplaceNode(oldNode, newNode.WithTriviaFrom(oldNode)));
    }
}

/// <summary>Wraps the receiver of an unproven throwing operator in <c>ToNonEmptyList()</c> (NEL005).</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ToNonEmptyListCodeFixProvider))]
[Shared]
public sealed class ToNonEmptyListCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Descriptors.UnprovenSequenceId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
            return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InvocationExpressionSyntax invocation)
                continue;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            // Extension syntax wraps the receiver; static syntax (Enumerable.Max(xs)) wraps the first argument.
            bool isStatic = model.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol { ReducedFrom: null };
            ExpressionSyntax? sequence = isStatic
                ? (invocation.ArgumentList.Arguments.Count > 0 ? invocation.ArgumentList.Arguments[0].Expression : null)
                : memberAccess.Expression;
            if (sequence is null)
                continue;

            InvocationExpressionSyntax wrapped = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    sequence.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("ToNonEmptyList")));

            const string title = "Prove non-empty with ToNonEmptyList()";
            ExpressionSyntax target = sequence;
            context.RegisterCodeFix(
                CodeAction.Create(title, ct => ReplaceAsync(context.Document, target, wrapped, ct), title),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceAsync(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken ct)
    {
        SyntaxNode root = (await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))!;
        SyntaxNode newRoot = root.ReplaceNode(oldNode, newNode.WithTriviaFrom(oldNode));

        if (newRoot is CompilationUnitSyntax unit && !unit.Usings.Any(u => u.Name?.ToString() == WellKnown.Namespace))
        {
            SyntaxTriviaList trailing = unit.Usings.Count > 0 ? unit.Usings[unit.Usings.Count - 1].GetTrailingTrivia() : SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed);
            newRoot = unit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(WellKnown.Namespace).WithLeadingTrivia(SyntaxFactory.Space)).WithTrailingTrivia(trailing));
        }

        return document.WithSyntaxRoot(newRoot);
    }
}

/// <summary>
/// v3 migration fix: <c>NonEmptyList&lt;T&gt;</c> no longer derives from <c>List&lt;T&gt;</c>. Wherever the compiler reports a
/// failed conversion from a non-empty list to <c>List&lt;T&gt;</c> (CS1503 argument, CS0029 assignment, CS0266 explicit cast
/// needed), offer <c>.ToList()</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ListConversionCodeFixProvider))]
[Shared]
public sealed class ListConversionCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS1503", "CS0029", "CS0266", "CS1662");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
            return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            ExpressionSyntax? expression = node as ExpressionSyntax ?? (node as ArgumentSyntax)?.Expression ?? node.FirstAncestorOrSelf<ExpressionSyntax>();
            if (expression is null)
                continue;

            TypeInfo typeInfo = model.GetTypeInfo(expression, context.CancellationToken);
            if (!WellKnown.IsNonEmptyType(typeInfo.Type))
                continue;

            ITypeSymbol? target = typeInfo.ConvertedType;
            if (target is null || target.OriginalDefinition.ToDisplayString() != "System.Collections.Generic.List<T>")
            {
                // For arguments the converted type is often unavailable; look at the parameter type instead.
                if (expression.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: { } call } } argument &&
                    model.GetSymbolInfo(call, context.CancellationToken).CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault() is { } candidate)
                {
                    int index = ((ArgumentListSyntax)argument.Parent!).Arguments.IndexOf(argument);
                    if (index >= 0 && index < candidate.Parameters.Length)
                        target = candidate.Parameters[index].Type;
                }

                if (target is null || target.OriginalDefinition.ToDisplayString() != "System.Collections.Generic.List<T>")
                    continue;
            }

            InvocationExpressionSyntax toList = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    expression.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("ToList")));

            const string title = "Convert with ToList() (NonEmptyList no longer derives from List<T>)";
            ExpressionSyntax captured = expression;
            context.RegisterCodeFix(
                CodeAction.Create(title, ct => ReplaceAsync(context.Document, captured, toList, ct), title),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceAsync(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken ct)
    {
        SyntaxNode root = (await document.GetSyntaxRootAsync(ct).ConfigureAwait(false))!;
        return document.WithSyntaxRoot(root.ReplaceNode(oldNode, newNode.WithTriviaFrom(oldNode)));
    }
}
