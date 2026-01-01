using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class NonEmptyListSaveChangesInterceptorTests
{
    private static DbContextOptions<TestDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseNonEmptyListValidation()
            .Options;
    }

    [Fact]
    public void SaveChanges_WithValidNonEmptyList_ShouldSucceed()
    {
        using var context = new TestDbContext(CreateOptions());

        var entity = new TestEntityWithTags
        {
            Name = "Test",
            Tags = new NonEmptyList<string>("tag1", "tag2")
        };

        context.EntitiesWithTags.Add(entity);
        int result = context.SaveChanges();

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task SaveChangesAsync_WithValidNonEmptyList_ShouldSucceed()
    {
        await using var context = new TestDbContext(CreateOptions());

        var entity = new TestEntityWithTags
        {
            Name = "Test",
            Tags = new NonEmptyList<string>("tag1")
        };

        context.EntitiesWithTags.Add(entity);
        int result = await context.SaveChangesAsync();

        Assert.Equal(1, result);
    }

    [Fact]
    public void SaveChanges_WithMultipleEntities_ShouldValidateAll()
    {
        using var context = new TestDbContext(CreateOptions());

        var entity1 = new TestEntityWithTags
        {
            Name = "Test1",
            Tags = new NonEmptyList<string>("tag1")
        };

        var entity2 = new TestEntityWithTags
        {
            Name = "Test2",
            Tags = new NonEmptyList<string>("tag2")
        };

        context.EntitiesWithTags.AddRange(entity1, entity2);
        int result = context.SaveChanges();

        Assert.Equal(2, result);
    }

    [Fact]
    public void SaveChanges_WithValidImmutableNonEmptyList_ShouldSucceed()
    {
        using var context = new TestDbContext(CreateOptions());

        var entity = new TestEntityWithImmutableTags
        {
            Name = "Test",
            Tags = new ImmutableNonEmptyList<string>("tag1", "tag2")
        };

        context.EntitiesWithImmutableTags.Add(entity);
        int result = context.SaveChanges();

        Assert.Equal(1, result);
    }

    [Fact]
    public void SaveChanges_OnUpdate_ShouldValidate()
    {
        using var context = new TestDbContext(CreateOptions());

        var entity = new TestEntityWithTags
        {
            Name = "Test",
            Tags = new NonEmptyList<string>("tag1")
        };

        context.EntitiesWithTags.Add(entity);
        context.SaveChanges();

        // Update the entity
        entity.Name = "Updated";
        entity.Tags = new NonEmptyList<string>("newtag1", "newtag2");
        int result = context.SaveChanges();

        Assert.Equal(1, result);
    }

    [Fact]
    public void SaveChanges_WithMultipleLists_ShouldValidateAll()
    {
        using var context = new TestDbContext(CreateOptions());

        var entity = new TestEntityWithMultipleLists
        {
            Numbers = new NonEmptyList<int>(1, 2, 3),
            Strings = new NonEmptyList<string>("a", "b")
        };

        context.EntitiesWithMultipleLists.Add(entity);
        int result = context.SaveChanges();

        Assert.Equal(1, result);
    }
}
