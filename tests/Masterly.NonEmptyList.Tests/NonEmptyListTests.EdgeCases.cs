using System.Text;
using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// JSON, async and factory edge cases not reached by the feature-oriented suites.
/// </summary>
public partial class NonEmptyListTests
{
    #region JSON

    [Fact]
    public void JsonFactory_ShouldOnlyAcceptNonEmptyList()
    {
        NonEmptyListJsonConverterFactory factory = new();

        Assert.True(factory.CanConvert(typeof(NonEmptyList<int>)));
        Assert.False(factory.CanConvert(typeof(int)));
        Assert.False(factory.CanConvert(typeof(List<int>)));
    }

    [Theory]
    [InlineData("5", "Expected start of array")]
    [InlineData("[\"a\", null]", "NonEmptyList cannot contain null elements")]
    public void Json_InvalidPayloads_ShouldThrowWithDescriptiveMessage(string json, string message)
    {
        JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptyList<string>>(json));

        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void JsonConverter_ReadingNullToken_ShouldReturnNull()
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes("null"));
        reader.Read();

        NonEmptyList<int>? result = new NonEmptyListJsonConverter<int>()
            .Read(ref reader, typeof(NonEmptyList<int>), JsonSerializerOptions.Default);

        Assert.Null(result);
    }

    #endregion

    #region Async

    private static async IAsyncEnumerable<int> AsyncRange(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    [Fact]
    public async Task ToNonEmptyListAsync_FromAsyncEnumerable_ShouldBufferOrReturnNullWhenEmpty()
    {
        NonEmptyList<int>? list = await AsyncRange(3).ToNonEmptyListAsync();
        NonEmptyList<int>? empty = await AsyncRange(0).ToNonEmptyListAsync();

        Assert.Equal(new[] { 1, 2, 3 }, list);
        Assert.Null(empty);
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IAsyncEnumerable<int>)null!).ToNonEmptyListAsync());
    }

    [Fact]
    public async Task MapParallelAsync_WithDegreeBelowOne_ShouldThrow()
    {
        NonEmptyList<int> list = new(1);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => list.MapParallelAsync(x => Task.FromResult(x), 0));
    }

    #endregion

    #region Factory

    [Fact]
    public void Factory_SingletonAndRepeat_ShouldCreateLists()
    {
        Assert.Equal(new[] { 5 }, NonEmptyList.Singleton(5));
        Assert.Equal(new[] { "x", "x", "x" }, NonEmptyList.Repeat("x", 3));
        Assert.Equal(new[] { "x" }, NonEmptyList.Repeat("x", 1));
        Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() => NonEmptyList.Repeat("x", 0)).ParamName);
    }

    [Fact]
    public void NonEmptyCollectionAttribute_ShouldExposeDefaultsAndSettings()
    {
        NonEmptyCollectionAttribute attribute = new(typeof(string));

        Assert.Equal(typeof(string), attribute.ElementType);
        Assert.Equal(1, attribute.MinCount);
        Assert.Equal(0, attribute.MaxCount);
        Assert.False(attribute.Distinct);
        Assert.False(attribute.Immutable);

        attribute.MinCount = 2;
        attribute.MaxCount = 5;
        attribute.Distinct = true;
        attribute.Immutable = true;
        Assert.Equal((2, 5, true, true), (attribute.MinCount, attribute.MaxCount, attribute.Distinct, attribute.Immutable));

        Assert.Throws<ArgumentNullException>(() => new NonEmptyCollectionAttribute(null!));
    }

    #endregion
}
