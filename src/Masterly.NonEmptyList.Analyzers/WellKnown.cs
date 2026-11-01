using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Masterly.NonEmptyList.Analyzers;

/// <summary>Symbol and syntax helpers shared by the analyzers.</summary>
internal static class WellKnown
{
    public const string Namespace = "Masterly.NonEmptyList";

    private static readonly ImmutableHashSet<string> NonEmptyTypeNames = ImmutableHashSet.Create(
        "NonEmptyList",
        "ImmutableNonEmptyList",
        "ConstrainedList",
        "OrderedNonEmptyList",
        "NonEmptyGrouping",
        "NonEmptySet",
        "NonEmptyEnumerable");

    private static readonly ImmutableHashSet<string> FactoryClassNames = ImmutableHashSet.Create(
        "NonEmptyList",
        "ImmutableNonEmptyList",
        "ConstrainedList",
        "NonEmptySet",
        "NonEmptyDictionary",
        "EventStream",
        "NonEmptyListLinqExtensions",
        "ConstrainedListExtensions");

    /// <summary>True if the type is one of the library's non-empty collection types, or derives from one.</summary>
    public static bool IsNonEmptyType(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current is INamedTypeSymbol named && IsInLibrary(named) && NonEmptyTypeNames.Contains(named.Name))
                return true;
        }

        return false;
    }

    /// <summary>True if the method is a library factory that rejects empty input.</summary>
    public static bool IsNonEmptyFactory(IMethodSymbol method)
    {
        if (!IsInLibrary(method.ContainingType))
            return false;

        if (!FactoryClassNames.Contains(method.ContainingType.Name))
            return false;

        switch (method.Name)
        {
            case "From":
            case "FromSpan":
            case "Create":
            case "ToNonEmptyList":
            case "ToConstrainedList":
            case "ToNonEmptySet":
            case "ToNonEmptyDictionary":
                return (method.ReducedFrom ?? method).Parameters.Length >= 1;
            default:
                return false;
        }
    }

    public static bool IsInLibrary(ISymbol? symbol)
        => symbol?.ContainingNamespace?.ToDisplayString() == Namespace;

    /// <summary>True if the expression is a sequence the analyzer can prove is always empty.</summary>
    public static bool IsProvablyEmpty(ExpressionSyntax expression, SemanticModel model)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax paren:
                return IsProvablyEmpty(paren.Expression, model);

            case CollectionExpressionSyntax collection:
                return collection.Elements.Count == 0;

            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                return literal.Token.ValueText.Length == 0;

            case ArrayCreationExpressionSyntax array:
                if (array.Initializer is not null)
                    return array.Initializer.Expressions.Count == 0;
                foreach (ArrayRankSpecifierSyntax rank in array.Type.RankSpecifiers)
                {
                    foreach (ExpressionSyntax size in rank.Sizes)
                    {
                        if (size is LiteralExpressionSyntax { Token.Value: 0 })
                            return true;
                    }
                }

                return false;

            case ImplicitArrayCreationExpressionSyntax implicitArray:
                return implicitArray.Initializer.Expressions.Count == 0;

            case ObjectCreationExpressionSyntax creation:
                {
                    if (creation.Initializer is { Expressions.Count: > 0 })
                        return false;
                    if (creation.ArgumentList is { Arguments.Count: > 0 })
                        return false;
                    ITypeSymbol? type = model.GetTypeInfo(creation).Type;
                    return IsEmptyWhenDefaultConstructed(type);
                }

            case ImplicitObjectCreationExpressionSyntax implicitCreation:
                {
                    if (implicitCreation.Initializer is { Expressions.Count: > 0 })
                        return false;
                    if (implicitCreation.ArgumentList.Arguments.Count > 0)
                        return false;
                    ITypeSymbol? type = model.GetTypeInfo(implicitCreation).Type;
                    return IsEmptyWhenDefaultConstructed(type);
                }

            case InvocationExpressionSyntax invocation:
                {
                    if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
                        return false;

                    string container = method.ContainingType.ToDisplayString();
                    if (method.Name == "Empty" && method.Parameters.Length == 0 &&
                        (container == "System.Array" || container == "System.Linq.Enumerable" || container == "System.Collections.Immutable.ImmutableArray"))
                        return true;

                    return false;
                }

            case MemberAccessExpressionSyntax member:
                {
                    ISymbol? symbol = model.GetSymbolInfo(member).Symbol;
                    if (symbol is IFieldSymbol { Name: "Empty", ContainingType.SpecialType: SpecialType.System_String })
                        return true;
                    if (symbol is IPropertySymbol or IFieldSymbol && symbol.Name == "Empty" && symbol.IsStatic &&
                        (symbol.ContainingType.ToDisplayString().StartsWith("System.Collections.Immutable.", System.StringComparison.Ordinal) ||
                         symbol.ContainingType.OriginalDefinition.ToDisplayString() is "System.ReadOnlySpan<T>" or "System.Span<T>"))
                        return true;
                    return false;
                }

            default:
                return false;
        }
    }

    private static bool IsEmptyWhenDefaultConstructed(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        string name = named.OriginalDefinition.ToDisplayString();
        return name is "System.Collections.Generic.List<T>"
            or "System.Collections.Generic.HashSet<T>"
            or "System.Collections.Generic.Queue<T>"
            or "System.Collections.Generic.Stack<T>"
            or "System.Collections.Generic.LinkedList<T>"
            or "System.Collections.Generic.SortedSet<T>"
            or "System.Collections.Generic.Dictionary<TKey, TValue>"
            or "System.Collections.ObjectModel.Collection<T>"
            or "System.Collections.ObjectModel.ObservableCollection<T>"
            or "System.ReadOnlySpan<T>"
            or "System.Span<T>";
    }

    /// <summary>
    /// True if the expression is a construction the analyzer recognizes as non-empty: a non-empty typed value, a literal
    /// collection with elements, Enumerable.Range/Repeat with a positive literal count, or a Prepend/Append result.
    /// </summary>
    public static bool IsProvenNonEmpty(ExpressionSyntax expression, SemanticModel model)
    {
        if (IsNonEmptyType(model.GetTypeInfo(expression).Type))
            return true;

        switch (expression)
        {
            case ParenthesizedExpressionSyntax paren:
                return IsProvenNonEmpty(paren.Expression, model);

            case CastExpressionSyntax cast:
                return IsProvenNonEmpty(cast.Expression, model);

            case CollectionExpressionSyntax collection:
                foreach (CollectionElementSyntax element in collection.Elements)
                {
                    if (element is ExpressionElementSyntax)
                        return true;
                }

                return false;

            case ArrayCreationExpressionSyntax array:
                if (array.Initializer is not null)
                    return array.Initializer.Expressions.Count > 0;

                // new int[3] holds three default elements: non-empty whenever every dimension is a positive constant.
                foreach (ArrayRankSpecifierSyntax rank in array.Type.RankSpecifiers)
                {
                    foreach (ExpressionSyntax size in rank.Sizes)
                    {
                        Optional<object?> length = model.GetConstantValue(size);
                        if (!length.HasValue || length.Value is not int n || n <= 0)
                            return false;
                    }
                }

                return true;

            case ImplicitArrayCreationExpressionSyntax implicitArray:
                return implicitArray.Initializer.Expressions.Count > 0;

            case ObjectCreationExpressionSyntax creation:
                return creation.Initializer is { Expressions.Count: > 0 } || IsNonEmptyType(model.GetTypeInfo(creation).Type);

            case ImplicitObjectCreationExpressionSyntax implicitCreation:
                return implicitCreation.Initializer is { Expressions.Count: > 0 };

            case InvocationExpressionSyntax invocation:
                {
                    if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
                        return false;

                    string container = method.ContainingType.ToDisplayString();
                    if (container == "System.Linq.Enumerable")
                    {
                        if (method.Name is "Prepend" or "Append")
                            return true;

                        if (method.Name is "Range" or "Repeat" && invocation.ArgumentList.Arguments.Count == 2)
                        {
                            Optional<object?> count = model.GetConstantValue(invocation.ArgumentList.Arguments[1].Expression);
                            return count.HasValue && count.Value is int n && n > 0;
                        }

                        // Order-preserving operators keep non-emptiness.
                        if (method.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" or "Select" or "Reverse" or "Distinct" or "Order" or "OrderDescending" or "ToList" or "ToArray" or "AsEnumerable" or "Cast" or "Concat" or "Union" or "DefaultIfEmpty")
                        {
                            if (method.Name == "DefaultIfEmpty")
                                return true;

                            // Extension syntax passes the source as the receiver, static syntax (Enumerable.Select(xs, f)) as
                            // the first argument.
                            List<ExpressionSyntax> operands = new();
                            if (method.ReducedFrom is not null)
                            {
                                if (invocation.Expression is not MemberAccessExpressionSyntax receiver)
                                    return false;
                                operands.Add(receiver.Expression);
                            }

                            operands.AddRange(invocation.ArgumentList.Arguments.Select(argument => argument.Expression));

                            // Concat and Union are non-empty when either side is, the rest when their source is.
                            int sources = method.Name is "Concat" or "Union" ? 2 : 1;
                            return operands.Take(sources).Any(operand => IsProvenNonEmpty(operand, model));
                        }
                    }

                    return false;
                }

            default:
                return false;
        }
    }
}
