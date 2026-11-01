using System.Reflection;

namespace Masterly.NonEmptyList.AspNetCore;

/// <summary>
/// Describes what a non-empty type guarantees about its cardinality, derived from the type alone: the element type,
/// whether it is a dictionary, the minimum and maximum item counts and whether elements are unique. Used to document
/// the guarantee in OpenAPI and to explain binding failures.
/// </summary>
public sealed record NonEmptyShape(Type ElementType, bool IsDictionary, int MinItems, int? MaxItems, bool UniqueItems)
{
    /// <summary>Returns the shape of a non-empty collection type, or <see langword="null"/> for any other type.</summary>
    public static NonEmptyShape? Of(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        for (Type? current = underlying; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (!current.IsGenericType)
                continue;

            Type definition = current.GetGenericTypeDefinition();
            Type[] args = current.GetGenericArguments();

            if (definition == typeof(NonEmptyDictionary<,>))
                return new NonEmptyShape(args[1], true, 1, null, false);

            if (definition == typeof(NonEmptySet<>))
                return new NonEmptyShape(args[0], false, 1, null, true);

            if (definition == typeof(ConstrainedList<,>))
            {
                (int min, int? max, bool unique) = Analyze(args[1]);
                return new NonEmptyShape(args[0], false, Math.Max(1, min), max, unique);
            }

            if (definition == typeof(NonEmptyList<>) || definition == typeof(ImmutableNonEmptyList<>))
            {
                // [NonEmptyCollection]-generated classes expose MinCount/MaxCount/IsDistinct constants.
                int min = ReadConst(underlying, "MinCount") ?? 1;
                int? max = ReadConst(underlying, "MaxCount") is int m and not int.MaxValue ? m : null;
                bool unique = ReadConstBool(underlying, "IsDistinct") ?? false;
                return new NonEmptyShape(args[0], false, Math.Max(1, min), max, unique);
            }
        }

        return null;
    }

    private static (int Min, int? Max, bool Unique) Analyze(Type constraint)
    {
        if (!constraint.IsGenericType)
        {
            if (constraint == typeof(Distinct))
                return (1, null, true);
            return (1, null, false);
        }

        Type definition = constraint.GetGenericTypeDefinition();
        Type[] args = constraint.GetGenericArguments();

        if (definition == typeof(AtLeast<>))
            return (Cardinality(args[0]), null, false);
        if (definition == typeof(AtMost<>))
            return (1, Cardinality(args[0]), false);
        if (definition == typeof(Exactly<>))
        {
            int n = Cardinality(args[0]);
            return (n, n, false);
        }

        if (definition == typeof(And<,>))
        {
            (int min1, int? max1, bool unique1) = Analyze(args[0]);
            (int min2, int? max2, bool unique2) = Analyze(args[1]);
            int? max = max1 is null ? max2 : max2 is null ? max1 : Math.Min(max1.Value, max2.Value);
            return (Math.Max(min1, min2), max, unique1 || unique2);
        }

        if (definition == typeof(Or<,>))
        {
            (int min1, int? max1, bool unique1) = Analyze(args[0]);
            (int min2, int? max2, bool unique2) = Analyze(args[1]);
            int? max = max1 is null || max2 is null ? null : Math.Max(max1.Value, max2.Value);
            return (Math.Min(min1, min2), max, unique1 && unique2);
        }

        return (1, null, false);
    }

    private static int Cardinality(Type cardinality)
    {
        PropertyInfo? value = cardinality.GetProperty("Value", BindingFlags.Public | BindingFlags.Static);
        return value?.GetValue(null) is int n ? n : 1;
    }

    private static int? ReadConst(Type type, string name)
        => type.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetRawConstantValue() as int?;

    private static bool? ReadConstBool(Type type, string name)
        => type.GetField(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetRawConstantValue() as bool?;
}
