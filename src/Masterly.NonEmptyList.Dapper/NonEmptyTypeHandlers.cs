using System.Data;
using System.Reflection;
using System.Text.Json;
using Dapper;

namespace Masterly.NonEmptyList.Dapper;

/// <summary>
/// Stores a non-empty collection as a JSON text column and rebuilds it through the type's <c>From</c> factory when
/// reading, so an empty or invalid column value fails loudly instead of producing an empty list.
/// </summary>
/// <typeparam name="TCollection">The non-empty collection type.</typeparam>
public sealed class NonEmptyJsonTypeHandler<TCollection> : SqlMapper.TypeHandler<TCollection> where TCollection : class
{
    private readonly JsonSerializerOptions? _options;

    /// <summary>Creates a handler using the given System.Text.Json options.</summary>
    public NonEmptyJsonTypeHandler(JsonSerializerOptions? options = null) => _options = options;

    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, TCollection? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value, _options);
    }

    /// <inheritdoc />
    public override TCollection? Parse(object value)
    {
        if (value is null or DBNull)
            return null;

        string json = value switch
        {
            string s => s,
            byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
            _ => value.ToString() ?? string.Empty,
        };

        if (string.IsNullOrWhiteSpace(json))
            throw new DataException($"Column value for {typeof(TCollection).Name} is empty");

        try
        {
            return JsonSerializer.Deserialize<TCollection>(json, _options)
                ?? throw new DataException($"Column value for {typeof(TCollection).Name} deserialized to null");
        }
        catch (JsonException ex)
        {
            throw new DataException($"Column value for {typeof(TCollection).Name} is invalid: {ex.Message}", ex);
        }
    }
}

/// <summary>Stores a <see cref="NonEmptyString"/> as text and rejects blank values when reading.</summary>
public sealed class NonEmptyStringTypeHandler : SqlMapper.TypeHandler<NonEmptyString>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, NonEmptyString value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.HasValue ? value.Value : DBNull.Value;
    }

    /// <inheritdoc />
    public override NonEmptyString Parse(object value)
    {
        string? text = value is null or DBNull ? null : value.ToString();
        return NonEmptyString.TryCreate(text, out NonEmptyString result)
            ? result
            : throw new DataException("Column value for NonEmptyString is null, empty or whitespace");
    }
}

/// <summary>Registration helpers. Call once at startup.</summary>
public static class NonEmptyDapper
{
    private static readonly object Gate = new();
    private static readonly HashSet<Type> Registered = new();

    /// <summary>
    /// Registers JSON handlers for <see cref="NonEmptyList{T}"/>, <see cref="ImmutableNonEmptyList{T}"/> and <see cref="NonEmptySet{T}"/>
    /// of <typeparamref name="T"/>. Idempotent.
    /// </summary>
    public static void AddTypeHandlers<T>(JsonSerializerOptions? options = null)
    {
        AddTypeHandler<NonEmptyList<T>>(options);
        AddTypeHandler<ImmutableNonEmptyList<T>>(options);
        AddTypeHandler<NonEmptySet<T>>(options);
    }

    /// <summary>Registers a JSON handler for <see cref="NonEmptyDictionary{TKey, TValue}"/>. Idempotent.</summary>
    public static void AddDictionaryHandler<TKey, TValue>(JsonSerializerOptions? options = null) where TKey : notnull
        => AddTypeHandler<NonEmptyDictionary<TKey, TValue>>(options);

    /// <summary>Registers a JSON handler for <see cref="ConstrainedList{T, TConstraint}"/>. Idempotent.</summary>
    public static void AddConstrainedHandler<T, TConstraint>(JsonSerializerOptions? options = null) where TConstraint : IListConstraint
        => AddTypeHandler<ConstrainedList<T, TConstraint>>(options);

    /// <summary>
    /// Registers a JSON handler for any non-empty collection type, including <c>[NonEmptyCollection]</c>-generated classes.
    /// Idempotent.
    /// </summary>
    public static void AddTypeHandler<TCollection>(JsonSerializerOptions? options = null) where TCollection : class
    {
        lock (Gate)
        {
            if (!Registered.Add(typeof(TCollection)))
                return;

            SqlMapper.AddTypeHandler(new NonEmptyJsonTypeHandler<TCollection>(options));
        }
    }

    /// <summary>Registers the <see cref="NonEmptyString"/> handler. Idempotent.</summary>
    public static void AddNonEmptyStringHandler()
    {
        lock (Gate)
        {
            if (!Registered.Add(typeof(NonEmptyString)))
                return;

            NonEmptyStringTypeHandler handler = new();
            SqlMapper.AddTypeHandler(handler);
            SqlMapper.AddTypeHandler(typeof(NonEmptyString?), handler);
        }
    }

    /// <summary>
    /// Scans a type for properties whose types are non-empty collections or <see cref="NonEmptyString"/> and registers
    /// handlers for each. Useful for registering everything a DTO needs in one call.
    /// </summary>
    public static void AddTypeHandlersFor<TEntity>(JsonSerializerOptions? options = null)
    {
        foreach (PropertyInfo property in typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Type type = property.PropertyType;
            Type underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(NonEmptyString))
            {
                AddNonEmptyStringHandler();
                continue;
            }

            if (IsNonEmptyCollection(underlying))
            {
                lock (Gate)
                {
                    if (!Registered.Add(underlying))
                        continue;

                    object handler = Activator.CreateInstance(typeof(NonEmptyJsonTypeHandler<>).MakeGenericType(underlying), options)!;
                    SqlMapper.AddTypeHandler(underlying, (SqlMapper.ITypeHandler)handler);
                }
            }
        }
    }

    private static bool IsNonEmptyCollection(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (!current.IsGenericType)
                continue;

            Type definition = current.GetGenericTypeDefinition();
            if (definition == typeof(NonEmptyList<>) || definition == typeof(ImmutableNonEmptyList<>) || definition == typeof(NonEmptySet<>) ||
                definition == typeof(ConstrainedList<,>) || definition == typeof(NonEmptyDictionary<,>))
                return true;
        }

        return false;
    }
}
