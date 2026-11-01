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
        int result = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    public void SaveChanges_WithNullRequiredList_ShouldThrowNamingTheProperty()
    {
        using var context = new TestDbContext(CreateOptions());

        context.EntitiesWithTags.Add(new TestEntityWithTags { Name = "Test", Tags = null! });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Contains("'Tags'", ex.Message);
        Assert.Contains("cannot be null", ex.Message);
    }

    [Fact]
    public void SaveChanges_WithNullOnNullableProperties_ShouldSucceed()
    {
        using var context = new TestDbContext(CreateOptions());

        context.EntitiesWithValidatedShapes.Add(new TestEntityWithValidatedShapes { OptionalTags = null, OptionalSet = null });

        Assert.Equal(1, context.SaveChanges());
    }

    [Fact]
    public void SaveChanges_WithNullDerivedType_ShouldThrow()
    {
        using var context = new TestDbContext(CreateOptions());

        context.EntitiesWithValidatedShapes.Add(new TestEntityWithValidatedShapes { Pair = null! });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Contains("'Pair'", ex.Message);
    }

    [Fact]
    public void SaveChanges_WithNullNonEmptySet_ShouldThrow()
    {
        using var context = new TestDbContext(CreateOptions());

        context.EntitiesWithValidatedShapes.Add(new TestEntityWithValidatedShapes { RequiredSet = null! });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Contains("'RequiredSet'", ex.Message);
    }
    [Fact]
    public void SaveChanges_WithNonEmptyCollectionEmptiedBehindItsBack_ShouldThrow()
    {
        // A value built by bypassing the public API (reflection, a lax deserializer) can still be empty; the
        // interceptor is the last line of defence before it reaches the database.
        using var context = new TestDbContext(CreateOptions());
        var entity = new TestEntityWithValidatedShapes { RequiredSet = NonEmptySet.Of("a") };
        var items = (HashSet<string>)typeof(NonEmptySet<string>)
            .GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(entity.RequiredSet)!;
        items.Clear();

        context.EntitiesWithValidatedShapes.Add(entity);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Contains("'RequiredSet'", ex.Message);
        Assert.Contains("is empty", ex.Message);
    }

    [Fact]
    public async Task SavingChanges_WithoutContext_ShouldPassTheResultThrough()
    {
        var interceptor = new NonEmptyListSaveChangesInterceptor();
        var eventData = new Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData(
            new TestEventDefinition(), (_, _) => string.Empty, context: null);
        var result = Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>.SuppressWithResult(7);

        Assert.Equal(7, interceptor.SavingChanges(eventData, result).Result);
        Assert.Equal(7, (await interceptor.SavingChangesAsync(eventData, result, TestContext.Current.CancellationToken)).Result);
    }

#pragma warning disable EF1001 // LoggingOptions is internal API; it is only needed to build an event definition.
    private sealed class TestEventDefinition()
        : Microsoft.EntityFrameworkCore.Diagnostics.EventDefinitionBase(
            new Microsoft.EntityFrameworkCore.Diagnostics.Internal.LoggingOptions(),
            new Microsoft.Extensions.Logging.EventId(1),
            Microsoft.Extensions.Logging.LogLevel.Debug,
            "Test");
#pragma warning restore EF1001
}
