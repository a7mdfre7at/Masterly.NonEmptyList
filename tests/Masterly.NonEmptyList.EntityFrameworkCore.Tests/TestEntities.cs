using System.ComponentModel.DataAnnotations.Schema;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

/// <summary>
/// Test entity with a NonEmptyList property stored as JSON.
/// </summary>
public class TestEntityWithTags
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NonEmptyList<string> Tags { get; set; } = new("default");
}

/// <summary>
/// Test entity with an ImmutableNonEmptyList property stored as JSON.
/// </summary>
public class TestEntityWithImmutableTags
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ImmutableNonEmptyList<string> Tags { get; set; } = new ImmutableNonEmptyList<string>("default");
}

/// <summary>
/// Test parent entity for relationship testing.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public NonEmptyList<OrderItem> OrderItems { get; set; } = null!;
}

/// <summary>
/// Test child entity for relationship testing.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
}

/// <summary>
/// Test entity with multiple NonEmptyList properties.
/// </summary>
public class TestEntityWithMultipleLists
{
    public int Id { get; set; }
    public NonEmptyList<int> Numbers { get; set; } = new(0);
    public NonEmptyList<string> Strings { get; set; } = new("default");
}

/// <summary>
/// Test entity with complex type in NonEmptyList.
/// </summary>
public class TestEntityWithComplexList
{
    public int Id { get; set; }
    public NonEmptyList<Address> Addresses { get; set; } = new(new Address());
}

/// <summary>
/// Complex type for testing JSON serialization.
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

/// <summary>
/// Test entity for many-to-many relationship (left side).
/// </summary>
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NonEmptyList<Course> Courses { get; set; } = null!;
}

/// <summary>
/// Test entity for many-to-many relationship (right side).
/// </summary>
public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public NonEmptyList<Student> Students { get; set; } = null!;
}

/// <summary>
/// Test entity for many-to-many with explicit join entity.
/// </summary>
public class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NonEmptyList<Book> Books { get; set; } = null!;
}

/// <summary>
/// Test entity for many-to-many with explicit join entity.
/// </summary>
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public NonEmptyList<Author> Authors { get; set; } = null!;
}

/// <summary>
/// Explicit join entity for Author-Book relationship.
/// </summary>
public class AuthorBook
{
    public int AuthorId { get; set; }
    public int BookId { get; set; }
    public DateTime AssignedDate { get; set; }
    public Author Author { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

/// <summary>
/// Test entity for SaveChanges validation of derived and nullable non-empty properties. The shapes are not mapped,
/// because the interceptor inspects every public property of the entity, mapped or not.
/// </summary>
public class TestEntityWithValidatedShapes
{
    public int Id { get; set; }

    [NotMapped]
    public ConstrainedList<int, AtLeast<Two>> Pair { get; set; } = [1, 2];

    [NotMapped]
    public NonEmptyList<string>? OptionalTags { get; set; }

    [NotMapped]
    public NonEmptySet<string>? OptionalSet { get; set; }

    [NotMapped]
    public NonEmptySet<string> RequiredSet { get; set; } = NonEmptySet.Of("a");
}

/// <summary>
/// Principal whose non-empty navigation has no inverse navigation on the dependent.
/// </summary>
public class Playlist
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NonEmptyList<Track> Tracks { get; set; } = null!;
}

/// <summary>
/// Dependent of <see cref="Playlist"/>, reachable only through the foreign key.
/// </summary>
public class Track
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PlaylistId { get; set; }
}

/// <summary>
/// Principal whose dependents have a nullable foreign key, so the relationship is optional unless configured otherwise.
/// </summary>
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NonEmptyList<ProjectTask> Tasks { get; set; } = null!;
}

/// <summary>
/// Dependent of <see cref="Project"/> with a nullable foreign key.
/// </summary>
public class ProjectTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
}

/// <summary>
/// Principal whose navigation is declared as an interface, so configuring it as a NonEmptyList needs a cast.
/// </summary>
public class Album
{
    public int Id { get; set; }
    public IReadOnlyCollection<Track> Tracks { get; set; } = null!;
}

/// <summary>
/// Entity with an ImmutableNonEmptyList whose element type is itself an entity.
/// </summary>
public class TrackCollection
{
    public int Id { get; set; }
    public ImmutableNonEmptyList<Track> Tracks { get; set; } = null!;
}
