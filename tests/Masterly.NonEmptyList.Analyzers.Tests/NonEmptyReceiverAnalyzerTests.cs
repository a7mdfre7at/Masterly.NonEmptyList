using Microsoft.CodeAnalysis.Testing;

namespace Masterly.NonEmptyList.Analyzers.Tests;

public class NonEmptyReceiverAnalyzerTests
{
    [Fact]
    public async Task ThrowingOperators_OnNonEmptyList_ShouldReportWithReplacement()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs, ImmutableNonEmptyList<int> im)
                {
                    var a = {|#0:xs.First()|};
                    var b = {|#1:xs.Last()|};
                    var c = {|#2:xs.FirstOrDefault()|};
                    var d = {|#3:xs.LastOrDefault()|};
                    var e = {|#4:xs.Aggregate((x, y) => x + y)|};
                    var f = {|#5:xs.Count()|};
                    var g = {|#6:im.First()|};
                    var h = {|#7:xs.LongCount()|};
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source,
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(0).WithArguments("First()", "Head"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(1).WithArguments("Last()", "Last"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(2).WithArguments("FirstOrDefault()", "Head"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(3).WithArguments("LastOrDefault()", "Last"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(4).WithArguments("Aggregate()", "Reduce"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(5).WithArguments("Count()", "Count"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(6).WithArguments("First()", "Head"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(7).WithArguments("LongCount()", "Count")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OperatorsWithPredicates_OrOnPlainSequences_ShouldNotReport()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs, List<int> list)
                {
                    var a = xs.First(x => x > 1);
                    var b = xs.Aggregate(0, (acc, x) => acc + x);
                    var c = list.First();
                    var d = xs.Head;
                    var e = xs.Max();
                    var f = xs.Count;
                    var g = xs.Any(x => x > 1);
                    var h = list.Count == 0;
                    var i = xs.Count == 5;
                    var j = xs.Count > 1;
                    var k = xs.Count >= 2;
                    var l = xs.Count < 2;
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RedundantEmptinessChecks_ShouldReport()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs, NonEmptySet<int> set)
                {
                    var a = {|#0:xs.Any()|};
                    var b = {|#1:xs.Count == 0|};
                    var c = {|#2:xs.Count != 0|};
                    var d = {|#3:xs.Count > 0|};
                    var e = {|#4:xs.Count >= 1|};
                    var f = {|#5:0 < xs.Count|};
                    var g = {|#6:xs.Count < 1|};
                    var h = {|#7:xs.Count <= 0|};
                    var i = {|#8:set.Count == 0|};
                    var j = {|#9:xs.DefaultIfEmpty()|};
                    var k = {|#10:1 > xs.Count|};
                    var l = {|#11:{|#12:xs.Count()|} == 0|};
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source,
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(0).WithArguments("Any()", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(1).WithArguments("xs.Count == 0", "false"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(2).WithArguments("xs.Count != 0", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(3).WithArguments("xs.Count > 0", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(4).WithArguments("xs.Count >= 1", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(5).WithArguments("0 < xs.Count", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(6).WithArguments("xs.Count < 1", "false"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(7).WithArguments("xs.Count <= 0", "false"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(8).WithArguments("set.Count == 0", "false"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(9).WithArguments("DefaultIfEmpty()", "the list itself"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(10).WithArguments("1 > xs.Count", "false"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(12).WithArguments("Count()", "Count"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(11).WithArguments("xs.Count() == 0", "false")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldReplaceFirstWithHead()
    {
        const string source = """
            class C
            {
                int M(NonEmptyList<int> xs) => {|#0:xs.First()|};
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(NonEmptyList<int> xs) => xs.Head;
            }
            """;

        await Verify.CodeFix<NonEmptyReceiverAnalyzer, TotalMemberCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(0).WithArguments("First()", "Head")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldReplaceAggregateWithReduce_KeepingArguments()
    {
        const string source = """
            class C
            {
                int M(NonEmptyList<int> xs) => {|#0:xs.Aggregate((a, b) => a + b)|};
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(NonEmptyList<int> xs) => xs.Reduce((a, b) => a + b);
            }
            """;

        await Verify.CodeFix<NonEmptyReceiverAnalyzer, TotalMemberCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(0).WithArguments("Aggregate()", "Reduce")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldReplaceRedundantCheckWithConstant()
    {
        const string source = """
            class C
            {
                bool M(NonEmptyList<int> xs) => {|#0:xs.Count == 0|} || {|#1:xs.Any()|};
            }
            """;

        const string fixedSource = """
            class C
            {
                bool M(NonEmptyList<int> xs) => false || true;
            }
            """;

        await Verify.CodeFix<NonEmptyReceiverAnalyzer, RedundantCheckCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(0).WithArguments("xs.Count == 0", "false"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(1).WithArguments("Any()", "true")).RunAsync(TestContext.Current.CancellationToken);
    }
}
