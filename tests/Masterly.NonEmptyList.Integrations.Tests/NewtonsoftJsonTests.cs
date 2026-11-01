using Masterly.NonEmptyList.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Masterly.NonEmptyList.Integrations.Tests;

public class NewtonsoftJsonTests
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings().AddNonEmptyConverters();

    private sealed record Dto(
        NonEmptyList<int> Numbers,
        ImmutableNonEmptyList<string> Names,
        NonEmptySet<string> Tags,
        NonEmptyDictionary<string, int> Limits,
        ConstrainedList<int, AtLeast<Two>> Pair,
        NonEmptyString Title,
        NonEmptyList<int>? Optional);

    [Fact]
    public void RoundTrip_ShouldPreserveEveryType()
    {
        Dto dto = new(
            NonEmptyList.Of(1, 2),
            ImmutableNonEmptyList.Of("a", "b"),
            NonEmptySet.Of("x", "y"),
            NonEmptyDictionary.OfPairs(("gold", 10), ("silver", 5)),
            ConstrainedList<int, AtLeast<Two>>.From(new[] { 7, 8 }),
            new NonEmptyString("title"),
            null);

        string json = JsonConvert.SerializeObject(dto, Settings);
        Dto back = JsonConvert.DeserializeObject<Dto>(json, Settings)!;

        Assert.Contains("\"Numbers\":[1,2]", json);
        Assert.Contains("\"Limits\":{\"gold\":10,\"silver\":5}", json);
        Assert.Contains("\"Title\":\"title\"", json);
        Assert.Contains("\"Optional\":null", json);
        Assert.Equal(dto.Numbers, back.Numbers);
        Assert.Equal(dto.Names, back.Names);
        Assert.Equal(dto.Tags, back.Tags);
        Assert.Equal(dto.Limits, back.Limits);
        Assert.Equal(dto.Pair, back.Pair);
        Assert.Equal(dto.Title, back.Title);
        Assert.Null(back.Optional);
    }

    [Fact]
    public void EmptyAndInvalidInput_ShouldThrow()
    {
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptyList<int>>("[]", Settings));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptySet<int>>("[]", Settings));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptyDictionary<string, int>>("{}", Settings));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<ConstrainedList<int, AtLeast<Two>>>("[1]", Settings));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptyString>("\"  \"", Settings));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptyString>("null", Settings));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptyString>("5", Settings));
        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptyList<string>>("[\"a\", null]", Settings));
        Assert.Null(JsonConvert.DeserializeObject<NonEmptyList<int>>("null", Settings));
    }

    [Fact]
    public void Helpers_ShouldSerializeAndDeserialize()
    {
        NonEmptyList<int> list = NonEmptyList.Of(3);

        string json = NonEmptyJsonConverterExtensions.SerializeNonEmpty(list);
        NonEmptyList<int>? back = NonEmptyJsonConverterExtensions.DeserializeNonEmpty<NonEmptyList<int>>(json);
        NonEmptyList<int>? fromToken = JArray.Parse("[4,5]").ToNonEmpty<NonEmptyList<int>>();

        Assert.Equal("[3]", json);
        Assert.Equal(list, back);
        Assert.Equal(new[] { 4, 5 }, fromToken!.ToArray());

        JsonSerializer serializer = new JsonSerializer().AddNonEmptyConverters().AddNonEmptyConverters();
        Assert.Single(serializer.Converters);
        JsonSerializerSettings settings = new JsonSerializerSettings().AddNonEmptyConverters().AddNonEmptyConverters();
        Assert.Single(settings.Converters);
    }

    [Fact]
    public void CanConvert_ShouldOnlyAcceptNonEmptyTypes()
    {
        NonEmptyJsonConverter converter = new();

        Assert.True(converter.CanConvert(typeof(NonEmptyList<int>)));
        Assert.True(converter.CanConvert(typeof(NonEmptyString)));
        Assert.True(converter.CanConvert(typeof(NonEmptyString?)));
        Assert.True(converter.CanConvert(typeof(NonEmptyDictionary<string, string>)));
        Assert.False(converter.CanConvert(typeof(List<int>)));
        Assert.False(converter.CanConvert(typeof(string)));
    }

    [Fact]
    public void WithoutConverter_EmptyArray_IsStillRejected_ButWithARawArgumentException()
    {
        // Json.NET finds the IEnumerable<T> constructor on its own, so the guarantee holds even without the converter.
        // The converter turns the ArgumentException into a JsonSerializationException with path information and adds
        // support for the types Json.NET cannot construct (NonEmptyString, NonEmptyDictionary, derived collections).
        Assert.ThrowsAny<Exception>(() => JsonConvert.DeserializeObject<NonEmptyList<int>>("[]"));
        Assert.Equal(2, JsonConvert.DeserializeObject<NonEmptyList<int>>("[1,2]")!.Count);
    }
}
