using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Masterly.NonEmptyList.Configuration;

/// <summary>
/// Binds configuration into objects that contain non-empty collections. The default <see cref="ConfigurationBinder"/>
/// cannot create a <see cref="NonEmptyList{T}"/> because the type has no parameterless constructor, so this binder takes
/// over for those properties and delegates everything else to the default binder. A required non-empty property whose
/// configuration is missing or empty fails with an <see cref="InvalidOperationException"/> that names the key, which
/// surfaces at startup when combined with <c>ValidateOnStart()</c>.
/// </summary>
public static class NonEmptyConfigurationBinder
{
    /// <summary>Binds the configuration section into a new instance of <typeparamref name="TOptions"/>.</summary>
    public static TOptions Get<TOptions>(IConfiguration configuration) where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configuration);
        TOptions instance = (TOptions)(Activator.CreateInstance(typeof(TOptions))
            ?? throw new InvalidOperationException($"Cannot create an instance of {typeof(TOptions)}"));
        Bind(configuration, instance);
        return instance;
    }

    /// <summary>Binds the configuration section onto an existing instance, property by property.</summary>
    public static void Bind(IConfiguration configuration, object instance)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(instance);
        BindObject(configuration, instance, depth: 0);
    }

    /// <summary>Returns <see langword="true"/> if the binder handles the type itself instead of delegating to the default binder.</summary>
    public static bool IsNonEmptyType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type == typeof(NonEmptyString) || type == typeof(NonEmptyString?) || FindNonEmptyCollectionShape(type) is not null;
    }

    /// <summary>
    /// Reads a non-empty value of <paramref name="targetType"/> from a section. Accepts a JSON-style array
    /// (<c>Hosts:0</c>, <c>Hosts:1</c>) or a comma-separated scalar (<c>Hosts=a,b</c>). Returns <see langword="null"/>
    /// when the section has no value and no children.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the configured value is present but empty or invalid.</exception>
    public static object? ReadNonEmpty(IConfigurationSection section, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(targetType);

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(NonEmptyString))
        {
            if (section.Value is null && !section.GetChildren().Any())
                return null;
            return NonEmptyString.TryCreate(section.Value, out NonEmptyString result)
                ? result
                : throw new InvalidOperationException($"Configuration key '{section.Path}' must be a non-empty string.");
        }

        Shape? shape = FindNonEmptyCollectionShape(underlying)
            ?? throw new ArgumentException($"{targetType} is not a non-empty collection type", nameof(targetType));

        bool hasChildren = section.GetChildren().Any();
        if (section.Value is null && !hasChildren)
            return null;

        object materialized;
        try
        {
            if (shape.IsDictionary)
            {
                Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(shape.KeyType!, shape.ElementType);
                materialized = section.Get(dictionaryType) ?? Activator.CreateInstance(dictionaryType)!;
                if (((ICollection)materialized).Count == 0)
                    throw new InvalidOperationException($"Configuration key '{section.Path}' must contain at least one entry.");
            }
            else
            {
                Type listType = typeof(List<>).MakeGenericType(shape.ElementType);
                IList list;
                if (hasChildren)
                {
                    list = (IList)(section.Get(listType) ?? Activator.CreateInstance(listType)!);
                }
                else
                {
                    list = (IList)Activator.CreateInstance(listType)!;
                    TypeConverter converter = TypeDescriptor.GetConverter(shape.ElementType);
                    foreach (string part in section.Value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        list.Add(converter.ConvertFromInvariantString(part));
                }

                if (list.Count == 0)
                    throw new InvalidOperationException($"Configuration key '{section.Path}' must contain at least one element.");

                materialized = list;
            }

            return InvokeFrom(shape.From, materialized);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException inner)
        {
            throw new InvalidOperationException($"Configuration key '{section.Path}' is invalid: {inner.Message}", inner);
        }
        catch (InvalidOperationException ex) when (!ex.Message.StartsWith("Configuration key", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Configuration key '{section.Path}' is invalid: {ex.Message}", ex);
        }
    }

    private static void BindObject(IConfiguration configuration, object instance, int depth)
    {
        if (depth > 32)
            throw new InvalidOperationException("Configuration object graph is too deep (possible cycle).");

        Type type = instance.GetType();
        NullabilityInfoContext nullability = new();

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 || property.GetMethod is null)
                continue;

            IConfigurationSection section = configuration.GetSection(property.Name);
            Type propertyType = property.PropertyType;

            if (IsNonEmptyType(propertyType))
            {
                if (property.SetMethod is null)
                    continue;

                object? value = ReadNonEmpty(section, propertyType);
                if (value is null)
                {
                    object? current = property.GetValue(instance);
                    bool hasValue = current is not null && (current is not NonEmptyString text || text.HasValue);
                    bool required = !hasValue && !IsNullable(property, nullability);
                    if (required)
                        throw new InvalidOperationException($"Configuration key '{section.Path}' is required and must contain at least one element.");
                    continue;
                }

                property.SetValue(instance, value);
                continue;
            }

            if (!section.Exists())
                continue;

            if (IsComplexObject(propertyType))
            {
                object? current = property.GetValue(instance);
                if (current is null)
                {
                    if (property.SetMethod is null || propertyType.GetConstructor(Type.EmptyTypes) is null)
                        continue;

                    current = Activator.CreateInstance(propertyType)!;
                    property.SetValue(instance, current);
                }

                BindObject(section, current, depth + 1);
                continue;
            }

            if (property.SetMethod is null)
                continue;

            object? bound = section.Get(propertyType);
            if (bound is not null)
                property.SetValue(instance, bound);
        }
    }

    private static bool IsNullable(PropertyInfo property, NullabilityInfoContext context)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
            return true;

        return context.Create(property).WriteState == NullabilityState.Nullable;
    }

    private static bool IsComplexObject(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri) ||
            type == typeof(DateOnly) || type == typeof(TimeOnly) || type.IsValueType)
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(type))
            return false;

        if (TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string)))
            return false;

        return type.IsClass;
    }

    private static Shape? FindNonEmptyCollectionShape(Type type)
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
        return from.Invoke(null, args);
    }

    private sealed record Shape(Type ElementType, Type? KeyType, bool IsDictionary, MethodInfo From);
}
