using System.Text.Json;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class NonEmptyListValueConverterTests
{
    [Fact]
    public void Serialize_ShouldConvertToJson()
    {
        var converter = new NonEmptyListValueConverter<int>();
        var list = new NonEmptyList<int>(1, 2, 3);

        // Access the ConvertToProvider expression
        var serialized = converter.ConvertToProviderExpression.Compile()(list);

        Assert.Equal("[1,2,3]", serialized);
    }

    [Fact]
    public void Deserialize_ShouldConvertFromJson()
    {
        var converter = new NonEmptyListValueConverter<int>();
        string json = "[1,2,3]";

        var result = converter.ConvertFromProviderExpression.Compile()(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result.Head);
        Assert.Equal(new[] { 1, 2, 3 }, result.ToArray());
    }

    [Fact]
    public void Deserialize_WithEmptyJson_ShouldThrow()
    {
        var converter = new NonEmptyListValueConverter<int>();

        Assert.Throws<InvalidOperationException>(() =>
            converter.ConvertFromProviderExpression.Compile()(""));
    }

    [Fact]
    public void Deserialize_WithEmptyArray_ShouldThrow()
    {
        var converter = new NonEmptyListValueConverter<int>();

        Assert.Throws<InvalidOperationException>(() =>
            converter.ConvertFromProviderExpression.Compile()("[]"));
    }

    [Fact]
    public void Serialize_WithStringList_ShouldHandleSpecialCharacters()
    {
        var converter = new NonEmptyListValueConverter<string>();
        var list = new NonEmptyList<string>("hello", "world", "with \"quotes\"");

        var serialized = converter.ConvertToProviderExpression.Compile()(list);

        Assert.Contains("hello", serialized);
        Assert.Contains("world", serialized);
        Assert.Contains("quotes", serialized);
    }

    [Fact]
    public void Roundtrip_ShouldPreserveData()
    {
        var converter = new NonEmptyListValueConverter<int>();
        var original = new NonEmptyList<int>(1, 2, 3, 4, 5);

        var serialized = converter.ConvertToProviderExpression.Compile()(original);
        var deserialized = converter.ConvertFromProviderExpression.Compile()(serialized);

        Assert.Equal(original.Count, deserialized.Count);
        Assert.Equal(original.ToArray(), deserialized.ToArray());
    }

    [Fact]
    public void WithCustomJsonOptions_ShouldApplyOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var converter = new NonEmptyListValueConverter<int>(options);
        var list = new NonEmptyList<int>(1, 2);

        var serialized = converter.ConvertToProviderExpression.Compile()(list);

        // Indented JSON should contain newlines
        Assert.Contains("\n", serialized);
    }

    [Fact]
    public void Serialize_WithComplexType_ShouldSerializeCorrectly()
    {
        var converter = new NonEmptyListValueConverter<Address>();
        var list = new NonEmptyList<Address>(
            new Address { Street = "123 Main St", City = "Springfield", ZipCode = "12345" });

        var serialized = converter.ConvertToProviderExpression.Compile()(list);

        Assert.Contains("123 Main St", serialized);
        Assert.Contains("Springfield", serialized);
        Assert.Contains("12345", serialized);
    }

    [Fact]
    public void Deserialize_WithComplexType_ShouldDeserializeCorrectly()
    {
        var converter = new NonEmptyListValueConverter<Address>();
        string json = "[{\"Street\":\"123 Main St\",\"City\":\"Springfield\",\"ZipCode\":\"12345\"}]";

        var result = converter.ConvertFromProviderExpression.Compile()(json);

        Assert.Single(result);
        Assert.Equal("123 Main St", result.Head.Street);
        Assert.Equal("Springfield", result.Head.City);
        Assert.Equal("12345", result.Head.ZipCode);
    }
}
