using System.ComponentModel;
using System.Globalization;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// ISpanParsable / TypeConverter support, which is what lets ASP.NET Core bind a NonEmptyList from a query string.
/// </summary>
public partial class NonEmptyListTests
{
    private enum Color { Red, Green, Blue }

    private static T ParseViaInterface<T>(string text) where T : IParsable<T> => T.Parse(text, CultureInfo.InvariantCulture);

    [Fact]
    public void Parse_ShouldSplitOnCommasAndTrim()
    {
        NonEmptyList<int> list = NonEmptyList<int>.Parse("1, 2,3 ");

        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());
    }

    [Fact]
    public void TryParse_ShouldRejectEmptyBlankAndMalformedInput()
    {
        Assert.False(NonEmptyList<int>.TryParse("", out _));
        Assert.False(NonEmptyList<int>.TryParse("   ", out _));
        Assert.False(NonEmptyList<int>.TryParse((string?)null, null, out _));
        Assert.False(NonEmptyList<int>.TryParse("1,,2", out _));
        Assert.False(NonEmptyList<int>.TryParse("1,x", out _));
        Assert.Throws<FormatException>(() => NonEmptyList<int>.Parse("nope"));
    }

    [Fact]
    public void Parse_ShouldSupportGuidsStringsAndEnums()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();

        NonEmptyList<Guid> guids = NonEmptyList<Guid>.Parse($"{a},{b}");
        NonEmptyList<string> strings = NonEmptyList<string>.Parse("alpha, beta");
        NonEmptyList<Color> colors = NonEmptyList<Color>.Parse("red,Blue");

        Assert.Equal(new[] { a, b }, guids.ToArray());
        Assert.Equal(new[] { "alpha", "beta" }, strings.ToArray());
        Assert.Equal(new[] { Color.Red, Color.Blue }, colors.ToArray());
        Assert.False(NonEmptyList<Color>.TryParse("purple", out _));
    }

    [Fact]
    public void Parse_ShouldHonorFormatProvider()
    {
        NonEmptyList<double> values = NonEmptyList<double>.Parse("1.5,2.5", CultureInfo.InvariantCulture);

        Assert.Equal(new[] { 1.5, 2.5 }, values.ToArray());
    }

    [Fact]
    public void NonEmptyList_ShouldSatisfyIParsableConstraint()
    {
        NonEmptyList<int> list = ParseViaInterface<NonEmptyList<int>>("7,8");

        Assert.Equal(new[] { 7, 8 }, list.ToArray());
    }

    [Fact]
    public void TypeConverter_ShouldRoundTripThroughTypeDescriptor()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(NonEmptyList<int>));

        Assert.IsType<NonEmptyListTypeConverter>(converter);
        Assert.True(converter.CanConvertFrom(typeof(string)));

        object? converted = converter.ConvertFromInvariantString("4,5,6");
        string? text = converter.ConvertToInvariantString(new NonEmptyList<int>(1, 2));

        Assert.Equal(new[] { 4, 5, 6 }, Assert.IsType<NonEmptyList<int>>(converted).ToArray());
        Assert.Equal("1,2", text);
        Assert.Throws<FormatException>(() => converter.ConvertFromInvariantString(""));
    }
}
