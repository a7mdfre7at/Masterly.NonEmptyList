using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// A value converter for Entity Framework Core that converts NonEmptyList&lt;T&gt; to and from JSON strings for database storage.
/// </summary>
/// <typeparam name="T">The type of elements in the NonEmptyList.</typeparam>
public class NonEmptyListValueConverter<T> : ValueConverter<NonEmptyList<T>, string>
{
    /// <summary>
    /// Creates a new instance of NonEmptyListValueConverter with default JSON serializer options.
    /// </summary>
    public NonEmptyListValueConverter()
        : this(null)
    {
    }

    /// <summary>
    /// Creates a new instance of NonEmptyListValueConverter with custom JSON serializer options.
    /// </summary>
    /// <param name="jsonSerializerOptions">Optional JSON serializer options for customizing serialization behavior.</param>
    public NonEmptyListValueConverter(JsonSerializerOptions? jsonSerializerOptions)
        : base(
            list => Serialize(list, jsonSerializerOptions),
            json => Deserialize(json, jsonSerializerOptions))
    {
    }

    private static string Serialize(NonEmptyList<T> list, JsonSerializerOptions? options)
    {
        return JsonSerializer.Serialize(list, options);
    }

    private static NonEmptyList<T> Deserialize(string json, JsonSerializerOptions? options)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Cannot deserialize an empty or null JSON string to NonEmptyList.");

        List<T>? items = JsonSerializer.Deserialize<List<T>>(json, options);

        if (items is null || items.Count == 0)
            throw new InvalidOperationException("Cannot create a NonEmptyList from an empty JSON array.");

        return NonEmptyList<T>.From(items);
    }
}
