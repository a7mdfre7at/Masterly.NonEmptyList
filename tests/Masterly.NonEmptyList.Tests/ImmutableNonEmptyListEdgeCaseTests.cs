using System.Text;
using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

public class ImmutableNonEmptyListEdgeCaseTests
{
    #region Construction

    [Fact]
    public void Constructor_WithNullOtherItems_ShouldCreateSingleElementList()
    {
        ImmutableNonEmptyList<int> list = new(1, (IEnumerable<int>)null!);

        Assert.Equal(new[] { 1 }, list);
    }

    [Fact]
    public void Constructor_WithLazyOtherItems_ShouldMaterializeThem()
    {
        ImmutableNonEmptyList<int> list = new(1, Enumerable.Range(2, 2).Where(_ => true));

        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void From_WithNull_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ImmutableNonEmptyList<int>.From(null!));
    }

    [Fact]
    public void TryFrom_ShouldRejectNullAndNullElementsAndAcceptLazySequences()
    {
        Assert.False(ImmutableNonEmptyList<string>.TryFrom(null, out _));
        Assert.False(ImmutableNonEmptyList<string>.TryFrom(new[] { "a", null! }, out ImmutableNonEmptyList<string>? withNull));
        Assert.Null(withNull);

        Assert.True(ImmutableNonEmptyList<int>.TryFrom(Enumerable.Range(1, 2).Where(_ => true), out ImmutableNonEmptyList<int>? lazy));
        Assert.Equal(new[] { 1, 2 }, lazy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Repeat_WithCountBelowOne_ShouldThrow(int count)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableNonEmptyList<int>.Repeat(1, count));

        Assert.Equal("count", ex.ParamName);
        // Enumerable.Repeat throws the same type and ParamName, so only the message proves the guard ran.
        Assert.StartsWith("Count must be at least 1", ex.Message);
    }

    [Fact]
    public void Create_FromSpanContainingNull_ShouldThrow()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => ImmutableNonEmptyList.Create<string>(new string[] { "a", null! }));
        Assert.StartsWith("ImmutableNonEmptyList cannot contain null values", ex.Message);

        Assert.Throws<ArgumentNullException>(() =>
        {
            ImmutableNonEmptyList<string?> list = ["a", null];
            return list;
        });
    }

    [Fact]
    public void StaticFactory_FromAndSingleton_ShouldCreateLists()
    {
        Assert.Equal(new[] { 1, 2 }, ImmutableNonEmptyList.From(new[] { 1, 2 }));
        Assert.Equal(new[] { 9 }, ImmutableNonEmptyList.Singleton(9));
    }

    #endregion

    #region Operations

    [Fact]
    public void First_ShouldBeTheHead()
    {
        Assert.Equal(4, ImmutableNonEmptyList.Of(4, 5).First);
    }

    [Fact]
    public void Init_ShouldBeCachedAcrossReads()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList.Of(1, 2, 3);

        ImmutableNonEmptyList<int>? first = list.Init;
        ImmutableNonEmptyList<int>? second = list.Init;

        Assert.Equal(new[] { 1, 2 }, first);
        Assert.Same(first, second);
    }

    [Fact]
    public void Tail_ShouldBeCachedAcrossReads()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList.Of(1, 2, 3);

        Assert.Same(list.Tail, list.Tail);
        Assert.Equal(new[] { 2, 3 }, list.Tail);
    }

    [Fact]
    public void Match_OnSeveralElements_ShouldCallMultipleWithTheTail()
    {
        string result = ImmutableNonEmptyList.Of(1, 2, 3).Match(h => "single", (h, t) => $"{h}|{string.Join(",", t)}");

        Assert.Equal("1|2,3", result);
    }

    [Fact]
    public void Equals_SameInstance_ShouldBeTrue()
    {
        // NeverEqual is not equal even to itself, so only the reference short-circuit can return true.
        ImmutableNonEmptyList<NeverEqual> list = ImmutableNonEmptyList.Of(new NeverEqual());

        Assert.True(list.Equals(list));
        Assert.False(list.Equals(ImmutableNonEmptyList.Of(new NeverEqual())));
    }

    private sealed class NeverEqual
    {
        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => 0;
    }

    [Fact]
    public void AppendRange_ShouldHandleEmptyLazyAndNullContainingInput()
    {
        ImmutableNonEmptyList<string> list = ImmutableNonEmptyList.Of("a");

        Assert.Same(list, list.AppendRange(Array.Empty<string>()));
        Assert.Equal(new[] { "a", "b", "c" }, list.AppendRange(new[] { "b", "c" }.Select(s => s)));
        Assert.Throws<ArgumentNullException>(() => list.AppendRange(new[] { "b", null! }));
    }

    [Fact]
    public void RemoveAt_ShouldValidateIndexAndReturnNullForLastElement()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList.Of(1, 2);

        // ParamName pins the guard: without it, RemoveAt(-1) reaches Array.Copy, which throws with "length".
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1)).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(2)).ParamName);
        Assert.Null(ImmutableNonEmptyList.Of(1).RemoveAt(0));
    }

    [Fact]
    public void SetItemAndInsert_ShouldValidateIndex()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList.Of(1, 2);

        // ParamName pins the guard: without it, Insert(-1) reaches Array.Copy, which throws with "length".
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => list.SetItem(-1, 0)).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => list.SetItem(2, 0)).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, 0)).ParamName);
        Assert.Equal("index", Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(3, 0)).ParamName);
        Assert.Equal(new[] { 1, 2, 3 }, list.Insert(2, 3));
    }

    [Fact]
    public void MapWithIndex_ShouldPassElementAndIndex()
    {
        ImmutableNonEmptyList<string> mapped = ImmutableNonEmptyList.Of("a", "b").MapWithIndex((s, i) => $"{i}:{s}");

        Assert.Equal(new[] { "0:a", "1:b" }, mapped);
        Assert.Throws<ArgumentNullException>(() => ImmutableNonEmptyList.Of(1).MapWithIndex<int>(null!));
    }

    [Fact]
    public void Match_OnSingleElement_ShouldCallSingle()
    {
        string result = ImmutableNonEmptyList.Of(1).Match(h => $"single {h}", (h, t) => $"multi {h}");

        Assert.Equal("single 1", result);
    }

    [Fact]
    public void TakeAndSkip_ShouldReturnNullAtTheEdges()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList.Of(1, 2, 3);

        Assert.Null(list.Take(0));
        Assert.Null(list.Skip(3));
        Assert.Equal(new[] { 1, 2, 3 }, list.Skip(-1));
    }

    [Fact]
    public void Intersperse_OnSingleElement_ShouldReturnSameInstance()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList.Of(1);

        Assert.Same(list, list.Intersperse(0));
    }

    [Fact]
    public void EqualsObjectAndToString_ShouldDescribeTheList()
    {
        ImmutableNonEmptyList<int> list = ImmutableNonEmptyList.Of(1, 2);

        Assert.True(list.Equals((object)ImmutableNonEmptyList.Of(1, 2)));
        Assert.False(list.Equals((object)new NonEmptyList<int>(1, 2)));
        Assert.False(list.Equals((object?)null));
        Assert.Equal("ImmutableNonEmptyList: [1, 2]", list.ToString());
    }

    #endregion

    #region JSON

    [Fact]
    public void JsonFactory_ShouldOnlyAcceptImmutableNonEmptyList()
    {
        ImmutableNonEmptyListJsonConverterFactory factory = new();

        Assert.True(factory.CanConvert(typeof(ImmutableNonEmptyList<int>)));
        Assert.False(factory.CanConvert(typeof(int)));
        Assert.False(factory.CanConvert(typeof(List<int>)));
    }

    [Theory]
    [InlineData("5", "Expected start of array")]
    [InlineData("[]", "ImmutableNonEmptyList cannot be empty")]
    [InlineData("[\"a\", null]", "ImmutableNonEmptyList cannot contain null elements")]
    public void Json_InvalidPayloads_ShouldThrowJsonException(string json, string message)
    {
        JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ImmutableNonEmptyList<string>>(json));

        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void JsonConverter_ReadingNullToken_ShouldReturnNull()
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes("null"));
        reader.Read();

        ImmutableNonEmptyList<int>? result = new ImmutableNonEmptyListJsonConverter<int>()
            .Read(ref reader, typeof(ImmutableNonEmptyList<int>), JsonSerializerOptions.Default);

        Assert.Null(result);
    }

    [Fact]
    public void Json_NullLiteral_ShouldDeserializeToNull()
    {
        Assert.Null(JsonSerializer.Deserialize<ImmutableNonEmptyList<int>>("null"));
    }

    #endregion
}
