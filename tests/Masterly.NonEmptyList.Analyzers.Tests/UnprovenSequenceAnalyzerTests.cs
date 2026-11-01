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
}
