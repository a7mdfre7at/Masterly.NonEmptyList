using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

public class NonEmptyDictionaryTests
{
    [Fact]
    public void Construction_ShouldRequireOneEntry()
    {
        NonEmptyDictionary<string, int> single = new("a", 1);
        NonEmptyDictionary<string, int> pairs = new(new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2));

        Assert.Single(single);
        Assert.Equal(2, pairs.Count);
        Assert.Throws<ArgumentException>(() => NonEmptyDictionary<string, int>.From(new Dictionary<string, int>()));
        Assert.Throws<ArgumentException>(() => new NonEmptyDictionary<string, int>(new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("a", 2)));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyDictionary<string, string>("a", null!));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyDictionary<string, string>(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => NonEmptyDictionary<string, string>.From(new[] { new KeyValuePair<string, string>("a", null!) }));
        Assert.Throws<ArgumentNullException>(() => NonEmptyDictionary<string, int>.From(null!));
    }

    [Fact]
    public void Factories_ShouldInferTypes()
    {
        var of = NonEmptyDictionary.Of("usd", 1.0m);
        var ofTuples = NonEmptyDictionary.OfPairs(("usd", 1.0m), ("eur", 0.9m));
        var fromPairs = new Dictionary<string, int> { ["a"] = 1 }.ToNonEmptyDictionary();
        var projected = new[] { "a", "bb" }.ToNonEmptyDictionary(s => s, s => s.Length);
        var fromList = NonEmptyList.Of("x", "yy").ToNonEmptyDictionary(s => s.Length);
        var fromStatic = NonEmptyDictionary.From(new Dictionary<int, int> { [1] = 1 });
        var projectedStatic = NonEmptyDictionary<string, int>.From(new[] { "a" }, s => s, s => s.Length);

        Assert.Single(of);
        Assert.Equal(2, ofTuples.Count);
        Assert.Equal(1, fromPairs["a"]);
        Assert.Equal(2, projected["bb"]);
        Assert.Equal("yy", fromList[2]);
        Assert.Single(fromStatic);
        Assert.Single(projectedStatic);
        Assert.Single(NonEmptyDictionary<string, int>.Singleton("k", 1));
        Assert.True(NonEmptyDictionary<string, int>.TryFrom(new Dictionary<string, int> { ["a"] = 1 }, out _));
        Assert.False(NonEmptyDictionary<string, int>.TryFrom(new Dictionary<string, int>(), out _));
        Assert.False(NonEmptyDictionary<string, int>.TryFrom(null, out _));
        Assert.False(NonEmptyDictionary<string, int>.TryFrom(new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("a", 2) }, out _));
        Assert.False(NonEmptyDictionary<string, string>.TryFrom(new[] { new KeyValuePair<string, string>("a", null!) }, out _));
        Assert.True(new Dictionary<string, int> { ["a"] = 1 }.TryToNonEmptyDictionary(out _));
    }

    [Fact]
    public void Remove_ShouldNeverEmptyTheDictionary()
    {
        NonEmptyDictionary<string, int> dict = NonEmptyDictionary.OfPairs(("a", 1), ("b", 2));

        Assert.False(dict.Remove("zzz"));
        Assert.True(dict.Remove("a"));
        Assert.Throws<InvalidOperationException>(() => dict.Remove("b"));
        Assert.Throws<InvalidOperationException>(() => dict.Remove("b", out _));
        Assert.False(dict.Remove("zzz", out int missing));
        Assert.Equal(0, missing);
        Assert.Throws<NotSupportedException>(() => dict.Clear());
        Assert.Equal("b", dict.Any().Key);

        dict.Add("c", 3);
        Assert.True(dict.Remove("c", out int removed));
        Assert.Equal(3, removed);
        dict.Add("c", 3);
        Assert.True(((ICollection<KeyValuePair<string, int>>)dict).Remove(new KeyValuePair<string, int>("c", 3)));
        Assert.False(((ICollection<KeyValuePair<string, int>>)dict).Remove(new KeyValuePair<string, int>("b", 999)));
    }

    [Fact]
    public void AddAndIndexer_ShouldValidate()
    {
        NonEmptyDictionary<string, string> dict = new("a", "1");

        dict["b"] = "2";
        dict["a"] = "one";
        ((ICollection<KeyValuePair<string, string>>)dict).Add(new KeyValuePair<string, string>("c", "3"));

        Assert.Equal("one", dict["a"]);
        Assert.Equal(3, dict.Count);
        Assert.Throws<ArgumentException>(() => dict.Add("a", "dup"));
        Assert.Throws<ArgumentNullException>(() => dict.Add("d", null!));
        Assert.Throws<ArgumentNullException>(() => dict["d"] = null!);
        Assert.True(dict.TryAdd("d", "4"));
        Assert.False(dict.TryAdd("d", "4"));
        Assert.Throws<ArgumentNullException>(() => dict.TryAdd("e", null!));
        Assert.Throws<KeyNotFoundException>(() => dict["missing"]);
    }

    [Fact]
    public void Lookups_ShouldForward()
    {
        NonEmptyDictionary<string, int> dict = NonEmptyDictionary.OfPairs(("a", 1), ("b", 2));

        Assert.True(dict.ContainsKey("a"));
        Assert.True(dict.ContainsValue(2));
        Assert.True(dict.TryGetValue("b", out int b));
        Assert.Equal(2, b);
        Assert.Equal(9, dict.GetValueOrDefault("zzz", 9));
        Assert.Equal(1, dict.GetValueOrDefault("a", 9));
        Assert.True(((ICollection<KeyValuePair<string, int>>)dict).Contains(new KeyValuePair<string, int>("a", 1)));
        Assert.False(((ICollection<KeyValuePair<string, int>>)dict).IsReadOnly);
        Assert.Equal(new[] { "a", "b" }, dict.Keys.OrderBy(k => k));
        Assert.Equal(new[] { 1, 2 }, dict.Values.OrderBy(v => v));
        Assert.Equal(2, ((IDictionary<string, int>)dict).Keys.Count);
        Assert.Equal(2, ((IDictionary<string, int>)dict).Values.Count);
        Assert.Equal(2, ((IReadOnlyDictionary<string, int>)dict).Keys.Count());
        Assert.Equal(2, ((IReadOnlyDictionary<string, int>)dict).Values.Count());
        Assert.Same(EqualityComparer<string>.Default, dict.Comparer);

        var target = new KeyValuePair<string, int>[2];
        ((ICollection<KeyValuePair<string, int>>)dict).CopyTo(target, 0);
        Assert.Contains(new KeyValuePair<string, int>("a", 1), target);
    }

    [Fact]
    public void NonEmptyProjections_ShouldWork()
    {
        NonEmptyDictionary<string, int> dict = NonEmptyDictionary.OfPairs(("a", 1), ("b", 2));

        Assert.Equal(2, dict.KeyList().Count);
        Assert.Equal(3, dict.ValueList().Sum());
        Assert.Equal(2, dict.ToNonEmptyList().Count);
        Assert.Equal(20, dict.MapValues(v => v * 10)["b"]);
        Assert.Equal("a1", dict.Map((k, v) => k + v)["a"]);
        Assert.Equal(new[] { "b" }, dict.Where((_, v) => v > 1)!.Keys);
        Assert.Null(dict.Where((_, v) => v > 5));
        Assert.Throws<InvalidOperationException>(() => dict.MapValues<string>(_ => null!));
        Assert.Throws<InvalidOperationException>(() => dict.Map<string>((_, _) => null!));

        Dictionary<string, int> copy = dict.ToDictionary();
        copy.Clear();
        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void Equality_ToString_Enumeration_ShouldWork()
    {
        NonEmptyDictionary<string, int> a = NonEmptyDictionary.OfPairs(("a", 1), ("b", 2));
        NonEmptyDictionary<string, int> b = NonEmptyDictionary.OfPairs(("b", 2), ("a", 1));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, NonEmptyDictionary.OfPairs(("a", 1), ("b", 3)));
        Assert.NotEqual(a, NonEmptyDictionary.Of("a", 1));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.StartsWith("NonEmptyDictionary: {", a.ToString());
        Assert.Equal(2, ((System.Collections.IEnumerable)a).Cast<object>().Count());
        Assert.Equal(2, ((IEnumerable<KeyValuePair<string, int>>)a).Count());
        int total = 0;
        foreach (KeyValuePair<string, int> pair in a)
            total += pair.Value;
        Assert.Equal(3, total);
    }

    [Fact]
    public void Json_ShouldRoundTripAndRejectEmpty()
    {
        NonEmptyDictionary<string, int> dict = NonEmptyDictionary.OfPairs(("a", 1), ("b", 2));

        string json = JsonSerializer.Serialize(dict);
        NonEmptyDictionary<string, int>? back = JsonSerializer.Deserialize<NonEmptyDictionary<string, int>>(json);

        Assert.Equal("{\"a\":1,\"b\":2}", json);
        Assert.Equal(dict, back);
        Assert.Null(JsonSerializer.Deserialize<NonEmptyDictionary<string, int>>("null"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptyDictionary<string, int>>("{}"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<NonEmptyDictionary<string, string>>("{\"a\":null}"));
        Assert.Equal("b", JsonSerializer.Deserialize<NonEmptyDictionary<int, string>>("{\"1\":\"a\",\"2\":\"b\"}")![2]);
    }
}
