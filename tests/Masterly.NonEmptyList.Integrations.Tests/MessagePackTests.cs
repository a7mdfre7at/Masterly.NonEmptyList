#pragma warning disable CA2260 // derived collection is a legitimate use of NonEmptyList<T> as a base
using Masterly.NonEmptyList.MessagePack;
using MessagePack;

namespace Masterly.NonEmptyList.Integrations.Tests;

public class MessagePackTests
{
    private static readonly MessagePackSerializerOptions Options = NonEmptyMessagePackExtensions.NonEmptyStandard;

    [MessagePackObject(SuppressSourceGeneration = true)]
    public sealed class Dto
    {
        [Key(0)] public NonEmptyList<int> Numbers { get; set; } = NonEmptyList.Of(0);
        [Key(1)] public ImmutableNonEmptyList<string> Names { get; set; } = ImmutableNonEmptyList.Of("x");
        [Key(2)] public NonEmptySet<string> Tags { get; set; } = NonEmptySet.Of("x");
        [Key(3)] public NonEmptyDictionary<string, int> Limits { get; set; } = NonEmptyDictionary.Of("x", 1);
        [Key(4)] public ConstrainedList<int, AtLeast<Two>> Pair { get; set; } = ConstrainedList<int, AtLeast<Two>>.From(new[] { 1, 2 });
        [Key(5)] public NonEmptyString Title { get; set; } = new("x");
        [Key(6)] public NonEmptyList<int>? Optional { get; set; }
        [Key(7)] public NonEmptyString? OptionalTitle { get; set; }
    }

    [Fact]
    public void RoundTrip_ShouldPreserveEveryType()
    {
        Dto dto = new()
        {
            Numbers = NonEmptyList.Of(1, 2, 3),
            Names = ImmutableNonEmptyList.Of("a", "b"),
            Tags = NonEmptySet.Of("t1", "t2"),
            Limits = NonEmptyDictionary.OfPairs(("gold", 10), ("silver", 5)),
            Pair = ConstrainedList<int, AtLeast<Two>>.From(new[] { 7, 8, 9 }),
            Title = new NonEmptyString("title"),
            Optional = null,
            OptionalTitle = new NonEmptyString("maybe"),
        };

        byte[] bytes = MessagePackSerializer.Serialize(dto, Options);
        Dto back = MessagePackSerializer.Deserialize<Dto>(bytes, Options);

        Assert.Equal(dto.Numbers, back.Numbers);
        Assert.Equal(dto.Names, back.Names);
        Assert.Equal(dto.Tags, back.Tags);
        Assert.Equal(dto.Limits, back.Limits);
        Assert.Equal(dto.Pair, back.Pair);
        Assert.Equal(dto.Title, back.Title);
        Assert.Null(back.Optional);
        Assert.Equal("maybe", back.OptionalTitle!.Value.Value);
    }

    [Fact]
    public void TopLevelValues_ShouldRoundTrip()
    {
        Assert.Equal(NonEmptyList.Of(1), MessagePackSerializer.Deserialize<NonEmptyList<int>>(MessagePackSerializer.Serialize(NonEmptyList.Of(1), Options), Options));
        Assert.Equal(new NonEmptyString("s"), MessagePackSerializer.Deserialize<NonEmptyString>(MessagePackSerializer.Serialize(new NonEmptyString("s"), Options), Options));
        Assert.Null(MessagePackSerializer.Deserialize<NonEmptyList<int>>(MessagePackSerializer.Serialize<NonEmptyList<int>?>(null, Options), Options));
        Assert.Null(MessagePackSerializer.Deserialize<NonEmptySet<int>>(MessagePackSerializer.Serialize<NonEmptySet<int>?>(null, Options), Options));
        Assert.Null(MessagePackSerializer.Deserialize<NonEmptyDictionary<string, int>>(MessagePackSerializer.Serialize<NonEmptyDictionary<string, int>?>(null, Options), Options));
        Assert.Null(MessagePackSerializer.Deserialize<ConstrainedList<int, Sorted>>(MessagePackSerializer.Serialize<ConstrainedList<int, Sorted>?>(null, Options), Options));
    }

    [Fact]
    public void EmptyOrInvalidPayloads_ShouldThrow()
    {
        byte[] emptyArray = MessagePackSerializer.Serialize(Array.Empty<int>(), Options);
        byte[] emptyMap = MessagePackSerializer.Serialize(new Dictionary<string, int>(), Options);
        byte[] single = MessagePackSerializer.Serialize(new[] { 1 }, Options);
        byte[] unsorted = MessagePackSerializer.Serialize(new[] { 2, 1 }, Options);
        byte[] blank = MessagePackSerializer.Serialize("   ", Options);
        byte[] withNull = MessagePackSerializer.Serialize(new string?[] { "a", null }, Options);
        byte[] mapWithNull = MessagePackSerializer.Serialize(new Dictionary<string, string?> { ["a"] = null }, Options);

        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<NonEmptyList<int>>(emptyArray, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<ImmutableNonEmptyList<int>>(emptyArray, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<NonEmptySet<int>>(emptyArray, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<NonEmptyDictionary<string, int>>(emptyMap, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<ConstrainedList<int, AtLeast<Two>>>(single, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<ConstrainedList<int, Sorted>>(unsorted, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<NonEmptyString>(blank, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<NonEmptyList<string>>(withNull, Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<NonEmptyDictionary<string, string>>(mapWithNull, Options));
    }

    [Fact]
    public void Resolver_ShouldReturnNullForUnknownTypesAndHandleDerivedLists()
    {
        Assert.Null(NonEmptyResolver.Instance.GetFormatter<List<int>>());
        Assert.Null(NonEmptyResolver.Instance.GetFormatter<string>());
        Assert.NotNull(NonEmptyResolver.Instance.GetFormatter<NonEmptyList<int>>());
        Assert.NotNull(NonEmptyResolver.Instance.GetFormatter<NonEmptyString?>());
        Assert.NotNull(NonEmptyResolver.Instance.GetFormatter<Scores>());

        Scores scores = Scores.From(new[] { 1, 2 });
        byte[] bytes = MessagePackSerializer.Serialize(scores, Options);
        Scores back = MessagePackSerializer.Deserialize<Scores>(bytes, Options);
        Assert.Equal(scores, back);
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Scores>(MessagePackSerializer.Serialize(Array.Empty<int>(), Options), Options));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Scores>(MessagePackSerializer.Serialize(new[] { 1, 1 }, Options), Options));
    }

    [Fact]
    public void WithNonEmptyResolver_ShouldKeepExistingResolver()
    {
        MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithNonEmptyResolver();

        byte[] bytes = MessagePackSerializer.Serialize(new Dto { Numbers = NonEmptyList.Of(5) }, options);

        Assert.Equal(5, MessagePackSerializer.Deserialize<Dto>(bytes, options).Numbers.Head);
        Assert.Throws<ArgumentNullException>(() => ((MessagePackSerializerOptions)null!).WithNonEmptyResolver());
    }

    [Fact]
    public void NilPayloads_ShouldDeserializeToNullForEveryNullableCollection()
    {
        byte[] nil = MessagePackSerializer.Serialize<int[]?>(null, Options);

        Assert.Null(MessagePackSerializer.Deserialize<ImmutableNonEmptyList<int>>(nil, Options));
        Assert.Null(MessagePackSerializer.Deserialize<Scores>(nil, Options));
        Assert.Equal(nil, MessagePackSerializer.Serialize<Scores?>(null, Options));
    }

    [Fact]
    public void DerivedListWithoutFactory_ShouldExplainWhatIsMissing()
    {
        byte[] bytes = MessagePackSerializer.Serialize(new[] { 1 }, Options);

        Exception ex = Assert.ThrowsAny<Exception>(() => MessagePackSerializer.Deserialize<NoFactoryList>(bytes, Options));

        Exception root = ex;
        while (root.InnerException is not null)
            root = root.InnerException;
        Assert.Contains("does not expose a static From(IEnumerable<Int32>) factory", root.Message);
    }

    [Fact]
    public void DerivedListFactory_FailingForOtherReasons_ShouldNotBeReportedAsAnInvalidPayload()
    {
        byte[] ok = MessagePackSerializer.Serialize(new[] { 1, 2 }, Options);
        byte[] unlucky = MessagePackSerializer.Serialize(new[] { 13 }, Options);

        Assert.Equal(new[] { 1, 2 }, MessagePackSerializer.Deserialize<Unlucky>(ok, Options).ToArray());
        MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Unlucky>(unlucky, Options));

        // MessagePack wraps whatever escapes a formatter; the root cause is the factory's own exception, not a payload error.
        Assert.IsType<NotSupportedException>(ex.InnerException);
        Assert.DoesNotContain("Unlucky:", ex.Message);
    }
}

/// <summary>A derived collection, as the source generator would produce, with a distinct invariant.</summary>
public sealed class Scores : NonEmptyList<int>
{
    private Scores(NonEmptyList<int> source) : base(source)
    {
        if (Enumerable.Distinct(this).Count() != Count)
            throw new ArgumentException("Scores must be distinct");
    }

    public static new Scores From(IEnumerable<int> items) => new(NonEmptyList<int>.From(items));
}
