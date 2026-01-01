using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class NonEmptyListRelationshipTests
{
    private static DbContextOptions<TestDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public void CanConfigure_OneToMany_Relationship()
    {
        using var context = new TestDbContext(CreateOptions());

        var order = new Order
        {
            OrderNumber = "ORD-001",
            OrderItems = new NonEmptyList<OrderItem>(
                new OrderItem { ProductName = "Product 1", Price = 10.00m })
        };

        context.Orders.Add(order);
        context.SaveChanges();

        var savedOrder = context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefault(o => o.OrderNumber == "ORD-001");

        Assert.NotNull(savedOrder);
        Assert.Single(savedOrder.OrderItems);
        Assert.Equal("Product 1", savedOrder.OrderItems.Head.ProductName);
    }

    [Fact]
    public void CanAdd_MultipleItems_ToRelationship()
    {
        using var context = new TestDbContext(CreateOptions());

        var order = new Order
        {
            OrderNumber = "ORD-002",
            OrderItems = new NonEmptyList<OrderItem>(
                new OrderItem { ProductName = "Product 1", Price = 10.00m },
                new OrderItem { ProductName = "Product 2", Price = 20.00m },
                new OrderItem { ProductName = "Product 3", Price = 30.00m })
        };

        context.Orders.Add(order);
        context.SaveChanges();

        var savedOrder = context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefault(o => o.OrderNumber == "ORD-002");

        Assert.NotNull(savedOrder);
        Assert.Equal(3, savedOrder.OrderItems.Count);
    }

    [Fact]
    public void ForeignKey_ShouldBeSet_Correctly()
    {
        using var context = new TestDbContext(CreateOptions());

        var order = new Order
        {
            OrderNumber = "ORD-003",
            OrderItems = new NonEmptyList<OrderItem>(
                new OrderItem { ProductName = "Product 1", Price = 10.00m })
        };

        context.Orders.Add(order);
        context.SaveChanges();

        var orderItem = context.OrderItems.FirstOrDefault();

        Assert.NotNull(orderItem);
        Assert.Equal(order.Id, orderItem.OrderId);
    }

    [Fact]
    public void InverseNavigation_ShouldWork()
    {
        using var context = new TestDbContext(CreateOptions());

        var order = new Order
        {
            OrderNumber = "ORD-004",
            OrderItems = new NonEmptyList<OrderItem>(
                new OrderItem { ProductName = "Product 1", Price = 10.00m })
        };

        context.Orders.Add(order);
        context.SaveChanges();

        var orderItem = context.OrderItems
            .Include(oi => oi.Order)
            .FirstOrDefault();

        Assert.NotNull(orderItem);
        Assert.NotNull(orderItem.Order);
        Assert.Equal("ORD-004", orderItem.Order.OrderNumber);
    }

    [Fact]
    public void CanUpdate_RelatedItems()
    {
        using var context = new TestDbContext(CreateOptions());

        var order = new Order
        {
            OrderNumber = "ORD-005",
            OrderItems = new NonEmptyList<OrderItem>(
                new OrderItem { ProductName = "Product 1", Price = 10.00m })
        };

        context.Orders.Add(order);
        context.SaveChanges();

        // Update the order item
        var item = order.OrderItems.Head;
        item.Price = 15.00m;
        context.SaveChanges();

        var savedItem = context.OrderItems.FirstOrDefault();
        Assert.NotNull(savedItem);
        Assert.Equal(15.00m, savedItem.Price);
    }

    [Fact]
    public void CanQuery_WithInclude()
    {
        using var context = new TestDbContext(CreateOptions());

        // Add multiple orders
        for (int i = 1; i <= 3; i++)
        {
            context.Orders.Add(new Order
            {
                OrderNumber = $"ORD-{i:000}",
                OrderItems = new NonEmptyList<OrderItem>(
                    new OrderItem { ProductName = $"Product {i}", Price = i * 10m })
            });
        }
        context.SaveChanges();

        var orders = context.Orders
            .Include(o => o.OrderItems)
            .ToList();

        Assert.Equal(3, orders.Count);
        Assert.All(orders, o => Assert.NotEmpty(o.OrderItems));
    }
}
