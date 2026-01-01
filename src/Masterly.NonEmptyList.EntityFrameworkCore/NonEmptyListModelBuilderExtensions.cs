using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// Extension methods for ModelBuilder to auto-configure NonEmptyList properties.
/// </summary>
public static class NonEmptyListModelBuilderExtensions
{
    /// <summary>
    /// Automatically configures all NonEmptyList&lt;T&gt; and ImmutableNonEmptyList&lt;T&gt; properties
    /// in the model to use JSON value conversion.
    /// This should be called after all entity types have been configured.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     // Configure your entities first
    ///     modelBuilder.Entity&lt;MyEntity&gt;();
    ///
    ///     // Then apply NonEmptyList conventions
    ///     modelBuilder.ApplyNonEmptyListConventions();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder ApplyNonEmptyListConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            Type clrType = entityType.ClrType;
            PropertyInfo[] properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (IsNonEmptyListType(property.PropertyType))
                {
                    ConfigureNonEmptyListProperty(modelBuilder, entityType.ClrType, property);
                }
                else if (IsImmutableNonEmptyListType(property.PropertyType))
                {
                    ConfigureImmutableNonEmptyListProperty(modelBuilder, entityType.ClrType, property);
                }
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// Automatically configures all NonEmptyList&lt;T&gt; properties (but not ImmutableNonEmptyList)
    /// in the model to use JSON value conversion.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyNonEmptyListConventionsOnly(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            Type clrType = entityType.ClrType;
            PropertyInfo[] properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (IsNonEmptyListType(property.PropertyType))
                {
                    ConfigureNonEmptyListProperty(modelBuilder, entityType.ClrType, property);
                }
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// Automatically configures all ImmutableNonEmptyList&lt;T&gt; properties
    /// in the model to use JSON value conversion.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyImmutableNonEmptyListConventionsOnly(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            Type clrType = entityType.ClrType;
            PropertyInfo[] properties = clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (IsImmutableNonEmptyListType(property.PropertyType))
                {
                    ConfigureImmutableNonEmptyListProperty(modelBuilder, entityType.ClrType, property);
                }
            }
        }

        return modelBuilder;
    }

    private static bool IsNonEmptyListType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        return type.GetGenericTypeDefinition() == typeof(NonEmptyList<>);
    }

    private static bool IsImmutableNonEmptyListType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        return type.GetGenericTypeDefinition() == typeof(ImmutableNonEmptyList<>);
    }

    private static void ConfigureNonEmptyListProperty(ModelBuilder modelBuilder, Type entityType, PropertyInfo property)
    {
        Type elementType = property.PropertyType.GetGenericArguments()[0];

        // Skip if the element type is an entity type (should be configured as a relationship instead)
        if (modelBuilder.Model.FindEntityType(elementType) != null)
            return;

        Type converterType = typeof(NonEmptyListValueConverter<>).MakeGenericType(elementType);
        Type comparerType = typeof(NonEmptyListValueComparer<>).MakeGenericType(elementType);

        object converter = Activator.CreateInstance(converterType)!;
        object comparer = Activator.CreateInstance(comparerType)!;

        var entityBuilder = modelBuilder.Entity(entityType);
        var propertyBuilder = entityBuilder.Property(property.Name);

        // Use reflection to call HasConversion and set value comparer
        var hasConversionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
            .GetMethods()
            .First(m => m.Name == "HasConversion" &&
                       m.GetParameters().Length == 1 &&
                       m.GetParameters()[0].ParameterType == typeof(Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter));

        hasConversionMethod.Invoke(propertyBuilder, new[] { converter });
        propertyBuilder.Metadata.SetValueComparer((Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer)comparer);
    }

    private static void ConfigureImmutableNonEmptyListProperty(ModelBuilder modelBuilder, Type entityType, PropertyInfo property)
    {
        Type elementType = property.PropertyType.GetGenericArguments()[0];

        // Skip if the element type is an entity type (should be configured as a relationship instead)
        if (modelBuilder.Model.FindEntityType(elementType) != null)
            return;

        Type converterType = typeof(ImmutableNonEmptyListValueConverter<>).MakeGenericType(elementType);
        Type comparerType = typeof(ImmutableNonEmptyListValueComparer<>).MakeGenericType(elementType);

        object converter = Activator.CreateInstance(converterType)!;
        object comparer = Activator.CreateInstance(comparerType)!;

        var entityBuilder = modelBuilder.Entity(entityType);
        var propertyBuilder = entityBuilder.Property(property.Name);

        // Use reflection to call HasConversion and set value comparer
        var hasConversionMethod = typeof(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)
            .GetMethods()
            .First(m => m.Name == "HasConversion" &&
                       m.GetParameters().Length == 1 &&
                       m.GetParameters()[0].ParameterType == typeof(Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter));

        hasConversionMethod.Invoke(propertyBuilder, new[] { converter });
        propertyBuilder.Metadata.SetValueComparer((Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer)comparer);
    }
}
