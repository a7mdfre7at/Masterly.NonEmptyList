using System.Collections;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Masterly.NonEmptyList.Testing;

/// <summary>
/// AutoFixture support: <c>fixture.Customize(new NonEmptyCustomization())</c> lets <c>fixture.Create&lt;NonEmptyList&lt;T&gt;&gt;()</c>,
/// DTOs with non-empty properties, <see cref="NonEmptyString"/> and <c>[NonEmptyCollection]</c>-generated classes resolve.
/// </summary>
public sealed class NonEmptyCustomization : ICustomization
{
    /// <summary>The number of elements generated per collection. Defaults to the fixture's <see cref="IFixture.RepeatCount"/>, with a floor of 1.</summary>
    public int? Count { get; init; }

    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Insert(0, new NonEmptySpecimenBuilder(Count ?? fixture.RepeatCount));
    }
}

/// <summary>Creates non-empty instances for AutoFixture requests.</summary>
public sealed class NonEmptySpecimenBuilder : ISpecimenBuilder
{
    private readonly int? _count;

    /// <summary>
    /// Creates a builder that generates <paramref name="count"/> elements per collection (at least 1), or 3 when it
    /// is null. <see cref="NonEmptyCustomization"/> passes the fixture's <see cref="IFixture.RepeatCount"/>.
    /// </summary>
    public NonEmptySpecimenBuilder(int? count = null) => _count = count;

    /// <inheritdoc />
    public object Create(object request, ISpecimenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (request is not Type type)
            return new NoSpecimen();

        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(NonEmptyString))
        {
            string text = (string)context.Resolve(typeof(string));
            return new NonEmptyString(string.IsNullOrWhiteSpace(text) ? Guid.NewGuid().ToString("N") : text);
        }

        Shape? shape = FindShape(underlying);
        if (shape is null)
            return new NoSpecimen();

        int count = Math.Max(1, _count ?? 3);

        if (shape.IsDictionary)
        {
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(shape.KeyType!, shape.ElementType))!;
            for (int attempt = 0; dictionary.Count < count && attempt < count * 4; attempt++)
                dictionary[context.Resolve(shape.KeyType!)] = context.Resolve(shape.ElementType);

            if (dictionary.Count == 0)
                return new NoSpecimen();

            return InvokeFrom(shape.From, dictionary)!;
        }

        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(shape.ElementType))!;
        for (int i = 0; i < count; i++)
            list.Add(context.Resolve(shape.ElementType));

        while (true)
        {
            try
            {
                return InvokeFrom(shape.From, list)!;
            }
            catch (ArgumentException) when (list.Count > 1)
            {
                // The constraint rejected the batch (e.g. Distinct, Sorted, MaxCount). Shrink and retry; a single
                // element that still fails lets the constraint's own error through.
                list.RemoveAt(list.Count - 1);
            }
        }
    }

    private static Shape? FindShape(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (!current.IsGenericType)
                continue;

            Type definition = current.GetGenericTypeDefinition();
            Type[] args = current.GetGenericArguments();

            if (definition == typeof(NonEmptyDictionary<,>))
                return new Shape(args[1], args[0], true, FindFrom(type, typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]))));

            if (definition == typeof(NonEmptyList<>) || definition == typeof(ImmutableNonEmptyList<>) ||
                definition == typeof(NonEmptySet<>) || definition == typeof(ConstrainedList<,>))
                return new Shape(args[0], null, false, FindFrom(type, typeof(IEnumerable<>).MakeGenericType(args[0])));
        }

        return null;
    }

    private static MethodInfo FindFrom(Type type, Type parameterType)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            MethodInfo? method = current
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == "From" && m.GetParameters() is { Length: >= 1 } ps && ps[0].ParameterType == parameterType &&
                                     ps.Skip(1).All(p => p.IsOptional) && m.ReturnType.IsAssignableTo(type));
            if (method is not null)
                return method;
        }

        throw new InvalidOperationException($"{type} does not expose a static From({parameterType}) factory.");
    }


    private static object? InvokeFrom(MethodInfo from, object argument)
    {
        ParameterInfo[] parameters = from.GetParameters();
        object?[] args = new object?[parameters.Length];
        args[0] = argument;
        for (int i = 1; i < parameters.Length; i++)
            args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
        // Unwrapped, so a factory's own exception surfaces as itself rather than as TargetInvocationException.
        return from.Invoke(null, BindingFlags.DoNotWrapExceptions, null, args, null);
    }

    private sealed record Shape(Type ElementType, Type? KeyType, bool IsDictionary, MethodInfo From);
}
