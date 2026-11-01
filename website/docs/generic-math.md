---
id: generic-math
title: "Generic Math"
description: "Sum, Product, Average, Median, Mode, Max, Min and MinMax on a non-empty list: no empty-sequence exception, no seed value, and support for your own numeric types."
---

A non-empty list always has a first element, so its aggregates have no empty case to handle. `Sum` starts from the first element instead of a zero value, `Average` and `Median` never divide by zero, and `Max` never returns a nullable type. The operators are built on .NET generic math (`System.Numerics`), so they work for the built-in numeric types and for your own types that implement the right interfaces.

```csharp
using Masterly.NonEmptyList;

NonEmptyList<int> scores = [70, 85, 85, 90];

int total = scores.Sum();             // 330, checked
int max = scores.Max();               // 90, int and not int?
double mean = scores.Average();       // 82.5
double median = scores.Median();      // 85
int mostCommon = scores.Mode();       // 85
(int lo, int hi) = scores.MinMax();   // (70, 90)
```

## The operators

`Sum`, `Product`, `Average`, `Median` and `Mode` live in `NonEmptyListMathExtensions`. `Max`, `Min` and `MinMax` live in `NonEmptyListLinqExtensions` and only need a comparer, not a numeric type.

| Method | Constraint on the element (or result) type | Returns |
|--------|--------------------------------------------|---------|
| `Sum()` | `T : IAdditionOperators<T, T, T>` | `T` |
| `Sum(selector)` | `TResult : IAdditionOperators<TResult, TResult, TResult>` | `TResult` |
| `Product()` | `T : IMultiplyOperators<T, T, T>` | `T` |
| `Average()` | `T : INumberBase<T>` | `T` |
| `Average(selector)` | `TResult : INumberBase<TResult>` | `TResult` |
| `Average()` on `NonEmptyList<int>` or `NonEmptyList<long>` | none | `double` |
| `Average(selector)` with a selector returning `int` or `long` | none | `double` |
| `Median()` | `T : INumber<T>` | `T` |
| `Median()` on `NonEmptyList<int>` or `NonEmptyList<long>` | none | `double` |
| `Mode()`, `Mode(comparer)` | `T : notnull` | `T` |
| `Max()`, `Min()`, with an optional `IComparer<T>?` | none | `T` |
| `Max(selector)`, `Min(selector)` | none | `TResult` |
| `MinMax()`, with an optional `IComparer<T>?` | none | `(T Min, T Max)` |

None of these methods throws because the list is empty: there is no "Sequence contains no elements" path. They can still throw `ArgumentNullException` for a `null` list or selector, `OverflowException` from checked arithmetic, and whatever your own operators or selectors throw.

:::note

`Product`, `Median` and `Mode` have no selector overload. Project first, then aggregate: `orders.Select(o => o.Quantity).Product()`. Because `Select` on a non-empty list returns a non-empty list, this costs you nothing in safety.

:::

## Sum and Product

`Sum` adds the elements from left to right, starting with the first one. It does not need a zero value, which matters for types that have no natural zero, such as a money type that carries a currency. `Product` works the same way with `*` and needs no one value.

Both use checked arithmetic. For the built-in integer types, an overflow throws `OverflowException` instead of wrapping around, which matches `Enumerable.Sum`.

```csharp
NonEmptyList<int> big = [int.MaxValue, 1];

big.Sum();       // throws OverflowException

NonEmptyList<int> factors = [2, 3, 4];
int product = factors.Product();   // 24
```

- `decimal` throws `OverflowException` on overflow anyway.
- `double` and `float` do not throw. They overflow to infinity as usual.
- For your own types, the checked context uses your `checked operator +` (or `*`) if you define one, and your regular operator otherwise.

`Sum(selector)` projects each element and adds the results, with the same checked arithmetic:

```csharp
// OrderLine is your own type with a decimal Price and an int Quantity.
NonEmptyList<OrderLine> lines = GetLines();

decimal total = lines.Sum(l => l.Price * l.Quantity);
int units = lines.Sum(l => l.Quantity);
```

## Your own numeric types

`Sum` only asks for `IAdditionOperators<T, T, T>`, so a value type with a `+` operator is enough. You do not need to implement the full `INumber<T>` surface or provide a zero.

```csharp title="Money.cs"
using System.Numerics;

public readonly record struct Money(decimal Amount, string Currency)
    : IAdditionOperators<Money, Money, Money>
{
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Currency mismatch");

        return new Money(left.Amount + right.Amount, left.Currency);
    }
}
```

```csharp
NonEmptyList<Money> payments = [new(10m, "USD"), new(5m, "USD")];

Money total = payments.Sum();                 // Money(15, "USD"), no Money.Zero needed
decimal amount = payments.Sum(m => m.Amount); // 15
```

Your operator decides what happens with invalid input. Here, mixing currencies throws `InvalidOperationException` from the `+` operator.

`Average` and `Median` need more than addition. `Average` requires `INumberBase<T>` (it divides by the count, converted with `T.CreateChecked`), and `Median` requires `INumber<T>` (it sorts the elements). For a type like `Money`, project to the amount first: `payments.Average(m => m.Amount)`.

## Average

`Average` returns the same type as the elements for floating-point, `decimal` and user-defined numeric types. For `int` and `long`, there are dedicated overloads that return `double`, which matches the result types of `Enumerable.Average`.

```csharp
NonEmptyList<int> ints = [1, 2];
NonEmptyList<decimal> prices = [1m, 2m];

double a = ints.Average();                 // 1.5
decimal b = prices.Average();              // 1.5
double c = ints.Average(x => x * 2);       // 3, a selector returning int gives double
decimal d = ints.Average(x => (decimal)x); // 1.5, a selector returning decimal gives decimal
```

The `int` overloads add up in a `long` and the `long` overloads add up in a `decimal`, so a large list of large values does not overflow on the way to the average.

:::warning

Only `int` and `long` have the `double` overloads. Other integer types such as `short`, `byte` or `uint` use the generic `Average<T>()`, which returns `T` and therefore performs integer division: the average of `[1, 2]` as `short` is `1`. Convert to a floating-point type first, for example `values.Average(x => (double)x)`.

:::

## Median

`Median` sorts a copy of the list and returns the middle element. For an even number of elements, it averages the two middle values. The list itself is not reordered.

```csharp
NonEmptyList<int> odd = [5, 1, 3];
NonEmptyList<int> even = [4, 1, 3, 2];
NonEmptyList<double> doubles = [2.0, 1.0];

double m1 = odd.Median();      // 3
double m2 = even.Median();     // 2.5
double m3 = doubles.Median();  // 1.5
```

As with `Average`, only `int` and `long` lists return `double`. For other integer types, the generic `Median<T>()` returns `T`, and averaging the two middle values uses integer division.

## Mode

`Mode` returns the most frequent element. When several elements share the highest count, the one that appears first in the list wins. With a comparer, the element returned is the first occurrence from the winning group.

```csharp
NonEmptyList<string> tags = ["a", "b", "b", "c", "a"];

string common = tags.Mode();                                                    // "a": tied with "b", appears first
string folded = NonEmptyList.Of("A", "a", "b").Mode(StringComparer.OrdinalIgnoreCase); // "A"
```

## Max, Min and MinMax

`Max` and `Min` use `Comparer<T>.Default` unless you pass a comparer, so they work for any comparable type, not just numbers. They return `T`, never `T?`, and never throw for lack of elements. `MinMax` finds both in a single pass.

```csharp
NonEmptyList<DateOnly> dates = [new(2024, 5, 1), new(2023, 1, 9), new(2025, 2, 3)];

DateOnly latest = dates.Max();
(DateOnly earliest, DateOnly last) = dates.MinMax();

NonEmptyList<string> words = ["pear", "Apple", "fig"];
string first = words.Min(StringComparer.OrdinalIgnoreCase);   // "Apple"
int longest = words.Max(w => w.Length);                        // 5
```

To get the element with the largest or smallest key rather than the key itself, use the instance methods `MaxBy(keySelector)` and `MinBy(keySelector)`.

## Why the right overload is chosen

These methods take a `NonEmptyList<T>` receiver, so the compiler prefers them over the `Enumerable` methods with the same name. `scores.Sum()` binds to the checked, seedless version and `scores.Max()` returns `int`, not `int?`. See [How the compiler picks these operators](./linq.md#how-the-compiler-picks-these-operators).

Once the static type is an `IEnumerable<T>`, for example after `Where`, you are back on `Enumerable`. `Enumerable.Sum` returns `0` for an empty sequence and `Enumerable.Average` throws `InvalidOperationException`:

```csharp
NonEmptyList<int> scores = [70, 85, 90];

double passMean = scores.Where(s => s >= 80).Average();   // Enumerable.Average: throws if nothing passes

NonEmptyList<int>? passed = scores.Where(s => s >= 80).ToNonEmptyListOrNull();
double? safeMean = passed?.Average();                     // non-empty version, or null
```

## On spans

`NonEmptySpan<T>` has its own allocation-free `Sum` (checked, `T : IAdditionOperators<T, T, T>`) and `Average` (`T : INumberBase<T>`), plus `Max` and `Min`. Its `Average` always returns `T`, so an `int` span averages with integer division. See [Spans and lazy sequences](./spans-and-lazy-sequences.md).
