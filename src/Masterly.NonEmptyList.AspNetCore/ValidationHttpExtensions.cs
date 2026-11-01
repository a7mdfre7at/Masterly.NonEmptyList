using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Masterly.NonEmptyList.AspNetCore;

/// <summary>
/// Turns <see cref="Validation{T, TError}"/> results into HTTP responses. Because the error list is non-empty by construction,
/// an invalid result always produces a problem-details body with at least one error.
/// </summary>
public static class ValidationHttpExtensions
{
    /// <summary>Converts accumulated errors into RFC 7807 validation problem details, grouped by <see cref="ValidationError.Path"/>.</summary>
    /// <param name="errors">The errors.</param>
    /// <param name="title">An optional title. Defaults to "One or more validation errors occurred.".</param>
    /// <param name="status">The HTTP status code. Defaults to 400.</param>
    public static HttpValidationProblemDetails ToProblemDetails(this NonEmptyList<ValidationError> errors, string? title = null, int status = StatusCodes.Status400BadRequest)
    {
        ArgumentNullException.ThrowIfNull(errors);
        HttpValidationProblemDetails details = new(errors.ToErrorDictionary())
        {
            Status = status,
            Title = title ?? "One or more validation errors occurred.",
        };

        details.Extensions["codes"] = errors.Select(static e => e.Code).Distinct().ToArray();
        return details;
    }

    /// <summary>Returns a 400 validation problem response for the errors.</summary>
    public static IResult ToValidationProblem(this NonEmptyList<ValidationError> errors, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return Results.ValidationProblem(errors.ToErrorDictionary(), title: title ?? "One or more validation errors occurred.", extensions: new Dictionary<string, object?>
        {
            ["codes"] = errors.Select(static e => e.Code).Distinct().ToArray(),
        });
    }

    /// <summary>Maps a valid value with <paramref name="onValid"/>, or returns a 400 validation problem.</summary>
    public static IResult ToResult<T>(this Validation<T, ValidationError> validation, Func<T, IResult> onValid)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(onValid);
        return validation.Match(onValid, static errors => errors.ToValidationProblem());
    }

    /// <summary>Returns 200 OK with the value, or a 400 validation problem.</summary>
    public static IResult ToResult<T>(this Validation<T, ValidationError> validation)
        => validation.ToResult(static value => Results.Ok(value));

    /// <summary>Returns 201 Created at <paramref name="location"/> with the value, or a 400 validation problem.</summary>
    public static IResult ToCreatedResult<T>(this Validation<T, ValidationError> validation, Func<T, string> location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return validation.ToResult(value => Results.Created(location(value), value));
    }

    /// <summary>Awaits the validation, then maps it like <see cref="ToResult{T}(Validation{T, ValidationError}, Func{T, IResult})"/>.</summary>
    public static async Task<IResult> ToResultAsync<T>(this Task<Validation<T, ValidationError>> validation, Func<T, IResult> onValid)
    {
        ArgumentNullException.ThrowIfNull(validation);
        return (await validation.ConfigureAwait(false)).ToResult(onValid);
    }

    /// <summary>Awaits the validation, then returns 200 OK or a 400 validation problem.</summary>
    public static async Task<IResult> ToResultAsync<T>(this Task<Validation<T, ValidationError>> validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        return (await validation.ConfigureAwait(false)).ToResult();
    }

    /// <summary>MVC: maps a valid value with <paramref name="onValid"/>, or returns a <see cref="BadRequestObjectResult"/> with validation problem details.</summary>
    public static ActionResult ToActionResult<T>(this Validation<T, ValidationError> validation, Func<T, ActionResult> onValid)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(onValid);
        return validation.Match(onValid, static errors => new BadRequestObjectResult(errors.ToProblemDetails()));
    }

    /// <summary>MVC: returns <see cref="OkObjectResult"/> with the value, or a <see cref="BadRequestObjectResult"/> with validation problem details.</summary>
    public static ActionResult ToActionResult<T>(this Validation<T, ValidationError> validation)
        => validation.ToActionResult(static value => new OkObjectResult(value));

    /// <summary>MVC: adds every error to the model state under its <see cref="ValidationError.Path"/>.</summary>
    public static ModelStateDictionary AddErrors(this ModelStateDictionary modelState, NonEmptyList<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(errors);
        foreach (ValidationError error in errors)
            modelState.AddModelError(error.Path ?? string.Empty, error.Message);
        return modelState;
    }

    /// <summary>MVC: adds the errors of an invalid validation to the model state and returns whether it was valid.</summary>
    public static bool TryGetValue<T>(this Validation<T, ValidationError> validation, ModelStateDictionary modelState, out T value)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(modelState);

        if (validation.TryGetValue(out value!))
            return true;

        modelState.AddErrors(validation.Errors);
        return false;
    }
}
