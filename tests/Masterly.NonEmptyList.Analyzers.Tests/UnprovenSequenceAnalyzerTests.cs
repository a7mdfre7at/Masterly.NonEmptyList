using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Masterly.NonEmptyList.Analyzers.Tests;

public class UnprovenSequenceAnalyzerTests
{
    private const string EnableRule = """
        [*.cs]
        dotnet_diagnostic.NEL005.severity = warning
        """;

    [Fact]
    public async Task UnprovenReceivers_ShouldReportWhenEnabled()
    {
        const string source = """
            class C
            {
                void M(IEnumerable<int> xs, List<int> list, int[] array, IEnumerable<string> names)
                {
                    var a = {|#0:xs.First()|};
                    var b = {|#1:list.Max()|};
                    var c = {|#2:array.Average()|};
                    var d = {|#3:xs.Aggregate((x, y) => x + y)|};
                    var e = {|#4:xs.Single()|};
                    var f = {|#5:xs.Last()|};
                    var g = {|#6:xs.Where(x => x > 1).Min()|};
                    var h = {|#7:names.MaxBy(n => n.Length)|};
                    var i = {|#8:xs.ElementAt(0)|};
                }
            }
            """;

        var test = Verify.Analyzer<UnprovenSequenceAnalyzer>(source,
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("First()", "xs"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(1).WithArguments("Max()", "list"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(2).WithArguments("Average()", "array"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(3).WithArguments("Aggregate()", "xs"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(4).WithArguments("Single()", "xs"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(5).WithArguments("Last()", "xs"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(6).WithArguments("Min()", "xs.Where(x => x > 1)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(8).WithArguments("ElementAt()", "xs"));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ProvenReceivers_ShouldNotReport()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs, IEnumerable<int> source, int fallback, IEnumerable<string> names)
                {
                    var a = xs.Max();
                    var b = new[] { 1, 2 }.Max();
                    var c = Enumerable.Range(1, 10).Min();
                    var d = Enumerable.Repeat(1, 3).Average();
                    var e = source.Prepend(fallback).First();
                    var f = source.Append(fallback).Last();
                    var g = source.DefaultIfEmpty().First();
                    var h = xs.OrderBy(x => x).First();
                    var i = new List<int> { 1 }.Max();
                    var j = source.ToNonEmptyList().Max();
                    var k = xs.Select(x => x * 2).ToList().Max();
                    var l = source.Aggregate(0, (acc, x) => acc + x);
                    var m = source.Select(x => (int?)x).Max();
                    var n = new[] { 1 }.Concat(source).Max();
                    var o = source.Concat(new[] { 1 }).Max();
                    var p = xs.ReverseList().First();
                    var s = ((IEnumerable<int>)[1, 2]).Max();
                    var u = names.MaxBy(n => n.Length);
                    var v = source.Select(x => (object)x).Max();
                    var t = source.Union(xs).Max();
                }
            }
            """;

        var test = Verify.Analyzer<UnprovenSequenceAnalyzer>(source);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Rule_ShouldBeOptIn_AndRespectEditorConfigSeverity()
    {
        const string source = """
            class C
            {
                int M(IEnumerable<int> xs) => xs.First();
            }
            """;

        Assert.False(Descriptors.UnprovenSequence.IsEnabledByDefault);

        var test = Verify.Analyzer<UnprovenSequenceAnalyzer>(source);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "[*.cs]\ndotnet_diagnostic.NEL005.severity = none"));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldWrapReceiverInToNonEmptyList()
    {
        const string source = """
            class C
            {
                int M(IEnumerable<int> xs) => {|#0:xs.Max()|};
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(IEnumerable<int> xs) => xs.ToNonEmptyList().Max();
            }
            """;

        var test = Verify.CodeFix<UnprovenSequenceAnalyzer, ToNonEmptyListCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "xs"));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldAddUsingWhenMissing()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                int M(IEnumerable<int> xs) => {|#0:xs.Max()|};
            }
            """;

        const string fixedSource = """
            using System.Collections.Generic;
            using System.Linq;
            using Masterly.NonEmptyList;

            class C
            {
                int M(IEnumerable<int> xs) => xs.ToNonEmptyList().Max();
            }
            """;

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<UnprovenSequenceAnalyzer, ToNonEmptyListCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
        };
        Verify.Configure(test);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.ExpectedDiagnostics.Add(Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "xs"));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StaticCallForm_ShouldReportAndFixFirstArgument()
    {
        const string source = """
            class C
            {
                int M(IEnumerable<int> xs) => {|#0:Enumerable.Max(xs)|} + Enumerable.Max(new[] { 1 });
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(IEnumerable<int> xs) => Enumerable.Max(xs.ToNonEmptyList()) + Enumerable.Max(new[] { 1 });
            }
            """;

        var test = Verify.CodeFix<UnprovenSequenceAnalyzer, ToNonEmptyListCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "xs"));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FixedSizeArrays_ShouldCountAsProven()
    {
        // new int[3] holds three zeroes, so Max() cannot throw; only a zero or non-constant size stays unproven.
        const string source = """
            class C
            {
                void M(int n)
                {
                    var a = new int[3].Max();
                    var b = new int[] { 1 }.Max();
                    var c = {|#0:new int[0].Max()|};
                    var d = {|#1:new int[n].Max()|};
                    var e = {|#2:new int[] { }.Max()|};
                    const int size = 2;
                    var f = new int[size].Min();
                }
            }
            """;

        var test = Verify.Analyzer<UnprovenSequenceAnalyzer>(source,
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "new int[0]"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(1).WithArguments("Max()", "new int[n]"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(2).WithArguments("Max()", "new int[] { }"));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConstructionsThatMayBeEmpty_ShouldReport()
    {
        const string source = """
            class C
            {
                void M(IEnumerable<int> source, NonEmptyList<int> xs, int count)
                {
                    var a = {|#0:((IEnumerable<int>)[.. source]).Max()|};
                    var b = {|#1:new List<int>().Max()|};
                    var c = {|#2:Enumerable.Range(1, count).Max()|};
                    var d = {|#3:Enumerable.Repeat(1, 0).Max()|};
                    var e = {|#4:source.OrderBy(x => x).First()|};
                    var f = {|#5:source.Concat(source).Max()|};
                    var g = {|#6:source.Where(x => x > 0).Select(x => x).First()|};
                    var h = {|#7:xs.Where(x => x > 0).Max()|};
                    var i = {|#8:Enumerable.Concat(source, source).Max()|};
                    var j = {|#9:new HashSet<int>(source).Max()|};
                    var k = {|#10:Enumerable.Select(source, x => x).Max()|};
                    var l = new[] { 1 }?{|#11:.Select(x => x).Max()|};
                }
            }
            """;

        var test = Verify.Analyzer<UnprovenSequenceAnalyzer>(source,
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "((IEnumerable<int>)[.. source])"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(1).WithArguments("Max()", "new List<int>()"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(2).WithArguments("Max()", "Enumerable.Range(1, count)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(3).WithArguments("Max()", "Enumerable.Repeat(1, 0)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(4).WithArguments("First()", "source.OrderBy(x => x)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(5).WithArguments("Max()", "source.Concat(source)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(6).WithArguments("First()", "source.Where(x => x > 0).Select(x => x)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(7).WithArguments("Max()", "xs.Where(x => x > 0)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(8).WithArguments("Max()", "Enumerable.Concat(source, source)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(9).WithArguments("Max()", "new HashSet<int>(source)"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(10).WithArguments("Max()", "Enumerable.Select(source, x => x)"),
            // Conservative: a null-conditional source is not followed.
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(11).WithArguments("Max()", ".Select(x => x)"));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoreProvenConstructions_ShouldNotReport()
    {
        const string source = """
            class C
            {
                void M(IEnumerable<int> source, NonEmptyList<int> xs)
                {
                    var a = ((IEnumerable<int>)(new[] { 1 })).Max();
                    var b = new[] { 1 }.Reverse().Distinct().Cast<int>().AsEnumerable().ToArray().Max();
                    var c = xs.OrderByDescending(x => x).ThenBy(x => x).ThenByDescending(x => x).First();
                    var d = new[] { 3 }.Order().OrderDescending().Max();
                    var e = source.Union(new[] { 1 }).Max();
                    var f = new HashSet<int> { 1 }.Max();
                    var g = Enumerable.Range(0, 1).Last();
                    var h = new[] { 2.5 }.Average();
                    var i = new[] { "a" }.Single();
                    var j = new[] { 1 }.ElementAt(0);
                    var k = new[] { 1 }.Aggregate((x, y) => x + y);
                    var l = new[] { 1 }.MinBy(x => x);
                    var m = source.Average(x => (double?)x);
                    var n = Enumerable.Max(xs);
                    var o = ((IEnumerable<int>)xs).Max();
                    var p = Enumerable.Concat(new[] { 1 }, source).Max();
                    var q = Enumerable.Select(new[] { 1 }, x => x * 2).Max();
                    var r = source.Union(xs, EqualityComparer<int>.Default).Max();
                    var s = Enumerable.Union(source, xs, EqualityComparer<int>.Default).Max();
                }
            }
            """;

        var test = Verify.Analyzer<UnprovenSequenceAnalyzer>(source);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NonLinqAndUnresolvedCalls_ShouldNotReport()
    {
        // Same-named methods outside System.Linq.Enumerable and late-bound calls are out of scope. An unresolved source
        // proves nothing, so it stays reported next to the compiler error.
        const string source = """
            class Box
            {
                public int First() => 1;
                public int Max() => 2;
            }

            class C
            {
                int Local() => 1;

                void M(Box box, dynamic d)
                {
                    var a = box.First();
                    var b = box.Max();
                    var c = d.First();
                    var e = Local();
                    var f = {|#0:Enumerable.Max((IEnumerable<int>){|CS0103:Missing|}())|};
                }
            }
            """;

        var test = Verify.Analyzer<UnprovenSequenceAnalyzer>(source,
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "(IEnumerable<int>)Missing()"));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_InSingleLineFileWithoutUsings_ShouldAddTheUsingOnItsOwnLine()
    {
        const string source = "class C { int M(System.Collections.Generic.IEnumerable<int> xs) => {|#0:System.Linq.Enumerable.Max(xs)|}; }";
        const string fixedSource = "using Masterly.NonEmptyList;\r\n\r\nclass C { int M(System.Collections.Generic.IEnumerable<int> xs) => System.Linq.Enumerable.Max(xs.ToNonEmptyList()); }";

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<UnprovenSequenceAnalyzer, ToNonEmptyListCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
        };
        Verify.Configure(test);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.ExpectedDiagnostics.Add(Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "xs"));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_InFileWithoutUsings_ShouldAddTheUsing()
    {
        const string source = """
            class C
            {
                int M(System.Collections.Generic.IEnumerable<int> xs) => {|#0:System.Linq.Enumerable.Max(xs)|};
            }
            """;

        const string fixedSource = """
            using Masterly.NonEmptyList;

            class C
            {
                int M(System.Collections.Generic.IEnumerable<int> xs) => System.Linq.Enumerable.Max(xs.ToNonEmptyList());
            }
            """;

        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<UnprovenSequenceAnalyzer, ToNonEmptyListCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
        };
        Verify.Configure(test);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.ExpectedDiagnostics.Add(Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("Max()", "xs"));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldFixEveryOccurrence()
    {
        const string source = """
            class C
            {
                int M(IEnumerable<int> xs, List<int> ys) => {|#0:xs.First()|} + {|#1:ys.Where(y => y > 0).Last()|};
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(IEnumerable<int> xs, List<int> ys) => xs.ToNonEmptyList().First() + ys.Where(y => y > 0).ToNonEmptyList().Last();
            }
            """;

        var test = Verify.CodeFix<UnprovenSequenceAnalyzer, ToNonEmptyListCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(0).WithArguments("First()", "xs"),
            Verify.Diagnostic(Descriptors.UnprovenSequence).WithLocation(1).WithArguments("Last()", "ys.Where(y => y > 0)"));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", EnableRule));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
