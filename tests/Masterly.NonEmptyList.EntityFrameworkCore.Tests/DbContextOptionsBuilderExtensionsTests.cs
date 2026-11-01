using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

/// <summary>
/// Covers the generic and non-generic <c>UseNonEmptyList*</c> registrations: which interceptors each one adds, their
/// argument checks, and the behavior they switch on.
/// </summary>
public class DbContextOptionsBuilderExtensionsTests
{
    private static DbContextOptionsBuilder<TestDbContext> NewBuilder()
        => new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());

    private static Type[] InterceptorTypes(DbContextOptions options)
        => options.FindExtension<CoreOptionsExtension>()!.Interceptors!.Select(i => i.GetType()).OrderBy(t => t.Name).ToArray();

    private static readonly Type[] Both = [typeof(NonEmptyListQueryExpressionInterceptor), typeof(NonEmptyListSaveChangesInterceptor)];
    private static readonly Type[] ValidationOnly = [typeof(NonEmptyListSaveChangesInterceptor)];
    private static readonly Type[] TranslationOnly = [typeof(NonEmptyListQueryExpressionInterceptor)];

    public static TheoryData<string, Func<DbContextOptionsBuilder<TestDbContext>, DbContextOptionsBuilder>, Type[]> Registrations => new()
    {
        { "UseNonEmptyList<T>", b => b.UseNonEmptyList(), Both },
        { "UseNonEmptyList", b => ((DbContextOptionsBuilder)b).UseNonEmptyList(), Both },
        { "UseNonEmptyListValidation<T>", b => b.UseNonEmptyListValidation(), ValidationOnly },
        { "UseNonEmptyListValidation", b => ((DbContextOptionsBuilder)b).UseNonEmptyListValidation(), ValidationOnly },
        { "UseNonEmptyListQueryTranslation<T>", b => b.UseNonEmptyListQueryTranslation(), TranslationOnly },
        { "UseNonEmptyListQueryTranslation", b => ((DbContextOptionsBuilder)b).UseNonEmptyListQueryTranslation(), TranslationOnly },
    };

    [Theory]
    [MemberData(nameof(Registrations))]
    public void Registration_ShouldAddExactlyItsInterceptorsAndReturnTheSameBuilder(
        string name, Func<DbContextOptionsBuilder<TestDbContext>, DbContextOptionsBuilder> register, Type[] expected)
    {
        _ = name;
        DbContextOptionsBuilder<TestDbContext> builder = NewBuilder();

        DbContextOptionsBuilder returned = register(builder);

        Assert.Same(builder, returned);
        Assert.Equal(expected, InterceptorTypes(builder.Options));
    }

    [Fact]
    public void NullBuilder_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder)null!).UseNonEmptyList());
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder<TestDbContext>)null!).UseNonEmptyList());
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder)null!).UseNonEmptyListValidation());
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder<TestDbContext>)null!).UseNonEmptyListValidation());
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder)null!).UseNonEmptyListQueryTranslation());
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder<TestDbContext>)null!).UseNonEmptyListQueryTranslation());
    }

    [Fact]
    public void NonGenericUseNonEmptyList_ShouldValidateSavesAndTranslateQueries()
    {
        DbContextOptionsBuilder<TestDbContext> builder = NewBuilder();
        ((DbContextOptionsBuilder)builder).UseNonEmptyList();

        using var context = new TestDbContext(builder.Options);
        context.Orders.Add(new Order { OrderNumber = "A", OrderItems = new NonEmptyList<OrderItem>(new OrderItem { ProductName = "P" }) });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        Assert.Equal(new[] { "P" }, context.Orders.Select(o => o.OrderItems.Select(i => i.ProductName).ToList()).Single());

        context.EntitiesWithTags.Add(new TestEntityWithTags { Name = "x", Tags = null! });
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
    }

    [Fact]
    public void UseNonEmptyListValidation_ShouldNotTranslateNavigationQueries()
    {
        DbContextOptionsBuilder<TestDbContext> builder = NewBuilder().UseNonEmptyListValidation();

        using var context = new TestDbContext(builder.Options);
        context.Orders.Add(new Order { OrderNumber = "A", OrderItems = new NonEmptyList<OrderItem>(new OrderItem { ProductName = "P" }) });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        Assert.Throws<InvalidOperationException>(() => context.Orders.Select(o => o.OrderItems.Select(i => i.ProductName).ToList()).Single());
    }

    [Fact]
    public void UseNonEmptyListQueryTranslation_ShouldTranslateWithoutTheValidationInterceptor()
    {
        DbContextOptionsBuilder<TestDbContext> builder = NewBuilder().UseNonEmptyListQueryTranslation();

        using var context = new TestDbContext(builder.Options);
        context.Orders.Add(new Order { OrderNumber = "A", OrderItems = new NonEmptyList<OrderItem>(new OrderItem { ProductName = "P" }) });
        context.SaveChanges();
        context.ChangeTracker.Clear();

        Assert.Equal(new[] { "P" }, context.Orders.Select(o => o.OrderItems.Select(i => i.ProductName).ToList()).Single());
        Assert.DoesNotContain(typeof(NonEmptyListSaveChangesInterceptor), InterceptorTypes(builder.Options));
        Assert.IsAssignableFrom<IInterceptor>(builder.Options.FindExtension<CoreOptionsExtension>()!.Interceptors!.Single());
    }
}
