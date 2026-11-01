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

    [Fact]
    public async Task Count_ShouldOnlyBeReported_WhereTheReceiverHasACountProperty()
    {
        // NonEmptyEnumerable<T> is lazy and has no Count property, so suggesting 'Count' would not compile.
        const string source = """
            class C
            {
                void M(NonEmptyEnumerable<int> lazy, NonEmptySet<int> set)
                {
                    var a = lazy.Count();
                    var b = lazy.LongCount();
                    var c = {|#0:set.Count()|};
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source,
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(0).WithArguments("Count()", "Count")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MirroredAndLongComparisons_ShouldReport()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs)
                {
                    var a = {|#0:0 == xs.Count|};
                    var b = {|#1:0 >= xs.Count|};
                    var c = {|#2:1 <= xs.Count|};
                    var d = {|#3:0 != xs.Count|};
                    var e = {|#4:{|#5:xs.LongCount()|} > 0L|};
                    var f = {|#6:xs.Count > -3|};
                    const int none = 0;
                    var g = {|#7:xs.Count == none|};
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source,
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(0).WithArguments("0 == xs.Count", "false"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(1).WithArguments("0 >= xs.Count", "false"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(2).WithArguments("1 <= xs.Count", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(3).WithArguments("0 != xs.Count", "true"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(5).WithArguments("LongCount()", "Count"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(4).WithArguments("xs.LongCount() > 0L", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(6).WithArguments("xs.Count > -3", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(7).WithArguments("xs.Count == none", "false")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UndecidableComparisons_ShouldNotReport()
    {
        const string source = """
            class C
            {
                void M(NonEmptyList<int> xs, List<int> list, int n)
                {
                    var a = xs.Count == n;
                    var b = n < xs.Count;
                    var c = {|#0:xs.LongCount()|} > 3_000_000_000L;
                    var d = xs.Count == 0.0;
                    var e = xs.Count <= 1;
                    var f = 2 > xs.Count;
                    var g = list.Count == xs.Count;
                    var h = xs.Head == 0;
                    var i = xs.Count == -1;
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source,
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(0).WithArguments("LongCount()", "Count")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ListOnlyReplacements_ShouldNotBeOfferedOnSetsOrLazySequences()
    {
        // A set has no Head, Last or Reduce, so First(), Last() and Aggregate() stay; LINQ emptiness checks are still decided.
        const string source = """
            class C
            {
                void M(NonEmptySet<int> set, NonEmptyEnumerable<int> lazy)
                {
                    var a = set.First();
                    var b = set.Last();
                    var c = set.FirstOrDefault();
                    var d = set.LastOrDefault();
                    var e = set.Aggregate((x, y) => x + y);
                    var f = lazy.First();
                    var g = set.Any(); // NonEmptySet<T>.Any() picks an element; it is not the LINQ operator.
                    var h = {|#1:lazy.Any()|};
                    var i = {|#2:lazy.DefaultIfEmpty(5)|};
                    var j = set.Any(x => x > 1);
                    var k = {|#3:set.DefaultIfEmpty()|}.ToList();
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source,
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(1).WithArguments("Any()", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(2).WithArguments("DefaultIfEmpty()", "the list itself"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(3).WithArguments("DefaultIfEmpty()", "the list itself")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EveryListLikeType_AndDerivedTypes_ShouldReport()
    {
        const string source = """
            class Tags : NonEmptyList<string>
            {
                public Tags(string first) : base(first) { }
            }

            class C
            {
                void M(ConstrainedList<int, Sorted> sorted, OrderedNonEmptyList<int> ordered, NonEmptyGrouping<string, int> group, Tags tags)
                {
                    var a = {|#0:sorted.First()|};
                    var b = {|#1:ordered.Last()|};
                    var c = {|#2:group.Aggregate((x, y) => x + y)|};
                    var d = {|#3:tags.Count()|};
                    var e = {|#4:tags.Count == 0|};
                    var f = {|#5:tags.FirstOrDefault()|};
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source,
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(0).WithArguments("First()", "Head"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(1).WithArguments("Last()", "Last"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(2).WithArguments("Aggregate()", "Reduce"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(3).WithArguments("Count()", "Count"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(4).WithArguments("tags.Count == 0", "false"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(5).WithArguments("FirstOrDefault()", "Head")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OverloadsWithArguments_NonLinqAndLateBoundCalls_ShouldNotReport()
    {
        const string source = """
            class C
            {
                int Local() => 1;

                void M(NonEmptyList<int> xs, dynamic d, Func<int> f)
                {
                    var a = Local();
                    var b = f();
                    var c = d.First();
                    var e = xs.Map(x => x + 1);
                    var g = xs.Reduce((x, y) => x + y);
                    var h = Enumerable.First(xs);
                    var i = xs.FirstOrDefault(5);
                    var j = xs.LastOrDefault(5);
                    var k = xs.Last(x => x > 1);
                    var l = xs.Count(x => x > 1);
                    var m = xs.LongCount(x => x > 1);
                    var n = xs.Aggregate(0, (acc, x) => acc + x, acc => acc * 2);
                    var o = xs.Any(x => x > 1);
                }
            }
            """;

        await Verify.Analyzer<NonEmptyReceiverAnalyzer>(source).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldReplaceCountAndLastWithProperties_PreservingTrivia()
    {
        const string source = """
            class C
            {
                int M(NonEmptyList<int> xs) => /* a */ {|#0:xs.Count()|} /* b */ + {|#1:xs.Last()|};
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(NonEmptyList<int> xs) => /* a */ xs.Count /* b */ + xs.Last;
            }
            """;

        await Verify.CodeFix<NonEmptyReceiverAnalyzer, TotalMemberCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(0).WithArguments("Count()", "Count"),
            Verify.Diagnostic(Descriptors.ThrowingLinqOnNonEmpty).WithLocation(1).WithArguments("Last()", "Last")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldNotOfferAConstantForDefaultIfEmpty()
    {
        // "the list itself" is not a boolean, so there is no literal to substitute.
        const string source = """
            class C
            {
                IEnumerable<int> M(NonEmptyList<int> xs) => {|#0:xs.DefaultIfEmpty()|};
            }
            """;

        await Verify.CodeFix<NonEmptyReceiverAnalyzer, RedundantCheckCodeFixProvider>(source, source,
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(0).WithArguments("DefaultIfEmpty()", "the list itself")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CodeFix_ShouldFixAllRedundantChecksInDocument()
    {
        const string source = """
            class C
            {
                bool M(NonEmptyList<int> xs) => {|#0:xs.Count > 0|} && {|#1:0 < xs.Count|} && !{|#2:xs.Any()|};
            }
            """;

        const string fixedSource = """
            class C
            {
                bool M(NonEmptyList<int> xs) => true && true && !true;
            }
            """;

        await Verify.CodeFix<NonEmptyReceiverAnalyzer, RedundantCheckCodeFixProvider>(source, fixedSource,
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(0).WithArguments("xs.Count > 0", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(1).WithArguments("0 < xs.Count", "true"),
            Verify.Diagnostic(Descriptors.RedundantEmptinessCheck).WithLocation(2).WithArguments("Any()", "true")).RunAsync(TestContext.Current.CancellationToken);
    }
}
