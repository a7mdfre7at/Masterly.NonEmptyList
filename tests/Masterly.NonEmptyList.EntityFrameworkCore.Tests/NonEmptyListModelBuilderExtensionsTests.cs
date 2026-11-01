using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

public class NonEmptyListModelBuilderExtensionsTests
{
    [Fact]
    public void ApplyNonEmptyListConventions_ShouldConfigureNonEmptyListProperties()
    {
        var options = new DbContextOptionsBuilder<TestDbContextWithConventions>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContextWithConventions(options);

        // Add entity with NonEmptyList property
        var entity = new TestEntityWithTags
        {
            Name = "Test",
            Tags = new NonEmptyList<string>("tag1", "tag2")
        };

        context.EntitiesWithTags.Add(entity);
        context.SaveChanges();

        // Clear change tracker
        context.ChangeTracker.Clear();

        // Retrieve and verify
        var savedEntity = context.EntitiesWithTags.FirstOrDefault();
        Assert.NotNull(savedEntity);
        Assert.Equal(2, savedEntity.Tags.Count);
        Assert.Equal("tag1", savedEntity.Tags.Head);
    }

    [Fact]
    public void ApplyNonEmptyListConventions_ShouldConfigureImmutableNonEmptyListProperties()
    {
        var options = new DbContextOptionsBuilder<TestDbContextWithConventions>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContextWithConventions(options);

        var entity = new TestEntityWithImmutableTags
        {
            Name = "Test",
            Tags = new ImmutableNonEmptyList<string>("immutable1", "immutable2")
        };

        context.EntitiesWithImmutableTags.Add(entity);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var savedEntity = context.EntitiesWithImmutableTags.FirstOrDefault();
        Assert.NotNull(savedEntity);
        Assert.Equal(2, savedEntity.Tags.Count);
    }

    [Fact]
    public void ApplyNonEmptyListConventions_ShouldConfigureMultipleLists()
    {
        var options = new DbContextOptionsBuilder<TestDbContextWithConventions>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContextWithConventions(options);

        var entity = new TestEntityWithMultipleLists
        {
            Numbers = new NonEmptyList<int>(1, 2, 3),
            Strings = new NonEmptyList<string>("a", "b")
        };

        context.EntitiesWithMultipleLists.Add(entity);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var savedEntity = context.EntitiesWithMultipleLists.FirstOrDefault();
        Assert.NotNull(savedEntity);
        Assert.Equal(3, savedEntity.Numbers.Count);
        Assert.Equal(2, savedEntity.Strings.Count);
    }

    [Fact]
    public void HasNonEmptyListConversion_ShouldEnableRoundtrip()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntityWithTags
        {
            Name = "Roundtrip Test",
            Tags = new NonEmptyList<string>("first", "second", "third")
        };

        context.EntitiesWithTags.Add(entity);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var savedEntity = context.EntitiesWithTags.FirstOrDefault();
        Assert.NotNull(savedEntity);
        Assert.Equal(3, savedEntity.Tags.Count);
        Assert.Equal(new[] { "first", "second", "third" }, savedEntity.Tags.ToArray());
    }

    [Fact]
    public void HasImmutableNonEmptyListConversion_ShouldEnableRoundtrip()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntityWithImmutableTags
        {
            Name = "Immutable Roundtrip Test",
            Tags = new ImmutableNonEmptyList<string>("first", "second")
        };

        context.EntitiesWithImmutableTags.Add(entity);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var savedEntity = context.EntitiesWithImmutableTags.FirstOrDefault();
        Assert.NotNull(savedEntity);
        Assert.Equal(2, savedEntity.Tags.Count);
    }

    [Fact]
    public void ChangeTracking_ShouldDetectChanges()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntityWithTags
        {
            Name = "Change Test",
            Tags = new NonEmptyList<string>("original")
        };

        context.EntitiesWithTags.Add(entity);
        context.SaveChanges();

        // Modify the list
        entity.Tags = new NonEmptyList<string>("modified");

        var entry = context.Entry(entity);
        Assert.True(entry.Property(e => e.Tags).IsModified);
    }

    [Fact]
    public void ChangeTracking_ShouldNotDetectUnchanged()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntityWithTags
        {
            Name = "No Change Test",
            Tags = new NonEmptyList<string>("value")
        };

        context.EntitiesWithTags.Add(entity);
        context.SaveChanges();

        // Don't modify the list, just access it
        var _ = entity.Tags.Head;

        var entry = context.Entry(entity);
        // Since we haven't changed anything, it should not be modified
        Assert.Equal(EntityState.Unchanged, entry.State);
    }

    [Fact]
    public void ComplexType_ShouldSerializeCorrectly()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var entity = new TestEntityWithComplexList
        {
            Addresses = new NonEmptyList<Address>(
                new Address { Street = "123 Main", City = "Springfield", ZipCode = "12345" },
                new Address { Street = "456 Oak", City = "Shelbyville", ZipCode = "67890" })
        };

        context.EntitiesWithComplexLists.Add(entity);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var savedEntity = context.EntitiesWithComplexLists.FirstOrDefault();
        Assert.NotNull(savedEntity);
        Assert.Equal(2, savedEntity.Addresses.Count);
        Assert.Equal("123 Main", savedEntity.Addresses.Head.Street);
        Assert.Equal("Springfield", savedEntity.Addresses.Head.City);
    }
    private static Microsoft.EntityFrameworkCore.Metadata.IMutableProperty? FindTags<TEntity>(ModelBuilder modelBuilder)
        => modelBuilder.Model.FindEntityType(typeof(TEntity))!.FindProperty("Tags");

    [Fact]
    public void ApplyNonEmptyListConventionsOnly_ShouldConfigureMutableListsAndLeaveImmutableListsAlone()
    {
        // A convention-less builder shows exactly what the method configures.
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestEntityWithTags>();
        modelBuilder.Entity<TestEntityWithImmutableTags>();

        modelBuilder.ApplyNonEmptyListConventionsOnly();

        var tags = FindTags<TestEntityWithTags>(modelBuilder);
        Assert.NotNull(tags);
        Assert.IsType<NonEmptyListValueConverter<string>>(tags.GetValueConverter());
        Assert.IsType<NonEmptyListValueComparer<string>>(tags.GetValueComparer());
        Assert.Null(FindTags<TestEntityWithImmutableTags>(modelBuilder));
    }

    [Fact]
    public void ApplyImmutableNonEmptyListConventionsOnly_ShouldConfigureImmutableListsAndLeaveMutableListsAlone()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestEntityWithTags>();
        modelBuilder.Entity<TestEntityWithImmutableTags>();

        modelBuilder.ApplyImmutableNonEmptyListConventionsOnly();

        var tags = FindTags<TestEntityWithImmutableTags>(modelBuilder);
        Assert.NotNull(tags);
        Assert.IsType<ImmutableNonEmptyListValueConverter<string>>(tags.GetValueConverter());
        Assert.IsType<ImmutableNonEmptyListValueComparer<string>>(tags.GetValueComparer());
        Assert.Null(FindTags<TestEntityWithTags>(modelBuilder));
    }

    [Fact]
    public void ApplyNonEmptyListConventions_ShouldSkipListsOfEntityTypes()
    {
        // Lists whose element is an entity are relationships, not JSON columns.
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<Track>();
        modelBuilder.Entity<Playlist>();
        modelBuilder.Entity<TrackCollection>();

        modelBuilder.ApplyNonEmptyListConventions();

        Assert.Null(modelBuilder.Model.FindEntityType(typeof(Playlist))!.FindProperty(nameof(Playlist.Tracks)));
        Assert.Null(modelBuilder.Model.FindEntityType(typeof(TrackCollection))!.FindProperty(nameof(TrackCollection.Tracks)));
    }

    [Fact]
    public void ApplyNonEmptyListConventionsOnly_ShouldRoundTripThroughTheDatabase()
    {
        static void Configure(ModelBuilder mb)
        {
            mb.Entity<TestEntityWithTags>();
            mb.ApplyNonEmptyListConventionsOnly();
        }

        var options = ConfigurableDbContext.CreateOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var context = new ConfigurableDbContext(options, Configure))
        {
            context.Add(new TestEntityWithTags { Name = "x", Tags = new NonEmptyList<string>("a", "b") });
            context.SaveChanges();
        }

        using (var context = new ConfigurableDbContext(options, Configure))
            Assert.Equal(new[] { "a", "b" }, context.Set<TestEntityWithTags>().Single().Tags);
    }

    [Fact]
    public void ApplyImmutableNonEmptyListConventionsOnly_ShouldRoundTripThroughTheDatabase()
    {
        static void Configure(ModelBuilder mb)
        {
            mb.Entity<TestEntityWithImmutableTags>();
            mb.ApplyImmutableNonEmptyListConventionsOnly();
        }

        var options = ConfigurableDbContext.CreateOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var context = new ConfigurableDbContext(options, Configure))
        {
            context.Add(new TestEntityWithImmutableTags { Name = "x", Tags = new ImmutableNonEmptyList<string>("a", "b") });
            context.SaveChanges();
        }

        using (var context = new ConfigurableDbContext(options, Configure))
            Assert.Equal(new[] { "a", "b" }, context.Set<TestEntityWithImmutableTags>().Single().Tags);
    }
}
