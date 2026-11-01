---
id: examples
title: "Examples"
description: "End-to-end scenarios that combine non-empty types with EF Core, ASP.NET Core, configuration, batching and the zipper."
---

Each example below is a small, complete scenario. They show how the guarantee travels: it is checked once at the edge of the system (an HTTP request, a configuration file, a message stream), and the code behind that edge never tests for emptiness again.

The examples assume `using Masterly.NonEmptyList;` plus the namespaces of the packages they use.

## Orders with at least one line

An order without lines makes no sense, so the domain model says so with `NonEmptyList<OrderLine>`. EF Core stores the lines as an ordinary one-to-many relationship, the API validates the request and reports every problem at once, and reads use projections.

```csharp title="Order.cs"
public sealed class Order
{
    private Order() { }   // for EF Core

    public Order(string customer, NonEmptyList<OrderLine> lines)
    {
        Customer = customer;
        Lines = lines;
    }

    public int Id { get; private set; }
    public string Customer { get; private set; } = string.Empty;
    public NonEmptyList<OrderLine> Lines { get; private set; } = null!;
}

public sealed class OrderLine
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
}
```

```csharp title="ShopContext.cs"
using Masterly.NonEmptyList.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasNonEmptyManyWithOne(o => o.Lines, l => l.Order, l => l.OrderId);
    }
}
```

The request uses `NonEmptyList<OrderLineRequest>`, so a JSON body with `"lines": []` is a 400 before any of your code runs. The remaining rules are checked with `Validation`, which collects every error instead of stopping at the first:

```csharp title="OrderValidation.cs"
public sealed record CreateOrderRequest(string? Customer, NonEmptyList<OrderLineRequest> Lines);
public sealed record OrderLineRequest(string Sku, int Quantity, decimal UnitPrice);

public static class OrderValidation
{
    public static Validation<Order, ValidationError> Validate(CreateOrderRequest request)
        => Validation.Combine(
            Validation.NotBlank(request.Customer, new ValidationError("customer.required", "Customer is required", "customer")),
            request.Lines.Traverse(ToLine),
            (customer, lines) => new Order(customer, lines));

    private static Validation<OrderLine, ValidationError> ToLine(OrderLineRequest line)
        => Validation.Ensure(line, l => l.Quantity > 0,
                new ValidationError("line.quantity", $"Quantity for {line.Sku} must be positive", "lines"))
            .Map(l => new OrderLine { Sku = l.Sku, Quantity = l.Quantity, UnitPrice = l.UnitPrice });
}
```

`Traverse` validates every line and, when all of them pass, gives back a `NonEmptyList<OrderLine>`, so `new Order(customer, lines)` receives a list that is already proven non-empty.

```csharp title="Program.cs"
using Masterly.NonEmptyList.AspNetCore;
using Masterly.NonEmptyList.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<ShopContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("Shop"))
    .UseNonEmptyList());

WebApplication app = builder.Build();

app.MapPost("/orders", async (CreateOrderRequest request, ShopContext db, CancellationToken cancellationToken) =>
{
    Validation<Order, ValidationError> validation = OrderValidation.Validate(request);
    if (!validation.TryGetValue(out Order? order))
        return validation.Errors.ToValidationProblem();   // 400 with every error, grouped by path

    db.Orders.Add(order);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/orders/{order.Id}", new { order.Id });
});

app.MapGet("/orders/{id:int}", async (int id, ShopContext db, CancellationToken cancellationToken) =>
{
    var summary = await db.Orders
        .Where(o => o.Id == id)
        .Select(o => new
        {
            o.Id,
            o.Customer,
            LineCount = o.Lines.Count,
            FirstSku = o.Lines.Head.Sku,
            Total = o.Lines.Sum(l => l.Quantity * l.UnitPrice),
        })
        .SingleOrDefaultAsync(cancellationToken);

    return summary is null ? Results.NotFound() : Results.Ok(summary);
});
```

`UseNonEmptyList()` registers two interceptors. The query interceptor lets `o.Lines.Count`, `o.Lines.Head` and the `NonEmptyList` version of `Sum` translate inside the projection. The SaveChanges interceptor refuses to save an order whose `Lines` is `null`.

The read side uses a projection rather than `Include`, because EF Core cannot fill a `NonEmptyList<T>` navigation by starting from an empty list.

Read more: [Validation](./validation.md), [ASP.NET Core](./aspnetcore.md), [Entity Framework Core](./entity-framework-core.md).

## Looking up products by id

A lookup endpoint that takes ids from the query string. With `NonEmptyList<int>` as the parameter type, `?ids=` and a missing `ids` are both rejected with 400, so the query below never runs with an empty filter. The OpenAPI document states the guarantee of the response.

```csharp title="Program.cs"
using Masterly.NonEmptyList.AspNetCore;
using Masterly.NonEmptyList.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

// ShopContext here has a DbSet<Product> Products; Product has Id and Name
public sealed record ProductDto(int Id, string Name);

builder.Services.AddOpenApi(options => options.AddNonEmptySchemas());

WebApplication app = builder.Build();
app.MapOpenApi();

app.MapGet("/products", async (NonEmptyList<int> ids, ShopContext db, CancellationToken cancellationToken) =>
{
    int[] wanted = ids.ToArray();

    NonEmptyList<ProductDto>? products = await db.Products
        .Where(p => wanted.Contains(p.Id))
        .Select(p => new ProductDto(p.Id, p.Name))
        .ToNonEmptyListOrNullAsync(cancellationToken);

    return products is null ? Results.NotFound() : Results.Ok(products);
})
.Produces<NonEmptyList<ProductDto>>()
.Produces(StatusCodes.Status404NotFound);
```

| Request | Response |
|---------|----------|
| `GET /products?ids=1,2,3` | 200 with the matching products, or 404 if none match |
| `GET /products?ids=1, 2 ,3` | The same: each piece is trimmed |
| `GET /products?ids=` or `GET /products` | 400 |
| `GET /products?ids=1,abc` | 400 |

`ToNonEmptyListOrNullAsync` turns "no rows" into `null` instead of an empty list, so the 404 decision is a `null` check and the 200 body is a list with at least one product. In the OpenAPI document (.NET 9 or later), the 200 response schema gets `type: array` and `minItems: 1`.

Read more: [ASP.NET Core](./aspnetcore.md), [Entity Framework Core](./entity-framework-core.md).

## A batching event writer

A background service reads order events from a channel and writes them to the database in batches. `BatchNonEmpty(size, maxWait)` emits a batch when it is full or when the oldest waiting event has waited long enough, and it never emits an empty batch, so the writer does not need a "nothing to do" check.

```csharp title="OrderEventWriter.cs"
using System.Data;
using System.Threading.Channels;
using Dapper;

public sealed record OrderPlaced(int OrderId, decimal Total, DateTimeOffset PlacedAt);

// IDbConnectionFactory is your own abstraction that returns an open connection
public sealed class OrderEventWriter(
    Channel<OrderPlaced> channel,
    IDbConnectionFactory connections,
    ILogger<OrderEventWriter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IAsyncEnumerable<OrderPlaced> events = channel.Reader.ReadAllAsync(stoppingToken);

        await foreach (NonEmptyList<OrderPlaced> batch in events.BatchNonEmpty(500, TimeSpan.FromSeconds(2), stoppingToken))
        {
            await WriteAsync(batch, stoppingToken);

            logger.LogInformation(
                "Wrote {Count} orders worth {Total}, latest placed at {Latest}",
                batch.Count, batch.Sum(e => e.Total), batch.Max(e => e.PlacedAt));
        }
    }

    // Taking a NonEmptyList documents, and enforces, that there is always something to insert.
    private async Task WriteAsync(NonEmptyList<OrderPlaced> batch, CancellationToken cancellationToken)
    {
        using IDbConnection connection = connections.Open();
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO OrderEvents (OrderId, Total, PlacedAt) VALUES (@OrderId, @Total, @PlacedAt)",
            batch,
            cancellationToken: cancellationToken));
    }
}
```

`batch.Sum` and `batch.Max` are the `NonEmptyList` versions, so neither can fail for lack of data. The timer behind `maxWait` starts only when an event is waiting, so an idle channel does not wake the writer.

If you would rather take whatever is already in the channel, up to a limit, use `channel.Reader.ReadAllBatchesAsync(maxSize: 500, stoppingToken)` instead. Each batch then holds everything that was immediately available, which drains a busy channel in large batches and delivers single events promptly when it is quiet.

Read more: [Batching and streams](./batching-streams.md).

## Alert recipients that fail fast

An alerting service must always have someone to notify. Instead of checking the recipient list on every send, the options class makes it required, and the host refuses to start without it.

```csharp title="AlertOptions.cs"
public sealed class AlertOptions
{
    public required NonEmptyList<string> Recipients { get; init; }     // required
    public required NonEmptyString Sender { get; init; }               // required
    public NonEmptySet<string>? EscalationRecipients { get; init; }    // optional
    public string SmtpHost { get; init; } = "localhost";
    public int SmtpPort { get; init; } = 25;
}
```

```json title="appsettings.json"
{
  "Alerts": {
    "Recipients": ["ops@example.com", "oncall@example.com"],
    "Sender": "alerts@example.com",
    "EscalationRecipients": ["cto@example.com"],
    "SmtpHost": "smtp.example.com",
    "SmtpPort": 587
  }
}
```

```csharp title="Program.cs"
using Masterly.NonEmptyList.Configuration;

builder.Services.AddNonEmptyOptions<AlertOptions>("Alerts");
builder.Services.AddSingleton<AlertMailer>();
```

```csharp title="AlertMailer.cs"
using System.Net.Mail;
using Microsoft.Extensions.Options;

public sealed class AlertMailer(IOptions<AlertOptions> options)
{
    public async Task SendAsync(string subject, string body, bool critical, CancellationToken cancellationToken)
    {
        AlertOptions alerts = options.Value;

        using MailMessage message = new MailMessage
        {
            From = new MailAddress(alerts.Sender),   // NonEmptyString converts to string
            Subject = subject,
            Body = body,
        };

        foreach (string recipient in alerts.Recipients)
            message.To.Add(recipient);

        if (critical && alerts.EscalationRecipients is { } escalation)
        {
            foreach (string recipient in escalation)
                message.CC.Add(recipient);
        }

        using SmtpClient client = new SmtpClient(alerts.SmtpHost, alerts.SmtpPort);
        await client.SendMailAsync(message, cancellationToken);
    }
}
```

If a deployment ships with `"Recipients": []`, or without a `Recipients` key at all, the host stops during startup, and the exception message names `Alerts:Recipients`. A blank `Sender` stops it the same way. `EscalationRecipients` is nullable, so leaving it out is fine.

To set the recipients from the environment, for example in a container, use the indexed form:

```bash
Alerts__Recipients__0=ops@example.com
Alerts__Recipients__1=oncall@example.com
```

When there is no `Recipients` array in `appsettings.json`, a single comma-separated value works as well: `Alerts__Recipients=ops@example.com,oncall@example.com`.

Read more: [Configuration](./configuration.md).

## A checkout wizard

A checkout flow is a list of steps with a current position. `NonEmptyZipper<T>` models exactly that: a cursor that always points at an element. There is no "no current step" state to handle, and moving past either end is explicit.

```csharp title="CheckoutWizard.cs"
public enum CheckoutStep { Cart, Shipping, Payment, Review }

public sealed class CheckoutWizard
{
    private NonEmptyZipper<CheckoutStep> _steps;

    public CheckoutWizard(bool requiresShipping)
    {
        NonEmptyList<CheckoutStep> steps = requiresShipping
            ? [CheckoutStep.Cart, CheckoutStep.Shipping, CheckoutStep.Payment, CheckoutStep.Review]
            : [CheckoutStep.Cart, CheckoutStep.Payment, CheckoutStep.Review];

        _steps = steps.ToZipper();
    }

    public CheckoutStep Current => _steps.Focus;
    public string Progress => $"Step {_steps.Index + 1} of {_steps.Count}";
    public IEnumerable<CheckoutStep> Completed => _steps.Before;
    public bool CanGoBack => !_steps.IsAtStart;
    public bool IsLastStep => _steps.IsAtEnd;

    public void Next() => _steps = _steps.MoveRightOrStay();
    public void Back() => _steps = _steps.MoveLeftOrStay();

    // Jumping to a step that is not in this flow (Shipping for a digital order) keeps the current step.
    public void JumpTo(CheckoutStep step) => _steps = _steps.Find(s => s == step) ?? _steps;
}
```

`MoveRight()` and `MoveLeft()` return `null` at the ends. The `OrStay` versions used here keep the cursor where it is instead. `NonEmptyZipper<T>` is immutable, so every move returns a new zipper and the wizard just keeps the latest one.

The testing package has a generator for zippers, so you can check the wizard's navigation rules against any list and any starting position:

```csharp title="CheckoutWizardTests.cs"
using CsCheck;
using Masterly.NonEmptyList.Testing;

public class CheckoutWizardTests
{
    [Fact]
    public void Moving_NeverLeavesTheList()
    {
        NonEmptyGen.Zipper(Gen.Int).Sample(z =>
            z.MoveRightOrStay().Index < z.Count &&
            z.MoveLeftOrStay().Index >= 0 &&
            z.MoveRightOrStay().Count == z.Count);
    }
}
```

Read more: [Modeling types](./modeling-types.md), [Testing](./testing.md).
