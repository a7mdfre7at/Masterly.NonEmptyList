namespace Masterly.NonEmptyList.Analyzers.Tests.Domain;

public sealed record OrderLine(string Sku, int Quantity);

/// <summary>Distinct, at most three lines, mutable.</summary>
[NonEmptyCollection(typeof(OrderLine), Distinct = true, MaxCount = 3)]
public sealed partial class OrderLines;

/// <summary>Immutable value object: at least two waypoints.</summary>
[NonEmptyCollection(typeof(string), Immutable = true, MinCount = 2)]
public sealed partial class Route;

/// <summary>Plain non-empty collection with no extra invariants.</summary>
[NonEmptyCollection(typeof(int))]
public sealed partial class Scores;
