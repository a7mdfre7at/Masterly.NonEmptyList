using System.Diagnostics.CodeAnalysis;

namespace Masterly.NonEmptyList;

/// <summary>
/// The result of validating a value: either a valid <typeparamref name="T"/>, or a <see cref="NonEmptyList{T}"/> of
/// <typeparamref name="TError"/>. The error list is non-empty by construction, so an "invalid result with no errors"
/// cannot be represented. Unlike a fail-fast result type, <see cref="Validation.Combine{T1,T2,TResult,TError}"/>
/// accumulates the errors of every failed input instead of stopping at the first.
/// </summary>
/// <typeparam name="T">The type of the valid value.</typeparam>
/// <typeparam name="TError">The type of a single validation error.</typeparam>
public sealed class Validation<T, TError> : IEquatable<Validation<T, TError>>, IValidationErrors<TError>
{
    private readonly T? _value;
    private readonly NonEmptyList<TError>? _errors;

    private Validation(T value)
    {
        _value = value;
    }

    private Validation(NonEmptyList<TError> errors)
    {
        _errors = errors;
    }

    #region Construction

    /// <summary>Creates a valid result.</summary>
    public static Validation<T, TError> Valid(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Validation<T, TError>(value);
    }

    /// <summary>Creates an invalid result with a single error.</summary>
    public static Validation<T, TError> Invalid(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Validation<T, TError>(new NonEmptyList<TError>(error));
    }

    /// <summary>Creates an invalid result with one or more errors.</summary>
    public static Validation<T, TError> Invalid(TError first, params TError[] rest)
        => new(new NonEmptyList<TError>(first, rest));

    /// <summary>Creates an invalid result from an existing non-empty error list.</summary>
    public static Validation<T, TError> Invalid(NonEmptyList<TError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new Validation<T, TError>(errors);
    }

    /// <summary>Converts a value to a valid result.</summary>
    public static implicit operator Validation<T, TError>(T value) => Valid(value);

    #endregion

    #region State

    /// <summary>True when the value is valid.</summary>
    public bool IsValid => _errors is null;

    /// <summary>True when there is at least one error.</summary>
    public bool IsInvalid => _errors is not null;

    NonEmptyList<TError>? IValidationErrors<TError>.ErrorsOrNull => _errors;

    /// <summary>
    /// Gets the valid value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is invalid.</exception>
    public T Value => IsValid
        ? _value!
        : throw new InvalidOperationException($"Validation failed with {_errors!.Count} error(s): {string.Join("; ", _errors)}");

    /// <summary>
    /// Gets the errors. Always contains at least one element.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is valid.</exception>
    public NonEmptyList<TError> Errors => _errors ?? throw new InvalidOperationException("Validation succeeded; there are no errors.");

    /// <summary>Attempts to get the valid value.</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return IsValid;
    }

    /// <summary>Attempts to get the errors.</summary>
    public bool TryGetErrors([MaybeNullWhen(false)] out NonEmptyList<TError> errors)
    {
        errors = _errors;
        return IsInvalid;
    }

    /// <summary>Returns the valid value, or the fallback when invalid.</summary>
    public T GetValueOrDefault(T fallback) => IsValid ? _value! : fallback;

    /// <summary>Returns the valid value, or the result of the fallback function when invalid.</summary>
    public T GetValueOr(Func<NonEmptyList<TError>, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return IsValid ? _value! : fallback(_errors!);
    }

    /// <summary>Returns the valid value, or throws the exception produced by the factory when invalid.</summary>
    public T GetValueOrThrow(Func<NonEmptyList<TError>, Exception> exceptionFactory)
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);
        return IsValid ? _value! : throw exceptionFactory(_errors!);
    }

    #endregion

    #region Matching

    /// <summary>Handles both cases and returns a result.</summary>
    public TResult Match<TResult>(Func<T, TResult> valid, Func<NonEmptyList<TError>, TResult> invalid)
    {
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);

        return IsValid ? valid(_value!) : invalid(_errors!);
    }

    /// <summary>Handles both cases.</summary>
    public void Match(Action<T> valid, Action<NonEmptyList<TError>> invalid)
    {
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);

        if (IsValid)
            valid(_value!);
        else
            invalid(_errors!);
    }

    #endregion

    #region Transformation

    /// <summary>Transforms the valid value. Errors pass through unchanged.</summary>
    public Validation<TResult, TError> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return IsValid
            ? Validation<TResult, TError>.Valid(selector(_value!))
            : Validation<TResult, TError>.Invalid(_errors!);
    }

    /// <summary>Transforms every error. A valid value passes through unchanged.</summary>
    public Validation<T, TNewError> MapErrors<TNewError>(Func<TError, TNewError> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return IsValid
            ? Validation<T, TNewError>.Valid(_value!)
            : Validation<T, TNewError>.Invalid(_errors!.Map(selector));
    }

    /// <summary>
    /// Chains a dependent validation. This is fail-fast: if this result is invalid, <paramref name="binder"/> is not called.
    /// Use <see cref="Validation.Combine{T1,T2,TResult,TError}"/> to accumulate errors from independent validations.
    /// </summary>
    public Validation<TResult, TError> Bind<TResult>(Func<T, Validation<TResult, TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return IsValid
            ? binder(_value!)
            : Validation<TResult, TError>.Invalid(_errors!);
    }

    /// <summary>
    /// Adds an error when the predicate fails. Existing errors are kept, so several <c>Ensure</c> calls accumulate.
    /// The predicate is only evaluated when the result is currently valid.
    /// </summary>
    public Validation<T, TError> Ensure(Func<T, bool> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        if (IsInvalid)
            return this;

        return predicate(_value!) ? this : Invalid(error);
    }

    /// <summary>
    /// Adds an error produced from the value when the predicate fails.
    /// </summary>
    public Validation<T, TError> Ensure(Func<T, bool> predicate, Func<T, TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (IsInvalid)
            return this;

        return predicate(_value!) ? this : Invalid(errorFactory(_value!));
    }

    /// <summary>Runs an action on the valid value and returns this result unchanged.</summary>
    public Validation<T, TError> Tap(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsValid)
            action(_value!);

        return this;
    }

    /// <summary>Runs an action on the errors and returns this result unchanged.</summary>
    public Validation<T, TError> TapErrors(Action<NonEmptyList<TError>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsInvalid)
            action(_errors!);

        return this;
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(Validation<T, TError>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (IsValid != other.IsValid) return false;

        return IsValid
            ? EqualityComparer<T>.Default.Equals(_value, other._value)
            : _errors!.Equals(other._errors);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Validation<T, TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => IsValid ? HashCode.Combine(true, _value) : HashCode.Combine(false, _errors);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(Validation<T, TError>? left, Validation<T, TError>? right)
        => left is null ? right is null : left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(Validation<T, TError>? left, Validation<T, TError>? right) => !(left == right);

    /// <inheritdoc />
    public override string ToString() => IsValid
        ? $"Valid({_value})"
        : $"Invalid([{string.Join(", ", _errors!)}])";

    #endregion
}

/// <summary>
/// Factory and combinator methods for <see cref="Validation{T,TError}"/>.
/// </summary>
public static class Validation
{
    #region Factories

    /// <summary>Creates a valid result.</summary>
    public static Validation<T, TError> Valid<T, TError>(T value) => Validation<T, TError>.Valid(value);

    /// <summary>Creates an invalid result with a single error.</summary>
    public static Validation<T, TError> Invalid<T, TError>(TError error) => Validation<T, TError>.Invalid(error);

    /// <summary>Creates an invalid result from a non-empty error list.</summary>
    public static Validation<T, TError> Invalid<T, TError>(NonEmptyList<TError> errors) => Validation<T, TError>.Invalid(errors);

    /// <summary>Validates a value against a predicate.</summary>
    public static Validation<T, TError> Ensure<T, TError>(T value, Func<T, bool> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return predicate(value) ? Validation<T, TError>.Valid(value) : Validation<T, TError>.Invalid(error);
    }

    /// <summary>Validates that a reference is not null.</summary>
    public static Validation<T, TError> NotNull<T, TError>(T? value, TError error) where T : class
        => value is null ? Validation<T, TError>.Invalid(error) : Validation<T, TError>.Valid(value);

    /// <summary>Validates that a nullable value has a value.</summary>
    public static Validation<T, TError> HasValue<T, TError>(T? value, TError error) where T : struct
        => value.HasValue ? Validation<T, TError>.Valid(value.Value) : Validation<T, TError>.Invalid(error);

    /// <summary>Validates that a string is not null, empty or whitespace.</summary>
    public static Validation<string, TError> NotBlank<TError>(string? value, TError error)
        => string.IsNullOrWhiteSpace(value) ? Validation<string, TError>.Invalid(error) : Validation<string, TError>.Valid(value);

    /// <summary>Validates that a sequence has at least one element, producing a <see cref="NonEmptyList{T}"/>.</summary>
    public static Validation<NonEmptyList<T>, TError> NonEmpty<T, TError>(IEnumerable<T>? source, TError error)
        => NonEmptyList<T>.TryFrom(source, out NonEmptyList<T>? list)
            ? Validation<NonEmptyList<T>, TError>.Valid(list)
            : Validation<NonEmptyList<T>, TError>.Invalid(error);

    #endregion

    #region Combine

    /// <summary>
    /// Combines two independent validations. If both are valid the selector produces the result;
    /// otherwise every error from every invalid input is returned.
    /// </summary>
    public static Validation<TResult, TError> Combine<T1, T2, TResult, TError>(
        Validation<T1, TError> v1,
        Validation<T2, TError> v2,
        Func<T1, T2, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        NonEmptyList<TError>? errors = CollectErrors(v1, v2);
        return errors is null
            ? Validation<TResult, TError>.Valid(selector(v1.Value, v2.Value))
            : Validation<TResult, TError>.Invalid(errors);
    }

    /// <summary>Combines three independent validations, accumulating all errors.</summary>
    public static Validation<TResult, TError> Combine<T1, T2, T3, TResult, TError>(
        Validation<T1, TError> v1,
        Validation<T2, TError> v2,
        Validation<T3, TError> v3,
        Func<T1, T2, T3, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        NonEmptyList<TError>? errors = CollectErrors(v1, v2, v3);
        return errors is null
            ? Validation<TResult, TError>.Valid(selector(v1.Value, v2.Value, v3.Value))
            : Validation<TResult, TError>.Invalid(errors);
    }

    /// <summary>Combines four independent validations, accumulating all errors.</summary>
    public static Validation<TResult, TError> Combine<T1, T2, T3, T4, TResult, TError>(
        Validation<T1, TError> v1,
        Validation<T2, TError> v2,
        Validation<T3, TError> v3,
        Validation<T4, TError> v4,
        Func<T1, T2, T3, T4, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        NonEmptyList<TError>? errors = CollectErrors(v1, v2, v3, v4);
        return errors is null
            ? Validation<TResult, TError>.Valid(selector(v1.Value, v2.Value, v3.Value, v4.Value))
            : Validation<TResult, TError>.Invalid(errors);
    }

    /// <summary>Combines five independent validations, accumulating all errors.</summary>
    public static Validation<TResult, TError> Combine<T1, T2, T3, T4, T5, TResult, TError>(
        Validation<T1, TError> v1,
        Validation<T2, TError> v2,
        Validation<T3, TError> v3,
        Validation<T4, TError> v4,
        Validation<T5, TError> v5,
        Func<T1, T2, T3, T4, T5, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        NonEmptyList<TError>? errors = CollectErrors(v1, v2, v3, v4, v5);
        return errors is null
            ? Validation<TResult, TError>.Valid(selector(v1.Value, v2.Value, v3.Value, v4.Value, v5.Value))
            : Validation<TResult, TError>.Invalid(errors);
    }

    /// <summary>Combines six independent validations, accumulating all errors.</summary>
    public static Validation<TResult, TError> Combine<T1, T2, T3, T4, T5, T6, TResult, TError>(
        Validation<T1, TError> v1,
        Validation<T2, TError> v2,
        Validation<T3, TError> v3,
        Validation<T4, TError> v4,
        Validation<T5, TError> v5,
        Validation<T6, TError> v6,
        Func<T1, T2, T3, T4, T5, T6, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        NonEmptyList<TError>? errors = CollectErrors(v1, v2, v3, v4, v5, v6);
        return errors is null
            ? Validation<TResult, TError>.Valid(selector(v1.Value, v2.Value, v3.Value, v4.Value, v5.Value, v6.Value))
            : Validation<TResult, TError>.Invalid(errors);
    }

    /// <summary>Combines seven independent validations, accumulating all errors.</summary>
    public static Validation<TResult, TError> Combine<T1, T2, T3, T4, T5, T6, T7, TResult, TError>(
        Validation<T1, TError> v1,
        Validation<T2, TError> v2,
        Validation<T3, TError> v3,
        Validation<T4, TError> v4,
        Validation<T5, TError> v5,
        Validation<T6, TError> v6,
        Validation<T7, TError> v7,
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        NonEmptyList<TError>? errors = CollectErrors(v1, v2, v3, v4, v5, v6, v7);
        return errors is null
            ? Validation<TResult, TError>.Valid(selector(v1.Value, v2.Value, v3.Value, v4.Value, v5.Value, v6.Value, v7.Value))
            : Validation<TResult, TError>.Invalid(errors);
    }

    /// <summary>Combines eight independent validations, accumulating all errors.</summary>
    public static Validation<TResult, TError> Combine<T1, T2, T3, T4, T5, T6, T7, T8, TResult, TError>(
        Validation<T1, TError> v1,
        Validation<T2, TError> v2,
        Validation<T3, TError> v3,
        Validation<T4, TError> v4,
        Validation<T5, TError> v5,
        Validation<T6, TError> v6,
        Validation<T7, TError> v7,
        Validation<T8, TError> v8,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        NonEmptyList<TError>? errors = CollectErrors(v1, v2, v3, v4, v5, v6, v7, v8);
        return errors is null
            ? Validation<TResult, TError>.Valid(selector(v1.Value, v2.Value, v3.Value, v4.Value, v5.Value, v6.Value, v7.Value, v8.Value))
            : Validation<TResult, TError>.Invalid(errors);
    }

    private static NonEmptyList<TError>? CollectErrors<TError>(params IValidationErrors<TError>[] validations)
    {
        List<TError> errors = new();
        foreach (IValidationErrors<TError> validation in validations)
        {
            if (validation.ErrorsOrNull is { } list)
                errors.AddRange(list.Items);
        }

        return errors.Count == 0 ? null : NonEmptyList<TError>.FromTrustedList(errors);
    }

    #endregion

    #region Sequence and Traverse

    /// <summary>
    /// Turns a non-empty list of validations into a validation of a non-empty list. All errors are accumulated.
    /// </summary>
    public static Validation<NonEmptyList<T>, TError> Sequence<T, TError>(this NonEmptyList<Validation<T, TError>> validations)
    {
        ArgumentNullException.ThrowIfNull(validations);

        List<T> values = new(validations.Count);
        List<TError> errors = new();

        foreach (Validation<T, TError> validation in validations.Items)
        {
            if (validation.TryGetValue(out T? value))
                values.Add(value);
            else
                errors.AddRange(validation.Errors.Items);
        }

        return errors.Count == 0
            ? Validation<NonEmptyList<T>, TError>.Valid(NonEmptyList<T>.FromTrustedList(values))
            : Validation<NonEmptyList<T>, TError>.Invalid(NonEmptyList<TError>.FromTrustedList(errors));
    }

    /// <summary>
    /// Turns a sequence of validations into a validation of a list. All errors are accumulated.
    /// </summary>
    public static Validation<List<T>, TError> Sequence<T, TError>(this IEnumerable<Validation<T, TError>> validations)
    {
        ArgumentNullException.ThrowIfNull(validations);

        List<T> values = new();
        List<TError> errors = new();

        foreach (Validation<T, TError> validation in validations)
        {
            if (validation.TryGetValue(out T? value))
                values.Add(value);
            else
                errors.AddRange(validation.Errors.Items);
        }

        return errors.Count == 0
            ? Validation<List<T>, TError>.Valid(values)
            : Validation<List<T>, TError>.Invalid(NonEmptyList<TError>.FromTrustedList(errors));
    }

    /// <summary>
    /// Validates every element of a non-empty list and collects the results into a non-empty list. All errors are accumulated.
    /// </summary>
    public static Validation<NonEmptyList<TResult>, TError> Traverse<T, TResult, TError>(
        this NonEmptyList<T> source,
        Func<T, Validation<TResult, TError>> validator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(validator);

        return source.Map(validator).Sequence();
    }

    /// <summary>
    /// Validates every element of a sequence and collects the results into a list. All errors are accumulated.
    /// </summary>
    public static Validation<List<TResult>, TError> Traverse<T, TResult, TError>(
        this IEnumerable<T> source,
        Func<T, Validation<TResult, TError>> validator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(validator);

        return source.Select(validator).Sequence();
    }

    #endregion
}

/// <summary>
/// Internal view of a validation's errors regardless of its value type, used to accumulate errors across different <typeparamref name="TError"/> carriers.
/// </summary>
internal interface IValidationErrors<TError>
{
    NonEmptyList<TError>? ErrorsOrNull { get; }
}

/// <summary>
/// A general-purpose validation error with a machine-readable code, a human-readable message and an optional
/// property path. Pairs with <see cref="ValidationErrorExtensions.ToErrorDictionary"/> for ASP.NET Core problem details.
/// </summary>
/// <param name="Code">A stable identifier for the kind of error, for example <c>"required"</c>.</param>
/// <param name="Message">A description suitable for display.</param>
/// <param name="Path">The property or field the error refers to, or null for the whole object.</param>
public sealed record ValidationError(string Code, string Message, string? Path = null)
{
    /// <inheritdoc />
    public override string ToString() => Path is null ? $"{Code}: {Message}" : $"{Path} {Code}: {Message}";
}

/// <summary>
/// Helpers for working with lists of <see cref="ValidationError"/>.
/// </summary>
public static class ValidationErrorExtensions
{
    /// <summary>
    /// Groups errors by path into the shape expected by <c>ValidationProblemDetails</c>: a dictionary from property path to messages.
    /// Errors without a path are stored under the empty string.
    /// </summary>
    public static Dictionary<string, string[]> ToErrorDictionary(this NonEmptyList<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return errors.Items
            .GroupBy(e => e.Path ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
    }
}
