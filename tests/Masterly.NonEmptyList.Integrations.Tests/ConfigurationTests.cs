using System.ComponentModel;
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

    [Fact]
    public void GetNonEmptyList_CommaSeparated_WithUnparsableElement_ShouldThrowInvalidOperationNamingTheKey()
    {
        IConfiguration config = Build(("Ports", "80, eighty, 443"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<int>("Ports"));

        Assert.Contains("'Ports'", ex.Message);
        Assert.Contains("'eighty'", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void GetNonEmptyList_ArrayForm_WithUnparsableElement_ShouldAlsoThrowInvalidOperation()
    {
        IConfiguration config = Build(("Ports:0", "80"), ("Ports:1", "eighty"));

        Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<int>("Ports"));
    }

    [Fact]
    public void AddNonEmptyOptions_WithEmptyJsonArray_ShouldFailWithTheDocumentedMessage()
    {
        // The exact text is quoted in the package README; keep the two in step.
        IConfiguration config = BuildJson("""{ "Smtp": { "Hosts": [], "Sender": "noreply@example.com" } }""");
        ServiceCollection services = new();
        services.AddSingleton(config);
        services.AddOptions<SmtpOptions>().BindNonEmptyConfiguration("Smtp");
        using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);

#if NET10_0_OR_GREATER
        Assert.Equal("Configuration key 'Smtp:Hosts' must contain at least one element.", ex.Message);
#else
        // The .NET 8 JSON provider drops an empty array, so the key reads as missing.
        Assert.Equal("Configuration key 'Smtp:Hosts' is required and must contain at least one element.", ex.Message);
#endif
    }

    public sealed class Endpoint
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    [Fact]
    public void GetNonEmptyList_OfObjects_ShouldBindEachElementWithTheDefaultBinder()
    {
        IConfiguration config = BuildJson("""{ "Endpoints": [ { "Host": "a", "Port": 1 }, { "Host": "b", "Port": 2 } ] }""");

        NonEmptyList<Endpoint> endpoints = config.GetNonEmptyList<Endpoint>("Endpoints");

        Assert.Equal(new[] { "a", "b" }, endpoints.Select(e => e.Host).ToArray());
        Assert.Equal(2, endpoints.Last.Port);
    }

    [Fact]
    public void GetNonEmptyList_OfObjects_WithUnconvertibleMember_ShouldThrowInsteadOfDroppingTheElement()
    {
        IConfiguration config = BuildJson("""{ "Endpoints": [ { "Host": "a", "Port": 1 }, { "Host": "b", "Port": "eighty" } ] }""");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<Endpoint>("Endpoints"));

        Assert.StartsWith("Configuration key 'Endpoints' is invalid:", ex.Message);
        Assert.Contains("Endpoints:1:Port", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void GetNonEmptyList_OfScalars_WithAnEntryThatIsASection_ShouldThrowNamingTheEntry()
    {
        // The default binder would quietly turn the section into 0.
        IConfiguration config = Build(("Ports:0", "443"), ("Ports:1:Value", "80"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<int>("Ports"));

        Assert.Equal("Configuration key 'Ports:1' must be a single Int32 value, not a section.", ex.Message);
    }

    [Fact]
    public void GetNonEmptyList_ArrayForm_ShouldReadANullEntryAsAnEmptyString_SoPositionsNeverShift()
    {
        // The .NET 8 JSON provider turns a JSON null into "" and .NET 10 keeps it null; both read as "".
        IConfiguration config = Build(("Hosts:0", "a"), ("Hosts:1", null), ("Hosts:2", "c"));

        Assert.Equal(new[] { "a", "", "c" }, config.GetNonEmptyList<string>("Hosts").ToArray());
    }

    [Fact]
    public void GetNonEmptyList_ArrayForm_WithANullNumber_ShouldThrowInsteadOfSkippingIt()
    {
        IConfiguration config = Build(("Ports:0", "80"), ("Ports:1", null), ("Ports:2", "443"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<int>("Ports"));

        Assert.Equal("Configuration key 'Ports:1' has no value, which is not a valid Int32.", ex.Message);
    }

    [Fact]
    public void GetNonEmptyDictionary_WithBlankValue_ShouldFailWithTheEntryMessage()
    {
        IConfiguration config = Build(("Limits", ""));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyDictionary<string, int>("Limits"));

        Assert.Equal("Configuration key 'Limits' must contain at least one entry.", ex.Message);
    }

    [Fact]
    public void GetNonEmptyDictionary_WithUnconvertibleValue_ShouldThrowInsteadOfDroppingTheEntry()
    {
        IConfiguration config = Build(("Limits:gold", "lots"), ("Limits:silver", "5"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyDictionary<string, int>("Limits"));

        Assert.Equal("Configuration key 'Limits:gold' contains 'lots', which is not a valid Int32.", ex.Message);
        Assert.IsAssignableFrom<Exception>(ex.InnerException);
    }

    [Fact]
    public void GetNonEmptyDictionary_WithUnconvertibleKey_ShouldThrowNamingTheKey()
    {
        IConfiguration config = Build(("Codes:200", "ok"), ("Codes:teapot", "short and stout"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyDictionary<int, string>("Codes"));

        Assert.Equal("Configuration key 'Codes:teapot' contains 'teapot', which is not a valid Int32.", ex.Message);
        Assert.Equal("ok", Build(("Codes:200", "ok")).GetNonEmptyDictionary<int, string>("Codes")[200]);
    }

    [Fact]
    public void GetNonEmptyDictionary_OfObjects_ShouldBindEachValue_AndAnEmptyEntryAsADefaultObject()
    {
        // A null entry is how providers store {}; the Microsoft binder also makes it a default object.
        IConfiguration config = Build(("Sites:eu:Host", "eu.example"), ("Sites:eu:Port", "443"), ("Sites:us", null));

        NonEmptyDictionary<string, Endpoint> sites = config.GetNonEmptyDictionary<string, Endpoint>("Sites");

        Assert.Equal(2, sites.Count);
        Assert.Equal("eu.example", sites["eu"].Host);
        Assert.Equal(443, sites["eu"].Port);
        Assert.Equal(0, sites["us"].Port);
    }

    [Fact]
    public void ReadNonEmpty_DerivedCollection_ShouldUseItsOwnFactoryAndInvariant()
    {
        IConfiguration distinct = Build(("Scores:0", "1"), ("Scores:1", "2"));
        IConfiguration duplicate = Build(("Scores", "1,1"));

        Scores scores = Assert.IsType<Scores>(NonEmptyConfigurationBinder.ReadNonEmpty(distinct.GetSection("Scores"), typeof(Scores)));
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => NonEmptyConfigurationBinder.ReadNonEmpty(duplicate.GetSection("Scores"), typeof(Scores)));

        Assert.Equal(new[] { 1, 2 }, scores.ToArray());
        Assert.Equal("Configuration key 'Scores' is invalid: Scores must be distinct", ex.Message);
    }

    [Fact]
    public void ReadNonEmpty_DerivedCollectionWithoutFactory_ShouldExplainWhatIsMissing()
    {
        IConfiguration config = Build(("Items", "1"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => NonEmptyConfigurationBinder.ReadNonEmpty(config.GetSection("Items"), typeof(NoFactoryList)));

        Assert.Contains("does not expose a static From", ex.Message);
        Assert.Contains(nameof(NoFactoryList), ex.Message);
    }

    [Fact]
    public void ReadNonEmpty_DerivedCollectionFactory_FailingForOtherReasons_ShouldPropagateUnwrapped()
    {
        IConfiguration config = Build(("Items", "1, 13"));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => NonEmptyConfigurationBinder.ReadNonEmpty(config.GetSection("Items"), typeof(Unlucky)));

        Assert.Equal("13 is unlucky", ex.Message);
        Assert.Equal(new[] { 1, 2 }, Assert.IsType<Unlucky>(NonEmptyConfigurationBinder.ReadNonEmpty(Build(("Items", "1, 2")).GetSection("Items"), typeof(Unlucky))).ToArray());
    }

    public sealed class EdgeOptions
    {
        public NonEmptyList<string> Defaults { get; set; } = NonEmptyList.Of("default");
        public NonEmptyString Owner { get; set; } = new("owner");
        public NonEmptyString? Alias { get; set; }
        public Nested? Created { get; set; }
        public Nested? ReadOnly { get; } = null;
        public NoDefaultConstructor? Unbuildable { get; set; }
        public Version? Version { get; set; }
        public string this[int index] => index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public string WriteOnly { set => WriteOnlyCalls++; }
        public int WriteOnlyCalls { get; private set; }
    }

    public sealed class NoDefaultConstructor(string name)
    {
        public string Name { get; set; } = name;
    }

    [Fact]
    public void Binder_ShouldKeepInitializedDefaults_CreateMissingObjects_AndLetTheDefaultBinderBindTheRest()
    {
        IConfiguration config = BuildJson("""
            {
              "Created": { "Name": "made", "Fallbacks": ["f"] },
              "ReadOnly": { "Name": "ignored" },
              "Unbuildable": { "Name": "ignored" },
              "Version": "1.2.3",
              "WriteOnly": "x",
              "Item": "x"
            }
            """);

        EdgeOptions options = config.GetNonEmpty<EdgeOptions>();

        Assert.Equal("default", options.Defaults.Head);
        Assert.Equal("owner", options.Owner.Value);
        Assert.Null(options.Alias);
        Assert.Equal("made", options.Created!.Name);
        Assert.Equal("f", options.Created.Fallbacks!.Head);
        Assert.Null(options.ReadOnly);
        Assert.Equal("ignored", options.Unbuildable!.Name);   // the default binder uses its constructor
        Assert.Equal(new Version(1, 2, 3), options.Version);
        Assert.Equal(0, options.WriteOnlyCalls);
    }

    [Fact]
    public void BindNonEmpty_ShouldBindOntoAnExistingInstance()
    {
        IConfiguration config = Build(("Defaults", "x,y"), ("Alias", "nick"));
        EdgeOptions options = new();

        config.BindNonEmpty(options);

        Assert.Equal(new[] { "x", "y" }, options.Defaults.ToArray());
        Assert.Equal("nick", options.Alias!.Value.Value);
        Assert.Throws<ArgumentNullException>(() => config.BindNonEmpty(null!));
    }

    public sealed class Node
    {
        public Node? Child { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void Binder_ShouldRejectAnObjectGraphWithNonEmptyMembersDeeperThan64Levels()
    {
        // The limit guards this package's own recursion, which only covers objects that contain non-empty members.
        IConfiguration shallow = Build((string.Join(':', Enumerable.Repeat("Child", 15)) + ":Name", "leaf"));
        IConfiguration deep = Build((string.Join(':', Enumerable.Repeat("Child", 70)) + ":Name", "leaf"));

        TaggedNode node = shallow.GetNonEmpty<TaggedNode>();
        for (int i = 0; i < 15; i++)
            node = node.Child!;

        Assert.Equal("leaf", node.Name);
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => deep.GetNonEmpty<TaggedNode>());
        Assert.Contains("too deep", ex.Message);
        Assert.StartsWith("Configuration key 'Child:", ex.Message);
    }

    [Fact]
    public void Binder_ShouldLeaveAPlainObjectGraphToTheDefaultBinder_AtAnyDepth()
    {
        IConfiguration deep = Build((string.Join(':', Enumerable.Repeat("Child", 40)) + ":Name", "leaf"));

        Node node = deep.GetNonEmpty<Node>();
        for (int i = 0; i < 40; i++)
            node = node.Child!;

        Assert.Equal("leaf", node.Name);
    }

    public sealed class TaggedNode
    {
        public TaggedNode? Child { get; set; }
        public NonEmptyList<string>? Tags { get; set; }
        public string? Name { get; set; }
    }

    [Fact]
    public void Binder_RequiredPropertyOfDerivedCollectionWithoutFactory_ShouldExplainWhatIsMissing()
    {
        IConfiguration config = Build(("Items", "1"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmpty<NoFactoryOptions>());

        Assert.Contains("does not expose a static From", ex.Message);
    }

    public sealed class NoFactoryOptions
    {
        public NoFactoryList? Items { get; set; }
    }

    [Theory]
    [InlineData("Smtp")]
    [InlineData(null)]
    public void BindNonEmptyConfiguration_ShouldReloadThroughIOptionsMonitor(string? sectionPath)
    {
        string prefix = sectionPath is null ? "" : sectionPath + ":";
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [prefix + "Hosts:0"] = "before", [prefix + "Sender"] = "s" })
            .Build();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(config);
        services.AddOptions<SmtpOptions>().BindNonEmptyConfiguration(sectionPath);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<SmtpOptions> monitor = provider.GetRequiredService<IOptionsMonitor<SmtpOptions>>();

        Assert.Equal("before", monitor.CurrentValue.Hosts.Head);
        config[prefix + "Hosts:0"] = "after";
        config.Reload();

        Assert.Equal("after", monitor.CurrentValue.Hosts.Head);
    }

    [Fact]
    public void PublicEntryPoints_ShouldRejectNullArguments()
    {
        IConfiguration config = Build();

        Assert.Throws<ArgumentNullException>(() => NonEmptyConfigurationBinder.Get<SmtpOptions>(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyConfigurationBinder.Bind(null!, new object()));
        Assert.Throws<ArgumentNullException>(() => NonEmptyConfigurationBinder.IsNonEmptyType(null!));
        Assert.Throws<ArgumentNullException>(() => NonEmptyConfigurationBinder.ReadNonEmpty(null!, typeof(NonEmptyList<int>)));
        Assert.Throws<ArgumentNullException>(() => NonEmptyConfigurationBinder.ReadNonEmpty(config.GetSection("x"), null!));
        Assert.Throws<ArgumentNullException>(() => ((IConfiguration)null!).GetNonEmptyList<int>("x"));
        Assert.Throws<ArgumentNullException>(() => config.GetNonEmptyList<int>(null!));
        Assert.Throws<ArgumentNullException>(() => ((IConfiguration)null!).GetNonEmptyListOrNull<int>("x"));
        Assert.Throws<ArgumentNullException>(() => config.GetNonEmptyListOrNull<int>(null!));
        Assert.Throws<ArgumentNullException>(() => ((OptionsBuilder<SmtpOptions>)null!).BindNonEmptyConfiguration());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddNonEmptyOptions<SmtpOptions>("x"));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddNonEmptyOptions<SmtpOptions>(null!));
    }

    public sealed class RequiredHosts
    {
        public int Port { get; set; }
        public NonEmptyList<string> Hosts { get; set; } = null!;
    }

    public sealed class Named
    {
        public NonEmptyString Name { get; set; }
    }

    [Fact]
    public void GetNonEmptyDictionary_WithNestedPlainCollections_ShouldConvertEveryInnerValueStrictly()
    {
        IConfiguration nestedDictionary = Build(("C:a:x", "1"), ("C:a:y", "bad"));
        IConfiguration nestedList = Build(("C:a:0", "1"), ("C:a:1", "bad"));

        InvalidOperationException dictionaryError = Assert.Throws<InvalidOperationException>(() => nestedDictionary.GetNonEmptyDictionary<string, Dictionary<string, int>>("C"));
        InvalidOperationException listError = Assert.Throws<InvalidOperationException>(() => nestedList.GetNonEmptyDictionary<string, List<int>>("C"));
        InvalidOperationException arrayError = Assert.Throws<InvalidOperationException>(() => nestedList.GetNonEmptyDictionary<string, int[]>("C"));

        Assert.Equal("Configuration key 'C:a:y' contains 'bad', which is not a valid Int32.", dictionaryError.Message);
        Assert.Equal("Configuration key 'C:a:1' contains 'bad', which is not a valid Int32.", listError.Message);
        Assert.Equal(listError.Message, arrayError.Message);
    }

    [Fact]
    public void GetNonEmptyList_WithNestedPlainCollections_ShouldBindValidValuesAndRejectBadOnes()
    {
        IConfiguration valid = Build(("P:0:0", "1"), ("P:0:1", "2"), ("P:1:0", "3"));
        IConfiguration invalid = Build(("P:0:0", "1"), ("P:0:1", "x"));
        IConfiguration sets = Build(("S:0:0", "a"), ("S:0:1", "a"), ("S:0:2", "b"));

        NonEmptyList<List<int>> lists = valid.GetNonEmptyList<List<int>>("P");
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => invalid.GetNonEmptyList<int[]>("P"));
        NonEmptyList<HashSet<string>> unique = sets.GetNonEmptyList<HashSet<string>>("S");

        Assert.Equal(new[] { 1, 2 }, lists[0]);
        Assert.Equal(new[] { 3 }, lists[1]);
        Assert.Equal("Configuration key 'P:0:1' contains 'x', which is not a valid Int32.", ex.Message);
        Assert.Equal(2, unique.Head.Count);
    }

    [Fact]
    public void GetNonEmptyDictionary_WithNestedNonEmptyValues_ShouldReadEachAsANonEmptyCollection()
    {
        IConfiguration array = Build(("C:a:0", "1"), ("C:a:1", "2"));
        IConfiguration commaSeparated = Build(("C:a", "1,2"));
        IConfiguration duplicate = Build(("C:a:0", "1"), ("C:a:1", "1"));

        Assert.Equal(new[] { 1, 2 }, array.GetNonEmptyDictionary<string, ConstrainedList<int, Distinct>>("C")["a"]);
        Assert.Equal(new[] { 1, 2 }, commaSeparated.GetNonEmptyDictionary<string, NonEmptyList<int>>("C")["a"]);
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => duplicate.GetNonEmptyDictionary<string, ConstrainedList<int, Distinct>>("C"));
        Assert.StartsWith("Configuration key 'C:a' is invalid:", ex.Message);
    }

    [Fact]
    public void ObjectElements_ShouldEnforceTheirOwnRequiredNonEmptyMembers()
    {
        IConfiguration missingHosts = Build(("P:0:Port", "1"));
        IConfiguration missingName = Build(("C:a:Other", "1"));
        IConfiguration validHosts = Build(("C:a:Port", "1"), ("C:a:Hosts:0", "h1"), ("C:a:Hosts:1", "h2"));

        InvalidOperationException listError = Assert.Throws<InvalidOperationException>(() => missingHosts.GetNonEmptyList<RequiredHosts>("P"));
        InvalidOperationException dictionaryError = Assert.Throws<InvalidOperationException>(() => missingName.GetNonEmptyDictionary<string, Named>("C"));
        NonEmptyDictionary<string, RequiredHosts> bound = validHosts.GetNonEmptyDictionary<string, RequiredHosts>("C");

        Assert.Equal("Configuration key 'P:0:Hosts' is required and must contain at least one element.", listError.Message);
        Assert.Contains("'C:a:Name'", dictionaryError.Message);
        Assert.Equal(new[] { "h1", "h2" }, bound["a"].Hosts);
    }

    [Fact]
    public void ObjectElements_WrittenAsASingleValue_ShouldThrow()
    {
        IConfiguration config = Build(("P:0", "hello"), ("P:1:Port", "1"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<Endpoint>("P"));

        Assert.Equal("Configuration key 'P:0' must be a section with the members of Endpoint, not a single value.", ex.Message);
    }

    [Fact]
    public void GetNonEmptyDictionary_WithKeysThatConvertToTheSameKey_ShouldThrowInsteadOfOverwriting()
    {
        IConfiguration config = Build(("C:1", "a"), ("C:01", "b"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyDictionary<int, string>("C"));

        Assert.Contains("both convert to the key '1'", ex.Message);
        Assert.Contains("'C:1'", ex.Message);
        Assert.Contains("'C:01'", ex.Message);
    }

    [Fact]
    public void Values_ThatConvertToNull_ShouldThrowInsteadOfBeingDropped()
    {
        IConfiguration nullableValue = Build(("C:a", "1"), ("C:b", ""));
        IConfiguration emptyUri = Build(("P:0", "https://a.test"), ("P:1", ""));

        InvalidOperationException nullable = Assert.Throws<InvalidOperationException>(() => nullableValue.GetNonEmptyDictionary<string, int?>("C"));
        InvalidOperationException uri = Assert.Throws<InvalidOperationException>(() => emptyUri.GetNonEmptyList<Uri>("P"));

        Assert.Equal("Configuration key 'C:b' has no value, which is not a valid Int32.", nullable.Message);
        Assert.Equal("Configuration key 'P:1' has no value; a non-empty collection cannot contain null.", uri.Message);
    }

    [Fact]
    public void TypesWithAStringConverter_ShouldStillBindFromTheirObjectForm()
    {
        IConfiguration objectForm = Build(("Map:origin:X", "1"), ("Map:origin:Y", "2"));
        IConfiguration stringForm = Build(("Map:origin", "3, 4"));

        Assert.Equal(new System.Drawing.Point(1, 2), objectForm.GetNonEmptyDictionary<string, System.Drawing.Point>("Map")["origin"]);
        Assert.Equal(new System.Drawing.Point(3, 4), stringForm.GetNonEmptyDictionary<string, System.Drawing.Point>("Map")["origin"]);
    }

    public sealed class GetOnlyCollections
    {
        public List<int> Ports { get; } = new();
        public Dictionary<string, int> Limits { get; } = new();
        public string? Name { get; set; }
    }

    public sealed class OptionsWithGetOnlyList
    {
        public List<string> Hosts { get; } = new();
        public NonEmptyList<int> Ports { get; set; } = null!;
    }

    public sealed class KeyNamed
    {
        [ConfigurationKeyName("host_name")]
        public string? HostName { get; set; }

        [ConfigurationKeyName("tag_list")]
        public NonEmptyList<string>? Tags { get; set; }
    }

    public sealed record PositionalEndpoint(string Host, int Port);

    public sealed class WithRecordMember
    {
        public string? Name { get; set; }
        public PositionalEndpoint? Endpoint { get; set; }
    }

    public sealed class CtorWithNonEmptyMember(string host)
    {
        public string Host { get; } = host;
        public NonEmptyList<int> Ports { get; set; } = null!;
    }

    public sealed class MyIntList : List<int>;

    public sealed class PrivateSetter
    {
        public NonEmptyList<int>? Ports { get; private set; }
        public string? Name { get; private set; }
    }

    public sealed class RecursiveNode
    {
        public string? Name { get; set; }
        public NonEmptyList<RecursiveNode>? Children { get; set; }
    }

    [TypeConverter(typeof(StrictCodeConverter))]
    public sealed record StrictCode(string Value);

    public sealed class StrictCodeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

        public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
            => value is string text && text.StartsWith('C') ? new StrictCode(text) : throw new InvalidCastException($"bad code {value}");
    }

    [Fact]
    public void GetOnlyCollections_ShouldBeFilledByTheDefaultBinder_OnOptionsAndOnElements()
    {
        IConfiguration options = Build(("Hosts:0", "a"), ("Hosts:1", "b"), ("Ports:0", "1"));
        IConfiguration elements = Build(("P:0:Ports:0", "1"), ("P:0:Ports:1", "2"), ("P:0:Limits:k", "5"), ("P:0:Name", "n"));

        OptionsWithGetOnlyList bound = options.GetNonEmpty<OptionsWithGetOnlyList>();
        NonEmptyList<GetOnlyCollections> list = elements.GetNonEmptyList<GetOnlyCollections>("P");

        Assert.Equal(new[] { "a", "b" }, bound.Hosts);
        Assert.Equal(new[] { 1, 2 }, list.Head.Ports);
        Assert.Equal(5, list.Head.Limits["k"]);
    }

    [Fact]
    public void ConfigurationKeyName_ShouldBeHonoured_ForPlainAndNonEmptyMembers()
    {
        IConfiguration config = Build(("P:0:host_name", "h1"), ("P:0:tag_list", "a,b"));

        KeyNamed item = config.GetNonEmptyList<KeyNamed>("P").Head;

        Assert.Equal("h1", item.HostName);
        Assert.Equal(new[] { "a", "b" }, item.Tags!);
    }

    [Fact]
    public void ConstructorBoundTypes_ShouldBindThroughTheDefaultBinder_UnlessTheyHaveNonEmptyMembers()
    {
        IConfiguration member = Build(("P:0:Name", "a"), ("P:0:Endpoint:Host", "h"), ("P:0:Endpoint:Port", "25"));
        IConfiguration element = Build(("P:0:Host", "h"), ("P:0:Port", "25"));
        IConfiguration withNonEmpty = Build(("P:0:host", "h"), ("P:0:Ports:0", "1"));

        Assert.Equal(new PositionalEndpoint("h", 25), member.GetNonEmptyList<WithRecordMember>("P").Head.Endpoint);
        Assert.Equal(new PositionalEndpoint("h", 25), element.GetNonEmptyList<PositionalEndpoint>("P").Head);
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => withNonEmpty.GetNonEmptyList<CtorWithNonEmptyMember>("P"));
        Assert.Contains("needs a public parameterless constructor", ex.Message);
    }

    [Fact]
    public void ConcreteCollectionTypes_ShouldBeReadStrictly()
    {
        IConfiguration valid = Build(("P:0:0", "2"), ("P:0:1", "1"));
        IConfiguration invalid = Build(("P:0:0", "1"), ("P:0:1", "x"));
        IConfiguration sorted = Build(("P:0:b", "2"), ("P:0:a", "1"));

        Assert.Equal(new[] { 2, 1 }, valid.GetNonEmptyList<MyIntList>("P").Head);
        Assert.Equal(new[] { 1, 2 }, valid.GetNonEmptyList<SortedSet<int>>("P").Head);
        Assert.Equal(new[] { 2, 1 }, valid.GetNonEmptyList<System.Collections.ObjectModel.Collection<int>>("P").Head);
        Assert.Equal(1, sorted.GetNonEmptyList<SortedList<string, int>>("P").Head["a"]);
        Assert.Equal(2, sorted.GetNonEmptyList<SortedDictionary<string, int>>("P").Head["b"]);
        Assert.Equal(
            "Configuration key 'P:0:1' contains 'x', which is not a valid Int32.",
            Assert.Throws<InvalidOperationException>(() => invalid.GetNonEmptyList<SortedSet<int>>("P")).Message);
    }

    [Fact]
    public void NullForANestedNonEmptyValue_ShouldFailOnEveryTargetFramework()
    {
        // .NET 8 stores a JSON null as "" and .NET 10 as null; both must give the same error.
        IConfiguration asNull = Build(("P:0:0", null));
        IConfiguration asEmpty = Build(("P:0:0", ""));

        string nullMessage = Assert.Throws<InvalidOperationException>(() => asNull.GetNonEmptyList<List<NonEmptyList<int>>>("P")).Message;
        string emptyMessage = Assert.Throws<InvalidOperationException>(() => asEmpty.GetNonEmptyList<List<NonEmptyList<int>>>("P")).Message;

        Assert.Equal("Configuration key 'P:0:0' must contain at least one element.", nullMessage);
        Assert.Equal(nullMessage, emptyMessage);
    }

    [Fact]
    public void ADeepRecursiveGraph_ShouldFailWithTooDeep_InsteadOfOverflowingTheStack()
    {
        IConfiguration config = Build((string.Join(':', Enumerable.Repeat("Children:0", 200)) + ":Name", "leaf"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<RecursiveNode>("Children"));

        Assert.Contains("too deep", ex.Message);
    }

    [Fact]
    public void SpecialElementTypes_ShouldBindLikeTheDefaultBinder()
    {
        IConfiguration config = Build(("S:0", "a"), ("S:1:X", "2"), ("O:0", "a"), ("O:1", "b"), ("B:0", "AQID"));

        NonEmptyList<IConfigurationSection> sections = config.GetNonEmptyList<IConfigurationSection>("S");
        NonEmptyList<object> objects = config.GetNonEmptyList<object>("O");
        NonEmptyList<byte[]> bytes = config.GetNonEmptyList<byte[]>("B");

        Assert.Equal("a", sections.Head.Value);
        Assert.Equal("2", sections[1]["X"]);
        Assert.Equal(new object[] { "a", "b" }, objects);
        Assert.Equal(new byte[] { 1, 2, 3 }, bytes.Head);
        Assert.Contains("base64", Assert.Throws<InvalidOperationException>(() => Build(("B:0", "%%")).GetNonEmptyList<byte[]>("B")).Message);
    }

    [Fact]
    public void ACustomConverterException_ShouldNameTheKey()
    {
        IConfiguration config = Build(("C:0", "C1"), ("C:1", "zz"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => config.GetNonEmptyList<StrictCode>("C"));

        Assert.Equal("Configuration key 'C:1' contains 'zz', which is not a valid StrictCode.", ex.Message);
        Assert.IsType<InvalidCastException>(ex.InnerException);
    }

    [Fact]
    public void PrivateSetters_ShouldBeLeftAlone_LikeTheDefaultBinderDoes()
    {
        IConfiguration config = Build(("Ports:0", "1"), ("Name", "n"));

        PrivateSetter bound = config.GetNonEmpty<PrivateSetter>();

        Assert.Null(bound.Ports);
        Assert.Null(bound.Name);
    }

    public sealed class EndpointWithHosts
    {
        public NonEmptyList<string> Hosts { get; set; } = null!;
        public int Port { get; set; }
    }

    public sealed class GetOnlyNonEmptyCollections
    {
        public List<EndpointWithHosts> Endpoints { get; } = new();
        public Dictionary<string, NonEmptyList<int>> Map { get; } = new() { ["keep"] = NonEmptyList.Of(1) };
    }

    public sealed class NoInstanceToFill
    {
        public List<EndpointWithHosts>? Endpoints { get; }
    }

    public struct StructEndpoint
    {
        public NonEmptyList<string> Hosts { get; set; }
        public int Port { get; set; }
    }

    public sealed class WithStruct
    {
        public StructEndpoint Endpoint { get; set; }
        public NonEmptyList<StructEndpoint>? Items { get; set; }
    }

    public sealed class ThrowingSetter
    {
        public NonEmptyList<string> Hosts
        {
            get => NonEmptyList.Of("x");
            set => throw new ArgumentException("need two hosts");
        }
    }

    public sealed class ThrowingConstructor
    {
        public ThrowingConstructor() => throw new InvalidOperationException("boom");

        public NonEmptyList<string>? Hosts { get; set; }
    }

    public sealed class KeyCollision
    {
        public NonEmptyList<string>? Hosts { get; set; }

        [ConfigurationKeyName("Hosts")]
        public string? HostsRaw { get; set; }
    }

    public class BaseWithHosts
    {
        public NonEmptyList<string>? Hosts { get; set; }
    }

    public sealed class DerivedWithStringHosts : BaseWithHosts
    {
        public new string? Hosts { get; set; }
    }

    public sealed record PositionalOptions(NonEmptyList<string> Hosts);

    [Fact]
    public void GetOnlyCollections_WithNonEmptyElements_ShouldBeFilledInPlace_AndMergedLikeTheDefaultBinder()
    {
        IConfiguration config = Build(("Endpoints:0:Hosts:0", "a"), ("Endpoints:0:Port", "1"), ("Map:new", "2,3"));

        GetOnlyNonEmptyCollections bound = config.GetNonEmpty<GetOnlyNonEmptyCollections>();

        Assert.Equal("a", Assert.Single(bound.Endpoints).Hosts.Head);
        Assert.Equal(new[] { "keep", "new" }, bound.Map.Keys.OrderBy(k => k));
        Assert.Contains("has no public setter and no collection instance to fill",
            Assert.Throws<InvalidOperationException>(() => config.GetNonEmpty<NoInstanceToFill>()).Message);
        Assert.Contains("'Endpoints:0:Port'",
            Assert.Throws<InvalidOperationException>(() => Build(("Endpoints:0:Hosts:0", "a"), ("Endpoints:0:Port", "x")).GetNonEmpty<GetOnlyNonEmptyCollections>()).Message);
    }

    [Fact]
    public void Structs_WithNonEmptyMembers_ShouldBeBoundAndChecked()
    {
        IConfiguration valid = Build(("Endpoint:Hosts:0", "a"), ("Endpoint:Port", "5"), ("Items:0:Hosts", "b,c"), ("Items:0:Port", "6"));
        IConfiguration missing = Build(("Items:0:Port", "6"));

        WithStruct bound = valid.GetNonEmpty<WithStruct>();

        Assert.Equal("a", bound.Endpoint.Hosts.Head);
        Assert.Equal(5, bound.Endpoint.Port);
        Assert.Equal(new[] { "b", "c" }, bound.Items!.Head.Hosts);
        Assert.Equal("Configuration key 'Items:0:Hosts' is required and must contain at least one element.",
            Assert.Throws<InvalidOperationException>(() => missing.GetNonEmpty<WithStruct>()).Message);
    }

    [Fact]
    public void ObjectElements_ShouldReadAJsonNullAsAnEmptyString_OnEveryTargetFramework()
    {
        IConfiguration config = Build(("Items:0", "a"), ("Items:1", null));

        Assert.Equal(new object[] { "a", "" }, config.GetNonEmptyList<object>("Items"));
    }

    [Fact]
    public void ReadOnlyCollections_AndThrowingMembers_ShouldFailNamingTheKey()
    {
        IConfiguration immutable = Build(("Items:0:0", "1"));
        IConfiguration hosts = Build(("Hosts", "a"));

        Assert.Contains("is a read-only or unsupported collection type",
            Assert.Throws<InvalidOperationException>(() => immutable.GetNonEmptyList<System.Collections.Immutable.ImmutableArray<int>>("Items")).Message);
        InvalidOperationException setter = Assert.Throws<InvalidOperationException>(() => hosts.GetNonEmpty<ThrowingSetter>());
        Assert.Equal("Configuration key 'Hosts' could not be assigned to ThrowingSetter.Hosts: need two hosts", setter.Message);
        InvalidOperationException constructor = Assert.Throws<InvalidOperationException>(() => Build(("Items:0:Hosts", "a")).GetNonEmptyList<ThrowingConstructor>("Items"));
        Assert.Equal("Configuration key 'Items:0': creating ThrowingConstructor failed: boom", constructor.Message);
    }

    [Fact]
    public void PlainMembers_SharingAKeyWithANonEmptyMember_ShouldStillBeBound()
    {
        IConfiguration config = Build(("Hosts", "a,b"));

        KeyCollision collision = config.GetNonEmpty<KeyCollision>();
        DerivedWithStringHosts derived = config.GetNonEmpty<DerivedWithStringHosts>();

        Assert.Equal(new[] { "a", "b" }, collision.Hosts!);
        Assert.Equal("a,b", collision.HostsRaw);
        Assert.Equal("a,b", derived.Hosts);   // the most derived property wins, as with the default binder
    }

    [Fact]
    public void CommaSeparatedNullableElements_ShouldNameTheUnderlyingType()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Build(("Values", "1,x")).GetNonEmptyList<int?>("Values"));

        Assert.Equal("Configuration key 'Values' contains 'x', which is not a valid Int32.", ex.Message);
    }

    [Fact]
    public void GetNonEmpty_OnAPositionalRecord_ShouldExplainTheConstructorRequirement()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Build(("Hosts:0", "a")).GetNonEmpty<PositionalOptions>());

        Assert.StartsWith("PositionalOptions needs a public parameterless constructor", ex.Message);
    }
}
