using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Masterly.NonEmptyList.Configuration;

/// <summary>
/// Binds configuration into objects that contain non-empty collections. The default <see cref="ConfigurationBinder"/>
/// cannot create a <see cref="NonEmptyList{T}"/> because the type has no parameterless constructor, so this binder takes
/// over for those properties and delegates everything else to the default binder. A required non-empty property whose
/// configuration is missing or empty fails with an <see cref="InvalidOperationException"/> that names the key, which
/// surfaces at startup when combined with <c>ValidateOnStart()</c>.
/// </summary>
public static class NonEmptyConfigurationBinder
{
    /// <summary>Binds the configuration section into a new instance of <typeparamref name="TOptions"/>.</summary>
    public static TOptions Get<TOptions>(IConfiguration configuration) where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (typeof(TOptions).IsAbstract || typeof(TOptions).GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidOperationException($"{typeof(TOptions).Name} needs a public parameterless constructor to be bound; positional records and constructor-only classes are not supported as options.");

        TOptions instance = (TOptions)CreateInstance(typeof(TOptions), (configuration as IConfigurationSection)?.Path ?? string.Empty);
        Bind(configuration, instance);
        return instance;
    }

    /// <summary>Binds the configuration section onto an existing instance, property by property.</summary>
    public static void Bind(IConfiguration configuration, object instance)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(instance);
        BindObject(configuration, instance, depth: 0);
    }

    /// <summary>Returns <see langword="true"/> if the binder handles the type itself instead of delegating to the default binder.</summary>
    public static bool IsNonEmptyType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type == typeof(NonEmptyString) || type == typeof(NonEmptyString?) || FindNonEmptyCollectionShape(type) is not null;
    }

    /// <summary>
    /// Reads a non-empty value of <paramref name="targetType"/> from a section. Accepts a JSON-style array
    /// (<c>Hosts:0</c>, <c>Hosts:1</c>) or a comma-separated scalar (<c>Hosts=a,b</c>). Returns <see langword="null"/>
    /// when the section has no value and no children.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the configured value is present but empty or invalid.</exception>
    public static object? ReadNonEmpty(IConfigurationSection section, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(targetType);
        return ReadNonEmpty(section, targetType, depth: 0);
    }

    private const int MaxDepth = 64;

    private static object? ReadNonEmpty(IConfigurationSection section, Type targetType, int depth)
    {
        if (depth > MaxDepth)
            throw TooDeep(section.Path);

        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(NonEmptyString))
        {
            if (section.Value is null && !section.GetChildren().Any())
                return null;
            return NonEmptyString.TryCreate(section.Value, out NonEmptyString result)
                ? result
                : throw new InvalidOperationException($"Configuration key '{section.Path}' must be a non-empty string.");
        }

        Shape? shape = FindNonEmptyCollectionShape(underlying)
            ?? throw new ArgumentException($"{targetType} is not a non-empty collection type", nameof(targetType));

        bool hasChildren = section.GetChildren().Any();
        if (section.Value is null && !hasChildren)
            return null;

        object materialized;
        try
        {
            // Elements and entries are read one at a time and none is ever skipped: the Microsoft binder silently drops
            // a collection element or dictionary entry it cannot convert, which would turn ["80", "eighty"] into [80].
            if (shape.IsDictionary)
            {
                IDictionary dictionary = ReadEntries(section, shape.KeyType!, shape.ElementType, allowNullValues: false, depth);
                if (dictionary.Count == 0)
                    throw new InvalidOperationException($"Configuration key '{section.Path}' must contain at least one entry.");

                materialized = dictionary;
            }
            else
            {
                IList list;
                if (hasChildren)
                {
                    list = ReadItems(section, shape.ElementType, allowNullItems: false, depth);
                }
                else
                {
                    list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(shape.ElementType))!;
                    TypeConverter converter = TypeDescriptor.GetConverter(shape.ElementType);
                    foreach (string part in section.Value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        list.Add(ConvertScalar(section.Path, part, converter, Nullable.GetUnderlyingType(shape.ElementType) ?? shape.ElementType)
                            ?? throw new InvalidOperationException($"Configuration key '{section.Path}' contains '{part}', which converts to null; a non-empty collection cannot contain null."));
                    }
                }

                if (list.Count == 0)
                    throw new InvalidOperationException($"Configuration key '{section.Path}' must contain at least one element.");

                materialized = list;
            }

            return InvokeFrom(shape.From, materialized);
        }
        catch (ArgumentException inner)
        {
            throw new InvalidOperationException($"Configuration key '{section.Path}' is invalid: {inner.Message}", inner);
        }
        catch (InvalidOperationException ex) when (!ex.Message.StartsWith("Configuration key", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Configuration key '{section.Path}' is invalid: {ex.Message}", ex);
        }
    }

    private static IList ReadItems(IConfigurationSection section, Type elementType, bool allowNullItems, int depth)
    {
        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (IConfigurationSection child in section.GetChildren())
            list.Add(ReadElement(child, elementType, allowNullItems, depth));
        return list;
    }

    private static IDictionary ReadEntries(IConfigurationSection section, Type keyType, Type valueType, bool allowNullValues, int depth)
    {
        IDictionary dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))!;
        TypeConverter keyConverter = TypeDescriptor.GetConverter(keyType);
        Dictionary<object, string> sources = new();

        foreach (IConfigurationSection child in section.GetChildren())
        {
            object key = ConvertScalar(child.Path, child.Key, keyConverter, keyType)
                ?? throw new InvalidOperationException($"Configuration key '{child.Path}' converts to a null {keyType.Name} key.");

            // Two configuration keys such as "1" and "01" can convert to the same key; the later one must not silently win.
            if (sources.TryGetValue(key, out string? first))
                throw new InvalidOperationException($"Configuration keys '{first}' and '{child.Path}' both convert to the key '{key}'.");

            sources.Add(key, child.Path);
            dictionary.Add(key, ReadElement(child, valueType, allowNullValues, depth));
        }

        return dictionary;
    }

    // Reads one list element or dictionary value. Every configured child yields a value or an exception that names its
    // key, so positions never shift and entries never disappear. Elements of a non-empty collection may not be null;
    // elements of a plain nested collection may, as with the Microsoft binder.
    private static object? ReadElement(IConfigurationSection child, Type type, bool allowNull, int depth)
    {
        if (depth > MaxDepth)
            throw TooDeep(child.Path);

        bool hasChildren = child.GetChildren().Any();
        // The .NET 8 JSON provider reads a JSON null (and {}) as an empty string, .NET 10 as null; treat both alike.
        string text = child.Value ?? string.Empty;
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (IsNonEmptyType(underlying))
        {
            if (!hasChildren && text.Length == 0)
            {
                throw new InvalidOperationException(underlying == typeof(NonEmptyString)
                    ? $"Configuration key '{child.Path}' must be a non-empty string."
                    : $"Configuration key '{child.Path}' must contain at least one element.");
            }

            return ReadNonEmpty(child, underlying, depth + 1);
        }

        if (underlying == typeof(IConfigurationSection))
            return child;

        if (underlying == typeof(object))
            return hasChildren ? child.Get(typeof(object)) : text;

        if (underlying == typeof(byte[]) && !hasChildren && text.Length > 0)
        {
            try
            {
                return Convert.FromBase64String(text);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Configuration key '{child.Path}' is not a valid base64 value.", ex);
            }
        }

        if (TryReadCollection(child, underlying, text, hasChildren, depth, out object? collection))
            return collection;

        // A nullable converter turns "" into null, which only a plain collection may hold.
        TypeConverter converter = TypeDescriptor.GetConverter(allowNull ? type : underlying);
        if (!hasChildren && converter.CanConvertFrom(typeof(string)))
        {
            object? value = ConvertScalar(child.Path, text, converter, underlying);
            if (value is not null || allowNull)
                return value;

            throw new InvalidOperationException($"Configuration key '{child.Path}' has no value; a non-empty collection cannot contain null.");
        }

        if (typeof(IEnumerable).IsAssignableFrom(underlying))
            throw new InvalidOperationException($"Configuration key '{child.Path}': {underlying.Name} is a read-only or unsupported collection type; use an array, List<T>, HashSet<T> or Dictionary<TKey, TValue>.");

        // An object: a section with members, or {} which providers store as an empty value.
        if (!hasChildren && text.Length > 0)
            throw new InvalidOperationException($"Configuration key '{child.Path}' must be a section with the members of {underlying.Name}, not a single value.");

        // An int or a string has nothing a section could bind; the default binder would quietly produce its default.
        if (hasChildren && converter.CanConvertFrom(typeof(string)) && !HasSettableMembers(underlying))
            throw new InvalidOperationException($"Configuration key '{child.Path}' must be a single {underlying.Name} value, not a section.");

        if (NeedsOwnBinding(underlying))
        {
            // The object has non-empty members, which only this package can create and check, so it is created here
            // and bound by BindObject; its other members still go through the default binder.
            if (underlying.IsAbstract || (!underlying.IsValueType && underlying.GetConstructor(Type.EmptyTypes) is null))
                throw new InvalidOperationException($"Configuration key '{child.Path}': {underlying.Name} has non-empty members, so it needs a public parameterless constructor to be bound.");

            object instance = CreateInstance(underlying, child.Path);
            BindObject(child, instance, depth + 1);
            return instance;
        }

        // Every other object is bound entirely by the default binder, with its usual semantics: constructor binding,
        // [ConfigurationKeyName], get-only collections, public setters only.
        object? bound = hasChildren ? child.Get(underlying) : null;
        if (bound is not null)
            return bound;

        if (underlying.IsValueType || (!underlying.IsAbstract && underlying.GetConstructor(Type.EmptyTypes) is not null))
            return CreateInstance(underlying, child.Path);

        return allowNull
            ? null
            : throw new InvalidOperationException($"Configuration key '{child.Path}' has no value, and {underlying.Name} has no parameterless constructor to create a default.");
    }

    private static bool HasSettableMembers(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0);

    private enum CollectionKind
    {
        Array,
        List,
        HashSet,
        Dictionary,
        ConcreteCollection,
        ConcreteDictionary,
    }

    // Arrays, lists, sets and dictionaries, including concrete collection classes such as SortedSet<T>, Collection<T>
    // or a class deriving from List<T>. Interfaces are filled with List<T>, HashSet<T> or Dictionary<TKey, TValue>.
    private static bool TryGetCollectionShape(Type type, out CollectionKind kind, out Type elementType, out Type? keyType)
    {
        kind = default;
        elementType = typeof(object);
        keyType = null;

        if (type == typeof(string))
            return false;

        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
                return false;
            kind = CollectionKind.Array;
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsInterface && type.IsGenericType)
        {
            Type definition = type.GetGenericTypeDefinition();
            Type[] args = type.GetGenericArguments();

            if (definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>) || definition == typeof(IList<>) ||
                definition == typeof(IReadOnlyCollection<>) || definition == typeof(IReadOnlyList<>))
            {
                kind = CollectionKind.List;
                elementType = args[0];
                return true;
            }

            if (definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
            {
                kind = CollectionKind.HashSet;
                elementType = args[0];
                return true;
            }

            if (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
            {
                kind = CollectionKind.Dictionary;
                keyType = args[0];
                elementType = args[1];
                return true;
            }

            return false;
        }

        if (type.IsAbstract || type.IsInterface || (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) is null))
            return false;

        if (IsReadOnlyCollection(type))
            return false;

        Type? dictionary = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionary is not null)
        {
            kind = CollectionKind.ConcreteDictionary;
            keyType = dictionary.GetGenericArguments()[0];
            elementType = dictionary.GetGenericArguments()[1];
            return true;
        }

        Type? collection = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collection is not null)
        {
            kind = CollectionKind.ConcreteCollection;
            elementType = collection.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    // Collections nested inside a non-empty collection are read with the same strict rules, because handing them to
    // the default binder would bring back the silent dropping of values it cannot convert.
    private static bool TryReadCollection(IConfigurationSection child, Type type, string text, bool hasChildren, int depth, out object? collection)
    {
        collection = null;
        if (!TryGetCollectionShape(type, out CollectionKind kind, out Type elementType, out Type? keyType))
            return false;

        if (!hasChildren && text.Length > 0)
            throw new InvalidOperationException($"Configuration key '{child.Path}' must be a section of values for {type.Name}, not a single value.");

        if (keyType is not null)
        {
            IDictionary entries = ReadEntries(child, keyType, elementType, allowNullValues: true, depth + 1);
            if (kind == CollectionKind.Dictionary)
            {
                collection = entries;
                return true;
            }

            object target = CreateInstance(type, child.Path);
            MethodInfo add = typeof(ICollection<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, elementType)).GetMethod("Add")!;
            foreach (object pair in (IEnumerable)entries)
                add.Invoke(target, BindingFlags.DoNotWrapExceptions, null, new[] { pair }, null);
            collection = target;
            return true;
        }

        IList items = ReadItems(child, elementType, allowNullItems: true, depth + 1);
        switch (kind)
        {
            case CollectionKind.Array:
                Array array = Array.CreateInstance(elementType, items.Count);
                items.CopyTo(array, 0);
                collection = array;
                break;
            case CollectionKind.HashSet:
                collection = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType), items)!;
                break;
            case CollectionKind.ConcreteCollection:
                object target = CreateInstance(type, child.Path);
                MethodInfo add = typeof(ICollection<>).MakeGenericType(elementType).GetMethod("Add")!;
                foreach (object? item in items)
                    add.Invoke(target, BindingFlags.DoNotWrapExceptions, null, new[] { item }, null);
                collection = target;
                break;
            default:
                collection = items;
                break;
        }

        return true;
    }

    private static object? ConvertScalar(string path, string text, TypeConverter converter, Type elementType)
    {
        try
        {
            return converter.ConvertFromInvariantString(text);
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or InsufficientExecutionStackException))
        {
            // Any converter failure, including a custom converter's own exception type, names the key.
            throw new InvalidOperationException(text.Length == 0
                ? $"Configuration key '{path}' has no value, which is not a valid {elementType.Name}."
                : $"Configuration key '{path}' contains '{text}', which is not a valid {elementType.Name}.", ex);
        }
    }

    // Binds the members only this package can handle (non-empty types, and objects or collections that contain them),
    // then hands the rest of the object to the default binder through a view that hides those members, so everything
    // else keeps the default binder's exact semantics.
    private static void BindObject(IConfiguration configuration, object instance, int depth)
    {
        if (depth > MaxDepth)
            throw TooDeep((configuration as IConfigurationSection)?.Path ?? string.Empty);

        Type type = instance.GetType();
        NullabilityInfoContext nullability = new();
        HashSet<string> ownKeys = new(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo property in BindableProperties(type))
        {
            Type propertyType = property.PropertyType;
            if (!NeedsOwnBinding(propertyType))
                continue;

            string key = KeyOf(property);
            ownKeys.Add(key);
            IConfigurationSection section = configuration.GetSection(key);
            bool settable = property.SetMethod is { IsPublic: true };

            if (IsNonEmptyType(propertyType))
            {
                if (!settable)
                    continue;

                object? value = ReadNonEmpty(section, propertyType, depth + 1);
                if (value is null)
                {
                    object? current = property.GetValue(instance);
                    bool hasValue = current is not null && (current is not NonEmptyString text || text.HasValue);
                    bool required = !hasValue && !IsNullable(property, nullability);
                    if (required)
                        throw new InvalidOperationException($"Configuration key '{section.Path}' is required and must contain at least one element.");
                    continue;
                }

                SetMember(property, instance, value, section.Path);
                continue;
            }

            if (!section.Exists())
                continue;

            Type underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (TryGetCollectionShape(underlying, out _, out _, out _))
            {
                // A collection whose elements contain non-empty members: the default binder cannot create them. Like the
                // default binder, an existing collection is added to (a dictionary merged), and an array is extended.
                object? read = ReadElement(section, propertyType, allowNull: true, depth + 1);
                object? existing = property.GetValue(instance);
                if (existing is not null && TryFill(existing, read))
                    continue;

                if (existing is Array previous && read is Array next && settable)
                {
                    Array combined = Array.CreateInstance(previous.GetType().GetElementType()!, previous.Length + next.Length);
                    previous.CopyTo(combined, 0);
                    next.CopyTo(combined, previous.Length);
                    SetMember(property, instance, combined, section.Path);
                    continue;
                }

                if (!settable)
                    throw new InvalidOperationException($"Configuration key '{section.Path}': {type.Name}.{property.Name} has no public setter and no collection instance to fill.");

                SetMember(property, instance, read, section.Path);
                continue;
            }

            object? target = property.GetValue(instance);
            if (target is null)
            {
                if (!settable)
                    continue;

                if (underlying.IsAbstract || (!underlying.IsValueType && underlying.GetConstructor(Type.EmptyTypes) is null))
                    throw new InvalidOperationException($"Configuration key '{section.Path}': {underlying.Name} has non-empty members, so it needs a public parameterless constructor to be bound.");

                target = CreateInstance(underlying, section.Path);
                BindObject(section, target, depth + 1);
                SetMember(property, instance, target, section.Path);
                continue;
            }

            if (underlying.IsValueType && !settable)
                continue;

            BindObject(section, target, depth + 1);
            if (underlying.IsValueType)
                SetMember(property, instance, target, section.Path);
        }

        new HidingConfiguration(configuration, ownKeys).Bind(instance);

        // A plain member whose key is also used by a member this package bound, through [ConfigurationKeyName], was
        // hidden from the default binder above; bind it on its own so it is not lost.
        foreach (PropertyInfo property in BindableProperties(type))
        {
            if (property.SetMethod is not { IsPublic: true } || NeedsOwnBinding(property.PropertyType))
                continue;

            string key = KeyOf(property);
            if (!ownKeys.Contains(key))
                continue;

            IConfigurationSection section = configuration.GetSection(key);
            object? value = section.Exists() ? section.Get(property.PropertyType) : null;
            if (value is not null)
                SetMember(property, instance, value, section.Path);
        }
    }

    // The properties a binder sees: public, readable, not indexers, and only the most derived one when a property
    // hides a base property with 'new', as the default binder does.
    private static IEnumerable<PropertyInfo> BindableProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod is { IsPublic: true })
            .GroupBy(p => p.Name, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(p => InheritanceDepth(p.DeclaringType!)).First());

    private static int InheritanceDepth(Type type)
    {
        int depth = 0;
        for (Type? current = type; current is not null; current = current.BaseType)
            depth++;
        return depth;
    }

    // Adds the items of a freshly read collection to an existing writable one, or merges dictionary entries.
    private static bool TryFill(object existing, object? read)
    {
        if (read is null || existing is Array || IsReadOnlyCollection(existing.GetType()))
            return false;

        Type? dictionary = existing.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionary is not null)
        {
            PropertyInfo indexer = dictionary.GetProperty("Item")!;
            foreach (object pair in (IEnumerable)read)
            {
                Type pairType = pair.GetType();
                indexer.SetValue(existing, pairType.GetProperty("Value")!.GetValue(pair), new[] { pairType.GetProperty("Key")!.GetValue(pair) });
            }

            return true;
        }

        Type? collection = existing.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collection is null)
            return false;

        MethodInfo add = collection.GetMethod("Add")!;
        foreach (object? item in (IEnumerable)read)
            add.Invoke(existing, BindingFlags.DoNotWrapExceptions, null, new[] { item }, null);
        return true;
    }

    private static bool IsReadOnlyCollection(Type type)
    {
        if (type.Namespace?.StartsWith("System.Collections.Immutable", StringComparison.Ordinal) == true)
            return true;

        Type? collection = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        if (collection is null)
            return false;

        try
        {
            object probe = Activator.CreateInstance(type)!;
            return (bool)collection.GetProperty("IsReadOnly")!.GetValue(probe)!;
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or InsufficientExecutionStackException))
        {
            return true;
        }
    }

    private static object CreateInstance(Type type, string path)
    {
        try
        {
            return Activator.CreateInstance(type)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException($"Configuration key '{path}': creating {type.Name} failed: {ex.InnerException.Message}", ex.InnerException);
        }
        catch (MissingMethodException ex)
        {
            throw new InvalidOperationException($"Configuration key '{path}': {type.Name} needs a public parameterless constructor to be bound.", ex);
        }
    }

    private static void SetMember(PropertyInfo property, object instance, object? value, string path)
    {
        try
        {
            property.SetMethod!.Invoke(instance, BindingFlags.DoNotWrapExceptions, null, new[] { value }, null);
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or InsufficientExecutionStackException))
        {
            throw new InvalidOperationException($"Configuration key '{path}' could not be assigned to {property.DeclaringType!.Name}.{property.Name}: {ex.Message}", ex);
        }
    }

    private static InvalidOperationException TooDeep(string path)
        => new($"Configuration key '{path}' is nested too deep: more than {MaxDepth} levels of objects and collections.");

    private static string KeyOf(PropertyInfo property)
        => property.GetCustomAttribute<ConfigurationKeyNameAttribute>()?.Name ?? property.Name;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> OwnBindingCache = new();

    // True for the non-empty types, and for objects and collections that contain them anywhere: the default binder
    // cannot create those, so this package binds them.
    private static bool NeedsOwnBinding(Type type)
        => OwnBindingCache.GetOrAdd(type, static t => NeedsOwnBinding(t, new HashSet<Type>()));

    private static bool NeedsOwnBinding(Type type, HashSet<Type> visiting)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (IsNonEmptyType(underlying))
            return true;

        if (TryGetCollectionShape(underlying, out _, out Type elementType, out Type? keyType))
            return NeedsOwnBinding(elementType, visiting) || (keyType is not null && NeedsOwnBinding(keyType, visiting));

        if (!(IsComplexObject(underlying) || IsPlainStruct(underlying)) || !visiting.Add(underlying))
            return false;

        return BindableProperties(underlying).Any(p => NeedsOwnBinding(p.PropertyType, visiting));
    }

    // A struct written as a section of members, such as a struct with a NonEmptyList property. Scalars such as int,
    // decimal, DateTime or Guid have a string converter and are excluded.
    private static bool IsPlainStruct(Type type)
        => type.IsValueType && !type.IsPrimitive && !type.IsEnum && Nullable.GetUnderlyingType(type) is null &&
           !typeof(IEnumerable).IsAssignableFrom(type) && !TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));

    // The configuration seen by the default binder: the same keys, minus the members this package has bound itself.
    private sealed class HidingConfiguration : IConfiguration
    {
        private readonly IConfiguration _inner;
        private readonly HashSet<string> _hidden;

        public HidingConfiguration(IConfiguration inner, HashSet<string> hidden)
        {
            _inner = inner;
            _hidden = hidden;
        }

        public string? this[string key]
        {
            get => IsHidden(key) ? null : _inner[key];
            set => _inner[key] = value;
        }

        public IConfigurationSection GetSection(string key) => IsHidden(key) ? new EmptySection(key) : _inner.GetSection(key);

        public IEnumerable<IConfigurationSection> GetChildren() => _inner.GetChildren().Where(child => !_hidden.Contains(child.Key));

        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => _inner.GetReloadToken();

        private bool IsHidden(string key)
        {
            int delimiter = key.IndexOf(ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
            return _hidden.Contains(delimiter < 0 ? key : key[..delimiter]);
        }
    }

    // A section with no value and no children, which the default binder treats as "not configured".
    private sealed class EmptySection : IConfigurationSection, Microsoft.Extensions.Primitives.IChangeToken, IDisposable
    {
        public EmptySection(string path)
        {
            Path = path;
            Key = ConfigurationPath.GetSectionKey(path);
        }

        public string Key { get; }

        public string Path { get; }

        public string? Value
        {
            get => null;
            set => throw new NotSupportedException("A hidden configuration section cannot be written.");
        }

        public string? this[string key]
        {
            get => null;
            set => throw new NotSupportedException("A hidden configuration section cannot be written.");
        }

        public IConfigurationSection GetSection(string key) => new EmptySection(ConfigurationPath.Combine(Path, key));

        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => this;

        bool Microsoft.Extensions.Primitives.IChangeToken.HasChanged => false;

        bool Microsoft.Extensions.Primitives.IChangeToken.ActiveChangeCallbacks => false;

        IDisposable Microsoft.Extensions.Primitives.IChangeToken.RegisterChangeCallback(Action<object?> callback, object? state) => this;

        void IDisposable.Dispose()
        {
        }
    }

    private static bool IsNullable(PropertyInfo property, NullabilityInfoContext context)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
            return true;

        return context.Create(property).WriteState == NullabilityState.Nullable;
    }

    private static bool IsComplexObject(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri) ||
            type == typeof(DateOnly) || type == typeof(TimeOnly) || type.IsValueType)
            return false;

        if (typeof(IEnumerable).IsAssignableFrom(type))
            return false;

        if (TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string)))
            return false;

        return type.IsClass;
    }

    private static Shape? FindNonEmptyCollectionShape(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (!current.IsGenericType)
                continue;

            Type definition = current.GetGenericTypeDefinition();
            Type[] args = current.GetGenericArguments();

            if (definition == typeof(NonEmptyDictionary<,>))
                return new Shape(args[1], args[0], true, FindFrom(type, typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]))));

            if (definition == typeof(NonEmptyList<>) || definition == typeof(ImmutableNonEmptyList<>) ||
                definition == typeof(NonEmptySet<>) || definition == typeof(ConstrainedList<,>))
                return new Shape(args[0], null, false, FindFrom(type, typeof(IEnumerable<>).MakeGenericType(args[0])));
        }

        return null;
    }

    private static MethodInfo FindFrom(Type type, Type parameterType)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            MethodInfo? method = current
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == "From" && m.GetParameters() is { Length: >= 1 } ps && ps[0].ParameterType == parameterType &&
                                     ps.Skip(1).All(p => p.IsOptional) && m.ReturnType.IsAssignableTo(type));
            if (method is not null)
                return method;
        }

        throw new InvalidOperationException($"{type} does not expose a static From({parameterType}) factory.");
    }

    private static object? InvokeFrom(MethodInfo from, object argument)
    {
        ParameterInfo[] parameters = from.GetParameters();
        object?[] args = new object?[parameters.Length];
        args[0] = argument;
        for (int i = 1; i < parameters.Length; i++)
            args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
        // Unwrapped, so a factory's own exception surfaces as itself rather than as TargetInvocationException.
        return from.Invoke(null, BindingFlags.DoNotWrapExceptions, null, args, null);
    }

    private sealed record Shape(Type ElementType, Type? KeyType, bool IsDictionary, MethodInfo From);
}
