using Microsoft.CodeAnalysis.Testing;

namespace Masterly.NonEmptyList.Analyzers.Tests;

public class EmptyFactoryArgumentAnalyzerTests
{
    [Fact]
    public async Task ProvablyEmptyArguments_ShouldReport()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var a = {|#0:NonEmptyList.From(Array.Empty<int>())|};
                    var b = {|#1:NonEmptyList<int>.From(Enumerable.Empty<int>())|};
                    var c = {|#2:new int[0].ToNonEmptyList()|};
                    var d = {|#3:new List<int>().ToNonEmptyList()|};
                    var e = {|#4:NonEmptyList.From(new int[] { })|};
                    var f = {|#5:ImmutableNonEmptyList.From(new List<string>())|};
                    var g = {|#6:NonEmptySet.From(new HashSet<int>())|};
                    var h = {|#7:new NonEmptyList<int>(new List<int>())|};
                    var i = {|#8:NonEmptyList<int>.From([])|};
                    var j = {|#9:new ImmutableNonEmptyList<int>(Array.Empty<int>())|};
                    var k = {|#10:NonEmptyList.From((new int[] { }))|};
                    var l = {|#11:ConstrainedList<int, Sorted>.From(Enumerable.Empty<int>())|};
                    var m = {|#12:NonEmptyList.From(System.Collections.Immutable.ImmutableArray<int>.Empty)|};
                }
            }
            """;

        await Verify.Analyzer<EmptyFactoryArgumentAnalyzer>(source,
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(0).WithArguments("NonEmptyList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(1).WithArguments("NonEmptyList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(2).WithArguments("NonEmptyListLinqExtensions.ToNonEmptyList"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(3).WithArguments("NonEmptyListLinqExtensions.ToNonEmptyList"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(4).WithArguments("NonEmptyList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(5).WithArguments("ImmutableNonEmptyList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(6).WithArguments("NonEmptySet.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(7).WithArguments("new NonEmptyList<int>"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(8).WithArguments("NonEmptyList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(9).WithArguments("new ImmutableNonEmptyList<int>"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(10).WithArguments("NonEmptyList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(11).WithArguments("ConstrainedList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(12).WithArguments("NonEmptyList.From")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PossiblyNonEmptyArguments_ShouldNotReport()
    {
        const string source = """
            class C
            {
                void M(IEnumerable<int> source, int n)
                {
                    var a = NonEmptyList.From(source);
                    var b = NonEmptyList.From(new[] { 1 });
                    var c = NonEmptyList.From(new List<int> { 1 });
                    var d = source.ToNonEmptyList();
                    var e = NonEmptyList.From(new int[n]);
                    var f = new NonEmptyList<int>(1, new List<int>());
                    var g = new NonEmptyList<int>(new List<int>(source));
                    var g2 = new NonEmptyList<int>(new List<int> { 1 });
                    var g3 = new ImmutableNonEmptyList<int>(source);
                    var h = NonEmptyList.From(Enumerable.Range(1, 3));
                    var i = NonEmptyList<int>.From(new List<int>(capacity: 4) { 1 });
                    string s = "";
                    var j = s.Length;
                }
            }
            """;

        await Verify.Analyzer<EmptyFactoryArgumentAnalyzer>(source).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EmptyStringLiteral_ToNonEmptyString_ShouldNotBeFlaggedByThisRule_ButEmptyParseShould()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var chars = {|#0:"".ToNonEmptyList()|};
                }
            }
            """;

        await Verify.Analyzer<EmptyFactoryArgumentAnalyzer>(source,
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(0).WithArguments("NonEmptyListLinqExtensions.ToNonEmptyList")).RunAsync(TestContext.Current.CancellationToken);
    }
}
