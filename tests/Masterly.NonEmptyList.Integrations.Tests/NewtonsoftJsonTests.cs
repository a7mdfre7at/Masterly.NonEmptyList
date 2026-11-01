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

    private sealed record Wrapper(NonEmptyList<int> Ids, NonEmptyString Name);

    [Fact]
    public void Errors_ShouldCarryTheJsonPath()
    {
        JsonSerializationException empty = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<Wrapper>("""{"Ids": [], "Name": "a"}""", Settings));
        JsonSerializationException blank = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<Wrapper>("""{"Ids": [1], "Name": " "}""", Settings));

        Assert.Equal("Ids", empty.Path);
        Assert.Contains("NonEmptyList cannot be empty", empty.Message);
        Assert.Equal(
            "NonEmptyList cannot be empty. Path '', line 1, position 2.",
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<NonEmptyList<int>>("[]", Settings)).Message);
        Assert.Contains("Path 'Ids'", empty.Message);
        Assert.Equal(1, empty.LineNumber);
        Assert.Equal("Name", blank.Path);
        Assert.Contains("NonEmptyString cannot be empty or whitespace", blank.Message);
    }

    [Fact]
    public void Errors_FromATokenWithoutLineInfo_ShouldCarryThePathOnly()
    {
        JArray built = new();

        JsonSerializationException ex = Assert.Throws<JsonSerializationException>(() => built.ToNonEmpty<NonEmptyList<int>>());

        Assert.Equal("NonEmptyList cannot be empty. Path ''.", ex.Message);
        Assert.Equal(0, ex.LineNumber);
        Assert.Throws<ArgumentNullException>(() => ((JToken)null!).ToNonEmpty<NonEmptyList<int>>());
    }

    [Fact]
    public void DerivedCollections_ShouldRoundTripThroughTheirOwnFactory()
    {
        Scores scores = Scores.From(new[] { 2, 1 });

        string json = JsonConvert.SerializeObject(scores, Settings);
        Scores back = JsonConvert.DeserializeObject<Scores>(json, Settings)!;
        JsonSerializationException duplicate = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Scores>("[1,1]", Settings));

        Assert.Equal("[2,1]", json);
        Assert.Equal(scores, back);
        Assert.StartsWith("Scores: Scores must be distinct. Path ''", duplicate.Message);
        Assert.IsType<ArgumentException>(duplicate.InnerException);
    }

    [Fact]
    public void DerivedCollectionFactory_FailingForOtherReasons_ShouldPropagateUnwrapped()
    {
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonConvert.DeserializeObject<Unlucky>("[13]", Settings));

        Assert.Equal("13 is unlucky", ex.Message);
        Assert.Equal(new[] { 1, 2 }, JsonConvert.DeserializeObject<Unlucky>("[1,2]", Settings)!.ToArray());
    }

    [Fact]
    public void DerivedCollectionWithoutFactory_ShouldExplainWhatIsMissing()
    {
        JsonSerializationException ex = Assert.Throws<JsonSerializationException>(() => new NonEmptyJsonConverter().CanConvert(typeof(NoFactoryList)));

        Assert.Contains("does not expose a static From", ex.Message);
    }

    [Fact]
    public void ReadJson_CalledDirectlyForAnotherType_ShouldFail()
    {
        NonEmptyJsonConverter converter = new();
        using JsonTextReader reader = new(new StringReader("[1]"));
        reader.Read();

        JsonSerializationException ex = Assert.Throws<JsonSerializationException>(
            () => converter.ReadJson(reader, typeof(List<int>), null, JsonSerializer.CreateDefault()));

        Assert.Contains("is not a non-empty type", ex.Message);
    }

    [Fact]
    public void WriteJson_CalledDirectly_ShouldHandleNullAndPlainDictionariesAndRejectOtherValues()
    {
        NonEmptyJsonConverter converter = new();
        JsonSerializer serializer = JsonSerializer.CreateDefault();

        Assert.Equal("null", Write(null));
        Assert.Equal("{\"a\":1}", Write(new Dictionary<string, int> { ["a"] = 1 }));
        Assert.Throws<JsonSerializationException>(() => Write(42));

        string Write(object? value)
        {
            using StringWriter text = new();
            using JsonTextWriter writer = new(text);
            converter.WriteJson(writer, value, serializer);
            writer.Flush();
            return text.ToString();
        }
    }

    [Fact]
    public void Helpers_ShouldRejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ((JsonSerializerSettings)null!).AddNonEmptyConverters());
        Assert.Throws<ArgumentNullException>(() => ((JsonSerializer)null!).AddNonEmptyConverters());
        Assert.Equal("null", NonEmptyJsonConverterExtensions.SerializeNonEmpty(null, new JsonSerializerSettings()));
        Assert.Equal(NonEmptyList.Of(1), NonEmptyJsonConverterExtensions.DeserializeNonEmpty<NonEmptyList<int>>("[1]", new JsonSerializerSettings()));
    }
}
