using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class NonEmptyListQueryExtensionTests
{
    private static DbContextOptions<TestDbContext> CreateOptionsWithData(Action<TestDbContext> seedData)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        seedData(context);
        context.SaveChanges();

        return options;
    }

    [Fact]
    public void ToNonEmptyList_WithResults_ShouldReturnNonEmptyList()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test2", Tags = new NonEmptyList<string>("tag2") });
        });

        using var context = new TestDbContext(options);
        var result = context.EntitiesWithTags.ToNonEmptyList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ToNonEmptyList_WithNoResults_ShouldThrow()
    {
        var options = CreateOptionsWithData(_ => { });

        using var context = new TestDbContext(options);

        Assert.Throws<InvalidOperationException>(() =>
            context.EntitiesWithTags.ToNonEmptyList());
    }

    [Fact]
    public async Task ToNonEmptyListAsync_WithResults_ShouldReturnNonEmptyList()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
        });

        await using var context = new TestDbContext(options);
        var result = await context.EntitiesWithTags.ToNonEmptyListAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task ToNonEmptyListAsync_WithNoResults_ShouldThrow()
    {
        var options = CreateOptionsWithData(_ => { });

        await using var context = new TestDbContext(options);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.EntitiesWithTags.ToNonEmptyListAsync());
    }

    [Fact]
    public void TryToNonEmptyList_WithResults_ShouldReturnTrue()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
        });

        using var context = new TestDbContext(options);
        bool success = context.EntitiesWithTags.TryToNonEmptyList(out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void TryToNonEmptyList_WithNoResults_ShouldReturnFalse()
    {
        var options = CreateOptionsWithData(_ => { });

        using var context = new TestDbContext(options);
        bool success = context.EntitiesWithTags.TryToNonEmptyList(out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public async Task TryToNonEmptyListAsync_WithResults_ShouldReturnTrue()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
        });

        await using var context = new TestDbContext(options);
        var (success, result) = await context.EntitiesWithTags.TryToNonEmptyListAsync();

        Assert.True(success);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task TryToNonEmptyListAsync_WithNoResults_ShouldReturnFalse()
    {
        var options = CreateOptionsWithData(_ => { });

        await using var context = new TestDbContext(options);
        var (success, result) = await context.EntitiesWithTags.TryToNonEmptyListAsync();

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void ToNonEmptyListOrNull_WithResults_ShouldReturnNonEmptyList()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
        });

        using var context = new TestDbContext(options);
        var result = context.EntitiesWithTags.ToNonEmptyListOrNull();

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void ToNonEmptyListOrNull_WithNoResults_ShouldReturnNull()
    {
        var options = CreateOptionsWithData(_ => { });

        using var context = new TestDbContext(options);
        var result = context.EntitiesWithTags.ToNonEmptyListOrNull();

        Assert.Null(result);
    }

    [Fact]
    public async Task ToNonEmptyListOrNullAsync_WithResults_ShouldReturnNonEmptyList()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
        });

        await using var context = new TestDbContext(options);
        var result = await context.EntitiesWithTags.ToNonEmptyListOrNullAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ToNonEmptyListOrNullAsync_WithNoResults_ShouldReturnNull()
    {
        var options = CreateOptionsWithData(_ => { });

        await using var context = new TestDbContext(options);
        var result = await context.EntitiesWithTags.ToNonEmptyListOrNullAsync();

        Assert.Null(result);
    }

    [Fact]
    public void ToImmutableNonEmptyList_WithResults_ShouldReturnImmutableNonEmptyList()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test2", Tags = new NonEmptyList<string>("tag2") });
        });

        using var context = new TestDbContext(options);
        var result = context.EntitiesWithTags.ToImmutableNonEmptyList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ToImmutableNonEmptyList_WithNoResults_ShouldThrow()
    {
        var options = CreateOptionsWithData(_ => { });

        using var context = new TestDbContext(options);

        Assert.Throws<InvalidOperationException>(() =>
            context.EntitiesWithTags.ToImmutableNonEmptyList());
    }

    [Fact]
    public async Task ToImmutableNonEmptyListAsync_WithResults_ShouldReturnImmutableNonEmptyList()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
        });

        await using var context = new TestDbContext(options);
        var result = await context.EntitiesWithTags.ToImmutableNonEmptyListAsync();

        Assert.Single(result);
    }

    [Fact]
    public void ToImmutableNonEmptyListOrNull_WithNoResults_ShouldReturnNull()
    {
        var options = CreateOptionsWithData(_ => { });

        using var context = new TestDbContext(options);
        var result = context.EntitiesWithTags.ToImmutableNonEmptyListOrNull();

        Assert.Null(result);
    }

    [Fact]
    public async Task ToImmutableNonEmptyListOrNullAsync_WithResults_ShouldReturnImmutableNonEmptyList()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test1", Tags = new NonEmptyList<string>("tag1") });
        });

        await using var context = new TestDbContext(options);
        var result = await context.EntitiesWithTags.ToImmutableNonEmptyListOrNullAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public void ToNonEmptyList_WithFiltering_ShouldApplyFilter()
    {
        var options = CreateOptionsWithData(ctx =>
        {
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Apple", Tags = new NonEmptyList<string>("fruit") });
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Banana", Tags = new NonEmptyList<string>("fruit") });
            ctx.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Carrot", Tags = new NonEmptyList<string>("vegetable") });
        });

        using var context = new TestDbContext(options);
        var result = context.EntitiesWithTags
            .Where(e => e.Name.StartsWith("A") || e.Name.StartsWith("B"))
            .ToNonEmptyList();

        Assert.Equal(2, result.Count);
    }
}
