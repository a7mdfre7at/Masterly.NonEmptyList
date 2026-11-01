using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// Rewrites the type-preserving LINQ operators of <see cref="NonEmptyList{T}"/> back to their
/// <see cref="Enumerable"/> equivalents before EF Core compiles a query, so that expressions such as
/// <c>order.Items.Select(i => i.Name)</c> or <c>order.Items.Sum(i => i.Price)</c> on a NonEmptyList navigation
/// translate exactly as they would on a <see cref="List{T}"/>. Also rewrites the <c>Count</c>, <c>Head</c>,
/// <c>First</c> and <c>Last</c> properties to <c>Count()</c>, <c>First()</c> and <c>Last()</c>.
/// </summary>
/// <remarks>
/// Register with <see cref="DbContextOptionsBuilderExtensions.UseNonEmptyList(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)"/>
/// or <see cref="DbContextOptionsBuilderExtensions.UseNonEmptyListQueryTranslation(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)"/>.
/// </remarks>
public sealed class NonEmptyListQueryExpressionInterceptor : IQueryExpressionInterceptor
{
    /// <inheritdoc />
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
        => Rewriter.Instance.Visit(queryExpression);

    /// <summary>
    /// Rewrites a LINQ expression tree so that NonEmptyList operators become Enumerable operators.
    /// Exposed for testing and for custom query pipelines.
    /// </summary>
    /// <param name="expression">The expression to rewrite.</param>
    /// <returns>The rewritten expression, or the same instance when nothing changed.</returns>
    public static Expression Rewrite(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return Rewriter.Instance.Visit(expression);
    }

    private sealed class Rewriter : ExpressionVisitor
    {
        public static readonly Rewriter Instance = new();

        private static readonly HashSet<Type> SourceTypes = new()
        {
            typeof(NonEmptyListLinqExtensions),
            typeof(NonEmptyListMathExtensions),
        };

        private static readonly MethodInfo[] EnumerableMethods =
            typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo ToNonEmptyListMethod = typeof(NonEmptyListLinqExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(NonEmptyListLinqExtensions.ToNonEmptyList)
                         && m.GetParameters()[0].ParameterType.IsGenericType
                         && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        private static readonly ConcurrentDictionary<MethodInfo, MethodInfo?> EquivalentCache = new();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType is { } declaringType && SourceTypes.Contains(declaringType))
            {
                MethodInfo? target = EquivalentCache.GetOrAdd(node.Method, FindEnumerableEquivalent);
                if (target is not null)
                {
                    Expression? rewritten = TryBuildCall(node, target);
                    if (rewritten is not null)
                        return rewritten;
                }
            }

            try
            {
                return base.VisitMethodCall(node);
            }
            catch (ArgumentException)
            {
                // A rewritten child no longer fits this call's parameter types (for example a NonEmptyList-only
                // instance method applied to the result). Keep the subtree as written and let EF report it.
                return node;
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is not null
                && node.Member is PropertyInfo property
                && property.DeclaringType is { IsGenericType: true } declaringType
                && declaringType.GetGenericTypeDefinition() == typeof(NonEmptyList<>))
            {
                string? enumerableName = property.Name switch
                {
                    nameof(NonEmptyList<object>.Count) => nameof(Enumerable.Count),
                    nameof(NonEmptyList<object>.Head) => nameof(Enumerable.First),
                    nameof(NonEmptyList<object>.First) => nameof(Enumerable.First),
                    nameof(NonEmptyList<object>.Last) => nameof(Enumerable.Last),
                    _ => null,
                };

                if (enumerableName is not null)
                {
                    Expression source = Visit(node.Expression);
                    Type elementType = declaringType.GetGenericArguments()[0];
                    return Expression.Call(typeof(Enumerable), enumerableName, new[] { elementType }, source);
                }
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Expression body = Visit(node.Body);
            if (ReferenceEquals(body, node.Body))
                return node;

            Type returnType = typeof(T).GetMethod("Invoke")!.ReturnType;
            return Expression.Lambda<T>(Coerce(body, returnType), node.Name, node.TailCall, node.Parameters);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Constructor is null || node.Arguments.Count == 0)
                return base.VisitNew(node);

            ParameterInfo[] parameters = node.Constructor.GetParameters();
            Expression[] arguments = new Expression[node.Arguments.Count];
            bool changed = false;

            for (int i = 0; i < arguments.Length; i++)
            {
                Expression visited = Coerce(Visit(node.Arguments[i]), parameters[i].ParameterType);
                changed |= !ReferenceEquals(visited, node.Arguments[i]);
                arguments[i] = visited;
            }

            if (!changed)
                return node;

            return node.Members is null
                ? Expression.New(node.Constructor, arguments)
                : Expression.New(node.Constructor, arguments, node.Members);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            Type memberType = node.Member switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => node.Expression.Type,
            };

            Expression visited = Coerce(Visit(node.Expression), memberType);
            return ReferenceEquals(visited, node.Expression) ? node : node.Update(visited);
        }

        /// <summary>
        /// Builds the Enumerable call, adapting lambda arguments whose delegate type differs only by return type.
        /// Returns null when the call cannot be built, in which case the original node is kept and EF reports
        /// its usual translation error.
        /// </summary>
        private Expression? TryBuildCall(MethodCallExpression node, MethodInfo target)
        {
            ParameterInfo[] targetParameters = target.GetParameters();
            Expression[] arguments = new Expression[node.Arguments.Count];

            for (int i = 0; i < arguments.Length; i++)
            {
                Expression argument = node.Arguments[i];
                Type targetType = targetParameters[i].ParameterType;

                if (argument is LambdaExpression lambda && lambda.Type != targetType && typeof(Delegate).IsAssignableFrom(targetType))
                {
                    Expression body = Visit(lambda.Body);
                    Type returnType = targetType.GetMethod("Invoke")!.ReturnType;
                    arguments[i] = Expression.Lambda(targetType, Coerce(body, returnType), lambda.Name, lambda.TailCall, lambda.Parameters);
                }
                else
                {
                    arguments[i] = Visit(argument);
                }
            }

            try
            {
                return Expression.Call(target, arguments);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Makes an expression fit an expected type when a rewrite changed it from NonEmptyList to IEnumerable.
        /// The materialization back into a NonEmptyList happens on the client, after the subquery has been evaluated.
        /// </summary>
        private static Expression Coerce(Expression expression, Type expectedType)
        {
            if (expectedType.IsAssignableFrom(expression.Type))
                return expression;

            if (expectedType.IsGenericType
                && expectedType.GetGenericTypeDefinition() == typeof(NonEmptyList<>)
                && TryGetEnumerableElementType(expression.Type, out Type? elementType)
                && elementType == expectedType.GetGenericArguments()[0])
            {
                return Expression.Call(ToNonEmptyListMethod.MakeGenericMethod(elementType), expression);
            }

            return expression;
        }

        private static bool TryGetEnumerableElementType(Type type, out Type? elementType)
        {
            foreach (Type candidate in type.IsInterface ? new[] { type }.Concat(type.GetInterfaces()) : type.GetInterfaces())
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elementType = candidate.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = null;
            return false;
        }

        /// <summary>
        /// Finds the <see cref="Enumerable"/> method with the same name whose parameters accept the arguments of the
        /// NonEmptyList operator. Lambda parameters match when they differ only by a return type that is assignable.
        /// </summary>
        private static MethodInfo? FindEnumerableEquivalent(MethodInfo source)
        {
            Type[] typeArguments = source.IsGenericMethod ? source.GetGenericArguments() : Type.EmptyTypes;
            ParameterInfo[] sourceParameters = source.GetParameters();

            foreach (MethodInfo candidate in EnumerableMethods)
            {
                if (candidate.Name != source.Name)
                    continue;

                ParameterInfo[] candidateParameters = candidate.GetParameters();
                if (candidateParameters.Length != sourceParameters.Length)
                    continue;

                MethodInfo closed;
                if (candidate.IsGenericMethodDefinition)
                {
                    int arity = candidate.GetGenericArguments().Length;
                    if (arity > typeArguments.Length)
                        continue;

                    try
                    {
                        closed = candidate.MakeGenericMethod(typeArguments.Take(arity).ToArray());
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }
                else
                {
                    closed = candidate;
                }

                if (ParametersCompatible(sourceParameters, closed.GetParameters()))
                    return closed;
            }

            return null;
        }

        private static bool ParametersCompatible(ParameterInfo[] source, ParameterInfo[] target)
        {
            for (int i = 0; i < source.Length; i++)
            {
                Type sourceType = source[i].ParameterType;
                Type targetType = target[i].ParameterType;

                if (targetType.IsAssignableFrom(sourceType))
                    continue;

                if (!IsDelegateCompatible(sourceType, targetType))
                    return false;
            }

            return true;
        }

        private static bool IsDelegateCompatible(Type sourceDelegate, Type targetDelegate)
        {
            if (!typeof(Delegate).IsAssignableFrom(sourceDelegate) || !typeof(Delegate).IsAssignableFrom(targetDelegate))
                return false;

            MethodInfo sourceInvoke = sourceDelegate.GetMethod("Invoke")!;
            MethodInfo targetInvoke = targetDelegate.GetMethod("Invoke")!;

            ParameterInfo[] sourceParameters = sourceInvoke.GetParameters();
            ParameterInfo[] targetParameters = targetInvoke.GetParameters();
            if (sourceParameters.Length != targetParameters.Length)
                return false;

            for (int i = 0; i < sourceParameters.Length; i++)
            {
                if (sourceParameters[i].ParameterType != targetParameters[i].ParameterType)
                    return false;
            }

            return targetInvoke.ReturnType.IsAssignableFrom(sourceInvoke.ReturnType);
        }
    }
}
