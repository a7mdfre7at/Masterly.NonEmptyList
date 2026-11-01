using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

/// <summary>
/// Verifies that NonEmptyList LINQ operators used inside EF Core projections are rewritten to Enumerable
/// operators and translate like List&lt;T&gt; would.
/// </summary>
public class NonEmptyListQueryExpressionInterceptorTests
{
    private static DbContextOptions<TestDbContext> CreateOptions(bool withTranslation = true)
    {
        DbContextOptionsBuilder<TestDbContext> builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());

        if (withTranslation)
            builder.UseNonEmptyList();

        return builder.Options;
    }

    private static int SeedOrder(DbContextOptions<TestDbContext> options)
    {
        using var context = new TestDbContext(options);

        var order = new Order
        {
            OrderNumber = "ORD-LINQ",
            OrderItems = new NonEmptyList<OrderItem>(
                new OrderItem { ProductName = "Widget", Price = 30m },
                new OrderItem { ProductName = "Gadget", Price = 10m },
                new OrderItem { ProductName = "Doohickey", Price = 20m })
        };

        context.Orders.Add(order);
        context.SaveChanges();
        return order.Id;
    }

    private sealed record OrderSummary(string OrderNumber, NonEmptyList<string> ProductNames);

    [Fact]
    public void Select_OnNonEmptyListNavigation_IsTranslated()
    {
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);
        context.ChangeTracker.Clear();

        var result = context.Orders
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.OrderNumber,
                Names = o.OrderItems.Select(i => i.ProductName).ToList(),
            })
            .Single();

        Assert.Equal(3, result.Names.Count);
        Assert.Contains("Widget", result.Names);
    }

    [Fact]
    public void OrderByThenBy_OnNonEmptyListNavigation_IsTranslated()
    {
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);
        context.ChangeTracker.Clear();

        List<string> names = context.Orders
            .Where(o => o.Id == id)
            .Select(o => o.OrderItems.OrderBy(i => i.Price).ThenBy(i => i.ProductName).Select(i => i.ProductName).ToList())
            .Single();

        Assert.Equal(new[] { "Gadget", "Doohickey", "Widget" }, names);
    }

    [Fact]
    public void SumMaxMin_OnNonEmptyListNavigation_AreTranslated()
    {
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);
        context.ChangeTracker.Clear();

        var result = context.Orders
            .Where(o => o.Id == id)
            .Select(o => new
            {
                Total = o.OrderItems.Sum(i => i.Price),
                Highest = o.OrderItems.Max(i => i.Price),
                Lowest = o.OrderItems.Min(i => i.Price),
            })
            .Single();

        Assert.Equal(60m, result.Total);
        Assert.Equal(30m, result.Highest);
        Assert.Equal(10m, result.Lowest);
    }

    [Fact]
    public void CountAndHeadProperties_OnNonEmptyListNavigation_AreTranslated()
    {
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);
        context.ChangeTracker.Clear();

        var result = context.Orders
            .Where(o => o.Id == id)
            .Select(o => new
            {
                ItemCount = o.OrderItems.Count,
                FirstName = o.OrderItems.Head.ProductName,
                LastName = o.OrderItems.Last.ProductName,
            })
            .Single();

        Assert.Equal(3, result.ItemCount);
        Assert.Equal("Widget", result.FirstName);
        Assert.Equal("Doohickey", result.LastName);
    }

    [Fact]
    public void Projection_IntoNonEmptyListMember_MaterializesOnClient()
    {
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);
        context.ChangeTracker.Clear();

        OrderSummary summary = context.Orders
            .Where(o => o.Id == id)
            .Select(o => new OrderSummary(o.OrderNumber, o.OrderItems.Select(i => i.ProductName)))
            .Single();

        Assert.Equal("ORD-LINQ", summary.OrderNumber);
        Assert.Equal(3, summary.ProductNames.Count);
        Assert.Equal("Widget", summary.ProductNames.Head);
    }

    [Fact]
    public void WithoutTranslation_SelectOnNavigation_Throws()
    {
        var options = CreateOptions(withTranslation: false);
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);
        context.ChangeTracker.Clear();

        Assert.Throws<InvalidOperationException>(() =>
            context.Orders
                .Where(o => o.Id == id)
                .Select(o => o.OrderItems.Select(i => i.ProductName).ToList())
                .Single());
    }

    [Fact]
    public void Rewrite_LeavesUnrelatedExpressionsUntouched()
    {
        System.Linq.Expressions.Expression<Func<List<int>, int>> expression = xs => xs.Select(x => x * 2).Sum();

        System.Linq.Expressions.Expression rewritten = NonEmptyListQueryExpressionInterceptor.Rewrite(expression);

        Assert.Same(expression, rewritten);
    }
}
