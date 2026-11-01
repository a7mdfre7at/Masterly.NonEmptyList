using System.Text.Json;
using Masterly.NonEmptyList.Analyzers.Tests.Domain;

namespace Masterly.NonEmptyList.Analyzers.Tests;

/// <summary>Exercises the collections the generator produced for this very project.</summary>
public class GeneratedCollectionTests
{
    private static readonly OrderLine A = new("A", 1);
    private static readonly OrderLine B = new("B", 2);
    private static readonly OrderLine C = new("C", 3);
    private static readonly OrderLine D = new("D", 4);

    [Fact]
    public void GeneratedType_ShouldDeriveFromNonEmptyListAndExposeMetadata()
    {
        OrderLines lines = OrderLines.Of(A, B);

        Assert.IsAssignableFrom<NonEmptyList<OrderLine>>(lines);
        Assert.Equal(1, OrderLines.MinCount);
        Assert.Equal(3, OrderLines.MaxCount);
        Assert.True(OrderLines.IsDistinct);
        Assert.False(OrderLines.IsImmutable);
        Assert.Equal("at least 1 element, at most 3, distinct", OrderLines.Description);
        Assert.Equal("at least 2 elements, immutable", Route.Description);
        Assert.Equal("at least 1 element", Scores.Description);
        Assert.Equal(int.MaxValue, Scores.MaxCount);
        Assert.Equal(A, lines.Head);
        Assert.Equal("OrderLines: [OrderLine { Sku = A, Quantity = 1 }, OrderLine { Sku = B, Quantity = 2 }]", lines.ToString());
    }

    [Fact]
    public void Construction_ShouldEnforceInvariants()
    {
        Assert.Equal(2, new OrderLines(new[] { A, B }).Count);
        Assert.Equal(2, OrderLines.From(new List<OrderLine> { A, B }).Count);
        Assert.Throws<ArgumentException>(() => OrderLines.From(Array.Empty<OrderLine>()));
        Assert.Throws<ArgumentException>(() => OrderLines.Of(A, A));
        Assert.Throws<ArgumentException>(() => OrderLines.Of(A, B, C, D));
        Assert.Throws<ArgumentException>(() => Route.Of("only"));
        Assert.Equal(2, Route.Of("a", "b").Count);
        Assert.Throws<ArgumentException>(() => OrderLines.From(null!));
    }

    [Fact]
    public void CollectionExpression_ShouldUseGeneratedBuilder()
    {
        OrderLines lines = [A, B];
        Scores scores = [1, 2, 3];

        Assert.Equal(2, lines.Count);
        Assert.Equal(6, scores.Sum());
        Assert.Throws<ArgumentException>(() =>
        {
            OrderLines dup = [A, A];
            return dup;
        });
    }

    [Fact]
    public void TryFrom_Satisfies_Validate_ShouldNotThrow()
    {
        Assert.True(OrderLines.TryFrom(new[] { A }, out OrderLines? ok));
        Assert.Single(ok!);
        Assert.False(OrderLines.TryFrom(new[] { A, A }, out _));
        Assert.False(OrderLines.TryFrom(null, out _));
        Assert.False(OrderLines.TryFrom(Array.Empty<OrderLine>(), out _));
        Assert.True(OrderLines.Satisfies(new[] { A, B }));
        Assert.False(OrderLines.Satisfies(new[] { A, B, C, D }));

        Validation<OrderLines, ValidationError> valid = OrderLines.Validate(new[] { A });
        Validation<OrderLines, ValidationError> missing = OrderLines.Validate(null, "lines");
        Validation<OrderLines, ValidationError> empty = OrderLines.Validate(Array.Empty<OrderLine>(), "lines");
        Validation<OrderLines, ValidationError> nulls = OrderLines.Validate(new OrderLine[] { A, null! });
        Validation<OrderLines, ValidationError> both = OrderLines.Validate(new[] { A, A, B, C }, "lines");
        Validation<Route, ValidationError> tooFew = Route.Validate(new[] { "a" }, "route");

        Assert.True(valid.IsValid);
        Assert.Equal("required", missing.Errors.Head.Code);
        Assert.Equal("lines", missing.Errors.Head.Path);
        Assert.Equal("nonEmpty", empty.Errors.Head.Code);
        Assert.Equal("noNulls", nulls.Errors.Head.Code);
        Assert.Equal(new[] { "maxCount", "distinct" }, both.Errors.Select(e => e.Code).ToArray());
        Assert.Equal("minCount", tooFew.Errors.Head.Code);
    }

    [Fact]
    public void Mutations_ShouldRespectDistinctAndMaxCount()
    {
        OrderLines lines = OrderLines.Of(A, B);

        lines.Add(C);
        Assert.Throws<ArgumentException>(() => lines.Add(A));
        Assert.Throws<InvalidOperationException>(() => lines.Add(D));
        Assert.Throws<ArgumentException>(() => lines[0] = B);
        lines[0] = A;
        lines[0] = D;
        Assert.Equal(new[] { D, B, C }, lines.ToArray());
        lines.Remove(C);
        Assert.Throws<ArgumentException>(() => lines.Insert(0, B));
        lines.Insert(0, C);
        Assert.Equal(3, lines.Count);
        Assert.Throws<ArgumentException>(() => lines.AddRange(new[] { A, A }));
        Assert.Throws<InvalidOperationException>(() => lines.AddRange(new[] { A }));
        lines.RemoveAt(0);
        Assert.Throws<ArgumentException>(() => lines.AddRange(new[] { A, D }));
        Assert.Throws<ArgumentException>(() => lines.InsertRange(0, new[] { A, A }));
        lines.AddRange(new[] { A });
        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public void ImmutableGeneratedType_ShouldRejectMutationAndEnforceMinCount()
    {
        Route route = Route.Of("a", "b", "c");

        Assert.True(Route.IsImmutable);
        Assert.True(((ICollection<string>)route).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => route.Add("d"));
        Assert.Throws<NotSupportedException>(() => route.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => route.Sort());
        Assert.Equal("a", route.Head);
        Assert.Equal(new[] { "A", "B", "C" }, route.Select(s => s.ToUpperInvariant()).ToArray());
    }

    [Fact]
    public void MinCount_ShouldBlockRemovalsOnMutableType()
    {
        Scores scores = Scores.Of(1, 2);

        scores.Remove(1);
        Assert.Throws<InvalidOperationException>(() => scores.Remove(2));
        Assert.Single(scores);
    }

    [Fact]
    public void Json_ShouldRoundTripAndEnforceInvariants()
    {
        OrderLines lines = OrderLines.Of(A, B);
        var dto = new OrderDto(42, lines);

        string json = JsonSerializer.Serialize(dto);
        OrderDto back = JsonSerializer.Deserialize<OrderDto>(json)!;

        Assert.Equal("{\"Id\":42,\"Lines\":[{\"Sku\":\"A\",\"Quantity\":1},{\"Sku\":\"B\",\"Quantity\":2}]}", json);
        Assert.Equal(lines, back.Lines);
        Assert.Null(JsonSerializer.Deserialize<OrderLines>("null"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OrderLines>("[]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OrderLines>("[{\"Sku\":\"A\",\"Quantity\":1},{\"Sku\":\"A\",\"Quantity\":1}]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Route>("[\"a\"]"));
        Assert.Equal(2, JsonSerializer.Deserialize<Route>("[\"a\",\"b\"]")!.Count);
    }

    [Fact]
    public void GeneratedType_ShouldStillSupportLinqAndPatterns()
    {
        Scores scores = Scores.Of(3, 1, 2);

        NonEmptyList<int> sorted = scores.OrderBy(x => x);
        Assert.Equal(new[] { 1, 2, 3 }, sorted.ToArray());
        Assert.Equal(3, scores.Max());
        Assert.Equal(2.0, scores.Average());
        Assert.True(scores is [3, .. var rest] && rest.Count == 2);
    }

    private sealed record OrderDto(int Id, OrderLines Lines);
}
