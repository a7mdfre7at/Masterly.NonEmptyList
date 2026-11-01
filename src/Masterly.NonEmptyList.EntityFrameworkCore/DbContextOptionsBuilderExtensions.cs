using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// Extension methods for DbContextOptionsBuilder to configure NonEmptyList support.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Enables full NonEmptyList support: SaveChanges validation of NonEmptyList properties and translation of
    /// NonEmptyList LINQ operators (<c>Select</c>, <c>OrderBy</c>, <c>Sum</c>, <c>Count</c>, <c>Head</c>, ...) inside queries.
    /// This is the recommended single call.
    /// </summary>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same DbContextOptionsBuilder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options
    ///         .UseSqlServer(connectionString)
    ///         .UseNonEmptyList());
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder UseNonEmptyList(this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(new NonEmptyListSaveChangesInterceptor(), new NonEmptyListQueryExpressionInterceptor());
        return optionsBuilder;
    }

    /// <summary>
    /// Enables full NonEmptyList support: SaveChanges validation of NonEmptyList properties and translation of
    /// NonEmptyList LINQ operators inside queries. This is the recommended single call.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same DbContextOptionsBuilder for chaining.</returns>
    public static DbContextOptionsBuilder<TContext> UseNonEmptyList<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(new NonEmptyListSaveChangesInterceptor(), new NonEmptyListQueryExpressionInterceptor());
        return optionsBuilder;
    }

    /// <summary>
    /// Adds the NonEmptyList SaveChanges interceptor to validate that NonEmptyList properties
    /// always contain at least one element when saving changes.
    /// </summary>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same DbContextOptionsBuilder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options
    ///         .UseSqlServer(connectionString)
    ///         .UseNonEmptyListValidation());
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder UseNonEmptyListValidation(this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(new NonEmptyListSaveChangesInterceptor());
        return optionsBuilder;
    }

    /// <summary>
    /// Adds the NonEmptyList SaveChanges interceptor to validate that NonEmptyList properties
    /// always contain at least one element when saving changes.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same DbContextOptionsBuilder for chaining.</returns>
    public static DbContextOptionsBuilder<TContext> UseNonEmptyListValidation<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(new NonEmptyListSaveChangesInterceptor());
        return optionsBuilder;
    }

    /// <summary>
    /// Adds the query interceptor that rewrites NonEmptyList LINQ operators to their Enumerable equivalents before
    /// EF Core compiles a query. Without it, expressions such as <c>order.Items.Select(i => i.Name)</c> on a
    /// NonEmptyList navigation fail to translate.
    /// </summary>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same DbContextOptionsBuilder for chaining.</returns>
    public static DbContextOptionsBuilder UseNonEmptyListQueryTranslation(this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(new NonEmptyListQueryExpressionInterceptor());
        return optionsBuilder;
    }

    /// <summary>
    /// Adds the query interceptor that rewrites NonEmptyList LINQ operators to their Enumerable equivalents before
    /// EF Core compiles a query.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
    /// <returns>The same DbContextOptionsBuilder for chaining.</returns>
    public static DbContextOptionsBuilder<TContext> UseNonEmptyListQueryTranslation<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(new NonEmptyListQueryExpressionInterceptor());
        return optionsBuilder;
    }
}
