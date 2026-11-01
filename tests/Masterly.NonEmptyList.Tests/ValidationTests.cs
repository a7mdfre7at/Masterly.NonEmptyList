namespace Masterly.NonEmptyList.Tests;

public class ValidationTests
{
    private sealed record User(string Name, string Email, int Age);

    private static Validation<string, ValidationError> ValidateName(string? name) =>
        Validation.NotBlank(name, new ValidationError("required", "Name is required", "name"));

    private static Validation<string, ValidationError> ValidateEmail(string? email) =>
        Validation.NotBlank(email, new ValidationError("required", "Email is required", "email"))
            .Ensure(e => e.Contains('@'), new ValidationError("format", "Email must contain @", "email"));

    private static Validation<int, ValidationError> ValidateAge(int age) =>
        Validation.Ensure(age, a => a >= 18, new ValidationError("range", "Must be an adult", "age"));

    [Fact]
    public void Valid_ShouldExposeValue()
    {
        Validation<int, string> result = Validation<int, string>.Valid(5);

        Assert.True(result.IsValid);
        Assert.False(result.IsInvalid);
        Assert.Equal(5, result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Errors);
        Assert.Equal("Valid(5)", result.ToString());
    }

    [Fact]
    public void Invalid_ShouldExposeNonEmptyErrors()
    {
        Validation<int, string> result = Validation<int, string>.Invalid("bad", "worse");

        Assert.True(result.IsInvalid);
        Assert.Equal(2, result.Errors.Count);
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("2 error(s)", ex.Message);
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateValid()
    {
        Validation<int, string> result = 42;

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Combine_ShouldAccumulateAllErrors()
    {
        Validation<User, ValidationError> user = Validation.Combine(
            ValidateName(""),
            ValidateEmail("not-an-email"),
            ValidateAge(12),
            (name, email, age) => new User(name, email, age));

        Assert.True(user.IsInvalid);
        Assert.Equal(3, user.Errors.Count);
        Assert.Equal(new[] { "name", "email", "age" }, user.Errors.Select(e => e.Path).ToArray());
    }

    [Fact]
    public void Combine_ShouldBuildValueWhenAllValid()
    {
        Validation<User, ValidationError> user = Validation.Combine(
            ValidateName("Ada"),
            ValidateEmail("ada@example.com"),
            ValidateAge(36),
            (name, email, age) => new User(name, email, age));

        Assert.True(user.IsValid);
        Assert.Equal("Ada", user.Value.Name);
    }

    [Fact]
    public void Combine_WithTwoAndEightInputs_ShouldWork()
    {
        Validation<int, string> ok = 1;
        Validation<int, string> bad = Validation<int, string>.Invalid("x");

        Validation<int, string> two = Validation.Combine(ok, ok, (a, b) => a + b);
        Validation<int, string> eight = Validation.Combine(ok, bad, ok, bad, ok, ok, ok, bad, (a, b, c, d, e, f, g, h) => a);

        Assert.Equal(2, two.Value);
        Assert.Equal(3, eight.Errors.Count);
    }

    [Fact]
    public void Ensure_ShouldAddErrorOnlyWhenPredicateFails()
    {
        Validation<int, string> result = Validation<int, string>.Valid(5)
            .Ensure(x => x > 0, "must be positive")
            .Ensure(x => x > 10, "must exceed 10")
            .Ensure(x => x > 100, x => $"{x} must exceed 100");

        Assert.True(result.IsInvalid);
        Assert.Equal(new[] { "must exceed 10" }, result.Errors.ToArray());
    }

    [Fact]
    public void Map_Bind_MapErrors_ShouldTransform()
    {
        Validation<int, string> valid = 2;
        Validation<int, string> invalid = Validation<int, string>.Invalid("e1");

        Assert.Equal(4, valid.Map(x => x * 2).Value);
        Assert.Equal("E1", invalid.MapErrors(e => e.ToUpperInvariant()).Errors.Head);
        Assert.Equal("too small", valid.Bind(x => Validation<int, string>.Invalid("too small")).Errors.Head);
        Assert.Equal("e1", invalid.Bind(x => Validation<int, string>.Valid(x)).Errors.Head);
    }

    [Fact]
    public void Match_ShouldSelectBranch()
    {
        Validation<int, string> valid = 1;
        Validation<int, string> invalid = Validation<int, string>.Invalid("boom");

        Assert.Equal("ok:1", valid.Match(v => $"ok:{v}", errors => $"errors:{errors.Count}"));
        Assert.Equal("errors:1", invalid.Match(v => $"ok:{v}", errors => $"errors:{errors.Count}"));

        string? seen = null;
        invalid.Match(_ => seen = "valid", errors => seen = errors.Head);
        Assert.Equal("boom", seen);
    }

    [Fact]
    public void Sequence_And_Traverse_ShouldAccumulate()
    {
        NonEmptyList<Validation<int, string>> allValid = [1, 2, 3];
        NonEmptyList<Validation<int, string>> mixed = [1, Validation<int, string>.Invalid("a"), Validation<int, string>.Invalid("b")];

        Validation<NonEmptyList<int>, string> sequenced = allValid.Sequence();
        Validation<NonEmptyList<int>, string> failed = mixed.Sequence();
        Validation<NonEmptyList<int>, string> traversed = NonEmptyList.Of(1, 2).Traverse(x => Validation.Ensure(x, v => v > 0, "neg"));
        Validation<List<int>, string> enumerable = new[] { 1, -1, -2 }.Traverse(x => Validation.Ensure(x, v => v > 0, $"{x} is negative"));

        Assert.Equal(new[] { 1, 2, 3 }, sequenced.Value.ToArray());
        Assert.Equal(new[] { "a", "b" }, failed.Errors.ToArray());
        Assert.Equal(2, traversed.Value.Count);
        Assert.Equal(new[] { "-1 is negative", "-2 is negative" }, enumerable.Errors.ToArray());
    }

    [Fact]
    public void Helpers_ShouldValidateCommonShapes()
    {
        Assert.True(Validation.NotNull<string, string>("x", "null").IsValid);
        Assert.True(Validation.NotNull<string, string>(null, "null").IsInvalid);
        Assert.Equal(3, Validation.HasValue<int, string>(3, "missing").Value);
        Assert.True(Validation.HasValue<int, string>(null, "missing").IsInvalid);
        Assert.True(Validation.NonEmpty<int, string>(Array.Empty<int>(), "empty").IsInvalid);
        Assert.Equal(1, Validation.NonEmpty<int, string>(new[] { 1 }, "empty").Value.Head);
    }

    [Fact]
    public void GetValueOr_Variants_ShouldWork()
    {
        Validation<int, string> invalid = Validation<int, string>.Invalid("e");

        Assert.Equal(-1, invalid.GetValueOrDefault(-1));
        Assert.Equal(1, invalid.GetValueOr(errors => errors.Count));
        Assert.Throws<ApplicationException>(() => invalid.GetValueOrThrow(errors => new ApplicationException(errors.Head)));
        Assert.True(invalid.TryGetErrors(out NonEmptyList<string>? errors) && errors.Head == "e");
        Assert.False(invalid.TryGetValue(out _));
    }

    [Fact]
    public void ToErrorDictionary_ShouldGroupByPath()
    {
        NonEmptyList<ValidationError> errors =
        [
            new("required", "Name is required", "name"),
            new("format", "Bad email", "email"),
            new("format", "Email too long", "email"),
            new("conflict", "Duplicate"),
        ];

        Dictionary<string, string[]> dictionary = errors.ToErrorDictionary();

        Assert.Equal(new[] { "Bad email", "Email too long" }, dictionary["email"]);
        Assert.Equal(new[] { "Duplicate" }, dictionary[""]);
        Assert.Equal("email format: Bad email", errors[1].ToString());
    }

    [Fact]
    public void Equality_ShouldCompareStructurally()
    {
        Assert.Equal(Validation<int, string>.Valid(1), Validation<int, string>.Valid(1));
        Assert.NotEqual(Validation<int, string>.Valid(1), Validation<int, string>.Valid(2));
        Assert.Equal(Validation<int, string>.Invalid("a"), Validation<int, string>.Invalid("a"));
        Assert.True(Validation<int, string>.Invalid("a") != Validation<int, string>.Valid(1));
    }
}
