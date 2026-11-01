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

    [Fact]
    public async Task OtherProvablyEmptyShapes_ShouldReport()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var a = {|#0:string.Empty.ToNonEmptyList()|};
                    var b = {|#1:NonEmptyList<int>.FromSpan(new())|};
                    var c = {|#2:NonEmptyList.Create(new ReadOnlySpan<int>())|};
                    var d = {|#3:NonEmptyList<int>.FromSpan(ReadOnlySpan<int>.Empty)|};
                    var e = {|#4:NonEmptyDictionary.From(new Dictionary<int, string>())|};
                    var f = {|#5:EventStream.From(new Queue<int>())|};
                    var g = {|#6:ConstrainedList.From<int, Sorted>(new SortedSet<int>())|};
                    var h = {|#7:NonEmptyList<int>.From(new System.Collections.ObjectModel.Collection<int>())|};
                    var i = {|#8:new Stack<int>().ToNonEmptySet()|};
                    var j = {|#9:new LinkedList<int>().ToConstrainedList<int, Sorted>()|};
                    var k = {|#10:new System.Collections.ObjectModel.ObservableCollection<int>().ToNonEmptyList()|};
                    var l = {|#11:NonEmptyList<int>.FromSpan(new Span<int>())|};
                    var m = {|#12:new Dictionary<int, string>().ToNonEmptyDictionary()|};
                    var n = {|#13:NonEmptySet.Create<int>([])|};
                    var o = {|#14:NonEmptyList.From(System.Collections.Immutable.ImmutableList<int>.Empty)|};
                }
            }
            """;

        await Verify.Analyzer<EmptyFactoryArgumentAnalyzer>(source,
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(0).WithArguments("NonEmptyListLinqExtensions.ToNonEmptyList"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(1).WithArguments("NonEmptyList.FromSpan"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(2).WithArguments("NonEmptyList.Create"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(3).WithArguments("NonEmptyList.FromSpan"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(4).WithArguments("NonEmptyDictionary.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(5).WithArguments("EventStream.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(6).WithArguments("ConstrainedList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(7).WithArguments("NonEmptyList.From"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(8).WithArguments("NonEmptySet.ToNonEmptySet"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(9).WithArguments("ConstrainedListExtensions.ToConstrainedList"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(10).WithArguments("NonEmptyListLinqExtensions.ToNonEmptyList"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(11).WithArguments("NonEmptyList.FromSpan"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(12).WithArguments("NonEmptyDictionary.ToNonEmptyDictionary"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(13).WithArguments("NonEmptySet.Create"),
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(14).WithArguments("NonEmptyList.From")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnknownContentsAndNonRejectingCalls_ShouldNotReport()
    {
        const string source = """
            class Holder
            {
                public List<int> Items = new() { 1 };
                public static IEnumerable<T> Empty<T>() => new T[0];
            }

            class Tags : NonEmptyList<string>
            {
                public Tags(IEnumerable<string> items) : base(items) { }
            }

            class C
            {
                void M<T>(Holder holder, NonEmptySet<int> set, NonEmptyList<int> xs) where T : IEnumerable<int>, new()
                {
                    var a = NonEmptyList.From(new T());
                    var b = NonEmptyList.From(holder.Items);
                    var c = NonEmptyList.From(Holder.Empty<int>());
                    var d = NonEmptyList.From(Enumerable.Empty<int>().Append(1));
                    var e = NonEmptyEnumerable.Create(1, new List<int>());
                    var f = set.ToNonEmptyList();
                    var g = xs.Map(x => x + 1);
                    var h = new NonEmptyList<int>(1);
                    var i = new System.Collections.ObjectModel.Collection<int>(new List<int>());
                    var j = NonEmptyList<int>.TryFrom(new List<int>(), out var k);
                    var l = NonEmptyList.From(new int[holder.Items.Count]);
                    var m = new Tags(new List<string> { "a" });
                    var n = NonEmptyList.From(new List<int>(holder.Items));
                    var o = NonEmptyList<int>.FromSpan(new ReadOnlySpan<int>(new[] { 1 }));
                    var p = NonEmptyList.From("a");
                    var q = NonEmptyList<int>.FromSpan(new(new[] { 1 }));
                }
            }
            """;

        await Verify.Analyzer<EmptyFactoryArgumentAnalyzer>(source).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DerivedNonEmptyType_WithEmptyArgument_ShouldReport()
    {
        const string source = """
            class Tags : NonEmptyList<string>
            {
                public Tags(IEnumerable<string> items) : base(items) { }
            }

            class C
            {
                Tags M() => {|#0:new Tags(new List<string>())|};
            }
            """;

        await Verify.Analyzer<EmptyFactoryArgumentAnalyzer>(source,
            Verify.Diagnostic(Descriptors.EmptyFactoryArgument).WithLocation(0).WithArguments("new Tags")).RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LateBoundAndUnresolvedCalls_ShouldNotReport()
    {
        // Dynamic arguments defer overload resolution to run time and an unresolved name has no symbol: neither may crash
        // the analyzer or produce a diagnostic.
        const string source = """
            class C
            {
                void M(dynamic d)
                {
                    var a = NonEmptyList.From(d);
                    var b = new NonEmptyList<int>(d);
                    var c = NonEmptyList<int>.From({|CS0103:Missing|}());
                }
            }
            """;

        await Verify.Analyzer<EmptyFactoryArgumentAnalyzer>(source).RunAsync(TestContext.Current.CancellationToken);
    }
}
