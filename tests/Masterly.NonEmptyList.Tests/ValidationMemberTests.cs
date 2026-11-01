namespace Masterly.NonEmptyList.Tests;

public class ValidationMemberTests
{
    private static readonly Validation<int, string> Ok = Validation<int, string>.Valid(5);
    private static readonly Validation<int, string> Bad = Validation<int, string>.Invalid("e1", "e2");

    #region Accessors

    [Fact]
    public void GetValueOrDefault_ShouldReturnValueWhenValidAndFallbackWhenInvalid()
    {
        Assert.Equal(5, Ok.GetValueOrDefault(-1));
        Assert.Equal(-1, Bad.GetValueOrDefault(-1));
    }

    [Fact]
    public void GetValueOr_ShouldOnlyCallFallbackWhenInvalid()
    {
        Assert.Equal(5, Ok.GetValueOr(_ => throw new InvalidOperationException("fallback must not run")));
        Assert.Equal(2, Bad.GetValueOr(errors => errors.Count));
    }

    [Fact]
    public void GetValueOrThrow_ShouldReturnValueOrThrowTheFactoryException()
    {
        Assert.Equal(5, Ok.GetValueOrThrow(_ => new InvalidOperationException()));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => Bad.GetValueOrThrow(errors => new InvalidOperationException(string.Join("|", errors))));
        Assert.Equal("e1|e2", ex.Message);
    }

    [Fact]
    public void MatchAction_ShouldInvokeOnlyTheMatchingBranch()
    {
        List<string> calls = new();

        Ok.Match(v => calls.Add($"valid {v}"), e => calls.Add($"invalid {e.Count}"));
        Bad.Match(v => calls.Add($"valid {v}"), e => calls.Add($"invalid {e.Count}"));

        Assert.Equal(new[] { "valid 5", "invalid 2" }, calls);
    }

    [Fact]
    public void MatchAction_WithNullDelegates_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => Ok.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => Ok.Match(_ => { }, null!));
    }

    #endregion

    #region Transformations

    [Fact]
    public void Map_OnInvalid_ShouldKeepErrorsAndSkipSelector()
    {
        Validation<string, string> mapped = Bad.Map<string>(_ => throw new InvalidOperationException("must not run"));

        Assert.True(mapped.IsInvalid);
        Assert.Equal(new[] { "e1", "e2" }, mapped.Errors);
    }

    [Fact]
    public void MapErrors_OnValid_ShouldKeepValueAndSkipSelector()
    {
        Validation<int, int> mapped = Ok.MapErrors<int>(_ => throw new InvalidOperationException("must not run"));

        Assert.True(mapped.IsValid);
        Assert.Equal(5, mapped.Value);
    }

    [Fact]
    public void Ensure_OnInvalid_ShouldReturnSameInstanceWithoutCallingPredicate()
    {
        Assert.Same(Bad, Bad.Ensure(_ => throw new InvalidOperationException(), "x"));
        Assert.Same(Bad, Bad.Ensure(_ => throw new InvalidOperationException(), _ => "x"));
    }

    [Fact]
    public void EnsureWithFactory_WhenPredicatePasses_ShouldReturnSameInstance()
    {
        Validation<int, string> result = Ok.Ensure(v => v > 0, v => $"{v} bad");

        Assert.Same(Ok, result);
    }

    [Fact]
    public void EnsureWithFactory_WhenPredicateFails_ShouldBuildTheErrorFromTheValue()
    {
        Validation<int, string> result = Ok.Ensure(v => v > 10, v => $"{v} is too small");

        Assert.Equal(new[] { "5 is too small" }, result.Errors);
    }

    [Fact]
    public void Tap_ShouldRunActionOnlyWhenValidAndReturnSameInstance()
    {
        List<int> seen = new();

        Assert.Same(Ok, Ok.Tap(seen.Add));
        Assert.Same(Bad, Bad.Tap(seen.Add));

        Assert.Equal(new[] { 5 }, seen);
        Assert.Throws<ArgumentNullException>(() => Ok.Tap(null!));
    }

    [Fact]
    public void TapErrors_ShouldRunActionOnlyWhenInvalidAndReturnSameInstance()
    {
        List<int> seen = new();

        Assert.Same(Ok, Ok.TapErrors(e => seen.Add(e.Count)));
        Assert.Same(Bad, Bad.TapErrors(e => seen.Add(e.Count)));

        Assert.Equal(new[] { 2 }, seen);
        Assert.Throws<ArgumentNullException>(() => Ok.TapErrors(null!));
    }

    #endregion

    #region Equality and formatting

    [Fact]
    public void Equals_ShouldCompareStateValueAndErrors()
    {
        Assert.True(Ok.Equals(Validation<int, string>.Valid(5)));
        Assert.False(Ok.Equals(Validation<int, string>.Valid(6)));
        Assert.True(Bad.Equals(Validation<int, string>.Invalid("e1", "e2")));
        Assert.False(Bad.Equals(Validation<int, string>.Invalid("e1")));
        Assert.False(Ok.Equals(Bad));
        Assert.False(Ok.Equals((Validation<int, string>?)null));
        Assert.True(Ok.Equals(Ok));

        // A valid 0 and an invalid result (whose value is default(int) == 0) must differ by state alone.
        Assert.False(Validation<int, string>.Valid(0).Equals(Validation<int, string>.Invalid("e")));
        Assert.False(Validation<int, string>.Invalid("e").Equals(Validation<int, string>.Valid(0)));

        // Same number of errors: order and each element matter.
        Assert.False(Bad.Equals(Validation<int, string>.Invalid("e2", "e1")));
        Assert.False(Bad.Equals(Validation<int, string>.Invalid("e1", "eX")));
    }

    [Fact]
    public void EqualsObject_ShouldRequireSameValidationType()
    {
        Assert.True(Ok.Equals((object)Validation<int, string>.Valid(5)));
        Assert.False(Ok.Equals((object)5));
        Assert.False(Ok.Equals((object?)null));
    }

    [Fact]
    public void GetHashCode_ShouldMatchForEqualValidations()
    {
        Assert.Equal(Validation<int, string>.Valid(5).GetHashCode(), Ok.GetHashCode());
        Assert.Equal(Validation<int, string>.Invalid("e1", "e2").GetHashCode(), Bad.GetHashCode());
    }

    [Fact]
    public void EqualityOperators_ShouldHandleNulls()
    {
        Validation<int, string>? none = null;

        Assert.True(none == null);
        Assert.False(none == Ok);
        Assert.False(Ok == none);
        Assert.True(Ok == Validation<int, string>.Valid(5));
        Assert.True(Ok != Bad);
    }

    [Fact]
    public void ToString_ShouldDescribeBothStates()
    {
        Assert.Equal("Valid(5)", Ok.ToString());
        Assert.Equal("Invalid([e1, e2])", Bad.ToString());
    }

    [Fact]
    public void ValidationError_ToString_ShouldIncludePathOnlyWhenPresent()
    {
        Assert.Equal("required: Name is required", new ValidationError("required", "Name is required").ToString());
        Assert.Equal("name required: Name is required", new ValidationError("required", "Name is required", "name").ToString());
    }

    #endregion

    #region Static factories and Combine

    [Fact]
    public void StaticFactories_ShouldMatchTheGenericFactories()
    {
        Assert.Equal(Validation<int, string>.Valid(1), Validation.Valid<int, string>(1));
        Assert.Equal(Validation<int, string>.Invalid("x"), Validation.Invalid<int, string>("x"));
        Assert.Equal(Validation<int, string>.Invalid("x", "y"), Validation.Invalid<int, string>(NonEmptyList.Of("x", "y")));
    }

    [Fact]
    public void Combine_FourToSeven_ShouldBuildValueWhenAllValid()
    {
        Validation<int, string> v = Validation<int, string>.Valid(1);

        Assert.Equal(4, Validation.Combine(v, v, v, v, (a, b, c, d) => a + b + c + d).Value);
        Assert.Equal(5, Validation.Combine(v, v, v, v, v, (a, b, c, d, e) => a + b + c + d + e).Value);
        Assert.Equal(6, Validation.Combine(v, v, v, v, v, v, (a, b, c, d, e, f) => a + b + c + d + e + f).Value);
        Assert.Equal(7, Validation.Combine(v, v, v, v, v, v, v, (a, b, c, d, e, f, g) => a + b + c + d + e + f + g).Value);
        Assert.Equal(8, Validation.Combine(v, v, v, v, v, v, v, v, (a, b, c, d, e, f, g, h) => a + b + c + d + e + f + g + h).Value);
    }

    [Fact]
    public void Combine_TwoToEight_ShouldAccumulateErrorsInOrder()
    {
        Validation<int, string> v = Validation<int, string>.Valid(1);
        Validation<int, string> x = Validation<int, string>.Invalid("x");
        Validation<int, string> y = Validation<int, string>.Invalid("y");

        // Valid inputs contribute no errors.
        Assert.Equal(new[] { "x", "y" }, Validation.Combine(x, y, (a, b) => 0).Errors);
        Assert.Equal(new[] { "x", "y" }, Validation.Combine(x, v, v, y, (a, b, c, d) => 0).Errors);
        Assert.Equal(new[] { "x", "y" }, Validation.Combine(x, v, v, v, v, v, v, y, (a, b, c, d, e, f, g, h) => 0).Errors);

        // Every position invalid with its own error: each argument is collected, in argument order.
        Assert.Equal(new[] { "e1", "e2", "e3" },
            Validation.Combine(I("e1"), I("e2"), I("e3"), (a, b, c) => 0).Errors);
        Assert.Equal(new[] { "e1", "e2", "e3", "e4" },
            Validation.Combine(I("e1"), I("e2"), I("e3"), I("e4"), (a, b, c, d) => 0).Errors);
        Assert.Equal(new[] { "e1", "e2", "e3", "e4", "e5" },
            Validation.Combine(I("e1"), I("e2"), I("e3"), I("e4"), I("e5"), (a, b, c, d, e) => 0).Errors);
        Assert.Equal(new[] { "e1", "e2", "e3", "e4", "e5", "e6" },
            Validation.Combine(I("e1"), I("e2"), I("e3"), I("e4"), I("e5"), I("e6"), (a, b, c, d, e, f) => 0).Errors);
        Assert.Equal(new[] { "e1", "e2", "e3", "e4", "e5", "e6", "e7" },
            Validation.Combine(I("e1"), I("e2"), I("e3"), I("e4"), I("e5"), I("e6"), I("e7"), (a, b, c, d, e, f, g) => 0).Errors);
        Assert.Equal(new[] { "e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8" },
            Validation.Combine(I("e1"), I("e2"), I("e3"), I("e4"), I("e5"), I("e6"), I("e7"), I("e8"), (a, b, c, d, e, f, g, h) => 0).Errors);

        static Validation<int, string> I(string e) => Validation<int, string>.Invalid(e);
    }

    [Fact]
    public void Combine_FourToEight_WithNullSelector_ShouldThrow()
    {
        Validation<int, string> v = Validation<int, string>.Valid(1);

        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() => Validation.Combine(v, v, v, v, (Func<int, int, int, int, int>)null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() => Validation.Combine(v, v, v, v, v, (Func<int, int, int, int, int, int>)null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() => Validation.Combine(v, v, v, v, v, v, (Func<int, int, int, int, int, int, int>)null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() => Validation.Combine(v, v, v, v, v, v, v, (Func<int, int, int, int, int, int, int, int>)null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() => Validation.Combine(v, v, v, v, v, v, v, v, (Func<int, int, int, int, int, int, int, int, int>)null!)).ParamName);
    }

    [Fact]
    public void SequenceEnumerable_WhenAllValid_ShouldReturnValues()
    {
        Validation<List<int>, string> result = new[] { Validation<int, string>.Valid(1), Validation<int, string>.Valid(2) }.Sequence();

        Assert.True(result.IsValid);
        Assert.Equal(new[] { 1, 2 }, result.Value);
    }

    #endregion
}
