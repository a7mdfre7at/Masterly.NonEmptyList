using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Edge cases of the constrained list, set, dictionary, lazy enumerable, span and event stream that the
/// feature-oriented suites do not reach.
/// </summary>
public class CollectionEdgeCaseTests
{
    private static T ReadNullToken<T>(JsonConverter<T> converter) where T : class
    {
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes("null"));
        reader.Read();
        return converter.Read(ref reader, typeof(T), JsonSerializerOptions.Default)!;
    }

    #region ConstrainedList

    [Fact]
    public void Describe_ShouldUseSingularForOne()
    {
        Assert.Equal("at least 1 element", ConstrainedList<int, AtLeast<One>>.Description);
        Assert.Equal("at most 1 element", ConstrainedList<int, AtMost<One>>.Description);
        Assert.Equal("exactly 2 elements", ConstrainedList<int, Exactly<Two>>.Description);
    }

    [Fact]
    public void SortedDescending_ShouldDescribeAndEnforceOrder()
    {
        Assert.Equal("elements in descending order", ConstrainedList<int, SortedDescending>.Description);
        Assert.Equal(new[] { 3, 2, 1 }, ConstrainedList<int, SortedDescending>.From(new[] { 3, 2, 1 }));
        Assert.Throws<ArgumentException>(() => ConstrainedList<int, SortedDescending>.From(new[] { 1, 2 }));
    }

    [Fact]
    public void Or_ShouldAcceptEitherConstraintAndDescribeBoth()
    {
        Assert.Equal("elements in ascending order or elements in descending order", ConstrainedList<int, Or<Sorted, SortedDescending>>.Description);
        Assert.True(ConstrainedList<int, Or<Sorted, SortedDescending>>.Satisfies(new[] { 1, 2 }));
        Assert.True(ConstrainedList<int, Or<Sorted, SortedDescending>>.Satisfies(new[] { 2, 1 }));
        Assert.False(ConstrainedList<int, Or<Sorted, SortedDescending>>.Satisfies(new[] { 2, 1, 3 }));
    }

    [Fact]
    public void From_WithNullElement_ShouldThrowArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => ConstrainedList<string, NonEmpty>.From(new[] { "a", null! }));
    }

    [Fact]
    public void Second_AndThird_OnAndAndExactly_ShouldReturnPositions()
    {
        ConstrainedList<int, And<AtLeast<Three>, Distinct>> distinct = ConstrainedList<int, And<AtLeast<Three>, Distinct>>.From(new[] { 1, 2, 3 });
        ConstrainedList<int, Exactly<Two>> pair = ConstrainedList<int, Exactly<Two>>.From(new[] { 7, 8 });

        Assert.Equal(3, distinct.Third());
        Assert.Equal(8, pair.Second());
    }

    [Fact]
    public void ConstrainedJson_ShouldRecognizeOnlyConstrainedLists()
    {
        ConstrainedListJsonConverterFactory factory = new();

        Assert.True(factory.CanConvert(typeof(ConstrainedList<int, NonEmpty>)));
        Assert.False(factory.CanConvert(typeof(int)));
        Assert.False(factory.CanConvert(typeof(NonEmptyList<int>)));
        Assert.Null(ReadNullToken(new ConstrainedListJsonConverter<int, NonEmpty>()));
    }

    #endregion

    #region Set and dictionary

    [Fact]
    public void Set_EqualsObject_ShouldRejectOtherTypes()
    {
        NonEmptySet<int> set = NonEmptySet.Of(1, 2);

        Assert.True(set.Equals((object)NonEmptySet.Of(2, 1)));
        Assert.False(set.Equals((object)new NonEmptyList<int>(1, 2)));
    }

    [Fact]
    public void SetJson_ShouldRecognizeOnlySets()
    {
        NonEmptySetJsonConverterFactory factory = new();

        Assert.True(factory.CanConvert(typeof(NonEmptySet<int>)));
        Assert.True(factory.CanConvert(typeof(NonEmptySet<string>)));
        Assert.False(factory.CanConvert(typeof(int)));
        Assert.False(factory.CanConvert(typeof(HashSet<int>)));
        Assert.Null(ReadNullToken(new NonEmptySetJsonConverter<int>()));
    }

    [Fact]
    public void Dictionary_EqualsObject_ShouldRejectOtherTypes()
    {
        NonEmptyDictionary<string, int> dictionary = new("a", 1);

        Assert.True(dictionary.Equals((object)new NonEmptyDictionary<string, int>("a", 1)));
        Assert.False(dictionary.Equals((object)new Dictionary<string, int> { ["a"] = 1 }));
    }

    [Fact]
    public void DictionaryJson_ShouldRecognizeOnlyDictionaries()
    {
        NonEmptyDictionaryJsonConverterFactory factory = new();

        Assert.True(factory.CanConvert(typeof(NonEmptyDictionary<string, int>)));
        Assert.False(factory.CanConvert(typeof(int)));
        Assert.False(factory.CanConvert(typeof(Dictionary<string, int>)));
        Assert.Null(ReadNullToken(new NonEmptyDictionaryJsonConverter<string, int>()));
    }

    #endregion

    #region Lazy enumerable

    [Fact]
    public void TakeUntil_WhenPredicateNeverHolds_ShouldReturnTheWholeFiniteSequence()
    {
        NonEmptyEnumerable<int> finite = NonEmptyEnumerable.Create(1, 2, 3);

        Assert.Equal(new[] { 1, 2, 3 }, finite.TakeUntil(x => x > 10).ToArray());
    }

    [Fact]
    public void ElementAt_Zero_ShouldReturnHeadWithoutTouchingRest()
    {
        NonEmptyEnumerable<int> sequence = NonEmptyEnumerable.Create(5, ThrowingRest());

        Assert.Equal(5, sequence.ElementAt(0));

        static IEnumerable<int> ThrowingRest()
        {
            throw new InvalidOperationException("rest must not be evaluated");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }

    [Fact]
    public void Scan_OverFiniteSequence_ShouldEndWithTheTotal()
    {
        NonEmptyEnumerable<int> sums = NonEmptyEnumerable.Create(1, 2, 3).Scan(0, (acc, x) => acc + x);

        Assert.Equal(new[] { 1, 3, 6 }, sums.ToArray());
    }

    [Fact]
    public void NonGenericEnumerator_ShouldYieldTheSameElements()
    {
        IEnumerable sequence = NonEmptyEnumerable.Create(1, 2);

        Assert.Equal(new object[] { 1, 2 }, EnumerateNonGeneric(sequence));
    }

    #endregion

    #region Span and event stream

    [Fact]
    public void Span_Max_ShouldFindALaterLargerElement()
    {
        NonEmptySpan<int> span = NonEmptySpan.From(new[] { 1, 5, 3, 9 });

        Assert.Equal(9, span.Max());
    }

    private sealed record Happened(int N);

    [Fact]
    public void EventStream_CountEnumeratorAndEquals_ShouldReflectTheEvents()
    {
        EventStream<Happened> stream = EventStream.Start(new Happened(1)).Append(new Happened(2));
        IReadOnlyCollection<Happened> collection = stream;

        Assert.Equal(2, collection.Count);
        Assert.Equal(new object[] { new Happened(1), new Happened(2) }, EnumerateNonGeneric(stream));
        Assert.True(stream.Equals((object)EventStream.Start(new Happened(1)).Append(new Happened(2))));
        Assert.False(stream.Equals((object)new Happened(1)));
    }

    [Fact]
    public void Set_NonGenericEnumerator_ShouldYieldTheElements()
    {
        Assert.Equal(new object[] { "a", "b" }, EnumerateNonGeneric(NonEmptySet.Of("a", "b")));
    }

    #endregion

    /// <summary>Walks a sequence through <see cref="IEnumerable.GetEnumerator"/> only (Cast would reuse the generic enumerator).</summary>
    private static object[] EnumerateNonGeneric(IEnumerable sequence)
    {
        List<object> items = new();
        foreach (object item in sequence)
            items.Add(item);
        return items.ToArray();
    }
}
