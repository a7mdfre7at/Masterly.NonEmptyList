using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Masterly.NonEmptyList.EntityFrameworkCore;

/// <summary>
/// A SaveChanges interceptor that validates the non-empty collection properties of every added or modified entity
/// before saving. It checks <see cref="NonEmptyList{T}"/> and every type derived from it (such as
/// <c>ConstrainedList</c>, <c>OrderedNonEmptyList</c> and <c>[NonEmptyCollection]</c> classes),
/// <see cref="ImmutableNonEmptyList{T}"/>, <see cref="NonEmptySet{T}"/> and <see cref="NonEmptyDictionary{TKey, TValue}"/>.
/// A null value is rejected unless the property is declared nullable, for example <c>NonEmptyList&lt;T&gt;?</c>.
/// </summary>
public class NonEmptyListSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly Type[] NonEmptyGenericTypes =
    [
        typeof(NonEmptyList<>),
        typeof(ImmutableNonEmptyList<>),
        typeof(NonEmptySet<>),
        typeof(NonEmptyDictionary<,>),
    ];

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

        NullabilityInfoContext nullability = new();

        foreach (var entry in entries)
        {
            Type entityType = entry.Entity.GetType();
            PropertyInfo[] properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (property.GetIndexParameters().Length != 0 || !IsNonEmptyType(property.PropertyType))
                    continue;

                object? value = property.GetValue(entry.Entity);

                if (value is null)
                {
                    if (nullability.Create(property).ReadState == NullabilityState.Nullable)
                        continue;

                    throw new InvalidOperationException(
                        $"Non-empty property '{property.Name}' on entity '{entityType.Name}' cannot be null. " +
                        "Declare the property nullable if null is allowed; otherwise it must contain at least one element.");
                }

                PropertyInfo? countProperty = property.PropertyType.GetProperty("Count", typeof(int));
                if (countProperty is not null && (int)countProperty.GetValue(value)! == 0)
                {
                    throw new InvalidOperationException(
                        $"Non-empty property '{property.Name}' on entity '{entityType.Name}' is empty. " +
                        "It must always contain at least one element.");
                }
            }
        }
    }

    private static bool IsNonEmptyType(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && NonEmptyGenericTypes.Contains(current.GetGenericTypeDefinition()))
                return true;
        }

        return false;
    }
}
