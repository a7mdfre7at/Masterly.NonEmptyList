using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

/// <summary>
/// Tests for many-to-many relationships with NonEmptyList.
///
/// IMPORTANT: Due to EF Core's collection materialization mechanism, NonEmptyList navigation
/// properties cannot be directly populated using Include(). EF Core attempts to create an
/// empty collection first and then populate it, but NonEmptyList requires at least one element.
///
/// Supported patterns:
/// - Saving entities with NonEmptyList many-to-many navigations (relationship is stored correctly)
/// - Querying with projections (Select)
/// - Loading related data manually
///
/// Unsupported patterns:
/// - Using Include() on NonEmptyList navigation properties
/// </summary>
public class NonEmptyListManyToManyTests
{
    [Fact]
    public void ManyToMany_ShouldSaveRelationship()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var course1 = new Course { Title = "Math 101" };
        var course2 = new Course { Title = "Physics 101" };

        var student = new Student
        {
            Name = "John Doe",
            Courses = new NonEmptyList<Course>(course1, course2)
        };

        course1.Students = new NonEmptyList<Student>(student);
        course2.Students = new NonEmptyList<Student>(student);

        context.Students.Add(student);
        context.SaveChanges();

        // Verify entities were saved
        Assert.True(student.Id > 0);
        Assert.True(course1.Id > 0);
        Assert.True(course2.Id > 0);
    }

    [Fact]
    public void ManyToMany_ShouldQueryWithProjection()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var course1 = new Course { Title = "Chemistry 101" };
        var course2 = new Course { Title = "Biology 101" };

        var student = new Student
        {
            Name = "Jane Doe",
            Courses = new NonEmptyList<Course>(course1, course2)
        };

        course1.Students = new NonEmptyList<Student>(student);
        course2.Students = new NonEmptyList<Student>(student);

        context.Students.Add(student);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        // Use projection instead of Include
        var result = context.Students
            .Where(s => s.Id == student.Id)
            .Select(s => new
            {
                s.Name,
                CourseCount = s.Courses.Count(),
                CourseTitles = s.Courses.Select(c => c.Title).ToList()
            })
            .FirstOrDefault();

        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result.Name);
        Assert.Equal(2, result.CourseCount);
        Assert.Contains("Chemistry 101", result.CourseTitles);
        Assert.Contains("Biology 101", result.CourseTitles);
    }

    [Fact]
    public void ManyToMany_WithExplicitJoinEntity_ShouldSave()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var book = new Book { Title = "The Great Novel" };
        var author1 = new Author { Name = "Jane Austen" };
        var author2 = new Author { Name = "Emily Bronte" };

        author1.Books = new NonEmptyList<Book>(book);
        author2.Books = new NonEmptyList<Book>(book);
        book.Authors = new NonEmptyList<Author>(author1, author2);

        context.Books.Add(book);
        context.SaveChanges();

        // Verify entities were saved
        Assert.True(book.Id > 0);
        Assert.True(author1.Id > 0);
        Assert.True(author2.Id > 0);
    }

    [Fact]
    public void ManyToMany_WithExplicitJoinEntity_ShouldQueryWithProjection()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var book = new Book { Title = "Classic Literature" };
        var author1 = new Author { Name = "Author One" };
        var author2 = new Author { Name = "Author Two" };

        author1.Books = new NonEmptyList<Book>(book);
        author2.Books = new NonEmptyList<Book>(book);
        book.Authors = new NonEmptyList<Author>(author1, author2);

        context.Books.Add(book);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        // Query with projection
        var result = context.Books
            .Where(b => b.Id == book.Id)
            .Select(b => new
            {
                b.Title,
                AuthorCount = b.Authors.Count(),
                AuthorNames = b.Authors.Select(a => a.Name).ToList()
            })
            .FirstOrDefault();

        Assert.NotNull(result);
        Assert.Equal("Classic Literature", result.Title);
        Assert.Equal(2, result.AuthorCount);
        Assert.Contains("Author One", result.AuthorNames);
        Assert.Contains("Author Two", result.AuthorNames);
    }

    [Fact]
    public void ManyToMany_MultipleStudentsMultipleCourses_ShouldQueryCorrectly()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var course1 = new Course { Title = "English 101" };
        var course2 = new Course { Title = "French 101" };
        var course3 = new Course { Title = "Spanish 101" };

        var student1 = new Student { Name = "David" };
        var student2 = new Student { Name = "Emma" };
        var student3 = new Student { Name = "Frank" };

        // David takes English and French
        student1.Courses = new NonEmptyList<Course>(course1, course2);
        // Emma takes all three
        student2.Courses = new NonEmptyList<Course>(course1, course2, course3);
        // Frank takes only Spanish
        student3.Courses = new NonEmptyList<Course>(course3);

        course1.Students = new NonEmptyList<Student>(student1, student2);
        course2.Students = new NonEmptyList<Student>(student1, student2);
        course3.Students = new NonEmptyList<Student>(student2, student3);

        context.Students.AddRange(student1, student2, student3);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        // Query using projections
        var englishStudentCount = context.Courses
            .Where(c => c.Title == "English 101")
            .Select(c => c.Students.Count())
            .FirstOrDefault();
        Assert.Equal(2, englishStudentCount);

        var emmaCourseCount = context.Students
            .Where(s => s.Name == "Emma")
            .Select(s => s.Courses.Count())
            .FirstOrDefault();
        Assert.Equal(3, emmaCourseCount);

        var spanishStudentCount = context.Courses
            .Where(c => c.Title == "Spanish 101")
            .Select(c => c.Students.Count())
            .FirstOrDefault();
        Assert.Equal(2, spanishStudentCount);
    }

    [Fact]
    public void ManyToMany_ShouldQueryWithFilters()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var advancedCourse = new Course { Title = "Advanced Mathematics" };
        var beginnerCourse = new Course { Title = "Basic Mathematics" };

        var student1 = new Student { Name = "Grace" };
        var student2 = new Student { Name = "Henry" };

        student1.Courses = new NonEmptyList<Course>(advancedCourse);
        student2.Courses = new NonEmptyList<Course>(beginnerCourse, advancedCourse);

        advancedCourse.Students = new NonEmptyList<Student>(student1, student2);
        beginnerCourse.Students = new NonEmptyList<Student>(student2);

        context.Students.AddRange(student1, student2);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        // Query students taking advanced courses using projection
        var advancedStudentNames = context.Students
            .Where(s => s.Courses.Any(c => c.Title.StartsWith("Advanced")))
            .Select(s => s.Name)
            .ToList();

        Assert.Equal(2, advancedStudentNames.Count);
        Assert.Contains("Grace", advancedStudentNames);
        Assert.Contains("Henry", advancedStudentNames);
    }

    [Fact]
    public void ManyToMany_IncludeThrows_DemonstratesLimitation()
    {
        // This test demonstrates the limitation: Include() cannot work with NonEmptyList
        // because EF Core cannot create an empty NonEmptyList instance during materialization

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        var course = new Course { Title = "Test Course" };
        var student = new Student
        {
            Name = "Test Student",
            Courses = new NonEmptyList<Course>(course)
        };
        course.Students = new NonEmptyList<Student>(student);

        context.Students.Add(student);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        // This should throw because EF Core cannot instantiate NonEmptyList<Course>
        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.Students.Include(s => s.Courses).FirstOrDefault());

        Assert.Contains("NonEmptyList", exception.Message);
    }
}
