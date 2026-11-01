using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

public class NonEmptyStringTests
{
    [Fact]
    public void Constructor_ShouldRejectBlank()
    {
        NonEmptyString s = new("hello");

        Assert.Equal("hello", s.Value);
        Assert.True(s.HasValue);
        Assert.Equal(5, s.Length);
        Assert.Equal('h', s.Head);
        Assert.Equal('o', s.Last);
        Assert.Equal('e', s[1]);
        Assert.Throws<ArgumentException>(() => new NonEmptyString(null!));
        Assert.Throws<ArgumentException>(() => new NonEmptyString(""));
        Assert.Throws<ArgumentException>(() => new NonEmptyString("   "));
        Assert.Throws<ArgumentException>(() => (NonEmptyString)"");
        Assert.Throws<ArgumentException>(() => "".ToNonEmptyString());
    }

    [Fact]
    public void Default_ShouldReportNoValueAndThrowOnAccess()
    {
        NonEmptyString none = default;

        Assert.False(none.HasValue);
        Assert.Throws<InvalidOperationException>(() => none.Value);
        Assert.Equal(string.Empty, none.ToString());
        Assert.Equal(0, none.GetHashCode());
        Assert.NotEqual(none, new NonEmptyString("a"));
    }

    [Fact]
    public void TryCreate_FromNullable_ShouldHandleBlank()
    {
        Assert.True(NonEmptyString.TryCreate("x", out NonEmptyString ok));
        Assert.Equal("x", ok.Value);
        Assert.False(NonEmptyString.TryCreate(" ", out NonEmptyString blank));
        Assert.False(blank.HasValue);
        Assert.Null(NonEmptyString.FromNullable(null));
        Assert.Equal("y", NonEmptyString.FromNullable("y")!.Value.Value);
        Assert.Null("".ToNonEmptyStringOrNull());
        Assert.Equal("z", "z".ToNonEmptyStringOrNull()!.Value.Value);
    }

    [Fact]
    public void Parsing_ShouldImplementISpanParsable()
    {
        Assert.Equal("abc", NonEmptyString.Parse("abc", null).Value);
        Assert.Equal("abc", NonEmptyString.Parse("abc".AsSpan(), null).Value);
        Assert.Throws<ArgumentException>(() => NonEmptyString.Parse("", null));
        Assert.Throws<FormatException>(() => NonEmptyString.Parse(" ".AsSpan(), null));
        Assert.True(NonEmptyString.TryParse("q", null, out NonEmptyString a));
        Assert.Equal("q", a.Value);
        Assert.False(NonEmptyString.TryParse((string?)null, null, out _));
        Assert.True(NonEmptyString.TryParse("q".AsSpan(), null, out NonEmptyString b));
        Assert.Equal("q", b.Value);
        Assert.False(NonEmptyString.TryParse("  ".AsSpan(), null, out _));
        Assert.Equal("v", Generic<NonEmptyString>("v").Value);
    }

    private static T Generic<T>(string s) where T : ISpanParsable<T> => T.Parse(s, CultureInfo.InvariantCulture);

    [Fact]
    public void Transformations_ShouldStayNonEmpty()
    {
        NonEmptyString s = new("  Hello World  ");

        Assert.Equal("Hello World", s.Trim().Value);
        Assert.Equal("  HELLO WORLD  ", s.ToUpperInvariant().Value);
        Assert.Equal("  hello world  ", s.ToLowerInvariant().Value);
        Assert.Equal("  Hello World  !", s.Concat("!").Value);
        Assert.Equal(s, s.Concat(null));
        Assert.Equal(s, s.Concat(""));
        Assert.Equal("ab", (new NonEmptyString("a") + new NonEmptyString("b")).Value);
        Assert.Equal("ab", (new NonEmptyString("a") + "b").Value);
        Assert.Equal("Hel", new NonEmptyString("Hello").Truncate(3).Value);
        Assert.Equal("Hello", new NonEmptyString("Hello").Truncate(10).Value);
        Assert.Equal("H", new NonEmptyString("  Hello").Truncate(2).Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => new NonEmptyString("a").Truncate(0));
    }

    [Fact]
    public void Split_Join_CharList_ShouldBeNonEmpty()
    {
        NonEmptyString csv = new("a, b,,c ");

        NonEmptyList<NonEmptyString> parts = csv.Split(',');

        Assert.Equal(3, parts.Count);
        Assert.Equal("b", parts[1].Value);
        Assert.Equal("a|b|c", parts.Join("|").Value);
        Assert.Single(new NonEmptyString("x").Split(','));
        Assert.Equal(new[] { 'a', 'b' }, new NonEmptyString("ab").ToCharList().ToArray());
        Assert.Equal(2, new NonEmptyString("ab").AsSpan().Length);
        ReadOnlySpan<char> span = new NonEmptyString("ab");
        Assert.Equal(2, span.Length);
    }

    [Fact]
    public void Conversions_Equality_Comparison_ShouldWork()
    {
        NonEmptyString a = new("apple");
        NonEmptyString b = (NonEmptyString)"apple";
        string plain = a;

        Assert.Equal("apple", plain);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals("apple"));
        Assert.False(a.Equals(5));
        Assert.True(a.Equals(new NonEmptyString("APPLE"), StringComparison.OrdinalIgnoreCase));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.True(a.CompareTo(new NonEmptyString("banana")) < 0);
        Assert.True(((IComparable)a).CompareTo(b) == 0);
        Assert.Throws<ArgumentException>(() => ((IComparable)a).CompareTo("x"));
        Assert.Equal("apple", a.ToString(null, null));
        Assert.Equal("apple", $"{a}");

        Span<char> buffer = stackalloc char[10];
        Assert.True(a.TryFormat(buffer, out int written, default, null));
        Assert.Equal(5, written);
        Assert.False(a.TryFormat(buffer[..2], out written, default, null));
        Assert.Equal(0, written);
    }

    [Fact]
    public void Json_ShouldRoundTripIncludingAsPropertyNameAndRejectBlank()
    {
        var dto = new Dto(new NonEmptyString("name"), new Dictionary<NonEmptyString, int> { [new NonEmptyString("k")] = 1 });

        string json = JsonSerializer.Serialize(dto);
        Dto back = JsonSerializer.Deserialize<Dto>(json)!;

        Assert.Equal("{\"Name\":\"name\",\"Map\":{\"k\":1}}", json);
        Assert.Equal(dto.Name, back.Name);
        Assert.Equal(1, back.Map[new NonEmptyString("k")]);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptyString>("\"\""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptyString>("\"  \""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptyString>("5"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<NonEmptyString, int>>("{\"\":1}"));
    }

    [Fact]
    public void TypeConverter_ShouldConvertFromAndToString()
    {
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(NonEmptyString));

        Assert.IsType<NonEmptyStringTypeConverter>(converter);
        Assert.True(converter.CanConvertFrom(typeof(string)));
        Assert.True(converter.CanConvertTo(typeof(string)));
        Assert.Equal(new NonEmptyString("x"), converter.ConvertFromString("x"));
        Assert.Equal("x", converter.ConvertToString(new NonEmptyString("x")));
        Assert.Throws<FormatException>(() => converter.ConvertFromString(""));
        Assert.ThrowsAny<NotSupportedException>(() => converter.ConvertFrom(5));
        Assert.ThrowsAny<NotSupportedException>(() => converter.ConvertTo(new NonEmptyString("x"), typeof(int)));
    }

    private sealed record Dto(NonEmptyString Name, Dictionary<NonEmptyString, int> Map);
}
