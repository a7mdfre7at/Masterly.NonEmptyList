using System.Linq.Expressions;
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
        Expression<Func<List<int>, int>> expression = xs => xs.Select(x => x * 2).Sum();

        Expression rewritten = NonEmptyListQueryExpressionInterceptor.Rewrite(expression);

        Assert.Same(expression, rewritten);
    }
    private sealed class OrderNames
    {
        public NonEmptyList<string> Names { get; set; } = null!;

        public NonEmptyList<decimal> Prices = null!;
    }

    private static bool CallsNonEmptyListOperators(Expression expression)
    {
        var finder = new NonEmptyListCallFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    private sealed class NonEmptyListCallFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Found |= node.Method.DeclaringType == typeof(NonEmptyListLinqExtensions)
                     || node.Method.DeclaringType == typeof(NonEmptyListMathExtensions);
            return base.VisitMethodCall(node);
        }
    }

    [Fact]
    public void FirstProperty_OnNonEmptyListNavigation_IsTranslated()
    {
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);

        string first = context.Orders.Where(o => o.Id == id).Select(o => o.OrderItems.First.ProductName).Single();

        Assert.Equal("Widget", first);
    }

    [Fact]
    public void Projection_IntoNonEmptyListPropertyAndField_MaterializesOnClient()
    {
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);

        OrderNames result = context.Orders
            .Where(o => o.Id == id)
            .Select(o => new OrderNames
            {
                Names = o.OrderItems.Select(i => i.ProductName),
                Prices = o.OrderItems.Select(i => i.Price),
            })
            .Single();

        Assert.Equal(3, result.Names.Count);
        Assert.Contains("Gadget", result.Names);
        Assert.Equal(60m, result.Prices.Sum());
    }

    [Fact]
    public void Projection_ThatCannotBeExpressedOnEnumerable_IsLeftForEfToReport()
    {
        // OrderBy on a NonEmptyList returns OrderedNonEmptyList, which Enumerable.OrderBy cannot produce, so the
        // rewrite backs off and EF reports its usual untranslatable-navigation error instead of an ArgumentException.
        var options = CreateOptions();
        int id = SeedOrder(options);

        using var context = new TestDbContext(options);

        Assert.Throws<InvalidOperationException>(() =>
            context.Orders.Where(o => o.Id == id).Select(o => o.OrderItems.OrderBy(i => i.Price)).Single());
    }

    [Fact]
    public void Rewrite_InstanceMethodOnRewrittenResult_KeepsTheCallAsWritten()
    {
        // Contains is a NonEmptyList instance method, so it cannot be applied to the IEnumerable that Select becomes.
        Expression<Func<NonEmptyList<int>, bool>> expression = xs => xs.Select(x => x * 2).Contains(4);

        var rewritten = (Expression<Func<NonEmptyList<int>, bool>>)NonEmptyListQueryExpressionInterceptor.Rewrite(expression);

        Assert.True(rewritten.Compile()(new NonEmptyList<int>(1, 2)));
        Assert.False(rewritten.Compile()(new NonEmptyList<int>(3)));
    }

    [Fact]
    public void Rewrite_LambdaWhoseResultTypeEnumerableCannotProduce_ReturnsTheExpressionUnchanged()
    {
        Expression<Func<NonEmptyList<int>, OrderedNonEmptyList<int>>> expression = xs => xs.OrderBy(x => -x);

        Expression rewritten = NonEmptyListQueryExpressionInterceptor.Rewrite(expression);

        Assert.Same(expression, rewritten);
    }

    [Fact]
    public void Rewrite_SelectManyWithNonEmptySelector_AdaptsTheSelectorToEnumerable()
    {
        Expression<Func<NonEmptyList<NonEmptyList<int>>, int>> expression = xss => xss.SelectMany(xs => xs).Sum();

        var rewritten = (Expression<Func<NonEmptyList<NonEmptyList<int>>, int>>)NonEmptyListQueryExpressionInterceptor.Rewrite(expression);

        Assert.False(CallsNonEmptyListOperators(rewritten));
        var input = new NonEmptyList<NonEmptyList<int>>(new NonEmptyList<int>(1, 2), new NonEmptyList<int>(3));
        Assert.Equal(6, rewritten.Compile()(input));
    }

    [Fact]
    public void Rewrite_OperatorWithoutEnumerableEquivalent_IsLeftAlone()
    {
        Expression<Func<NonEmptyList<int>, (int Min, int Max)>> expression = xs => xs.MinMax();

        Assert.Same(expression, NonEmptyListQueryExpressionInterceptor.Rewrite(expression));
    }

    [Fact]
    public void Rewrite_OtherNonEmptyListProperties_AreLeftAlone()
    {
        Expression<Func<NonEmptyList<int>, NonEmptyList<int>?>> expression = xs => xs.Tail;

        Assert.Same(expression, NonEmptyListQueryExpressionInterceptor.Rewrite(expression));
    }

    [Fact]
    public void Rewrite_ConstructorsWithoutRewrittenArguments_AreLeftAlone()
    {
        Expression<Func<List<int>, object>> expression = xs => new { Empty = new object(), Pair = new KeyValuePair<int, int>(), Count = new KeyValuePair<int, int>(xs.Count, 1) };

        Assert.Same(expression, NonEmptyListQueryExpressionInterceptor.Rewrite(expression));
    }

    [Fact]
    public void Rewrite_MemberInitWithoutRewrittenValues_IsLeftAlone()
    {
        Expression<Func<NonEmptyList<string>, OrderNames>> expression = xs => new OrderNames { Names = xs };

        Assert.Same(expression, NonEmptyListQueryExpressionInterceptor.Rewrite(expression));
    }

    [Fact]
    public void Rewrite_NullExpression_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NonEmptyListQueryExpressionInterceptor.Rewrite(null!));
    }
    [Fact]
    public void Rewrite_IndexedSelect_MapsToTheIndexedEnumerableOverload()
    {
        Expression<Func<NonEmptyList<int>, int>> expression = xs => xs.Select((x, i) => x * i).Sum();

        var rewritten = (Expression<Func<NonEmptyList<int>, int>>)NonEmptyListQueryExpressionInterceptor.Rewrite(expression);

        Assert.False(CallsNonEmptyListOperators(rewritten));
        Assert.Equal(0 * 5 + 1 * 6 + 2 * 7, rewritten.Compile()(new NonEmptyList<int>(5, 6, 7)));
    }

    [Fact]
    public void Rewrite_NonGenericOperator_MapsToTheMatchingEnumerableOverload()
    {
        Expression<Func<NonEmptyList<int>, double>> expression = xs => xs.Average();

        var rewritten = (Expression<Func<NonEmptyList<int>, double>>)NonEmptyListQueryExpressionInterceptor.Rewrite(expression);

        Assert.False(CallsNonEmptyListOperators(rewritten));
        Assert.Equal(2.5, rewritten.Compile()(new NonEmptyList<int>(1, 2, 3, 4)));
    }
}
