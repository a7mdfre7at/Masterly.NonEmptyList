using Masterly.NonEmptyList.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Masterly.NonEmptyList.Integrations.Tests;

public class ConfigurationTests
{
    private static IConfiguration Build(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    private static IConfiguration BuildJson(string json)
        => new ConfigurationBuilder().AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json))).Build();

    public sealed class SmtpOptions
    {
        public required NonEmptyList<string> Hosts { get; init; }
        public int Port { get; init; } = 25;
        public NonEmptyList<int>? Retries { get; init; }
        public NonEmptySet<string>? Tags { get; set; }
        public NonEmptyString Sender { get; set; }
        public NonEmptyDictionary<string, int>? Limits { get; set; }
        public ConstrainedList<int, AtLeast<Two>>? Pair { get; set; }
        public Nested Inner { get; set; } = new();
        public List<string> Plain { get; set; } = new();
        public Uri? Endpoint { get; set; }
    }

    public sealed class Nested
    {
        public NonEmptyList<string>? Fallbacks { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void GetNonEmptyList_ShouldReadArrayAndCommaSeparatedForms()
    {
        IConfiguration array = Build(("Hosts:0", "a"), ("Hosts:1", "b"));
        IConfiguration scalar = Build(("Hosts", "a, b ,,c"));
        IConfiguration ints = Build(("Ports", "25,587"));

        Assert.Equal(new[] { "a", "b" }, array.GetNonEmptyList<string>("Hosts").ToArray());
        Assert.Equal(new[] { "a", "b", "c" }, scalar.GetNonEmptyList<string>("Hosts").ToArray());
        Assert.Equal(new[] { 25, 587 }, ints.GetNonEmptyList<int>("Ports").ToArray());
    }

    [Fact]
    public void GetNonEmptyList_ShouldFailNamingTheKey()
    {
        IConfiguration missing = Build(("Other", "x"));
        IConfiguration empty = Build(("Smtp:Hosts", ""));
        IConfiguration bad = Build(("Ports", "25,abc"));

        InvalidOperationException ex1 = Assert.Throws<InvalidOperationException>(() => missing.GetNonEmptyList<string>("Smtp:Hosts"));
        InvalidOperationException ex2 = Assert.Throws<InvalidOperationException>(() => empty.GetNonEmptyList<string>("Smtp:Hosts"));
        Assert.ThrowsAny<Exception>(() => bad.GetNonEmptyList<int>("Ports"));
        Assert.Contains("Smtp:Hosts", ex1.Message);
        Assert.Contains("Smtp:Hosts", ex2.Message);
        Assert.Null(missing.GetNonEmptyListOrNull<string>("Smtp:Hosts"));
        Assert.Equal(1, Build(("K", "1")).GetNonEmptyListOrNull<int>("K")!.Head);
    }

    [Fact]
    public void OtherAccessors_ShouldWork()
    {
        IConfiguration config = BuildJson("""
            {
              "Tags": ["a", "a", "b"],
              "Limits": { "gold": 10, "silver": 5 },
              "Pair": [1, 2],
              "Single": [1],
              "Sender": "noreply@example.com",
              "Blank": "   "
            }
            """);

        Assert.Equal(2, config.GetNonEmptySet<string>("Tags").Count);
        Assert.Equal(10, config.GetNonEmptyDictionary<string, int>("Limits")["gold"]);
        Assert.Equal(2, config.GetConstrainedList<int, AtLeast<Two>>("Pair").Second());
        Assert.Equal("noreply@example.com", config.GetNonEmptyString("Sender").Value);
        Assert.Throws<InvalidOperationException>(() => config.GetConstrainedList<int, AtLeast<Two>>("Single"));
        Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyString("Blank"));
        Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyString("Missing"));
        Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyDictionary<string, int>("Missing"));
        Assert.Throws<InvalidOperationException>(() => config.GetNonEmptySet<string>("Missing"));
    }

    [Fact]
    public void Binder_ShouldBindWholeOptionsObjectIncludingNested()
    {
        IConfiguration config = BuildJson("""
            {
              "Smtp": {
                "Hosts": ["smtp1", "smtp2"],
                "Port": 587,
                "Tags": ["x"],
                "Sender": "me@example.com",
                "Limits": { "a": 1 },
                "Pair": [3, 4],
                "Inner": { "Fallbacks": ["f1"], "Name": "inner" },
                "Plain": ["p1", "p2"],
                "Endpoint": "https://example.com/"
              }
            }
            """);

        SmtpOptions options = config.GetSection("Smtp").GetNonEmpty<SmtpOptions>();

        Assert.Equal(new[] { "smtp1", "smtp2" }, options.Hosts.ToArray());
        Assert.Equal(587, options.Port);
        Assert.Null(options.Retries);
        Assert.Single(options.Tags!);
        Assert.Equal("me@example.com", options.Sender.Value);
        Assert.Equal(1, options.Limits!["a"]);
        Assert.Equal(4, options.Pair!.Second());
        Assert.Equal("f1", options.Inner.Fallbacks!.Head);
        Assert.Equal("inner", options.Inner.Name);
        Assert.Equal(2, options.Plain.Count);
        Assert.Equal("https://example.com/", options.Endpoint!.ToString());
    }

    [Fact]
    public void Binder_ShouldFailForRequiredMissingAndAllowOptionalMissing()
    {
        IConfiguration missingHosts = BuildJson("""{ "Smtp": { "Port": 1, "Sender": "s" } }""");
        IConfiguration emptyHosts = BuildJson("""{ "Smtp": { "Hosts": [], "Sender": "s" } }""");
        IConfiguration ok = BuildJson("""{ "Smtp": { "Hosts": ["h"], "Sender": "s" } }""");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => missingHosts.GetSection("Smtp").GetNonEmpty<SmtpOptions>());
        Assert.Contains("Smtp:Hosts", ex.Message);
        Assert.Throws<InvalidOperationException>(() => emptyHosts.GetSection("Smtp").GetNonEmpty<SmtpOptions>());

        SmtpOptions options = ok.GetSection("Smtp").GetNonEmpty<SmtpOptions>();
        Assert.Null(options.Retries);
        Assert.Null(options.Tags);
        Assert.Null(options.Inner.Fallbacks);
        Assert.False(NonEmptyConfigurationBinder.IsNonEmptyType(typeof(int)));
        Assert.True(NonEmptyConfigurationBinder.IsNonEmptyType(typeof(NonEmptyString?)));
        Assert.Throws<ArgumentException>(() => NonEmptyConfigurationBinder.ReadNonEmpty(ok.GetSection("x"), typeof(int)));
    }

    [Fact]
    public void Binder_RequiredNonEmptyString_ShouldFailWhenMissing()
    {
        IConfiguration config = BuildJson("""{ "Smtp": { "Hosts": ["h"] } }""");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetSection("Smtp").GetNonEmpty<SmtpOptions>());
        Assert.Contains("Smtp:Sender", ex.Message);
    }

    [Fact]
    public void BindNonEmptyConfiguration_ShouldIntegrateWithOptionsAndValidateOnStart()
    {
        IConfiguration config = BuildJson("""{ "Smtp": { "Hosts": ["h1", "h2"], "Sender": "s" } }""");
        ServiceCollection services = new();
        services.AddSingleton(config);
        services.AddOptions<SmtpOptions>().BindNonEmptyConfiguration("Smtp").ValidateOnStart();

        using ServiceProvider provider = services.BuildServiceProvider();
        SmtpOptions options = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;

        Assert.Equal(2, options.Hosts.Count);
        Assert.Equal(25, options.Port);
    }

    [Fact]
    public void BindNonEmptyConfiguration_ShouldThrowWhenResolvedWithBadConfig()
    {
        IConfiguration config = BuildJson("""{ "Smtp": { "Hosts": [] } }""");
        ServiceCollection services = new();
        services.AddSingleton(config);
        services.AddNonEmptyOptions<SmtpOptions>("Smtp");

        using ServiceProvider provider = services.BuildServiceProvider();

        Exception ex = Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);
        Assert.Contains("Smtp:Hosts", ex.ToString());
    }

    [Fact]
    public void BindNonEmptyConfiguration_WithoutSection_ShouldBindRoot()
    {
        IConfiguration config = BuildJson("""{ "Hosts": ["root"], "Sender": "s" }""");
        ServiceCollection services = new();
        services.AddSingleton(config);
        services.AddOptions<SmtpOptions>().BindNonEmptyConfiguration();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("root", provider.GetRequiredService<IOptions<SmtpOptions>>().Value.Hosts.Head);
    }

    [Fact]
    public void StandardBinder_StillWorksForCommaSeparatedScalarsViaTypeConverter()
    {
        IConfiguration config = Build(("Hosts", "a,b"));

        NonEmptyList<string>? list = config.GetValue<NonEmptyList<string>>("Hosts");

        Assert.Equal(2, list!.Count);
    }
}
