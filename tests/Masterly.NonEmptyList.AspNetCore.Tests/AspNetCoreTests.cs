using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Masterly.NonEmptyList.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Masterly.NonEmptyList.AspNetCore.Tests;

public sealed record CreateOrder(NonEmptyList<string> Lines, string Customer);

public sealed record Order(NonEmptyList<string> Lines, NonEmptyString Customer);

public sealed record Quota(NonEmptyDictionary<string, int> Limits, ConstrainedList<int, AtMost<Three>> Top);

[ApiController]
[Route("mvc")]
public sealed class MvcController : ControllerBase
{
    [HttpGet("products")]
    public ActionResult<int> Products([FromQuery] NonEmptyList<int> ids) => ids.Count;

    [HttpGet("name/{name}")]
    public ActionResult<string> Name([FromRoute] NonEmptyString name) => name.Value.ToUpperInvariant();

    [HttpPost("orders")]
    public ActionResult<Order> Create([FromBody] CreateOrder request)
        => AspNetCoreTests.Validate(request).ToActionResult(order => new OkObjectResult(order));

    [HttpPost("orders/modelstate")]
    public IActionResult CreateWithModelState([FromBody] CreateOrder request)
    {
        if (!AspNetCoreTests.Validate(request).TryGetValue(ModelState, out Order order))
            return ValidationProblem(ModelState);

        return Ok(order);
    }
}

public class AspNetCoreTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public static Validation<Order, ValidationError> Validate(CreateOrder request)
        => Validation.Combine(
            Validation.Ensure(request.Lines, l => l.All(x => x.Length > 0), new ValidationError("blank", "Lines cannot be blank", "lines")),
            Validation.NotBlank(request.Customer, new ValidationError("required", "Customer is required", "customer")),
            (lines, customer) => new Order(lines, new NonEmptyString(customer)));

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(MvcController).Assembly);
#if NET9_0_OR_GREATER
        builder.Services.AddOpenApi(options => options.AddNonEmptySchemas());
#endif

        _app = builder.Build();
        _app.MapControllers();
#if NET9_0_OR_GREATER
        _app.MapOpenApi();
#endif
        _app.MapGet("/products", (NonEmptyList<int> ids) => Results.Ok(ids.Count));
        _app.MapGet("/tags", (NonEmptySet<string> tags) => Results.Ok(tags.Count));
        _app.MapGet("/hello/{name}", (NonEmptyString name) => Results.Ok(name.Value));
        _app.MapPost("/orders", (CreateOrder request) => Validate(request).ToResult());
        _app.MapPost("/orders/created", (CreateOrder request) => Validate(request).ToCreatedResult(o => $"/orders/{o.Lines.Count}"));
        _app.MapPost("/orders/async", (CreateOrder request) => Task.FromResult(Validate(request)).ToResultAsync());
        _app.MapPost("/orders/async-map", (CreateOrder request) => Task.FromResult(Validate(request)).ToResultAsync(o => Results.Accepted(null, o.Lines.Count)));
        _app.MapPost("/echo", (Order order) => Results.Ok(order));
        _app.MapGet("/schema/{kind}", (string kind) => Results.Ok(kind));
        _app.MapPost("/quota", (Quota quota) => Results.Ok(quota.Limits.Count));

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task MinimalApi_ShouldBindNonEmptyListFromQueryAndReject400OnEmpty()
    {
        HttpResponseMessage ok = await _client.GetAsync("/products?ids=1,2,3");
        HttpResponseMessage empty = await _client.GetAsync("/products?ids=");
        HttpResponseMessage missing = await _client.GetAsync("/products");
        HttpResponseMessage garbage = await _client.GetAsync("/products?ids=1,x");
        HttpResponseMessage tags = await _client.GetAsync("/tags?tags=a,a,b");
        HttpResponseMessage emptyTags = await _client.GetAsync("/tags?tags=");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("3", await ok.Content.ReadAsStringAsync());
        Assert.Equal("2", await tags.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, emptyTags.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, garbage.StatusCode);
    }

    [Fact]
    public async Task MinimalApi_ShouldBindNonEmptyStringFromRoute()
    {
        HttpResponseMessage ok = await _client.GetAsync("/hello/world");
        HttpResponseMessage blank = await _client.GetAsync("/hello/%20%20");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("\"world\"", await ok.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
    }

    [Fact]
    public async Task MinimalApi_EmptyJsonArrayInBody_ShouldBe400NotCrash()
    {
        HttpResponseMessage ok = await _client.PostAsJsonAsync("/echo", new { lines = new[] { "a" }, customer = "ada" });
        HttpResponseMessage empty = await _client.PostAsJsonAsync("/echo", new { lines = Array.Empty<string>(), customer = "ada" });
        HttpResponseMessage blankCustomer = await _client.PostAsJsonAsync("/echo", new { lines = new[] { "a" }, customer = " " });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blankCustomer.StatusCode);
    }

    [Fact]
    public async Task ValidationResults_ShouldProduceProblemDetailsWithPathsAndCodes()
    {
        HttpResponseMessage ok = await _client.PostAsJsonAsync("/orders", new { lines = new[] { "a" }, customer = "ada" });
        HttpResponseMessage invalid = await _client.PostAsJsonAsync("/orders", new { lines = new[] { "" }, customer = "" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType!.MediaType);

        JsonDocument problem = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        JsonElement errors = problem.RootElement.GetProperty("errors");
        Assert.Equal("Lines cannot be blank", errors.GetProperty("lines")[0].GetString());
        Assert.Equal("Customer is required", errors.GetProperty("customer")[0].GetString());
        Assert.Equal(2, problem.RootElement.GetProperty("codes").GetArrayLength());
    }

    [Fact]
    public async Task CreatedAndAsyncVariants_ShouldWork()
    {
        HttpResponseMessage created = await _client.PostAsJsonAsync("/orders/created", new { lines = new[] { "a", "b" }, customer = "ada" });
        HttpResponseMessage asyncOk = await _client.PostAsJsonAsync("/orders/async", new { lines = new[] { "a" }, customer = "ada" });
        HttpResponseMessage asyncBad = await _client.PostAsJsonAsync("/orders/async", new { lines = new[] { "a" }, customer = "" });
        HttpResponseMessage accepted = await _client.PostAsJsonAsync("/orders/async-map", new { lines = new[] { "a" }, customer = "ada" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/orders/2", created.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, asyncOk.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, asyncBad.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
    }

    [Fact]
    public async Task Mvc_ShouldBindViaTypeConverterAndReturnProblemDetails()
    {
        HttpResponseMessage ok = await _client.GetAsync("/mvc/products?ids=4,5");
        HttpResponseMessage empty = await _client.GetAsync("/mvc/products?ids=");
        HttpResponseMessage name = await _client.GetAsync("/mvc/name/ada");
        HttpResponseMessage createOk = await _client.PostAsJsonAsync("/mvc/orders", new { lines = new[] { "a" }, customer = "ada" });
        HttpResponseMessage createBad = await _client.PostAsJsonAsync("/mvc/orders", new { lines = new[] { "a" }, customer = "" });
        HttpResponseMessage modelStateBad = await _client.PostAsJsonAsync("/mvc/orders/modelstate", new { lines = new[] { "" }, customer = "" });
        HttpResponseMessage modelStateOk = await _client.PostAsJsonAsync("/mvc/orders/modelstate", new { lines = new[] { "a" }, customer = "ada" });
        HttpResponseMessage emptyBody = await _client.PostAsJsonAsync("/mvc/orders", new { lines = Array.Empty<string>(), customer = "ada" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("2", await ok.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal("ADA", await name.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, createOk.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, createBad.StatusCode);
        Assert.Contains("customer", await createBad.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, modelStateBad.StatusCode);
        Assert.Contains("lines", await modelStateBad.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, modelStateOk.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, emptyBody.StatusCode);
    }

    [Fact]
    public void ProblemDetailsHelpers_ShouldExposeCodesAndStatus()
    {
        NonEmptyList<ValidationError> errors = NonEmptyList.Of(
            new ValidationError("required", "Name is required", "name"),
            new ValidationError("range", "Age must be positive", "age"),
            new ValidationError("required", "Email is required", "email"));

        HttpValidationProblemDetails details = errors.ToProblemDetails("Bad input", 422);
        ModelStateDictionary modelState = new ModelStateDictionary().AddErrors(errors);

        Assert.Equal(422, details.Status);
        Assert.Equal("Bad input", details.Title);
        Assert.Equal(3, details.Errors.Count);
        Assert.Equal(new[] { "required", "range" }, (string[])details.Extensions["codes"]!);
        Assert.Equal(3, modelState.ErrorCount);
        Assert.Equal("Name is required", modelState["name"]!.Errors[0].ErrorMessage);
        Assert.Throws<ArgumentNullException>(() => ((NonEmptyList<ValidationError>)null!).ToProblemDetails());
        Assert.Throws<ArgumentNullException>(() => ((Validation<int, ValidationError>)null!).ToResult());
    }

    [Fact]
    public void NonEmptyShape_ShouldDescribeCardinalityFromTheType()
    {
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, null, false), NonEmptyShape.Of(typeof(NonEmptyList<int>)));
        Assert.Equal(new NonEmptyShape(typeof(string), false, 1, null, false), NonEmptyShape.Of(typeof(ImmutableNonEmptyList<string>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, null, true), NonEmptyShape.Of(typeof(NonEmptySet<int>)));
        Assert.Equal(new NonEmptyShape(typeof(int), true, 1, null, false), NonEmptyShape.Of(typeof(NonEmptyDictionary<string, int>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 2, null, false), NonEmptyShape.Of(typeof(ConstrainedList<int, AtLeast<Two>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, 3, false), NonEmptyShape.Of(typeof(ConstrainedList<int, AtMost<Three>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 4, 4, false), NonEmptyShape.Of(typeof(ConstrainedList<int, Exactly<Four>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 2, 5, true), NonEmptyShape.Of(typeof(ConstrainedList<int, And<AtLeast<Two>, And<AtMost<Five>, Distinct>>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, null, false), NonEmptyShape.Of(typeof(ConstrainedList<int, Or<AtLeast<Two>, Sorted>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, null, false), NonEmptyShape.Of(typeof(ConstrainedList<int, Sorted>)));
        Assert.Null(NonEmptyShape.Of(typeof(List<int>)));
        Assert.Null(NonEmptyShape.Of(typeof(string)));
    }

    [Fact]
    public void NonEmptyShape_ShouldCombineBoundsOfAndAndOrConstraints()
    {
        Assert.Equal(new NonEmptyShape(typeof(int), false, 2, 3, false), NonEmptyShape.Of(typeof(ConstrainedList<int, And<AtMost<Three>, AtLeast<Two>>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, 3, false), NonEmptyShape.Of(typeof(ConstrainedList<int, And<AtMost<Five>, AtMost<Three>>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, 5, false), NonEmptyShape.Of(typeof(ConstrainedList<int, Or<AtMost<Two>, AtMost<Five>>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, null, false), NonEmptyShape.Of(typeof(ConstrainedList<int, Or<AtMost<Two>, Sorted>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 2, null, true), NonEmptyShape.Of(typeof(ConstrainedList<int, Or<And<Distinct, AtLeast<Two>>, And<Distinct, AtLeast<Three>>>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, null, false), NonEmptyShape.Of(typeof(ConstrainedList<int, EvenCount<Two>>)));
        Assert.Throws<ArgumentNullException>(() => NonEmptyShape.Of(null!));
    }

    [Fact]
    public void NonEmptyShape_ShouldReadACardinalityThatImplementsValueExplicitly()
    {
        Assert.Equal(new NonEmptyShape(typeof(int), false, 12, null, false), NonEmptyShape.Of(typeof(ConstrainedList<int, AtLeast<Twelve>>)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 12, 12, false), NonEmptyShape.Of(typeof(ConstrainedList<int, Exactly<Twelve>>)));
    }

    [Fact]
    public void NonEmptyShape_ShouldReadTheBoundsOfGeneratedCollections()
    {
        Assert.Equal(new NonEmptyShape(typeof(string), false, 2, 5, true), NonEmptyShape.Of(typeof(Crew)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 1, null, false), NonEmptyShape.Of(typeof(Unbounded)));
        Assert.Equal(new NonEmptyShape(typeof(int), false, 3, null, false), NonEmptyShape.Of(typeof(Roster)));
    }

    [Fact]
    public void MvcHelpers_ShouldMapValidAndInvalidResults()
    {
        Validation<int, ValidationError> valid = Validation.Valid<int, ValidationError>(5);
        Validation<int, ValidationError> invalid = Validation.Invalid<int, ValidationError>(new ValidationError("bad", "Bad value"));
        ModelStateDictionary modelState = new();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(valid.ToActionResult());
        BadRequestObjectResult bad = Assert.IsType<BadRequestObjectResult>(invalid.ToActionResult());

        Assert.Equal(5, ok.Value);
        HttpValidationProblemDetails details = Assert.IsType<HttpValidationProblemDetails>(bad.Value);
        Assert.Equal(new[] { "Bad value" }, details.Errors[string.Empty]);
        Assert.False(invalid.TryGetValue(modelState, out _));
        Assert.Equal("Bad value", modelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.True(valid.TryGetValue(modelState, out int value));
        Assert.Equal(5, value);
    }

    [Fact]
    public async Task Helpers_ShouldRejectNullArguments()
    {
        Validation<int, ValidationError> valid = Validation.Valid<int, ValidationError>(1);
        NonEmptyList<ValidationError> errors = NonEmptyList.Of(new ValidationError("c", "m"));

        Assert.Throws<ArgumentNullException>(() => ((NonEmptyList<ValidationError>)null!).ToValidationProblem());
        Assert.Throws<ArgumentNullException>(() => valid.ToResult(null!));
        Assert.Throws<ArgumentNullException>(() => valid.ToCreatedResult(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<Validation<int, ValidationError>>)null!).ToResultAsync());
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<Validation<int, ValidationError>>)null!).ToResultAsync(_ => Results.Ok()));
        Assert.Throws<ArgumentNullException>(() => ((Validation<int, ValidationError>)null!).ToActionResult(_ => new OkResult()));
        Assert.Throws<ArgumentNullException>(() => valid.ToActionResult(null!));
        Assert.Throws<ArgumentNullException>(() => ((ModelStateDictionary)null!).AddErrors(errors));
        Assert.Throws<ArgumentNullException>(() => new ModelStateDictionary().AddErrors(null!));
        Assert.Throws<ArgumentNullException>(() => ((Validation<int, ValidationError>)null!).TryGetValue(new ModelStateDictionary(), out _));
        Assert.Throws<ArgumentNullException>(() => valid.TryGetValue(null!, out _));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task OpenApi_ShouldDocumentMinItemsAndMinLength()
    {
        HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
        string json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonDocument document = JsonDocument.Parse(json);

        // Schemas for the non-empty types may be inlined or hoisted into components, so walk the whole document.
        int minItems = 0;
        int minLength = 0;
        int linesWithoutMinItems = 0;
        int arraysWithItems = 0;
        Walk(document.RootElement, null);

        Assert.True(minItems >= 1, "expected minItems: 1 on the non-empty list schema\n" + json);
        Assert.True(minLength >= 1, "expected minLength: 1 on the NonEmptyString schema\n" + json);
        Assert.Equal(0, linesWithoutMinItems);
#if NET10_0_OR_GREATER
        Assert.True(arraysWithItems >= 1, "expected the non-empty list schema to declare type array with items\n" + json);
#endif

        void Walk(JsonElement element, string? propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("minItems", out JsonElement mi) && mi.GetInt32() == 1)
                {
                    minItems++;
                    if (element.TryGetProperty("type", out JsonElement arrayType) && arrayType.GetString() == "array" && element.TryGetProperty("items", out _))
                        arraysWithItems++;
                }

                if (element.TryGetProperty("minLength", out JsonElement ml) && ml.GetInt32() == 1)
                    minLength++;
                if (propertyName == "lines" && element.TryGetProperty("type", out JsonElement type) && type.GetString() == "array" && !element.TryGetProperty("minItems", out _))
                    linesWithoutMinItems++;

                foreach (JsonProperty property in element.EnumerateObject())
                    Walk(property.Value, property.Name);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in element.EnumerateArray())
                    Walk(item, null);
            }
        }
    }

    [Fact]
    public void SchemaTransformer_Apply_ShouldSetConstraints()
    {
        var list = new Microsoft.OpenApi.OpenApiSchema();
        var set = new Microsoft.OpenApi.OpenApiSchema();
        var pair = new Microsoft.OpenApi.OpenApiSchema { MaxItems = 10 };
        var text = new Microsoft.OpenApi.OpenApiSchema();
        var dictionary = new Microsoft.OpenApi.OpenApiSchema();
        var plain = new Microsoft.OpenApi.OpenApiSchema();

        NonEmptyOpenApiSchemaTransformer.Apply(list, typeof(NonEmptyList<int>));
        NonEmptyOpenApiSchemaTransformer.Apply(set, typeof(NonEmptySet<int>));
        NonEmptyOpenApiSchemaTransformer.Apply(pair, typeof(ConstrainedList<int, And<AtLeast<Two>, AtMost<Three>>>));
        NonEmptyOpenApiSchemaTransformer.Apply(text, typeof(NonEmptyString));
        NonEmptyOpenApiSchemaTransformer.Apply(dictionary, typeof(NonEmptyDictionary<string, int>));
        NonEmptyOpenApiSchemaTransformer.Apply(plain, typeof(List<int>));

        Assert.Equal(1, list.MinItems);
        Assert.Equal(1, set.MinItems);
        Assert.True(set.UniqueItems);
        Assert.Equal(2, pair.MinItems);
        Assert.Equal(3, pair.MaxItems);
        Assert.Equal(1, text.MinLength);
        Assert.Equal(1, dictionary.MinProperties);
        Assert.Null(plain.MinItems);
    }

    [Fact]
    public void SchemaTransformer_Apply_ShouldKeepStricterExistingBoundsAndHandleNullableTypes()
    {
        var bounded = new Microsoft.OpenApi.OpenApiSchema();
        var tighter = new Microsoft.OpenApi.OpenApiSchema { MinItems = 4, MaxItems = 2, MinLength = 3 };
        var nullableText = new Microsoft.OpenApi.OpenApiSchema();

        NonEmptyOpenApiSchemaTransformer.Apply(bounded, typeof(ConstrainedList<int, AtMost<Three>>));
        NonEmptyOpenApiSchemaTransformer.Apply(tighter, typeof(ConstrainedList<int, AtMost<Three>>));
        NonEmptyOpenApiSchemaTransformer.Apply(nullableText, typeof(NonEmptyString?));

        Assert.Equal(1, bounded.MinItems);
        Assert.Equal(3, bounded.MaxItems);
        Assert.Null(bounded.UniqueItems);
        Assert.Equal(4, tighter.MinItems);
        Assert.Equal(2, tighter.MaxItems);
        Assert.Equal(1, nullableText.MinLength);
        Assert.Throws<ArgumentNullException>(() => NonEmptyOpenApiSchemaTransformer.Apply(null!, typeof(int)));
        Assert.Throws<ArgumentNullException>(() => NonEmptyOpenApiSchemaTransformer.Apply(bounded, null!));
        Assert.Throws<ArgumentNullException>(() => ((Microsoft.AspNetCore.OpenApi.OpenApiOptions)null!).AddNonEmptySchemas());
    }

    [Fact]
    public async Task OpenApi_ShouldDescribeNonEmptyDictionariesAsObjectsWithAtLeastOneProperty()
    {
        string json = await _client.GetStringAsync("/openapi/v1.json");
        JsonDocument document = JsonDocument.Parse(json);

        JsonElement quota = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("Quota").GetProperty("properties");
        JsonElement limits = Resolve(quota.GetProperty("limits"));
        JsonElement top = Resolve(quota.GetProperty("top"));

        Assert.True(HasType(limits, "object"), json);
        Assert.Equal(1, limits.GetProperty("minProperties").GetInt32());
        Assert.True(HasType(Resolve(limits.GetProperty("additionalProperties")), "integer"), json);
        Assert.True(HasType(top, "array"), json);
        Assert.Equal(3, top.GetProperty("maxItems").GetInt32());

        // OpenAPI 3.1 writes "type" as a string or, for nullable schemas, as an array of strings.
        static bool HasType(JsonElement schema, string expected)
            => schema.TryGetProperty("type", out JsonElement type)
               && (type.ValueKind == JsonValueKind.String ? type.GetString() == expected : type.EnumerateArray().Any(t => t.GetString() == expected));

        JsonElement Resolve(JsonElement schema)
            => schema.TryGetProperty("$ref", out JsonElement reference)
                ? document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(reference.GetString()!["#/components/schemas/".Length..])
                : schema;
    }
#endif
}

/// <summary>A cardinality that implements <see cref="ICardinality.Value"/> explicitly.</summary>
public readonly struct Twelve : ICardinality
{
    static int ICardinality.Value => 12;
}

/// <summary>A user-defined generic constraint the shape analysis knows nothing about.</summary>
public readonly struct EvenCount<TCount> : IListConstraint where TCount : ICardinality
{
    public static bool Holds<T>(ReadOnlySpan<T> items) => items.Length % 2 == 0;

    public static string Describe() => "an even number of items";
}

#pragma warning disable CA2260 // derived collections are a legitimate use of NonEmptyList<T> as a base
/// <summary>Shaped like a <c>[NonEmptyCollection(MinCount = 2, MaxCount = 5, Distinct = true)]</c>-generated class.</summary>
public sealed class Crew : NonEmptyList<string>
{
    public const int MinCount = 2;
    public const int MaxCount = 5;
    public const bool IsDistinct = true;

    public Crew() : base(NonEmptyList.Of("a", "b"))
    {
    }
}

/// <summary>Shaped like an unbounded generated class: MaxCount is int.MaxValue.</summary>
public sealed class Unbounded : NonEmptyList<int>
{
    public const int MinCount = 1;
    public const int MaxCount = int.MaxValue;
    public const bool IsDistinct = false;

    public Unbounded() : base(NonEmptyList.Of(1))
    {
    }
}

/// <summary>A hand-written collection that declares its bound as a static field instead of a constant.</summary>
public sealed class Roster : NonEmptyList<int>
{
    public static readonly int MinCount = 3;

    public Roster() : base(NonEmptyList.Of(1, 2, 3))
    {
    }
}
#pragma warning restore CA2260
