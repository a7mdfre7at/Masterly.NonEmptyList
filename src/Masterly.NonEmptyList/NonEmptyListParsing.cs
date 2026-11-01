using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Masterly.NonEmptyList;

/// <summary>
/// Parses a single element of a comma-separated list.
/// </summary>
internal interface IElementParser<T>
{
    bool TryParse(ReadOnlySpan<char> text, IFormatProvider? provider, [MaybeNullWhen(false)] out T value);
}

/// <summary>
/// Resolves, once per element type, how to parse an element from text:
/// strings as-is, then <see cref="ISpanParsable{TSelf}"/>, <see cref="IParsable{TSelf}"/>, enums, and finally the
/// element type's <see cref="TypeConverter"/>.
/// </summary>
internal static class ElementParser<T>
{
    public static readonly IElementParser<T>? Instance = Create();

    private static IElementParser<T>? Create()
    {
        Type type = typeof(T);

        if (type == typeof(string))
            return (IElementParser<T>)(object)new StringElementParser();

        if (Implements(type, typeof(ISpanParsable<>)))
            return (IElementParser<T>)Activator.CreateInstance(typeof(SpanParsableElementParser<>).MakeGenericType(type))!;

        if (Implements(type, typeof(IParsable<>)))
            return (IElementParser<T>)Activator.CreateInstance(typeof(ParsableElementParser<>).MakeGenericType(type))!;

        if (type.IsEnum)
            return new EnumElementParser<T>();

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        return converter.CanConvertFrom(typeof(string))
            ? new TypeConverterElementParser<T>(converter)
            : null;
    }

    /// <summary>
    /// True when <paramref name="type"/> implements <paramref name="selfInterface"/> closed over itself,
    /// for example <c>int : ISpanParsable&lt;int&gt;</c>. Avoids MakeGenericType, which enforces constraints and throws.
    /// </summary>
    private static bool Implements(Type type, Type selfInterface)
    {
        foreach (Type candidate in type.GetInterfaces())
        {
            if (candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == selfInterface
                && candidate.GetGenericArguments()[0] == type)
                return true;
        }

        return false;
    }

    private sealed class StringElementParser : IElementParser<string>
    {
        public bool TryParse(ReadOnlySpan<char> text, IFormatProvider? provider, [MaybeNullWhen(false)] out string value)
        {
            value = text.ToString();
            return true;
        }
    }
}

internal sealed class SpanParsableElementParser<T> : IElementParser<T> where T : ISpanParsable<T>
{
    public bool TryParse(ReadOnlySpan<char> text, IFormatProvider? provider, [MaybeNullWhen(false)] out T value)
        => T.TryParse(text, provider, out value);
}

internal sealed class ParsableElementParser<T> : IElementParser<T> where T : IParsable<T>
{
    public bool TryParse(ReadOnlySpan<char> text, IFormatProvider? provider, [MaybeNullWhen(false)] out T value)
        => T.TryParse(text.ToString(), provider, out value);
}

internal sealed class EnumElementParser<T> : IElementParser<T>
{
    public bool TryParse(ReadOnlySpan<char> text, IFormatProvider? provider, [MaybeNullWhen(false)] out T value)
    {
        if (Enum.TryParse(typeof(T), text.ToString(), ignoreCase: true, out object? parsed) && parsed is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }
}

internal sealed class TypeConverterElementParser<T> : IElementParser<T>
{
    private readonly TypeConverter _converter;

    public TypeConverterElementParser(TypeConverter converter) => _converter = converter;

    public bool TryParse(ReadOnlySpan<char> text, IFormatProvider? provider, [MaybeNullWhen(false)] out T value)
    {
        try
        {
            CultureInfo culture = provider as CultureInfo ?? CultureInfo.CurrentCulture;
            object? converted = _converter.ConvertFromString(null, culture, text.ToString());
            if (converted is T typed)
            {
                value = typed;
                return true;
            }
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or NotSupportedException or OverflowException)
        {
            // Fall through and report failure.
        }

        value = default;
        return false;
    }
}

/// <summary>
/// A <see cref="TypeConverter"/> that converts between a comma-separated string and a <see cref="NonEmptyList{T}"/>.
/// Used by ASP.NET Core MVC model binding and any other component that discovers converters through
/// <see cref="TypeDescriptor"/>.
/// </summary>
public sealed class NonEmptyListTypeConverter : TypeConverter
{
    private readonly Type? _listType;
    private readonly MethodInfo? _tryParse;

    /// <summary>
    /// Creates a converter that is not bound to a specific list type. Conversion from string is not supported on this instance;
    /// <see cref="TypeDescriptor"/> always uses the <see cref="NonEmptyListTypeConverter(Type)"/> constructor.
    /// </summary>
    public NonEmptyListTypeConverter() { }

    /// <summary>
    /// Creates a converter for the specified closed <see cref="NonEmptyList{T}"/> type, or a type derived from it.
    /// A derived type such as <c>ConstrainedList&lt;T, TConstraint&gt;</c> inherits the <see cref="TypeConverterAttribute"/>,
    /// so <see cref="TypeDescriptor.GetConverter(Type)"/> must not throw for it. It converts from a string only if it
    /// declares its own <c>TryParse(string, IFormatProvider, out TSelf)</c>; otherwise <see cref="CanConvertFrom(ITypeDescriptorContext?, Type)"/>
    /// returns <see langword="false"/> for strings.
    /// </summary>
    /// <param name="type">The closed list type, for example <c>typeof(NonEmptyList&lt;int&gt;)</c>.</param>
    /// <exception cref="ArgumentException">Thrown if the type is not a <see cref="NonEmptyList{T}"/> or derived from one.</exception>
    public NonEmptyListTypeConverter(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!DerivesFromNonEmptyList(type))
            throw new ArgumentException($"{nameof(NonEmptyListTypeConverter)} only supports NonEmptyList<T> and types derived from it, not {type.Name}.", nameof(type));

        _listType = type;
        _tryParse = type.GetMethod(
            "TryParse",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            new[] { typeof(string), typeof(IFormatProvider), type.MakeByRefType() },
            modifiers: null);
    }

    private static bool DerivesFromNonEmptyList(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(NonEmptyList<>))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => (_tryParse is not null && sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text && _tryParse is not null)
        {
            object?[] args = { text, culture, null };
            bool ok = (bool)_tryParse.Invoke(null, args)!;

            return ok
                ? args[2]
                : throw new FormatException($"'{text}' is not a valid comma-separated, non-empty list for {_listType!.Name}.");
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is System.Collections.IEnumerable items)
        {
            List<string> parts = new();
            foreach (object? item in items)
                parts.Add(Convert.ToString(item, culture ?? CultureInfo.CurrentCulture) ?? string.Empty);

            return string.Join(",", parts);
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
