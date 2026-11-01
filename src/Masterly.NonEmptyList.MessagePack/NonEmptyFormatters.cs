using System.Reflection;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Masterly.NonEmptyList.MessagePack;

/// <summary>Formatter for <see cref="NonEmptyList{T}"/>. Serializes as a MessagePack array and rejects empty arrays.</summary>
public sealed class NonEmptyListFormatter<T> : IMessagePackFormatter<NonEmptyList<T>?>
{
    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, NonEmptyList<T>? value, MessagePackSerializerOptions options)
        => SequenceFormatter.Write(ref writer, value, options);

    /// <inheritdoc />
    public NonEmptyList<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        List<T>? items = SequenceFormatter.Read<T>(ref reader, options, nameof(NonEmptyList<T>));
        return items is null ? null : NonEmptyList<T>.From(items);
    }
}

/// <summary>Formatter for <see cref="ImmutableNonEmptyList{T}"/>.</summary>
public sealed class ImmutableNonEmptyListFormatter<T> : IMessagePackFormatter<ImmutableNonEmptyList<T>?>
{
    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, ImmutableNonEmptyList<T>? value, MessagePackSerializerOptions options)
        => SequenceFormatter.Write(ref writer, value, options);

    /// <inheritdoc />
    public ImmutableNonEmptyList<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        List<T>? items = SequenceFormatter.Read<T>(ref reader, options, nameof(ImmutableNonEmptyList<T>));
        return items is null ? null : ImmutableNonEmptyList<T>.From(items);
    }
}

/// <summary>Formatter for <see cref="NonEmptySet{T}"/>.</summary>
public sealed class NonEmptySetFormatter<T> : IMessagePackFormatter<NonEmptySet<T>?>
{
    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, NonEmptySet<T>? value, MessagePackSerializerOptions options)
        => SequenceFormatter.Write(ref writer, value, options);

    /// <inheritdoc />
    public NonEmptySet<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        List<T>? items = SequenceFormatter.Read<T>(ref reader, options, nameof(NonEmptySet<T>));
        return items is null ? null : NonEmptySet<T>.From(items);
    }
}

/// <summary>Formatter for <see cref="ConstrainedList{T, TConstraint}"/>. Checks the constraint after reading.</summary>
public sealed class ConstrainedListFormatter<T, TConstraint> : IMessagePackFormatter<ConstrainedList<T, TConstraint>?>
    where TConstraint : IListConstraint
{
    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, ConstrainedList<T, TConstraint>? value, MessagePackSerializerOptions options)
        => SequenceFormatter.Write(ref writer, value, options);

    /// <inheritdoc />
    public ConstrainedList<T, TConstraint>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        List<T>? items = SequenceFormatter.Read<T>(ref reader, options, "ConstrainedList");
        if (items is null)
            return null;

        return ConstrainedList<T, TConstraint>.TryFrom(items, out ConstrainedList<T, TConstraint>? result)
            ? result
            : throw new MessagePackSerializationException($"ConstrainedList does not satisfy the constraint: expected {ConstrainedList<T, TConstraint>.Description}");
    }
}

/// <summary>
/// Formatter for classes that derive from <see cref="NonEmptyList{T}"/>, such as <c>[NonEmptyCollection]</c>-generated
/// collections. Uses the type's static <c>From(IEnumerable&lt;T&gt;)</c> factory so every invariant is re-checked.
/// </summary>
public sealed class DerivedNonEmptyListFormatter<TList, T> : IMessagePackFormatter<TList?>
    where TList : NonEmptyList<T>
{
    private static readonly MethodInfo From = FindFrom();

    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, TList? value, MessagePackSerializerOptions options)
        => SequenceFormatter.Write(ref writer, value, options);

    /// <inheritdoc />
    public TList? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        List<T>? items = SequenceFormatter.Read<T>(ref reader, options, typeof(TList).Name);
        if (items is null)
            return null;

        try
        {
            // Unwrapped, so a factory's own exception surfaces as itself rather than as TargetInvocationException.
            return (TList)From.Invoke(null, BindingFlags.DoNotWrapExceptions, null, new object[] { items }, null)!;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new MessagePackSerializationException($"{typeof(TList).Name}: {ex.Message}", ex);
        }
    }

    private static MethodInfo FindFrom()
    {
        Type parameter = typeof(IEnumerable<T>);
        for (Type? current = typeof(TList); current is not null; current = current.BaseType)
        {
            MethodInfo? method = current
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == "From" && m.GetParameters() is { Length: 1 } ps && ps[0].ParameterType == parameter && typeof(TList).IsAssignableFrom(m.ReturnType));
            if (method is not null)
                return method;
        }

        throw new MessagePackSerializationException($"{typeof(TList)} does not expose a static From(IEnumerable<{typeof(T).Name}>) factory");
    }
}

/// <summary>Formatter for <see cref="NonEmptyDictionary{TKey, TValue}"/>. Serializes as a MessagePack map and rejects empty maps.</summary>
public sealed class NonEmptyDictionaryFormatter<TKey, TValue> : IMessagePackFormatter<NonEmptyDictionary<TKey, TValue>?>
    where TKey : notnull
{
    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, NonEmptyDictionary<TKey, TValue>? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        IMessagePackFormatter<TKey> keyFormatter = options.Resolver.GetFormatterWithVerify<TKey>();
        IMessagePackFormatter<TValue> valueFormatter = options.Resolver.GetFormatterWithVerify<TValue>();
        writer.WriteMapHeader(value.Count);
        foreach (KeyValuePair<TKey, TValue> pair in value)
        {
            keyFormatter.Serialize(ref writer, pair.Key, options);
            valueFormatter.Serialize(ref writer, pair.Value, options);
        }
    }

    /// <inheritdoc />
    public NonEmptyDictionary<TKey, TValue>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return null;

        int count = reader.ReadMapHeader();
        if (count == 0)
            throw new MessagePackSerializationException("NonEmptyDictionary cannot be empty");

        IMessagePackFormatter<TKey> keyFormatter = options.Resolver.GetFormatterWithVerify<TKey>();
        IMessagePackFormatter<TValue> valueFormatter = options.Resolver.GetFormatterWithVerify<TValue>();
        options.Security.DepthStep(ref reader);
        try
        {
            Dictionary<TKey, TValue> items = new(count);
            for (int i = 0; i < count; i++)
            {
                TKey key = keyFormatter.Deserialize(ref reader, options);
                TValue value = valueFormatter.Deserialize(ref reader, options);
                if (value is null)
                    throw new MessagePackSerializationException("NonEmptyDictionary cannot contain null values");
                items[key] = value;
            }

            return NonEmptyDictionary<TKey, TValue>.From(items);
        }
        finally
        {
            reader.Depth--;
        }
    }
}

/// <summary>Formatter for <see cref="NonEmptyString"/>. Serializes as a MessagePack string and rejects blank strings.</summary>
public sealed class NonEmptyStringFormatter : IMessagePackFormatter<NonEmptyString>
{
    /// <inheritdoc />
    public void Serialize(ref MessagePackWriter writer, NonEmptyString value, MessagePackSerializerOptions options)
        => writer.Write(value.Value);

    /// <inheritdoc />
    public NonEmptyString Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        string? value = reader.ReadString();
        return NonEmptyString.TryCreate(value, out NonEmptyString result)
            ? result
            : throw new MessagePackSerializationException("NonEmptyString cannot be empty or whitespace");
    }
}

internal static class SequenceFormatter
{
    public static void Write<T>(ref MessagePackWriter writer, IReadOnlyCollection<T>? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        writer.WriteArrayHeader(value.Count);
        foreach (T item in value)
            formatter.Serialize(ref writer, item, options);
    }

    public static List<T>? Read<T>(ref MessagePackReader reader, MessagePackSerializerOptions options, string typeName)
    {
        if (reader.TryReadNil())
            return null;

        int count = reader.ReadArrayHeader();
        if (count == 0)
            throw new MessagePackSerializationException($"{typeName} cannot be empty");

        IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();
        options.Security.DepthStep(ref reader);
        try
        {
            List<T> items = new(count);
            for (int i = 0; i < count; i++)
            {
                T item = formatter.Deserialize(ref reader, options);
                if (item is null)
                    throw new MessagePackSerializationException($"{typeName} cannot contain null elements");
                items.Add(item);
            }

            return items;
        }
        finally
        {
            reader.Depth--;
        }
    }
}

/// <summary>
/// Resolves formatters for every non-empty type. Combine with the standard resolver:
/// <c>MessagePackSerializerOptions.Standard.WithNonEmptyResolver()</c>.
/// </summary>
public sealed class NonEmptyResolver : IFormatterResolver
{
    /// <summary>The shared instance.</summary>
    public static readonly NonEmptyResolver Instance = new();

    private NonEmptyResolver()
    {
    }

    /// <inheritdoc />
    public IMessagePackFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

    private static class Cache<T>
    {
        public static readonly IMessagePackFormatter<T>? Formatter = (IMessagePackFormatter<T>?)CreateFormatter(typeof(T));
    }

    internal static object? CreateFormatter(Type type)
    {
        if (type == typeof(NonEmptyString))
            return new NonEmptyStringFormatter();

        if (type == typeof(NonEmptyString?))
            return new global::MessagePack.Formatters.NullableFormatter<NonEmptyString>();

        if (type.IsGenericType)
        {
            Type definition = type.GetGenericTypeDefinition();
            Type[] args = type.GetGenericArguments();

            if (definition == typeof(NonEmptyList<>))
                return Activator.CreateInstance(typeof(NonEmptyListFormatter<>).MakeGenericType(args));
            if (definition == typeof(ImmutableNonEmptyList<>))
                return Activator.CreateInstance(typeof(ImmutableNonEmptyListFormatter<>).MakeGenericType(args));
            if (definition == typeof(NonEmptySet<>))
                return Activator.CreateInstance(typeof(NonEmptySetFormatter<>).MakeGenericType(args));
            if (definition == typeof(ConstrainedList<,>))
                return Activator.CreateInstance(typeof(ConstrainedListFormatter<,>).MakeGenericType(args));
            if (definition == typeof(NonEmptyDictionary<,>))
                return Activator.CreateInstance(typeof(NonEmptyDictionaryFormatter<,>).MakeGenericType(args));
        }

        // Classes deriving from NonEmptyList<T>, such as [NonEmptyCollection]-generated collections.
        for (Type? current = type.BaseType; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(NonEmptyList<>))
                return Activator.CreateInstance(typeof(DerivedNonEmptyListFormatter<,>).MakeGenericType(type, current.GetGenericArguments()[0]));
        }

        return null;
    }
}

/// <summary>Registration helpers.</summary>
public static class NonEmptyMessagePackExtensions
{
    /// <summary>Returns options whose resolver tries the non-empty formatters first, then the existing resolver.</summary>
    public static MessagePackSerializerOptions WithNonEmptyResolver(this MessagePackSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.WithResolver(CompositeResolver.Create(NonEmptyResolver.Instance, options.Resolver));
    }

    /// <summary>Options based on <see cref="MessagePackSerializerOptions.Standard"/> with the non-empty formatters.</summary>
    public static MessagePackSerializerOptions NonEmptyStandard { get; } = MessagePackSerializerOptions.Standard.WithNonEmptyResolver();
}
