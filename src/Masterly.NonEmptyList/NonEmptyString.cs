using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList;

/// <summary>
/// A string that is never null, empty or whitespace-only. Converts implicitly to <see cref="string"/>, so it drops into
/// existing APIs, and binds from query strings, route values, configuration and JSON with an empty value rejected at the boundary.
/// </summary>
[JsonConverter(typeof(NonEmptyStringJsonConverter))]
[TypeConverter(typeof(NonEmptyStringTypeConverter))]
[DebuggerDisplay("{Value}")]
public readonly struct NonEmptyString :
    IEquatable<NonEmptyString>,
    IComparable<NonEmptyString>,
    IComparable,
    ISpanParsable<NonEmptyString>,
    ISpanFormattable
{
    private readonly string? _value;

    /// <summary>Wraps a string that has content.</summary>
    /// <exception cref="ArgumentException">Thrown if the string is null, empty or whitespace.</exception>
    public NonEmptyString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty or whitespace", nameof(value));

        _value = value;
    }

    /// <summary>
    /// The underlying string. A default-initialized <see cref="NonEmptyString"/> has no value and throws here;
    /// use <see cref="HasValue"/> to detect one.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown for a default-initialized instance.</exception>
    public string Value => _value ?? throw new InvalidOperationException("This NonEmptyString was default-initialized and holds no value. Create it with the constructor, Parse or TryParse.");

    /// <summary><see langword="false"/> only for a default-initialized instance, which the constructor can never produce.</summary>
    public bool HasValue => _value is not null;

    /// <summary>The number of characters. Always at least 1.</summary>
    public int Length => Value.Length;

    /// <summary>The first character.</summary>
    public char Head => Value[0];

    /// <summary>The last character.</summary>
    public char Last => Value[^1];

    /// <summary>Gets the character at <paramref name="index"/>.</summary>
    public char this[int index] => Value[index];

    /// <summary>Wraps a string, returning <see langword="false"/> instead of throwing for blank input.</summary>
    public static bool TryCreate([NotNullWhen(true)] string? value, out NonEmptyString result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        result = new NonEmptyString(value);
        return true;
    }

    /// <summary>Wraps a string, or returns <see langword="null"/> for blank input.</summary>
    public static NonEmptyString? FromNullable(string? value) => TryCreate(value, out NonEmptyString result) ? result : null;

    /// <inheritdoc />
    public static NonEmptyString Parse(string s, IFormatProvider? provider) => new(s);

    /// <inheritdoc />
    public static NonEmptyString Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        => s.IsWhiteSpace() ? throw new FormatException("Value cannot be empty or whitespace") : new NonEmptyString(s.ToString());

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NonEmptyString result) => TryCreate(s, out result);

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out NonEmptyString result)
    {
        if (s.IsWhiteSpace())
        {
            result = default;
            return false;
        }

        result = new NonEmptyString(s.ToString());
        return true;
    }

    /// <summary>Returns a trimmed copy. Trimming can never produce an empty value because whitespace-only input is rejected.</summary>
    public NonEmptyString Trim() => new(Value.Trim());

    /// <summary>Returns an upper-case copy using the invariant culture.</summary>
    public NonEmptyString ToUpperInvariant() => new(Value.ToUpperInvariant());

    /// <summary>Returns a lower-case copy using the invariant culture.</summary>
    public NonEmptyString ToLowerInvariant() => new(Value.ToLowerInvariant());

    /// <summary>Concatenates another string. The result is non-empty.</summary>
    public NonEmptyString Concat(string? other) => string.IsNullOrEmpty(other) ? this : new NonEmptyString(Value + other);

    /// <summary>Truncates to at most <paramref name="maxLength"/> characters. Always keeps at least one.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the length is less than 1.</exception>
    public NonEmptyString Truncate(int maxLength)
    {
        if (maxLength < 1)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Length must be at least 1");

        string value = Value;
        if (value.Length <= maxLength)
            return this;

        string cut = value[..maxLength];
        return string.IsNullOrWhiteSpace(cut) ? new NonEmptyString(value.TrimStart()[..1]) : new NonEmptyString(cut);
    }

    /// <summary>Splits on a separator and drops blank parts. The result is non-empty because the string has content.</summary>
    public NonEmptyList<NonEmptyString> Split(char separator)
    {
        List<NonEmptyString> parts = new();
        foreach (string part in Value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(part))
                parts.Add(new NonEmptyString(part));
        }

        if (parts.Count == 0)
            parts.Add(Trim());

        return NonEmptyList<NonEmptyString>.FromTrustedList(parts);
    }

    /// <summary>The characters as a non-empty list.</summary>
    public NonEmptyList<char> ToCharList() => NonEmptyList<char>.FromTrustedList(new List<char>(Value));

    /// <summary>Returns the string as a span.</summary>
    public ReadOnlySpan<char> AsSpan() => Value.AsSpan();

    /// <summary>Converts to the underlying string.</summary>
    public static implicit operator string(NonEmptyString value) => value.Value;

    /// <summary>Wraps a string. Throws for blank input, so prefer the constructor or <see cref="TryCreate"/> at trust boundaries.</summary>
    /// <exception cref="ArgumentException">Thrown if the string is null, empty or whitespace.</exception>
    public static explicit operator NonEmptyString(string value) => new(value);

    /// <summary>Converts to a span of characters.</summary>
    public static implicit operator ReadOnlySpan<char>(NonEmptyString value) => value.Value.AsSpan();

    /// <summary>Ordinal equality.</summary>
    public bool Equals(NonEmptyString other) => string.Equals(_value, other._value, StringComparison.Ordinal);

    /// <summary>Equality using a comparison.</summary>
    public bool Equals(NonEmptyString other, StringComparison comparison) => string.Equals(_value, other._value, comparison);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NonEmptyString other ? Equals(other) : obj is string s && string.Equals(_value, s, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => _value is null ? 0 : string.GetHashCode(_value, StringComparison.Ordinal);

    /// <summary>Ordinal comparison.</summary>
    public int CompareTo(NonEmptyString other) => string.CompareOrdinal(_value, other._value);

    int IComparable.CompareTo(object? obj) => obj is NonEmptyString other ? CompareTo(other) : throw new ArgumentException("Object is not a NonEmptyString", nameof(obj));

    /// <summary>Ordinal equality.</summary>
    public static bool operator ==(NonEmptyString left, NonEmptyString right) => left.Equals(right);

    /// <summary>Ordinal inequality.</summary>
    public static bool operator !=(NonEmptyString left, NonEmptyString right) => !left.Equals(right);

    /// <summary>Concatenation. The result is non-empty.</summary>
    public static NonEmptyString operator +(NonEmptyString left, NonEmptyString right) => new(left.Value + right.Value);

    /// <summary>Concatenation. The result is non-empty.</summary>
    public static NonEmptyString operator +(NonEmptyString left, string? right) => left.Concat(right);

    /// <inheritdoc />
    public override string ToString() => _value ?? string.Empty;

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string value = ToString();
        if (destination.Length < value.Length)
        {
            charsWritten = 0;
            return false;
        }

        value.AsSpan().CopyTo(destination);
        charsWritten = value.Length;
        return true;
    }
}

/// <summary>System.Text.Json converter for <see cref="NonEmptyString"/>. Reads and writes a JSON string.</summary>
public sealed class NonEmptyStringJsonConverter : JsonConverter<NonEmptyString>
{
    /// <inheritdoc />
    public override NonEmptyString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a JSON string");

        string? value = reader.GetString();
        return NonEmptyString.TryCreate(value, out NonEmptyString result)
            ? result
            : throw new JsonException("NonEmptyString cannot be empty or whitespace");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, NonEmptyString value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);

    /// <inheritdoc />
    public override NonEmptyString ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => NonEmptyString.TryCreate(reader.GetString(), out NonEmptyString result) ? result : throw new JsonException("NonEmptyString cannot be empty or whitespace");

    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, NonEmptyString value, JsonSerializerOptions options) => writer.WritePropertyName(value.Value);
}

/// <summary>
/// <see cref="TypeConverter"/> for <see cref="NonEmptyString"/>, used by MVC model binding and configuration binding.
/// </summary>
public sealed class NonEmptyStringTypeConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s)
            return NonEmptyString.TryCreate(s, out NonEmptyString result) ? result : throw new FormatException("Value cannot be empty or whitespace");

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is NonEmptyString s)
            return s.ToString();

        return base.ConvertTo(context, culture, value, destinationType);
    }
}

/// <summary>Extension methods for <see cref="NonEmptyString"/>.</summary>
public static class NonEmptyStringExtensions
{
    /// <summary>Wraps a string, throwing for blank input.</summary>
    /// <exception cref="ArgumentException">Thrown if the string is null, empty or whitespace.</exception>
    public static NonEmptyString ToNonEmptyString(this string value) => new(value);

    /// <summary>Wraps a string, or returns <see langword="null"/> for blank input.</summary>
    public static NonEmptyString? ToNonEmptyStringOrNull(this string? value) => NonEmptyString.FromNullable(value);

    /// <summary>Joins non-empty strings with a separator. The result is non-empty because the list is.</summary>
    public static NonEmptyString Join(this NonEmptyList<NonEmptyString> parts, string separator)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return new NonEmptyString(string.Join(separator, parts.Items.Select(static p => p.Value)));
    }
}
