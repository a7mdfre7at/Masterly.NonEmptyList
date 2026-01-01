using System.Text.Json;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// JSON converter factory for NonEmptyList types.
/// </summary>
public class NonEmptyListJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether this factory can convert the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to check.</param>
    /// <returns>True if the type is a NonEmptyList; otherwise, false.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        return typeToConvert.GetGenericTypeDefinition() == typeof(NonEmptyList<>);
    }

    /// <summary>
    /// Creates a converter for the specified NonEmptyList type.
    /// </summary>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>A JSON converter for the specified type.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type elementType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(NonEmptyListJsonConverter<>).MakeGenericType(elementType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// JSON converter for NonEmptyList&lt;T&gt;.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class NonEmptyListJsonConverter<T> : JsonConverter<NonEmptyList<T>>
{
    /// <summary>
    /// Reads and converts the JSON to a NonEmptyList.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>A NonEmptyList containing the deserialized elements.</returns>
    /// <exception cref="JsonException">Thrown if the JSON array is empty or invalid.</exception>
    public override NonEmptyList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array");

        List<T> items = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            T? item = JsonSerializer.Deserialize<T>(ref reader, options);
            if (item is null)
                throw new JsonException("NonEmptyList cannot contain null elements");

            items.Add(item);
        }

        if (items.Count == 0)
            throw new JsonException("NonEmptyList cannot be empty");

        return new NonEmptyList<T>(items[0], items.Skip(1));
    }

    /// <summary>
    /// Writes a NonEmptyList as a JSON array.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The NonEmptyList to serialize.</param>
    /// <param name="options">The serialization options.</param>
    public override void Write(Utf8JsonWriter writer, NonEmptyList<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (T item in value)
        {
            JsonSerializer.Serialize(writer, item, options);
        }

        writer.WriteEndArray();
    }
}
