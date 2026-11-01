using Microsoft.CodeAnalysis.Testing;

namespace Masterly.NonEmptyList.Analyzers.Tests;

public class EmptyCollectionExpressionAnalyzerTests
{
    [Fact]
    public async Task EmptyCollectionExpression_ToNonEmptyList_ShouldReport()
    {
        const string source = """
            class C
            {
                void M()
                {
                    NonEmptyList<int> xs = {|#0:[]|};
                }
            }
            """;

        await Verify.Analyzer<EmptyCollectionExpressionAnalyzer>(source,
            Verify.Diagnostic(Descriptors.EmptyCollectionExpression).WithLocation(0).WithArguments("NonEmptyList<int>")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EmptyCollectionExpression_ToOtherNonEmptyTypes_ShouldReport()
    {
        const string source = """
            class C
            {
                void M()
                {
                    ImmutableNonEmptyList<string> a = {|#0:[]|};
                    ConstrainedList<int, AtLeast<Two>> b = {|#1:[]|};
                    NonEmptySet<int> c = {|#2:[]|};
                    Take({|#3:[]|});
                }

                void Take(NonEmptyList<int> xs) { }
            }
            """;

        await Verify.Analyzer<EmptyCollectionExpressionAnalyzer>(source,
            Verify.Diagnostic(Descriptors.EmptyCollectionExpression).WithLocation(0).WithArguments("ImmutableNonEmptyList<string>"),
            Verify.Diagnostic(Descriptors.EmptyCollectionExpression).WithLocation(1).WithArguments("ConstrainedList<int, AtLeast<Two>>"),
            Verify.Diagnostic(Descriptors.EmptyCollectionExpression).WithLocation(2).WithArguments("NonEmptySet<int>"),
            Verify.Diagnostic(Descriptors.EmptyCollectionExpression).WithLocation(3).WithArguments("NonEmptyList<int>")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NonEmptyCollectionExpression_OrOtherTargets_ShouldNotReport()
    {
        const string source = """
            class C
            {
                void M()
                {
                    NonEmptyList<int> xs = [1, 2];
                    NonEmptyList<int> spread = [.. xs];
                    List<int> empty = [];
                    int[] array = [];
                }
            }
            """;

        await Verify.Analyzer<EmptyCollectionExpressionAnalyzer>(source).RunAsync(TestContext.Current.CancellationToken);
    }
}
