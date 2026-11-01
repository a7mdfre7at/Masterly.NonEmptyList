using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

/// <summary>
/// Test DbContext for EF Core integration tests.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<TestEntityWithTags> EntitiesWithTags => Set<TestEntityWithTags>();
    public DbSet<TestEntityWithImmutableTags> EntitiesWithImmutableTags => Set<TestEntityWithImmutableTags>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<TestEntityWithMultipleLists> EntitiesWithMultipleLists => Set<TestEntityWithMultipleLists>();
    public DbSet<TestEntityWithComplexList> EntitiesWithComplexLists => Set<TestEntityWithComplexList>();
    public DbSet<TestEntityWithValidatedShapes> EntitiesWithValidatedShapes => Set<TestEntityWithValidatedShapes>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure NonEmptyList property with JSON conversion
        modelBuilder.Entity<TestEntityWithTags>()
            .Property(e => e.Tags)
            .HasNonEmptyListConversion();

        // Configure ImmutableNonEmptyList property with JSON conversion
        modelBuilder.Entity<TestEntityWithImmutableTags>()
            .Property(e => e.Tags)
            .HasImmutableNonEmptyListConversion();

        // Configure Order -> OrderItems relationship
        modelBuilder.Entity<Order>()
            .HasNonEmptyManyWithOne(
                o => o.OrderItems,
                oi => oi.Order,
                oi => oi.OrderId);

        // Configure multiple NonEmptyList properties
        modelBuilder.Entity<TestEntityWithMultipleLists>(entity =>
        {
            entity.Property(e => e.Numbers).HasNonEmptyListConversion();
            entity.Property(e => e.Strings).HasNonEmptyListConversion();
        });

        // Configure complex type NonEmptyList
        modelBuilder.Entity<TestEntityWithComplexList>()
            .Property(e => e.Addresses)
            .HasNonEmptyListConversion();

        // Configure Student <-> Course many-to-many relationship
        modelBuilder.Entity<Student>()
            .HasNonEmptyManyToMany(
                s => s.Courses,
                c => c.Students);

        // Configure Author <-> Book many-to-many with explicit join entity
        modelBuilder.Entity<Author>()
            .HasMany(a => a.Books)
            .WithMany(b => b.Authors)
            .UsingEntity<AuthorBook>(
                l => l.HasOne(ab => ab.Book).WithMany().HasForeignKey(ab => ab.BookId),
                r => r.HasOne(ab => ab.Author).WithMany().HasForeignKey(ab => ab.AuthorId),
                j =>
                {
                    j.HasKey(ab => new { ab.AuthorId, ab.BookId });
                });
    }
}

/// <summary>
/// Test DbContext that uses ApplyNonEmptyListConventions.
/// </summary>
public class TestDbContextWithConventions : DbContext
{
    public TestDbContextWithConventions(DbContextOptions<TestDbContextWithConventions> options)
        : base(options)
    {
    }

    public DbSet<TestEntityWithTags> EntitiesWithTags => Set<TestEntityWithTags>();
    public DbSet<TestEntityWithImmutableTags> EntitiesWithImmutableTags => Set<TestEntityWithImmutableTags>();
    public DbSet<TestEntityWithMultipleLists> EntitiesWithMultipleLists => Set<TestEntityWithMultipleLists>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure all entities first
        modelBuilder.Entity<TestEntityWithTags>();
        modelBuilder.Entity<TestEntityWithImmutableTags>();
        modelBuilder.Entity<TestEntityWithMultipleLists>();

        // Apply conventions to auto-configure NonEmptyList properties
        modelBuilder.ApplyNonEmptyListConventions();
    }
}

/// <summary>
/// DbContext whose model is supplied by the test, for exercising individual configuration overloads.
/// Each distinct configuration delegate gets its own cached model.
/// </summary>
public class ConfigurableDbContext : DbContext
{
    public ConfigurableDbContext(DbContextOptions<ConfigurableDbContext> options, Action<ModelBuilder> configure)
        : base(options)
    {
        Configure = configure;
    }

    public Action<ModelBuilder> Configure { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => Configure(modelBuilder);

    public static DbContextOptionsBuilder<ConfigurableDbContext> CreateOptionsBuilder()
        => new DbContextOptionsBuilder<ConfigurableDbContext>()
            .ReplaceService<IModelCacheKeyFactory, PerConfigurationModelCacheKeyFactory>();

    private sealed class PerConfigurationModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
            => context is ConfigurableDbContext configurable
                ? (configurable.Configure, designTime)
                : (context.GetType(), designTime);
    }
}
