using System.ComponentModel;
using System.Globalization;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// The element-parser fallbacks (IParsable only, TypeConverter, nothing) and the edges of <see cref="NonEmptyListTypeConverter"/>.
/// </summary>
public partial class NonEmptyListTests
{
    /// <summary>Implements <see cref="IParsable{TSelf}"/> but not <see cref="ISpanParsable{TSelf}"/>.</summary>
    private sealed record ParsableOnly(int Value) : IParsable<ParsableOnly>
    {
        public static ParsableOnly Parse(string s, IFormatProvider? provider) => new(int.Parse(s, provider));

        public static bool TryParse(string? s, IFormatProvider? provider, out ParsableOnly result)
        {
            bool ok = int.TryParse(s, NumberStyles.Integer, provider, out int value);
            result = new ParsableOnly(value);
            return ok;
        }
    }

    /// <summary>Only parseable through its <see cref="TypeConverter"/>.</summary>
    [TypeConverter(typeof(CelsiusConverter))]
    private sealed record Celsius(int Degrees);

    private sealed class CelsiusConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            string text = (string)value;
            if (text == "none")
                return null;
            if (text == "huge")
                throw new OverflowException();
            if (text == "bad-arg")
                throw new ArgumentException("bad");
            if (text == "unsupported")
                throw new NotSupportedException();
            if (text == "boom")
                throw new InvalidOperationException();
            if (!text.EndsWith('C'))
                throw new FormatException("expected a C suffix");
            // AllowThousands makes the result depend on the culture's group separator.
            return new Celsius(int.Parse(text[..^1], NumberStyles.Integer | NumberStyles.AllowThousands, culture));
        }
    }

    /// <summary>
    /// Has no parser at all: its converter reports that it cannot convert from string, even though
    /// <see cref="TypeConverter.ConvertFromString(ITypeDescriptorContext?, CultureInfo?, string)"/> would succeed.
    /// This pins that the element parser honors <see cref="TypeConverter.CanConvertFrom(Type)"/>.
    /// </summary>
    [TypeConverter(typeof(UnparsableConverter))]
    private sealed record Unparsable(int Value);

    private sealed class UnparsableConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => false;

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            => new Unparsable(int.Parse((string)value, culture));
    }

    [Fact]
    public void TryParse_ElementOnlyImplementingIParsable_ShouldUseIt()
    {
        Assert.True(NonEmptyList<ParsableOnly>.TryParse("1, 2", out NonEmptyList<ParsableOnly>? list));
        Assert.Equal(new[] { new ParsableOnly(1), new ParsableOnly(2) }, list);

        Assert.False(NonEmptyList<ParsableOnly>.TryParse("1, x", out _));
    }

    [Fact]
    public void TryParse_ElementWithTypeConverter_ShouldUseTheConverter()
    {
        Assert.True(NonEmptyList<Celsius>.TryParse("20C, -5C", out NonEmptyList<Celsius>? list));
        Assert.Equal(new[] { new Celsius(20), new Celsius(-5) }, list);
    }

    [Fact]
    public void TryParse_ElementWithTypeConverter_ShouldHonorTheProviderCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // A CultureInfo provider wins over the current culture: "." is a group separator in de-DE only.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.True(NonEmptyList<Celsius>.TryParse("1.000C".AsSpan(), new CultureInfo("de-DE"), out NonEmptyList<Celsius>? list));
            Assert.Equal(new Celsius(1000), list!.Head);

            // A provider that is not a CultureInfo falls back to the current culture, not to the invariant one.
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.True(NonEmptyList<Celsius>.TryParse("1.000C".AsSpan(), NumberFormatInfo.InvariantInfo, out NonEmptyList<Celsius>? fallback));
            Assert.Equal(new Celsius(1000), fallback!.Head);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("20C, 20F")]   // FormatException
    [InlineData("20C, huge")]  // OverflowException
    [InlineData("20C, bad-arg")]      // ArgumentException
    [InlineData("20C, unsupported")]  // NotSupportedException
    [InlineData("none")]       // converter returns null, which is not a Celsius
    public void TryParse_ElementWithTypeConverter_ShouldReportFailureInsteadOfThrowing(string text)
    {
        Assert.False(NonEmptyList<Celsius>.TryParse(text, out NonEmptyList<Celsius>? list));
        Assert.Null(list);
    }

    [Fact]
    public void TryParse_ElementWithTypeConverter_ShouldNotSwallowUnrelatedExceptions()
    {
        Assert.Throws<InvalidOperationException>(() => NonEmptyList<Celsius>.TryParse("boom", out _));
    }

    [Fact]
    public void TryParse_ElementWithoutAnyParser_ShouldReturnFalse()
    {
        Assert.False(NonEmptyList<Unparsable>.TryParse("1,2", out NonEmptyList<Unparsable>? list));
        Assert.Null(list);
    }

    [Fact]
    public void TypeConverter_Constructor_ShouldRejectTypesOtherThanNonEmptyList()
    {
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() => new NonEmptyListTypeConverter(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() => new NonEmptyListTypeConverter(typeof(int))).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() => new NonEmptyListTypeConverter(typeof(List<int>))).ParamName);
    }

    [Fact]
    public void TypeConverter_Unbound_ShouldNotConvertFromString()
    {
        NonEmptyListTypeConverter converter = new();

        Assert.False(converter.CanConvertFrom(typeof(string)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, "1,2"));
    }

    [Fact]
    public void TypeConverter_ShouldDeferNonStringConversionsToTheBaseConverter()
    {
        NonEmptyListTypeConverter converter = new(typeof(NonEmptyList<int>));

        Assert.False(converter.CanConvertFrom(typeof(int)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, 42));

        Assert.True(converter.CanConvertTo(typeof(string)));
        Assert.False(converter.CanConvertTo(typeof(int)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, new NonEmptyList<int>(1), typeof(int)));
    }

    [Fact]
    public void TypeConverter_ConvertTo_WithoutCulture_ShouldUseCurrentCulture()
    {
        NonEmptyListTypeConverter converter = new(typeof(NonEmptyList<double>));
        NonEmptyList<double> list = new(1.5, 2.5);
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1,5,2,5", converter.ConvertTo(null, null, list, typeof(string)));
            Assert.Equal("1.5,2.5", converter.ConvertTo(null, CultureInfo.InvariantCulture, list, typeof(string)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private sealed class Faceless
    {
        public override string? ToString() => null;
    }

    [Fact]
    public void TypeConverter_ConvertTo_ElementWithNullToString_ShouldWriteAnEmptyPart()
    {
        NonEmptyListTypeConverter converter = new(typeof(NonEmptyList<Faceless>));

        object? text = converter.ConvertTo(null, CultureInfo.InvariantCulture, new NonEmptyList<Faceless>(new Faceless(), new Faceless()), typeof(string));

        Assert.Equal(",", text);
    }

    [Fact]
    public void TypeConverter_ConvertFrom_InvalidText_ShouldThrowFormatException()
    {
        NonEmptyListTypeConverter converter = new(typeof(NonEmptyList<int>));

        FormatException ex = Assert.Throws<FormatException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, "1,,2"));
        Assert.Contains("'1,,2'", ex.Message);
    }

    [Fact]
    public void TypeConverter_ForADerivedList_ShouldNotThrow_AndShouldDeclineStrings()
    {
        // ConstrainedList inherits [TypeConverter] from NonEmptyList<T>; TypeDescriptor must not throw for it.
        System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(ConstrainedList<int, Distinct>));

        Assert.IsType<NonEmptyListTypeConverter>(converter);
        Assert.False(converter.CanConvertFrom(typeof(string)));
        Assert.True(System.ComponentModel.TypeDescriptor.GetConverter(typeof(NonEmptyList<int>)).CanConvertFrom(typeof(string)));
    }
}
