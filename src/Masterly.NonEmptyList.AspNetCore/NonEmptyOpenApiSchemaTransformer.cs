#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif

namespace Masterly.NonEmptyList.AspNetCore;

/// <summary>
/// Makes the non-empty guarantee visible in the OpenAPI document: <c>minItems: 1</c> for every non-empty collection,
/// <c>uniqueItems: true</c> for sets and <c>Distinct</c> constraints, <c>minItems</c>/<c>maxItems</c> derived from
/// <c>AtLeast</c>, <c>AtMost</c> and <c>Exactly</c>, and <c>minLength: 1</c> for <see cref="NonEmptyString"/>.
/// Register with <c>builder.Services.AddOpenApi(o =&gt; o.AddNonEmptySchemas())</c>.
/// </summary>
public sealed class NonEmptyOpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc />
    public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        Type type = context.JsonTypeInfo.Type;
        Apply(schema, type);

        // The non-empty types use custom JSON converters, so the generator does not know their JSON shape. Fill it in.
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        NonEmptyShape? shape = NonEmptyShape.Of(underlying);
#if NET10_0_OR_GREATER
        if (underlying == typeof(NonEmptyString))
        {
            schema.Type ??= JsonSchemaType.String;
        }
        else if (shape is { IsDictionary: true })
        {
            schema.Type ??= JsonSchemaType.Object;
            schema.AdditionalProperties ??= await context.GetOrCreateSchemaAsync(shape.ElementType, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else if (shape is not null)
        {
            schema.Type ??= JsonSchemaType.Array;
            schema.Items ??= await context.GetOrCreateSchemaAsync(shape.ElementType, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
#else
        if (underlying == typeof(NonEmptyString))
            schema.Type ??= "string";
        else if (shape is { IsDictionary: true })
            schema.Type ??= "object";
        else if (shape is not null)
            schema.Type ??= "array";
        await Task.CompletedTask.ConfigureAwait(false);
#endif
    }

    /// <summary>Applies the non-empty constraints for <paramref name="type"/> to <paramref name="schema"/>, if it is a non-empty type.</summary>
    public static void Apply(OpenApiSchema schema, Type type)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(type);

        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(NonEmptyString))
        {
            schema.MinLength = Math.Max(schema.MinLength ?? 0, 1);
            return;
        }

        NonEmptyShape? shape = NonEmptyShape.Of(underlying);
        if (shape is null)
            return;

        if (shape.IsDictionary)
        {
            schema.MinProperties = Math.Max(schema.MinProperties ?? 0, 1);
            return;
        }

        schema.MinItems = Math.Max(schema.MinItems ?? 0, shape.MinItems);
        if (shape.MaxItems is { } max)
            schema.MaxItems = schema.MaxItems is { } existing ? Math.Min(existing, max) : max;
        if (shape.UniqueItems)
            schema.UniqueItems = true;
    }
}

/// <summary>Registration helpers for the OpenAPI transformer.</summary>
public static class NonEmptyOpenApiExtensions
{
    /// <summary>Adds the schema transformer that documents the non-empty guarantee.</summary>
    public static OpenApiOptions AddNonEmptySchemas(this OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddSchemaTransformer<NonEmptyOpenApiSchemaTransformer>();
        return options;
    }
}
#endif
