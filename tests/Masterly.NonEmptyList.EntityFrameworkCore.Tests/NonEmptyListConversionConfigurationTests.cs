using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

/// <summary>
/// Exercises the value-conversion overloads that take JSON options and/or an element comparer, against SQLite so the
/// stored JSON can be read back as text.
/// </summary>
public sealed class NonEmptyListConversionConfigurationTests : IDisposable
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public NonEmptyListConversionConfigurationTests() => _connection.Open();

    public void Dispose() => _connection.Dispose();

    private ConfigurableDbContext CreateContext(Action<ModelBuilder> configure)
    {
        var options = ConfigurableDbContext.CreateOptionsBuilder().UseSqlite(_connection).Options;
        var context = new ConfigurableDbContext(options, configure);
        context.Database.EnsureCreated();
        return context;
    }

    private string ReadRawColumn(string table, string column)
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = $"SELECT \"{column}\" FROM \"{table}\"";
        return (string)command.ExecuteScalar()!;
    }

    [Fact]
    public void HasNonEmptyListConversion_WithJsonOptions_ShouldSerializeWithThoseOptions()
    {
        static void Configure(ModelBuilder mb) => mb.Entity<TestEntityWithComplexList>().Property(e => e.Addresses).HasNonEmptyListConversion(CamelCase);

        using (var context = CreateContext(Configure))
        {
            context.Add(new TestEntityWithComplexList
            {
                Addresses = new NonEmptyList<Address>(new Address { Street = "Main", City = "Amman", ZipCode = "11110" })
            });
            context.SaveChanges();
        }

        string json = ReadRawColumn(nameof(TestEntityWithComplexList), nameof(TestEntityWithComplexList.Addresses));
        Assert.Contains("\"street\":\"Main\"", json);
        Assert.DoesNotContain("\"Street\"", json);

        using (var context = CreateContext(Configure))
        {
            Address address = Assert.Single(context.Set<TestEntityWithComplexList>().Single().Addresses);
            Assert.Equal("Main", address.Street);
            Assert.Equal("Amman", address.City);
        }
    }

    [Fact]
    public void HasNonEmptyListConversion_WithElementComparer_ShouldUseItForChangeDetection()
    {
        using var context = CreateContext(mb => mb.Entity<TestEntityWithTags>().Property(e => e.Tags).HasNonEmptyListConversion(StringComparer.OrdinalIgnoreCase));

        var entity = new TestEntityWithTags { Name = "x", Tags = new NonEmptyList<string>("a", "b") };
        context.Add(entity);
        context.SaveChanges();

        entity.Tags = new NonEmptyList<string>("A", "B");
        context.ChangeTracker.DetectChanges();
        Assert.False(context.Entry(entity).Property(e => e.Tags).IsModified);

        entity.Tags = new NonEmptyList<string>("A", "C");
        context.ChangeTracker.DetectChanges();
        Assert.True(context.Entry(entity).Property(e => e.Tags).IsModified);
    }

    [Fact]
    public void HasNonEmptyListConversion_WithJsonOptionsAndElementComparer_ShouldApplyBoth()
    {
        static void Configure(ModelBuilder mb) => mb.Entity<TestEntityWithTags>().Property(e => e.Tags).HasNonEmptyListConversion(Indented, StringComparer.OrdinalIgnoreCase);

        using (var context = CreateContext(Configure))
        {
            var entity = new TestEntityWithTags { Name = "x", Tags = new NonEmptyList<string>("a", "b") };
            context.Add(entity);
            context.SaveChanges();

            entity.Tags = new NonEmptyList<string>("A", "B");
            context.ChangeTracker.DetectChanges();
            Assert.False(context.Entry(entity).Property(e => e.Tags).IsModified);
        }

        Assert.Contains("\n", ReadRawColumn(nameof(TestEntityWithTags), nameof(TestEntityWithTags.Tags)));

        using (var context = CreateContext(Configure))
            Assert.Equal(new[] { "a", "b" }, context.Set<TestEntityWithTags>().Single().Tags);
    }

    [Fact]
    public void HasImmutableNonEmptyListConversion_WithJsonOptions_ShouldSerializeWithThoseOptions()
    {
        static void Configure(ModelBuilder mb) => mb.Entity<TestEntityWithImmutableTags>().Property(e => e.Tags).HasImmutableNonEmptyListConversion(Indented);

        using (var context = CreateContext(Configure))
        {
            context.Add(new TestEntityWithImmutableTags { Name = "x", Tags = new ImmutableNonEmptyList<string>("a", "b") });
            context.SaveChanges();
        }

        Assert.Contains("\n", ReadRawColumn(nameof(TestEntityWithImmutableTags), nameof(TestEntityWithImmutableTags.Tags)));

        using (var context = CreateContext(Configure))
            Assert.Equal(new[] { "a", "b" }, context.Set<TestEntityWithImmutableTags>().Single().Tags);
    }

    [Fact]
    public void HasImmutableNonEmptyListConversion_WithElementComparer_ShouldUseItForChangeDetection()
    {
        using var context = CreateContext(mb => mb.Entity<TestEntityWithImmutableTags>().Property(e => e.Tags).HasImmutableNonEmptyListConversion(StringComparer.OrdinalIgnoreCase));

        var entity = new TestEntityWithImmutableTags { Name = "x", Tags = new ImmutableNonEmptyList<string>("a", "b") };
        context.Add(entity);
        context.SaveChanges();

        entity.Tags = new ImmutableNonEmptyList<string>("A", "B");
        context.ChangeTracker.DetectChanges();
        Assert.False(context.Entry(entity).Property(e => e.Tags).IsModified);

        entity.Tags = new ImmutableNonEmptyList<string>("A", "C");
        context.ChangeTracker.DetectChanges();
        Assert.True(context.Entry(entity).Property(e => e.Tags).IsModified);
    }
}
