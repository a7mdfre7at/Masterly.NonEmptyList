using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Masterly.NonEmptyList.Analyzers.Tests;

/// <summary>The v3 migration fix: NonEmptyList no longer converts to List&lt;T&gt;, so offer .ToList().</summary>
public class ListConversionCodeFixTests
{
    [Fact]
    public async Task Assignment_ShouldOfferToList()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs)
                {
                    List<int> list = {|CS0029:xs|};
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void M(NonEmptyList<int> xs)
                {
                    List<int> list = xs.ToList();
                }
            }
            """;

        var test = new CSharpCodeFixTest<EmptyDiagnosticAnalyzer, ListConversionCodeFixProvider, DefaultVerifier>
        {
            TestCode = Verify.Usings + source,
            FixedCode = Verify.Usings + fixedSource,
        };
        Verify.Configure(test);
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Argument_ShouldOfferToList()
    {
        const string source = """
            class C
            {
                void Consume(List<string> items) { }

                void M(NonEmptyList<string> xs)
                {
                    Consume({|CS1503:xs|});
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void Consume(List<string> items) { }

                void M(NonEmptyList<string> xs)
                {
                    Consume(xs.ToList());
                }
            }
            """;

        var test = new CSharpCodeFixTest<EmptyDiagnosticAnalyzer, ListConversionCodeFixProvider, DefaultVerifier>
        {
            TestCode = Verify.Usings + source,
            FixedCode = Verify.Usings + fixedSource,
        };
        Verify.Configure(test);
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnrelatedConversionError_ShouldNotOfferFix()
    {
        const string source = """
            class C
            {
                void M(string s)
                {
                    List<int> list = {|CS0029:s|};
                }
            }
            """;

        var test = new CSharpCodeFixTest<EmptyDiagnosticAnalyzer, ListConversionCodeFixProvider, DefaultVerifier>
        {
            TestCode = Verify.Usings + source,
            FixedCode = Verify.Usings + source,
        };
        Verify.Configure(test);
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LambdaReturn_ShouldOfferToList()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs)
                {
                    Func<List<int>> f = () => {|CS0029:{|CS1662:xs|}|};
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void M(NonEmptyList<int> xs)
                {
                    Func<List<int>> f = () => xs.ToList();
                }
            }
            """;

        await Run(source, fixedSource);
    }

    [Fact]
    public async Task ReturnAndDerivedTypes_ShouldOfferToList()
    {
        const string source = """
            class Tags : NonEmptyList<string>
            {
                public Tags(string first) : base(first) { }
            }

            class C
            {
                List<string> M(Tags tags) => {|CS0029:tags|};

                List<int> N(ImmutableNonEmptyList<int> xs)
                {
                    return {|CS0029:xs|};
                }
            }
            """;

        const string fixedSource = """
            class Tags : NonEmptyList<string>
            {
                public Tags(string first) : base(first) { }
            }

            class C
            {
                List<string> M(Tags tags) => tags.ToList();

                List<int> N(ImmutableNonEmptyList<int> xs)
                {
                    return xs.ToList();
                }
            }
            """;

        await Run(source, fixedSource);
    }

    [Fact]
    public async Task ConversionToOtherTargets_ShouldNotOfferToList()
    {
        // NonEmptyList -> HashSet<T> or -> string fails too, but ToList() would not help there.
        const string source = """
            class C
            {
                void Consume(HashSet<int> items) { }

                void M(NonEmptyList<int> xs)
                {
                    HashSet<int> set = {|CS0029:xs|};
                    string s = {|CS0029:xs|};
                    Consume({|CS1503:xs|});
                }
            }
            """;

        await Run(source, source);
    }

    [Fact]
    public async Task ArgumentToOverloadedMethod_ShouldOfferToList()
    {
        // With several candidates the compiler gives no converted type; the parameter of the first candidate decides.
        const string source = """
            class C
            {
                void Consume(List<int> items, int start) { }
                void Consume(List<int> items, string name) { }

                void M(NonEmptyList<int> xs)
                {
                    Consume({|CS1503:xs|}, 0);
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void Consume(List<int> items, int start) { }
                void Consume(List<int> items, string name) { }

                void M(NonEmptyList<int> xs)
                {
                    Consume(xs.ToList(), 0);
                }
            }
            """;

        await Run(source, fixedSource);
    }

    private static Task Run(string source, string fixedSource)
    {
        var test = new CSharpCodeFixTest<EmptyDiagnosticAnalyzer, ListConversionCodeFixProvider, DefaultVerifier>
        {
            TestCode = Verify.Usings + source,
            FixedCode = Verify.Usings + fixedSource,
        };
        Verify.Configure(test);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }
}
