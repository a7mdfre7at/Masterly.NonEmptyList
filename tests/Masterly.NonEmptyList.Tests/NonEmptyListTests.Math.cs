using System.Numerics;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Generic-math aggregations that have no empty-sequence failure mode.
/// </summary>
public partial class NonEmptyListTests
{
    private readonly record struct Money(decimal Amount, string Currency)
        : IAdditionOperators<Money, Money, Money>
    {
        public static Money operator +(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidOperationException("Currency mismatch");

            return new Money(left.Amount + right.Amount, left.Currency);
        }
    }

    [Fact]
    public void Sum_ShouldWorkForBuiltInAndUserDefinedTypes()
    {
        NonEmptyList<int> ints = [1, 2, 3];
        NonEmptyList<decimal> decimals = [1.5m, 2.5m];
        NonEmptyList<Money> money = [new(10m, "USD"), new(5m, "USD")];

        Assert.Equal(6, ints.Sum());
        Assert.Equal(4.0m, decimals.Sum());
        Assert.Equal(new Money(15m, "USD"), money.Sum());
        Assert.Equal(15m, money.Sum(m => m.Amount));
    }

    [Fact]
    public void Sum_ShouldUseCheckedArithmetic()
    {
        NonEmptyList<int> list = [int.MaxValue, 1];

        Assert.Throws<OverflowException>(() => list.Sum());
    }

    [Fact]
    public void Product_ShouldMultiply()
    {
        NonEmptyList<int> list = [2, 3, 4];

        Assert.Equal(24, list.Product());
        Assert.Equal(7, NonEmptyList.Of(7).Product());
    }

    [Fact]
    public void Average_ShouldMatchLinqResultTypes()
    {
        NonEmptyList<int> ints = [1, 2];
        NonEmptyList<long> longs = [1L, 2L];
        NonEmptyList<double> doubles = [1.0, 2.0];
        NonEmptyList<decimal> decimals = [1m, 2m];

        double intAverage = ints.Average();
        double longAverage = longs.Average();
        double doubleAverage = doubles.Average();
        decimal decimalAverage = decimals.Average();
        double selectorAverage = ints.Average(x => x * 2);
        decimal decimalSelectorAverage = ints.Average(x => (decimal)x);

        Assert.Equal(1.5, intAverage);
        Assert.Equal(1.5, longAverage);
        Assert.Equal(1.5, doubleAverage);
        Assert.Equal(1.5m, decimalAverage);
        Assert.Equal(3.0, selectorAverage);
        Assert.Equal(1.5m, decimalSelectorAverage);
    }

    [Fact]
    public void Median_ShouldHandleOddAndEvenCounts()
    {
        NonEmptyList<int> odd = [5, 1, 3];
        NonEmptyList<int> even = [4, 1, 3, 2];
        NonEmptyList<double> doubles = [2.0, 1.0];

        Assert.Equal(3.0, odd.Median());
        Assert.Equal(2.5, even.Median());
        Assert.Equal(1.5, doubles.Median());
        Assert.Equal(9.0, NonEmptyList.Of(9).Median());
    }

    [Fact]
    public void Mode_ShouldReturnMostFrequentElement_FirstOnTies()
    {
        NonEmptyList<string> words = ["a", "b", "b", "c", "a"];
        NonEmptyList<int> tie = [1, 2];

        Assert.Equal("a", words.Mode());
        Assert.Equal(1, tie.Mode());
        Assert.Equal("A", NonEmptyList.Of("A", "a", "b").Mode(StringComparer.OrdinalIgnoreCase));
    }
}
