using System.Text.Json;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class ImmutableNonEmptyListValueConverterTests
{
    [Fact]
    public void Serialize_ShouldConvertToJson()
    {
        var converter = new ImmutableNonEmptyListValueConverter<int>();
        var list = new ImmutableNonEmptyList<int>(1, 2, 3);

        var serialized = converter.ConvertToProviderExpression.Compile()(list);

        Assert.Equal("[1,2,3]", serialized);
    }

    [Fact]
    public void Deserialize_ShouldConvertFromJson()
    {
        var converter = new ImmutableNonEmptyListValueConverter<int>();
        string json = "[1,2,3]";

        var result = converter.ConvertFromProviderExpression.Compile()(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result.Head);
    }

    [Fact]
    public void Deserialize_WithEmptyJson_ShouldThrow()
    {
        var converter = new ImmutableNonEmptyListValueConverter<int>();

        Assert.Throws<InvalidOperationException>(() =>
            converter.ConvertFromProviderExpression.Compile()(""));
    }

    [Fact]
    public void Deserialize_WithEmptyArray_ShouldThrow()
    {
        var converter = new ImmutableNonEmptyListValueConverter<int>();

        Assert.Throws<InvalidOperationException>(() =>
            converter.ConvertFromProviderExpression.Compile()("[]"));
    }

    [Fact]
    public void Roundtrip_ShouldPreserveData()
    {
        var converter = new ImmutableNonEmptyListValueConverter<string>();
        var original = new ImmutableNonEmptyList<string>("a", "b", "c");

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
        var converter = new ImmutableNonEmptyListValueConverter<int>(options);
        var list = new ImmutableNonEmptyList<int>(1, 2);

        var serialized = converter.ConvertToProviderExpression.Compile()(list);

        Assert.Contains("\n", serialized);
    }
}
