using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Masterly.NonEmptyList.Configuration;

/// <summary>Configuration and options helpers for the non-empty types.</summary>
public static class NonEmptyConfigurationExtensions
{
    /// <summary>
    /// Reads a non-empty list from a key. Accepts an array (<c>Hosts:0</c>, <c>Hosts:1</c>) or a comma-separated value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the key is missing, empty or contains an unparsable element.</exception>
    public static NonEmptyList<T> GetNonEmptyList<T>(this IConfiguration configuration, string key)
        => (NonEmptyList<T>)Required(configuration, key, typeof(NonEmptyList<T>));

    /// <summary>Reads a non-empty list from a key, or returns <see langword="null"/> if the key is absent.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the key is present but empty or contains an unparsable element.</exception>
    public static NonEmptyList<T>? GetNonEmptyListOrNull<T>(this IConfiguration configuration, string key)
        => (NonEmptyList<T>?)Optional(configuration, key, typeof(NonEmptyList<T>));

    /// <summary>Reads a non-empty set from a key. Duplicates collapse.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the key is missing or empty.</exception>
    public static NonEmptySet<T> GetNonEmptySet<T>(this IConfiguration configuration, string key)
        => (NonEmptySet<T>)Required(configuration, key, typeof(NonEmptySet<T>));

    /// <summary>Reads a non-empty dictionary from an object-shaped section.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the section is missing or empty.</exception>
    public static NonEmptyDictionary<TKey, TValue> GetNonEmptyDictionary<TKey, TValue>(this IConfiguration configuration, string key) where TKey : notnull
        => (NonEmptyDictionary<TKey, TValue>)Required(configuration, key, typeof(NonEmptyDictionary<TKey, TValue>));

    /// <summary>Reads a constrained list from a key and checks the constraint.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the key is missing, empty or violates the constraint.</exception>
    public static ConstrainedList<T, TConstraint> GetConstrainedList<T, TConstraint>(this IConfiguration configuration, string key) where TConstraint : IListConstraint
        => (ConstrainedList<T, TConstraint>)Required(configuration, key, typeof(ConstrainedList<T, TConstraint>));

    /// <summary>Reads a string that must have content.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the key is missing or blank.</exception>
    public static NonEmptyString GetNonEmptyString(this IConfiguration configuration, string key)
        => (NonEmptyString)Required(configuration, key, typeof(NonEmptyString));

    /// <summary>Binds a section into a new <typeparamref name="TOptions"/>, handling non-empty properties.</summary>
    public static TOptions GetNonEmpty<TOptions>(this IConfiguration configuration) where TOptions : class
        => NonEmptyConfigurationBinder.Get<TOptions>(configuration);

    /// <summary>Binds a section onto an existing instance, handling non-empty properties.</summary>
    public static void BindNonEmpty(this IConfiguration configuration, object instance)
        => NonEmptyConfigurationBinder.Bind(configuration, instance);

    /// <summary>
    /// Replaces <c>BindConfiguration</c> for options classes that contain non-empty collections. Required properties whose
    /// configuration is missing or empty throw when the options are first resolved, so <c>ValidateOnStart()</c> turns a
    /// bad <c>appsettings.json</c> into a startup failure naming the key.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <param name="sectionPath">The configuration section to bind, or <see langword="null"/> to bind the root.</param>
    public static OptionsBuilder<TOptions> BindNonEmptyConfiguration<TOptions>(this OptionsBuilder<TOptions> builder, string? sectionPath = null)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configure<IConfiguration>((options, configuration) =>
        {
            IConfiguration section = sectionPath is null ? configuration : configuration.GetSection(sectionPath);
            NonEmptyConfigurationBinder.Bind(section, options);
        });

        builder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            IConfiguration section = sectionPath is null ? configuration : configuration.GetSection(sectionPath);
            return new ConfigurationChangeTokenSource<TOptions>(builder.Name, section);
        });

        return builder;
    }

    /// <summary>
    /// Registers <typeparamref name="TOptions"/>, binds it with the non-empty aware binder and validates it at startup.
    /// Shorthand for <c>AddOptions&lt;T&gt;().BindNonEmptyConfiguration(path).ValidateOnStart()</c>.
    /// </summary>
    public static OptionsBuilder<TOptions> AddNonEmptyOptions<TOptions>(this IServiceCollection services, string sectionPath)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sectionPath);
        return services.AddOptions<TOptions>().BindNonEmptyConfiguration(sectionPath).ValidateOnStart();
    }

    private static object Required(IConfiguration configuration, string key, Type type)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(key);

        IConfigurationSection section = configuration.GetSection(key);
        return NonEmptyConfigurationBinder.ReadNonEmpty(section, type)
            ?? throw new InvalidOperationException($"Configuration key '{section.Path}' is required and must contain at least one element.");
    }

    private static object? Optional(IConfiguration configuration, string key, Type type)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(key);
        return NonEmptyConfigurationBinder.ReadNonEmpty(configuration.GetSection(key), type);
    }
}
