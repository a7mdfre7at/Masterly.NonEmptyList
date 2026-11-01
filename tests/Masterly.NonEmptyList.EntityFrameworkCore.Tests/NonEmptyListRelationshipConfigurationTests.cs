using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Masterly.NonEmptyList.EntityFrameworkCore.Tests;

/// <summary>
/// Exercises every relationship-configuration overload of <see cref="NonEmptyListExtensions"/>: each test checks the
/// resulting model metadata and round-trips data through an in-memory database.
/// </summary>
public class NonEmptyListRelationshipConfigurationTests
{
    private static DbContextOptions<ConfigurableDbContext> CreateOptions()
        => ConfigurableDbContext.CreateOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseNonEmptyList()
            .Options;

    private static IForeignKey SingleForeignKey<TDependent>(DbContext context)
        => Assert.Single(context.Model.FindEntityType(typeof(TDependent))!.GetForeignKeys());

    private static int SaveOrder(DbContextOptions<ConfigurableDbContext> options, Action<ModelBuilder> configure)
    {
        using var context = new ConfigurableDbContext(options, configure);
        var order = new Order
        {
            OrderNumber = "ORD-1",
            OrderItems = new NonEmptyList<OrderItem>(
                new OrderItem { ProductName = "Widget", Price = 10m },
                new OrderItem { ProductName = "Gadget", Price = 20m })
        };
        context.Add(order);
        context.SaveChanges();
        return order.Id;
    }

    private static void AssertOrderItemsStored(DbContextOptions<ConfigurableDbContext> options, Action<ModelBuilder> configure, int orderId)
    {
        using var context = new ConfigurableDbContext(options, configure);
        List<string> names = context.Set<OrderItem>()
            .Where(i => i.OrderId == orderId)
            .OrderBy(i => i.ProductName)
            .Select(i => i.ProductName)
            .ToList();

        Assert.Equal(new[] { "Gadget", "Widget" }, names);
    }

    private static int SavePlaylist(DbContextOptions<ConfigurableDbContext> options, Action<ModelBuilder> configure)
    {
        using var context = new ConfigurableDbContext(options, configure);
        var playlist = new Playlist
        {
            Name = "Mix",
            Tracks = new NonEmptyList<Track>(new Track { Title = "One" }, new Track { Title = "Two" })
        };
        context.Add(playlist);
        context.SaveChanges();
        return playlist.Id;
    }

    private static void AssertTracksStored(DbContextOptions<ConfigurableDbContext> options, Action<ModelBuilder> configure, int playlistId)
    {
        using var context = new ConfigurableDbContext(options, configure);
        Assert.Equal(2, context.Set<Track>().Count(t => t.PlaylistId == playlistId));
    }

    private static void AssertOrderRelationship(Action<ModelBuilder> configure)
    {
        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, configure))
        {
            IForeignKey foreignKey = SingleForeignKey<OrderItem>(context);
            Assert.Equal(nameof(Order.OrderItems), foreignKey.PrincipalToDependent!.Name);
            Assert.Equal(nameof(OrderItem.Order), foreignKey.DependentToPrincipal!.Name);
            Assert.Equal(nameof(OrderItem.OrderId), Assert.Single(foreignKey.Properties).Name);
        }

        int id = SaveOrder(options, configure);
        AssertOrderItemsStored(options, configure, id);
    }

    private static void AssertPlaylistRelationship(Action<ModelBuilder> configure)
    {
        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, configure))
        {
            IForeignKey foreignKey = SingleForeignKey<Track>(context);
            Assert.Equal(nameof(Playlist.Tracks), foreignKey.PrincipalToDependent!.Name);
            Assert.Null(foreignKey.DependentToPrincipal);
            Assert.Equal(nameof(Track.PlaylistId), Assert.Single(foreignKey.Properties).Name);
        }

        int id = SavePlaylist(options, configure);
        AssertTracksStored(options, configure, id);
    }

    #region One-to-many by property name

    [Fact]
    public void HasNonEmptyMany_ByName_ShouldConfigureOneToMany()
        => AssertPlaylistRelationship(mb => mb.Entity<Playlist>().HasNonEmptyMany<Playlist, Track>(nameof(Playlist.Tracks)));

    [Fact]
    public void HasNonEmptyMany_ByNameWithForeignKeyName_ShouldUseThatForeignKey()
        => AssertPlaylistRelationship(mb => mb.Entity<Playlist>().HasNonEmptyMany<Playlist, Track>(nameof(Playlist.Tracks), nameof(Track.PlaylistId)));

    [Fact]
    public void HasNonEmptyManyWithOne_ByNameWithInverseExpression_ShouldConfigureBothNavigations()
        => AssertOrderRelationship(mb => mb.Entity<Order>().HasNonEmptyManyWithOne<Order, OrderItem>(nameof(Order.OrderItems), oi => oi.Order));

    [Fact]
    public void HasNonEmptyManyWithOne_ByNameWithInverseAndForeignKeyExpressions_ShouldConfigureBothNavigations()
        => AssertOrderRelationship(mb => mb.Entity<Order>().HasNonEmptyManyWithOne<Order, OrderItem>(nameof(Order.OrderItems), oi => oi.Order, oi => oi.OrderId));

    [Fact]
    public void HasNonEmptyManyWithOne_AllByName_ShouldConfigureBothNavigations()
        => AssertOrderRelationship(mb => mb.Entity<Order>().HasNonEmptyManyWithOne<Order, OrderItem>(nameof(Order.OrderItems), nameof(OrderItem.Order), nameof(OrderItem.OrderId)));

    #endregion

    #region One-to-many by expression

    [Fact]
    public void HasNonEmptyMany_ByExpression_ShouldConfigureOneToMany()
        => AssertPlaylistRelationship(mb => mb.Entity<Playlist>().HasNonEmptyMany(p => p.Tracks));

    [Fact]
    public void HasNonEmptyManyWithOne_ByExpressionWithInverse_ShouldConfigureBothNavigations()
        => AssertOrderRelationship(mb => mb.Entity<Order>().HasNonEmptyManyWithOne(o => o.OrderItems, oi => oi.Order));

    [Fact]
    public void HasNonEmptyManyWithOne_ByExpressionWithInverseAndForeignKey_ShouldConfigureBothNavigations()
        => AssertOrderRelationship(mb => mb.Entity<Order>().HasNonEmptyManyWithOne(o => o.OrderItems, oi => oi.Order, oi => oi.OrderId));

    [Fact]
    public void HasNonEmptyMany_WithCastNavigation_ShouldResolveThePropertyName()
    {
        // The cast makes the lambda body a Convert around the member access.
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<Track>().HasKey(t => t.Id);
        modelBuilder.Entity<Album>().HasKey(a => a.Id);

        modelBuilder.Entity<Album>().HasNonEmptyMany(a => (NonEmptyList<Track>)a.Tracks);

        Assert.NotNull(modelBuilder.Model.FindEntityType(typeof(Album))!.FindNavigation(nameof(Album.Tracks)));
    }

    [Fact]
    public void HasNonEmptyMany_WithNonMemberExpression_ShouldThrowArgumentException()
    {
        var modelBuilder = new ModelBuilder();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            modelBuilder.Entity<Playlist>().HasNonEmptyMany(p => p.Tracks.Distinct()));

        Assert.Equal("expression", ex.ParamName);
    }

    #endregion

    #region Delete behavior

    [Theory]
    [InlineData(DeleteBehavior.Cascade)]
    [InlineData(DeleteBehavior.Restrict)]
    public void HasNonEmptyManyWithDeleteBehavior_ShouldApplyTheDeleteBehavior(DeleteBehavior behavior)
    {
        void Configure(ModelBuilder mb) => mb.Entity<Order>().HasNonEmptyManyWithDeleteBehavior(o => o.OrderItems, oi => oi.Order, behavior);

        var options = CreateOptions();
        using var context = new ConfigurableDbContext(options, Configure);

        IForeignKey foreignKey = SingleForeignKey<OrderItem>(context);
        Assert.Equal(behavior, foreignKey.DeleteBehavior);
        Assert.Equal(nameof(OrderItem.OrderId), Assert.Single(foreignKey.Properties).Name);
    }

    [Theory]
    [InlineData(DeleteBehavior.Cascade)]
    [InlineData(DeleteBehavior.NoAction)]
    public void HasNonEmptyManyWithDeleteBehavior_WithForeignKey_ShouldApplyTheDeleteBehavior(DeleteBehavior behavior)
    {
        void Configure(ModelBuilder mb) => mb.Entity<Order>().HasNonEmptyManyWithDeleteBehavior(o => o.OrderItems, oi => oi.Order, oi => oi.OrderId, behavior);

        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, Configure))
        {
            IForeignKey foreignKey = SingleForeignKey<OrderItem>(context);
            Assert.Equal(behavior, foreignKey.DeleteBehavior);
            Assert.Equal(nameof(OrderItem.OrderId), Assert.Single(foreignKey.Properties).Name);
        }

        int id = SaveOrder(options, Configure);
        AssertOrderItemsStored(options, Configure, id);
    }

    #endregion

    #region Required relationships

    [Fact]
    public void ProjectTaskRelationship_WithoutConfiguration_IsOptional()
    {
        // Baseline for the required tests below: the nullable foreign key makes the relationship optional by default.
        using var context = new ConfigurableDbContext(CreateOptions(), mb => mb.Entity<Project>().HasMany(p => p.Tasks).WithOne(t => t.Project));

        Assert.False(SingleForeignKey<ProjectTask>(context).IsRequired);
    }

    [Fact]
    public void HasRequiredNonEmptyMany_ShouldMakeTheRelationshipRequired()
    {
        void Configure(ModelBuilder mb) => mb.Entity<Project>().HasRequiredNonEmptyMany(p => p.Tasks, t => t.Project!);

        using var context = new ConfigurableDbContext(CreateOptions(), Configure);

        IForeignKey foreignKey = SingleForeignKey<ProjectTask>(context);
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(nameof(Project.Tasks), foreignKey.PrincipalToDependent!.Name);
        Assert.Equal(nameof(ProjectTask.Project), foreignKey.DependentToPrincipal!.Name);
    }

    [Fact]
    public void HasRequiredNonEmptyMany_WithForeignKey_ShouldMakeTheRelationshipRequired()
    {
        void Configure(ModelBuilder mb) => mb.Entity<Project>().HasRequiredNonEmptyMany(p => p.Tasks, t => t.Project!, t => t.ProjectId);

        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, Configure))
        {
            IForeignKey foreignKey = SingleForeignKey<ProjectTask>(context);
            Assert.True(foreignKey.IsRequired);
            Assert.Equal(nameof(ProjectTask.ProjectId), Assert.Single(foreignKey.Properties).Name);
        }

        using (var context = new ConfigurableDbContext(options, Configure))
        {
            context.Add(new Project { Name = "P", Tasks = new NonEmptyList<ProjectTask>(new ProjectTask { Title = "T" }) });
            context.SaveChanges();
        }

        using (var context = new ConfigurableDbContext(options, Configure))
        {
            Assert.All(context.Set<ProjectTask>().ToList(), t => Assert.NotNull(t.ProjectId));
        }
    }

    #endregion

    #region Many-to-many

    private static void AssertStudentCourseSkipNavigation(DbContext context)
    {
        ISkipNavigation skip = context.Model.FindEntityType(typeof(Student))!.FindSkipNavigation(nameof(Student.Courses))!;
        Assert.NotNull(skip);
        Assert.Equal(nameof(Course.Students), skip.Inverse.Name);
    }

    private static void AssertStudentCoursesRoundTrip(DbContextOptions<ConfigurableDbContext> options, Action<ModelBuilder> configure)
    {
        int studentId;
        using (var context = new ConfigurableDbContext(options, configure))
        {
            var math = new Course { Title = "Math" };
            var physics = new Course { Title = "Physics" };
            var student = new Student { Name = "Ada", Courses = new NonEmptyList<Course>(math, physics) };
            math.Students = new NonEmptyList<Student>(student);
            physics.Students = new NonEmptyList<Student>(student);
            context.Add(student);
            context.SaveChanges();
            studentId = student.Id;
        }

        using (var context = new ConfigurableDbContext(options, configure))
        {
            List<string> titles = context.Set<Student>()
                .Where(s => s.Id == studentId)
                .SelectMany(s => s.Courses.Select(c => c.Title))
                .OrderBy(t => t)
                .ToList();

            Assert.Equal(new[] { "Math", "Physics" }, titles);
        }
    }

    [Fact]
    public void HasNonEmptyManyToMany_SingleExpression_ShouldConfigureTheLeftSide()
    {
        void Configure(ModelBuilder mb) => mb.Entity<Student>().HasNonEmptyManyToMany(s => s.Courses).WithMany(c => c.Students);

        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, Configure))
            AssertStudentCourseSkipNavigation(context);

        AssertStudentCoursesRoundTrip(options, Configure);
    }

    [Fact]
    public void HasNonEmptyManyToMany_ExpressionAndInverseName_ShouldConfigureBothSides()
    {
        void Configure(ModelBuilder mb) => mb.Entity<Student>().HasNonEmptyManyToMany(s => s.Courses, nameof(Course.Students));

        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, Configure))
            AssertStudentCourseSkipNavigation(context);

        AssertStudentCoursesRoundTrip(options, Configure);
    }

    [Fact]
    public void HasNonEmptyManyToMany_BothNames_ShouldConfigureBothSides()
    {
        void Configure(ModelBuilder mb) => mb.Entity<Student>().HasNonEmptyManyToMany<Student, Course>(nameof(Student.Courses), nameof(Course.Students));

        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, Configure))
            AssertStudentCourseSkipNavigation(context);

        AssertStudentCoursesRoundTrip(options, Configure);
    }

    [Fact]
    public void HasNonEmptyManyToManyWithJoinTable_ShouldUseTheNamedJoinTableAndKeys()
    {
        void Configure(ModelBuilder mb) => mb.Entity<Student>().HasNonEmptyManyToManyWithJoinTable(
            s => s.Courses, c => c.Students, "StudentCourses", "StudentId", "CourseId");

        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, Configure))
        {
            AssertStudentCourseSkipNavigation(context);
            IEntityType join = context.Model.FindEntityType("StudentCourses")!;
            Assert.NotNull(join);
            Assert.Equal(
                new[] { "CourseId", "StudentId" },
                join.GetForeignKeys().Select(fk => Assert.Single(fk.Properties).Name).OrderBy(n => n));
        }

        AssertStudentCoursesRoundTrip(options, Configure);

        using (var context = new ConfigurableDbContext(options, Configure))
            Assert.Equal(2, context.Set<Dictionary<string, object>>("StudentCourses").Count());
    }

    [Fact]
    public void HasNonEmptyManyToMany_WithJoinEntity_ShouldUseTheJoinEntityType()
    {
        void Configure(ModelBuilder mb) => mb.Entity<Author>().HasNonEmptyManyToMany<Author, Book, AuthorBook>(
            a => a.Books,
            b => b.Authors,
            j =>
            {
                // UsingEntity<TJoin> creates the two foreign keys itself, so the join entity's own navigations
                // would compete with them; the keys are found by name (AuthorId, BookId).
                j.Ignore(ab => ab.Author);
                j.Ignore(ab => ab.Book);
                j.HasKey(ab => new { ab.AuthorId, ab.BookId });
            });

        var options = CreateOptions();
        using (var context = new ConfigurableDbContext(options, Configure))
        {
            ISkipNavigation skip = context.Model.FindEntityType(typeof(Author))!.FindSkipNavigation(nameof(Author.Books))!;
            Assert.Equal(typeof(AuthorBook), skip.JoinEntityType.ClrType);
            Assert.Equal(
                new[] { nameof(AuthorBook.AuthorId), nameof(AuthorBook.BookId) },
                skip.JoinEntityType.FindPrimaryKey()!.Properties.Select(p => p.Name));
        }

        using (var context = new ConfigurableDbContext(options, Configure))
        {
            var first = new Book { Title = "First" };
            var second = new Book { Title = "Second" };
            var author = new Author { Name = "Le Guin", Books = new NonEmptyList<Book>(first, second) };
            first.Authors = new NonEmptyList<Author>(author);
            second.Authors = new NonEmptyList<Author>(author);
            context.Add(author);
            context.SaveChanges();
        }

        using (var context = new ConfigurableDbContext(options, Configure))
            Assert.Equal(2, context.Set<AuthorBook>().Count());
    }

    #endregion
}
