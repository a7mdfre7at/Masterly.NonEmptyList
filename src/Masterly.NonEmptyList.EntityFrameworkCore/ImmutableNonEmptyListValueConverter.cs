using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// A value converter for Entity Framework Core that converts ImmutableNonEmptyList&lt;T&gt; to and from JSON strings for database storage.
/// </summary>
/// <typeparam name="T">The type of elements in the ImmutableNonEmptyList.</typeparam>
public class ImmutableNonEmptyListValueConverter<T> : ValueConverter<ImmutableNonEmptyList<T>, string>
{
    /// <summary>
    /// Creates a new instance of ImmutableNonEmptyListValueConverter with default JSON serializer options.
    /// </summary>
    public ImmutableNonEmptyListValueConverter()
        : this(null)
    {
    }

    /// <summary>
    /// Creates a new instance of ImmutableNonEmptyListValueConverter with custom JSON serializer options.
    /// </summary>
    /// <param name="jsonSerializerOptions">Optional JSON serializer options for customizing serialization behavior.</param>
    public ImmutableNonEmptyListValueConverter(JsonSerializerOptions? jsonSerializerOptions)
        : base(
            list => Serialize(list, jsonSerializerOptions),
            json => Deserialize(json, jsonSerializerOptions))
    {
    }

    private static string Serialize(ImmutableNonEmptyList<T> list, JsonSerializerOptions? options)
    {
        return JsonSerializer.Serialize(list, options);
    }

    private static ImmutableNonEmptyList<T> Deserialize(string json, JsonSerializerOptions? options)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Cannot deserialize an empty or null JSON string to ImmutableNonEmptyList.");

        List<T>? items = JsonSerializer.Deserialize<List<T>>(json, options);

        if (items is null || items.Count == 0)
            throw new InvalidOperationException("Cannot create an ImmutableNonEmptyList from an empty JSON array.");

        return ImmutableNonEmptyList<T>.From(items);
    }
}
