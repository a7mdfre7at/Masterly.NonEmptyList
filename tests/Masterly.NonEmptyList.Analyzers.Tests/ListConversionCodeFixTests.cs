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
}
