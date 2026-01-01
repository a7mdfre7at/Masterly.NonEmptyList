using System.Text.Json;

namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region Serialization Tests

    [Fact]
    public void JsonSerialize_ShouldSerializeAsArray()
    {
        var list = new NonEmptyList<int>(1, 2, 3);
        var json = JsonSerializer.Serialize(list);

        Assert.Equal("[1,2,3]", json);
    }

    [Fact]
    public void JsonSerialize_WithStrings_ShouldSerializeCorrectly()
    {
        var list = new NonEmptyList<string>("a", "b", "c");
        var json = JsonSerializer.Serialize(list);

        Assert.Equal("[\"a\",\"b\",\"c\"]", json);
    }

    [Fact]
    public void JsonSerialize_WithComplexType_ShouldSerializeCorrectly()
    {
        var list = new NonEmptyList<TestPerson>(
            new TestPerson { Name = "Alice", Age = 30 },
            new TestPerson { Name = "Bob", Age = 25 }
        );
        var json = JsonSerializer.Serialize(list);

        Assert.Contains("\"Name\":\"Alice\"", json);
        Assert.Contains("\"Name\":\"Bob\"", json);
    }

    #endregion

    #region Deserialization Tests

    [Fact]
    public void JsonDeserialize_ShouldDeserializeFromArray()
    {
        var json = "[1,2,3]";
        var list = JsonSerializer.Deserialize<NonEmptyList<int>>(json);

        Assert.NotNull(list);
        Assert.Equal(3, list!.Count);
        Assert.Equal(new[] { 1, 2, 3 }, list.ToArray());
    }

    [Fact]
    public void JsonDeserialize_WithStrings_ShouldDeserializeCorrectly()
    {
        var json = "[\"a\",\"b\",\"c\"]";
        var list = JsonSerializer.Deserialize<NonEmptyList<string>>(json);

        Assert.NotNull(list);
        Assert.Equal(new[] { "a", "b", "c" }, list!.ToArray());
    }

    [Fact]
    public void JsonDeserialize_WithEmptyArray_ShouldThrow()
    {
        var json = "[]";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyList<int>>(json));
    }

    [Fact]
    public void JsonDeserialize_WithNull_ShouldReturnNull()
    {
        var json = "null";
        var list = JsonSerializer.Deserialize<NonEmptyList<int>>(json);

        Assert.Null(list);
    }

    [Fact]
    public void JsonDeserialize_WithComplexType_ShouldDeserializeCorrectly()
    {
        var json = "[{\"Name\":\"Alice\",\"Age\":30},{\"Name\":\"Bob\",\"Age\":25}]";
        var list = JsonSerializer.Deserialize<NonEmptyList<TestPerson>>(json);

        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(25, list[1].Age);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void JsonRoundTrip_ShouldPreserveData()
    {
        var original = new NonEmptyList<int>(1, 2, 3, 4, 5);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<NonEmptyList<int>>(json);

        Assert.NotNull(restored);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void JsonRoundTrip_WithComplexType_ShouldPreserveData()
    {
        var original = new NonEmptyList<TestPerson>(
            new TestPerson { Name = "Alice", Age = 30 },
            new TestPerson { Name = "Bob", Age = 25 }
        );

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<NonEmptyList<TestPerson>>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Count, restored!.Count);
        Assert.Equal("Alice", restored[0].Name);
        Assert.Equal("Bob", restored[1].Name);
    }

    #endregion

    #region Nested NonEmptyList Tests

    [Fact]
    public void JsonSerialize_NestedNonEmptyList_ShouldWork()
    {
        var list = new NonEmptyList<NonEmptyList<int>>(
            new NonEmptyList<int>(1, 2),
            new NonEmptyList<int>(3, 4)
        );

        var json = JsonSerializer.Serialize(list);
        Assert.Equal("[[1,2],[3,4]]", json);
    }

    [Fact]
    public void JsonDeserialize_NestedNonEmptyList_ShouldWork()
    {
        var json = "[[1,2],[3,4]]";
        var list = JsonSerializer.Deserialize<NonEmptyList<NonEmptyList<int>>>(json);

        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);
        Assert.Equal(new[] { 1, 2 }, list[0].ToArray());
        Assert.Equal(new[] { 3, 4 }, list[1].ToArray());
    }

    #endregion

    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}
