using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// A SaveChanges interceptor that validates NonEmptyList navigation properties
/// to ensure they always contain at least one element before saving changes.
/// </summary>
public class NonEmptyListSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateNonEmptyLists(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateNonEmptyLists(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ValidateNonEmptyLists(DbContext? context)
    {
        if (context is null)
            return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            Type entityType = entry.Entity.GetType();
            PropertyInfo[] properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (!IsNonEmptyListType(property.PropertyType))
                    continue;

                object? value = property.GetValue(entry.Entity);

                if (value is null)
                {
                    throw new InvalidOperationException(
                        $"NonEmptyList property '{property.Name}' on entity '{entityType.Name}' cannot be null. " +
                        "A NonEmptyList must always contain at least one element.");
                }

                // Use reflection to check Count property
                PropertyInfo? countProperty = property.PropertyType.GetProperty("Count");
                if (countProperty is not null)
                {
                    int count = (int)countProperty.GetValue(value)!;
                    if (count == 0)
                    {
                        throw new InvalidOperationException(
                            $"NonEmptyList property '{property.Name}' on entity '{entityType.Name}' is empty. " +
                            "A NonEmptyList must always contain at least one element.");
                    }
                }
            }
        }
    }

    private static bool IsNonEmptyListType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        Type genericTypeDef = type.GetGenericTypeDefinition();
        return genericTypeDef == typeof(NonEmptyList<>) ||
               genericTypeDef == typeof(ImmutableNonEmptyList<>);
    }
}
