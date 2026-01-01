using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring NonEmptyList properties in Entity Framework Core.
/// </summary>
public static class NonEmptyListExtensions
{
    /// <summary>
    /// Configures a property to use the NonEmptyList value converter and comparer.
    /// The property will be stored as a JSON string in the database.
    /// </summary>
    /// <typeparam name="T">The type of elements in the NonEmptyList.</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <returns>The property builder for further configuration.</returns>
    public static PropertyBuilder<NonEmptyList<T>> HasNonEmptyListConversion<T>(
        this PropertyBuilder<NonEmptyList<T>> builder)
    {
        builder.HasConversion(new NonEmptyListValueConverter<T>());
        builder.Metadata.SetValueComparer(new NonEmptyListValueComparer<T>());
        return builder;
    }

    /// <summary>
    /// Configures a property to use the NonEmptyList value converter and comparer with custom JSON options.
    /// </summary>
    /// <typeparam name="T">The type of elements in the NonEmptyList.</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <param name="jsonSerializerOptions">Custom JSON serializer options.</param>
    /// <returns>The property builder for further configuration.</returns>
    public static PropertyBuilder<NonEmptyList<T>> HasNonEmptyListConversion<T>(
        this PropertyBuilder<NonEmptyList<T>> builder,
        JsonSerializerOptions jsonSerializerOptions)
    {
        builder.HasConversion(new NonEmptyListValueConverter<T>(jsonSerializerOptions));
        builder.Metadata.SetValueComparer(new NonEmptyListValueComparer<T>());
        return builder;
    }

    /// <summary>
    /// Configures a property to use the NonEmptyList value converter and comparer with a custom element comparer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the NonEmptyList.</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <param name="elementComparer">Custom equality comparer for list elements.</param>
    /// <returns>The property builder for further configuration.</returns>
    public static PropertyBuilder<NonEmptyList<T>> HasNonEmptyListConversion<T>(
        this PropertyBuilder<NonEmptyList<T>> builder,
        IEqualityComparer<T> elementComparer)
    {
        builder.HasConversion(new NonEmptyListValueConverter<T>());
        builder.Metadata.SetValueComparer(new NonEmptyListValueComparer<T>(elementComparer));
        return builder;
    }

    /// <summary>
    /// Configures a property to use the NonEmptyList value converter and comparer with custom options.
    /// </summary>
    /// <typeparam name="T">The type of elements in the NonEmptyList.</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <param name="jsonSerializerOptions">Custom JSON serializer options.</param>
    /// <param name="elementComparer">Custom equality comparer for list elements.</param>
    /// <returns>The property builder for further configuration.</returns>
    public static PropertyBuilder<NonEmptyList<T>> HasNonEmptyListConversion<T>(
        this PropertyBuilder<NonEmptyList<T>> builder,
        JsonSerializerOptions jsonSerializerOptions,
        IEqualityComparer<T> elementComparer)
    {
        builder.HasConversion(new NonEmptyListValueConverter<T>(jsonSerializerOptions));
        builder.Metadata.SetValueComparer(new NonEmptyListValueComparer<T>(elementComparer));
        return builder;
    }

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList.
    /// This stores the related entities in a separate table with a foreign key relationship.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationPropertyName">The name of the NonEmptyList navigation property.</param>
    /// <returns>A collection navigation builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyMany&lt;Order, OrderItem&gt;("OrderItems");
    /// </code>
    /// </example>
    public static CollectionNavigationBuilder<TEntity, TRelated> HasNonEmptyMany<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        string navigationPropertyName)
        where TEntity : class
        where TRelated : class
    {
        return entityBuilder.HasMany<TRelated>(navigationPropertyName);
    }

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList,
    /// with a specified inverse navigation property on the dependent entity.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationPropertyName">The name of the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyManyWithOne&lt;Order, OrderItem&gt;("OrderItems", oi => oi.Order);
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyManyWithOne<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        string navigationPropertyName,
        Expression<Func<TRelated, TEntity?>> inverseNavigationExpression)
        where TEntity : class
        where TRelated : class
    {
        return entityBuilder
            .HasMany<TRelated>(navigationPropertyName)
            .WithOne(inverseNavigationExpression);
    }

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList,
    /// with both inverse navigation and foreign key specified.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationPropertyName">The name of the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <param name="foreignKeyExpression">Expression to specify the foreign key property on the dependent entity.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyManyWithOne&lt;Order, OrderItem&gt;("OrderItems", oi => oi.Order, oi => oi.OrderId);
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyManyWithOne<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        string navigationPropertyName,
        Expression<Func<TRelated, TEntity?>> inverseNavigationExpression,
        Expression<Func<TRelated, object?>> foreignKeyExpression)
        where TEntity : class
        where TRelated : class
    {
        return entityBuilder
            .HasMany<TRelated>(navigationPropertyName)
            .WithOne(inverseNavigationExpression)
            .HasForeignKey(foreignKeyExpression);
    }

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList,
    /// with a specified foreign key property name on the dependent entity.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationPropertyName">The name of the NonEmptyList navigation property.</param>
    /// <param name="foreignKeyPropertyName">The name of the foreign key property on the dependent entity.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyMany&lt;Order, OrderItem&gt;("OrderItems", "OrderId");
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyMany<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        string navigationPropertyName,
        string foreignKeyPropertyName)
        where TEntity : class
        where TRelated : class
    {
        return entityBuilder
            .HasMany<TRelated>(navigationPropertyName)
            .WithOne()
            .HasForeignKey(foreignKeyPropertyName);
    }

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList,
    /// with inverse navigation specified by property name.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationPropertyName">The name of the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationPropertyName">The name of the inverse navigation property on the dependent.</param>
    /// <param name="foreignKeyPropertyName">The name of the foreign key property on the dependent entity.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyManyWithOne&lt;Order, OrderItem&gt;("OrderItems", "Order", "OrderId");
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyManyWithOne<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        string navigationPropertyName,
        string inverseNavigationPropertyName,
        string foreignKeyPropertyName)
        where TEntity : class
        where TRelated : class
    {
        return entityBuilder
            .HasMany<TRelated>(navigationPropertyName)
            .WithOne(inverseNavigationPropertyName)
            .HasForeignKey(foreignKeyPropertyName);
    }

    #region ImmutableNonEmptyList Value Conversion

    /// <summary>
    /// Configures a property to use the ImmutableNonEmptyList value converter and comparer.
    /// The property will be stored as a JSON string in the database.
    /// </summary>
    /// <typeparam name="T">The type of elements in the ImmutableNonEmptyList.</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <returns>The property builder for further configuration.</returns>
    public static PropertyBuilder<ImmutableNonEmptyList<T>> HasImmutableNonEmptyListConversion<T>(
        this PropertyBuilder<ImmutableNonEmptyList<T>> builder)
    {
        builder.HasConversion(new ImmutableNonEmptyListValueConverter<T>());
        builder.Metadata.SetValueComparer(new ImmutableNonEmptyListValueComparer<T>());
        return builder;
    }

    /// <summary>
    /// Configures a property to use the ImmutableNonEmptyList value converter and comparer with custom JSON options.
    /// </summary>
    /// <typeparam name="T">The type of elements in the ImmutableNonEmptyList.</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <param name="jsonSerializerOptions">Custom JSON serializer options.</param>
    /// <returns>The property builder for further configuration.</returns>
    public static PropertyBuilder<ImmutableNonEmptyList<T>> HasImmutableNonEmptyListConversion<T>(
        this PropertyBuilder<ImmutableNonEmptyList<T>> builder,
        JsonSerializerOptions jsonSerializerOptions)
    {
        builder.HasConversion(new ImmutableNonEmptyListValueConverter<T>(jsonSerializerOptions));
        builder.Metadata.SetValueComparer(new ImmutableNonEmptyListValueComparer<T>());
        return builder;
    }

    /// <summary>
    /// Configures a property to use the ImmutableNonEmptyList value converter and comparer with a custom element comparer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the ImmutableNonEmptyList.</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <param name="elementComparer">Custom equality comparer for list elements.</param>
    /// <returns>The property builder for further configuration.</returns>
    public static PropertyBuilder<ImmutableNonEmptyList<T>> HasImmutableNonEmptyListConversion<T>(
        this PropertyBuilder<ImmutableNonEmptyList<T>> builder,
        IEqualityComparer<T> elementComparer)
    {
        builder.HasConversion(new ImmutableNonEmptyListValueConverter<T>());
        builder.Metadata.SetValueComparer(new ImmutableNonEmptyListValueComparer<T>(elementComparer));
        return builder;
    }

    #endregion

    #region Expression-based Relationship Configuration

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList using a lambda expression.
    /// This stores the related entities in a separate table with a foreign key relationship.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <returns>A collection navigation builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyMany(o => o.OrderItems);
    /// </code>
    /// </example>
    public static CollectionNavigationBuilder<TEntity, TRelated> HasNonEmptyMany<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder.HasMany<TRelated>(propertyName);
    }

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList using a lambda expression,
    /// with a specified inverse navigation property on the dependent entity.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyManyWithOne(o => o.OrderItems, oi => oi.Order);
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyManyWithOne<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression,
        Expression<Func<TRelated, TEntity?>> inverseNavigationExpression)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder
            .HasMany<TRelated>(propertyName)
            .WithOne(inverseNavigationExpression);
    }

    /// <summary>
    /// Configures a one-to-many relationship where the navigation property is a NonEmptyList using a lambda expression,
    /// with both inverse navigation and foreign key specified.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <param name="foreignKeyExpression">Expression to specify the foreign key property on the dependent entity.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyManyWithOne(o => o.OrderItems, oi => oi.Order, oi => oi.OrderId);
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyManyWithOne<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression,
        Expression<Func<TRelated, TEntity?>> inverseNavigationExpression,
        Expression<Func<TRelated, object?>> foreignKeyExpression)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder
            .HasMany<TRelated>(propertyName)
            .WithOne(inverseNavigationExpression)
            .HasForeignKey(foreignKeyExpression);
    }

    #endregion

    #region Cascade Delete Configuration

    /// <summary>
    /// Configures a one-to-many relationship with cascade delete behavior.
    /// When the principal entity is deleted, all related entities in the NonEmptyList are also deleted.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <param name="deleteBehavior">The delete behavior to apply.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasNonEmptyManyWithDeleteBehavior(o => o.OrderItems, oi => oi.Order, DeleteBehavior.Cascade);
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyManyWithDeleteBehavior<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression,
        Expression<Func<TRelated, TEntity?>> inverseNavigationExpression,
        DeleteBehavior deleteBehavior)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder
            .HasMany<TRelated>(propertyName)
            .WithOne(inverseNavigationExpression)
            .OnDelete(deleteBehavior);
    }

    /// <summary>
    /// Configures a one-to-many relationship with cascade delete behavior and foreign key.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <param name="foreignKeyExpression">Expression to specify the foreign key property on the dependent entity.</param>
    /// <param name="deleteBehavior">The delete behavior to apply.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasNonEmptyManyWithDeleteBehavior<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression,
        Expression<Func<TRelated, TEntity?>> inverseNavigationExpression,
        Expression<Func<TRelated, object?>> foreignKeyExpression,
        DeleteBehavior deleteBehavior)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder
            .HasMany<TRelated>(propertyName)
            .WithOne(inverseNavigationExpression)
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(deleteBehavior);
    }

    #endregion

    #region Required Navigation (Non-Empty Enforcement at Database Level)

    /// <summary>
    /// Configures a required one-to-many relationship where the navigation property is a NonEmptyList.
    /// This ensures that the foreign key is required (non-nullable) in the database.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .HasRequiredNonEmptyMany(o => o.OrderItems, oi => oi.Order);
    /// </code>
    /// </example>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasRequiredNonEmptyMany<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression,
        Expression<Func<TRelated, TEntity>> inverseNavigationExpression)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder
            .HasMany<TRelated>(propertyName)
            .WithOne(inverseNavigationExpression!)
            .IsRequired();
    }

    /// <summary>
    /// Configures a required one-to-many relationship with foreign key.
    /// </summary>
    /// <typeparam name="TEntity">The principal entity type.</typeparam>
    /// <typeparam name="TRelated">The dependent entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <param name="inverseNavigationExpression">Expression to specify the inverse navigation property on the dependent.</param>
    /// <param name="foreignKeyExpression">Expression to specify the foreign key property on the dependent entity.</param>
    /// <returns>A reference collection builder for further configuration.</returns>
    public static ReferenceCollectionBuilder<TEntity, TRelated> HasRequiredNonEmptyMany<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression,
        Expression<Func<TRelated, TEntity>> inverseNavigationExpression,
        Expression<Func<TRelated, object?>> foreignKeyExpression)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder
            .HasMany<TRelated>(propertyName)
            .WithOne(inverseNavigationExpression!)
            .HasForeignKey(foreignKeyExpression)
            .IsRequired();
    }

    #endregion

    #region Many-to-Many Relationship Configuration

    /// <summary>
    /// Configures a many-to-many relationship where the navigation property is a NonEmptyList.
    /// This creates a join table to store the relationship.
    /// </summary>
    /// <typeparam name="TEntity">The left entity type.</typeparam>
    /// <typeparam name="TRelated">The right entity type contained in the NonEmptyList.</typeparam>
    /// <param name="entityBuilder">The entity type builder.</param>
    /// <param name="navigationExpression">Expression to access the NonEmptyList navigation property.</param>
    /// <returns>A collection navigation builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Student&gt;()
    ///     .HasNonEmptyManyToMany(s => s.Courses);
    /// </code>
    /// </example>
    public static CollectionNavigationBuilder<TEntity, TRelated> HasNonEmptyManyToMany<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> entityBuilder,
        Expression<Func<TEntity, NonEmptyList<TRelated>?>> navigationExpression)
        where TEntity : class
        where TRelated : class
    {
        string propertyName = GetPropertyName(navigationExpression);
        return entityBuilder.HasMany<TRelated>(propertyName);
    }

    /// <summary>
    /// Configures a many-to-many relationship where both navigation properties are NonEmptyList.
    /// This creates a join table to store the relationship.
    /// </summary>
    /// <typeparam name="TLeft">The left entity type.</typeparam>
    /// <typeparam name="TRight">The right entity type.</typeparam>
    /// <param name="entityBuilder">The entity type builder for the left entity.</param>
    /// <param name="leftNavigationExpression">Expression to access the NonEmptyList on the left entity.</param>
    /// <param name="rightNavigationExpression">Expression to access the NonEmptyList on the right entity.</param>
    /// <returns>A collection collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Student&gt;()
    ///     .HasNonEmptyManyToMany(s => s.Courses, c => c.Students);
    /// </code>
    /// </example>
    public static CollectionCollectionBuilder<TRight, TLeft> HasNonEmptyManyToMany<TLeft, TRight>(
        this EntityTypeBuilder<TLeft> entityBuilder,
        Expression<Func<TLeft, NonEmptyList<TRight>?>> leftNavigationExpression,
        Expression<Func<TRight, NonEmptyList<TLeft>?>> rightNavigationExpression)
        where TLeft : class
        where TRight : class
    {
        string leftPropertyName = GetPropertyName(leftNavigationExpression);
        string rightPropertyName = GetPropertyName(rightNavigationExpression);

        return entityBuilder
            .HasMany<TRight>(leftPropertyName)
            .WithMany(rightPropertyName);
    }

    /// <summary>
    /// Configures a many-to-many relationship with a custom join entity type.
    /// Use this when you need to add additional properties to the join table.
    /// </summary>
    /// <typeparam name="TLeft">The left entity type.</typeparam>
    /// <typeparam name="TRight">The right entity type.</typeparam>
    /// <typeparam name="TJoin">The join entity type.</typeparam>
    /// <param name="entityBuilder">The entity type builder for the left entity.</param>
    /// <param name="leftNavigationExpression">Expression to access the NonEmptyList on the left entity.</param>
    /// <param name="rightNavigationExpression">Expression to access the NonEmptyList on the right entity.</param>
    /// <param name="configureJoinEntity">Action to configure the join entity.</param>
    /// <returns>The entity type builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Student&gt;()
    ///     .HasNonEmptyManyToMany&lt;Student, Course, StudentCourse&gt;(
    ///         s => s.Courses,
    ///         c => c.Students,
    ///         j => j.HasKey(sc => new { sc.StudentId, sc.CourseId }));
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TLeft> HasNonEmptyManyToMany<TLeft, TRight, TJoin>(
        this EntityTypeBuilder<TLeft> entityBuilder,
        Expression<Func<TLeft, NonEmptyList<TRight>?>> leftNavigationExpression,
        Expression<Func<TRight, NonEmptyList<TLeft>?>> rightNavigationExpression,
        Action<EntityTypeBuilder<TJoin>> configureJoinEntity)
        where TLeft : class
        where TRight : class
        where TJoin : class
    {
        string leftPropertyName = GetPropertyName(leftNavigationExpression);
        string rightPropertyName = GetPropertyName(rightNavigationExpression);

        entityBuilder
            .HasMany<TRight>(leftPropertyName)
            .WithMany(rightPropertyName)
            .UsingEntity<TJoin>(configureJoinEntity);

        return entityBuilder;
    }

    /// <summary>
    /// Configures a many-to-many relationship where the left side uses NonEmptyList
    /// and the right side uses a regular ICollection.
    /// </summary>
    /// <typeparam name="TLeft">The left entity type.</typeparam>
    /// <typeparam name="TRight">The right entity type.</typeparam>
    /// <param name="entityBuilder">The entity type builder for the left entity.</param>
    /// <param name="leftNavigationExpression">Expression to access the NonEmptyList on the left entity.</param>
    /// <param name="rightNavigationPropertyName">Name of the collection property on the right entity.</param>
    /// <returns>A collection collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Student&gt;()
    ///     .HasNonEmptyManyToMany(s => s.Courses, "Students");
    /// </code>
    /// </example>
    public static CollectionCollectionBuilder<TRight, TLeft> HasNonEmptyManyToMany<TLeft, TRight>(
        this EntityTypeBuilder<TLeft> entityBuilder,
        Expression<Func<TLeft, NonEmptyList<TRight>?>> leftNavigationExpression,
        string rightNavigationPropertyName)
        where TLeft : class
        where TRight : class
    {
        string leftPropertyName = GetPropertyName(leftNavigationExpression);

        return entityBuilder
            .HasMany<TRight>(leftPropertyName)
            .WithMany(rightNavigationPropertyName);
    }

    /// <summary>
    /// Configures a many-to-many relationship using property names.
    /// </summary>
    /// <typeparam name="TLeft">The left entity type.</typeparam>
    /// <typeparam name="TRight">The right entity type.</typeparam>
    /// <param name="entityBuilder">The entity type builder for the left entity.</param>
    /// <param name="leftNavigationPropertyName">Name of the NonEmptyList property on the left entity.</param>
    /// <param name="rightNavigationPropertyName">Name of the collection property on the right entity.</param>
    /// <returns>A collection collection builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Student&gt;()
    ///     .HasNonEmptyManyToMany&lt;Student, Course&gt;("Courses", "Students");
    /// </code>
    /// </example>
    public static CollectionCollectionBuilder<TRight, TLeft> HasNonEmptyManyToMany<TLeft, TRight>(
        this EntityTypeBuilder<TLeft> entityBuilder,
        string leftNavigationPropertyName,
        string rightNavigationPropertyName)
        where TLeft : class
        where TRight : class
    {
        return entityBuilder
            .HasMany<TRight>(leftNavigationPropertyName)
            .WithMany(rightNavigationPropertyName);
    }

    /// <summary>
    /// Configures a many-to-many relationship with explicit join table configuration.
    /// </summary>
    /// <typeparam name="TLeft">The left entity type.</typeparam>
    /// <typeparam name="TRight">The right entity type.</typeparam>
    /// <param name="entityBuilder">The entity type builder for the left entity.</param>
    /// <param name="leftNavigationExpression">Expression to access the NonEmptyList on the left entity.</param>
    /// <param name="rightNavigationExpression">Expression to access the NonEmptyList on the right entity.</param>
    /// <param name="joinTableName">The name of the join table.</param>
    /// <param name="leftForeignKeyName">The name of the left foreign key column.</param>
    /// <param name="rightForeignKeyName">The name of the right foreign key column.</param>
    /// <returns>The entity type builder for further configuration.</returns>
    /// <example>
    /// <code>
    /// modelBuilder.Entity&lt;Student&gt;()
    ///     .HasNonEmptyManyToManyWithJoinTable(
    ///         s => s.Courses,
    ///         c => c.Students,
    ///         "StudentCourses",
    ///         "StudentId",
    ///         "CourseId");
    /// </code>
    /// </example>
    public static EntityTypeBuilder<TLeft> HasNonEmptyManyToManyWithJoinTable<TLeft, TRight>(
        this EntityTypeBuilder<TLeft> entityBuilder,
        Expression<Func<TLeft, NonEmptyList<TRight>?>> leftNavigationExpression,
        Expression<Func<TRight, NonEmptyList<TLeft>?>> rightNavigationExpression,
        string joinTableName,
        string leftForeignKeyName,
        string rightForeignKeyName)
        where TLeft : class
        where TRight : class
    {
        string leftPropertyName = GetPropertyName(leftNavigationExpression);
        string rightPropertyName = GetPropertyName(rightNavigationExpression);

        entityBuilder
            .HasMany<TRight>(leftPropertyName)
            .WithMany(rightPropertyName)
            .UsingEntity(
                joinTableName,
                l => l.HasOne(typeof(TRight)).WithMany().HasForeignKey(rightForeignKeyName),
                r => r.HasOne(typeof(TLeft)).WithMany().HasForeignKey(leftForeignKeyName));

        return entityBuilder;
    }

    #endregion

    private static string GetPropertyName<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression innerMemberExpression)
            return innerMemberExpression.Member.Name;

        throw new ArgumentException("Expression must be a member access expression", nameof(expression));
    }
}
