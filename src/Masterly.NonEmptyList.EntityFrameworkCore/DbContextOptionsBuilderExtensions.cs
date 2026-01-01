using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// Extension methods for DbContextOptionsBuilder to configure NonEmptyList support.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
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
    public static DbContextOptionsBuilder UseNonEmptyListValidation(
        this DbContextOptionsBuilder optionsBuilder)
    {
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
    /// <example>
    /// <code>
    /// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    /// {
    ///     optionsBuilder
    ///         .UseSqlServer(connectionString)
    ///         .UseNonEmptyListValidation();
    /// }
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<TContext> UseNonEmptyListValidation<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
    {
        optionsBuilder.AddInterceptors(new NonEmptyListSaveChangesInterceptor());
        return optionsBuilder;
    }
}
