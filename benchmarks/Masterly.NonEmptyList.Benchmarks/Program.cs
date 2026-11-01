using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Masterly.NonEmptyList;

BenchmarkSwitcher.FromAssembly(typeof(CreationBenchmarks).Assembly).Run(args);

/// <summary>How much does the non-empty wrapper cost to build compared with the collections people use today?</summary>
[MemoryDiagnoser]
public class CreationBenchmarks
{
    [Params(1, 4, 100)]
    public int Count { get; set; }

    private int[] _source = Array.Empty<int>();

    [GlobalSetup]
    public void Setup() => _source = Enumerable.Range(1, Count).ToArray();

    [Benchmark(Baseline = true)]
    public List<int> List() => new(_source);

    [Benchmark]
    public NonEmptyList<int> NonEmptyList_From() => NonEmptyList<int>.From(_source);

    [Benchmark]
    public NonEmptyList<int> NonEmptyList_FromSpan() => NonEmptyList<int>.FromSpan(_source);

    [Benchmark]
    public NonEmptyList<int> NonEmptyList_CollectionExpression() => [.. _source];

    [Benchmark]
    public ImmutableNonEmptyList<int> ImmutableNonEmptyList_From() => ImmutableNonEmptyList<int>.From(_source);

    [Benchmark]
    public ImmutableList<int> ImmutableList() => System.Collections.Immutable.ImmutableList.Create(_source);

    [Benchmark]
    public ImmutableArray<int> ImmutableArray() => System.Collections.Immutable.ImmutableArray.Create(_source);
}

/// <summary>Reading the ends. The whole point of the type is that these never throw; they should not cost anything either.</summary>
[MemoryDiagnoser]
public class AccessBenchmarks
{
    private readonly List<int> _list = Enumerable.Range(1, 100).ToList();
    private readonly NonEmptyList<int> _nonEmpty = NonEmptyList<int>.From(Enumerable.Range(1, 100));
    private readonly ImmutableNonEmptyList<int> _immutable = ImmutableNonEmptyList<int>.From(Enumerable.Range(1, 100));

    [Benchmark(Baseline = true)]
    public int List_FirstLast() => _list[0] + _list[^1];

    [Benchmark]
    public int List_LinqFirstLast() => _list.First() + _list.Last();

    [Benchmark]
    public int NonEmptyList_HeadLast() => _nonEmpty.Head + _nonEmpty.Last;

    [Benchmark]
    public int ImmutableNonEmptyList_HeadLast() => _immutable.Head + _immutable.Last;

    [Benchmark]
    public NonEmptyList<int>? NonEmptyList_Tail() => _nonEmpty.Tail;

    [Benchmark]
    public int NonEmptySpan_Sum() => _nonEmpty.AsNonEmptySpan().Sum();

    [Benchmark]
    public int Enumerable_Sum() => Enumerable.Sum(_list);

    [Benchmark]
    public int NonEmptyList_Sum() => _nonEmpty.Sum();
}

/// <summary>Type-preserving LINQ versus the Enumerable operators it shadows.</summary>
[MemoryDiagnoser]
public class LinqBenchmarks
{
    private readonly List<int> _list = Enumerable.Range(1, 1000).ToList();
    private readonly NonEmptyList<int> _nonEmpty = NonEmptyList<int>.From(Enumerable.Range(1, 1000));

    [Benchmark(Baseline = true)]
    public List<int> Enumerable_Select_ToList() => Enumerable.ToList(Enumerable.Select(_list, x => x * 2));

    [Benchmark]
    public NonEmptyList<int> NonEmptyList_Select() => _nonEmpty.Select(x => x * 2);

    [Benchmark]
    public NonEmptyList<int> NonEmptyList_Map() => _nonEmpty.Map(x => x * 2);

    [Benchmark]
    public int Enumerable_Max() => Enumerable.Max(_list);

    [Benchmark]
    public int NonEmptyList_Max() => _nonEmpty.Max();

    [Benchmark]
    public List<int> Enumerable_OrderBy_ToList() => Enumerable.ToList(Enumerable.OrderByDescending(_list, x => x));

    [Benchmark]
    public NonEmptyList<int> NonEmptyList_OrderBy() => _nonEmpty.OrderByDescending(x => x);

    [Benchmark]
    public int Enumerable_Aggregate() => Enumerable.Aggregate(_list, (a, b) => a + b);

    [Benchmark]
    public int NonEmptyList_Reduce() => _nonEmpty.Reduce((a, b) => a + b);

    [Benchmark]
    public double Enumerable_Average() => Enumerable.Average(_list);

    [Benchmark]
    public double NonEmptyList_Average() => _nonEmpty.Average();
}

/// <summary>Mutation through the guarded interface implementations.</summary>
[MemoryDiagnoser]
public class MutationBenchmarks
{
    private List<int> _list = new();
    private NonEmptyList<int> _nonEmpty = NonEmptyList.Of(0);

    [IterationSetup]
    public void Setup()
    {
        _list = new List<int> { 0 };
        _nonEmpty = NonEmptyList.Of(0);
    }

    [Benchmark(Baseline = true)]
    public int List_Add1000()
    {
        for (int i = 0; i < 1000; i++)
            _list.Add(i);
        return _list.Count;
    }

    [Benchmark]
    public int NonEmptyList_Add1000()
    {
        for (int i = 0; i < 1000; i++)
            _nonEmpty.Add(i);
        return _nonEmpty.Count;
    }

    [Benchmark]
    public int IListOfT_Add1000()
    {
        IList<int> view = _nonEmpty;
        for (int i = 0; i < 1000; i++)
            view.Add(i);
        return view.Count;
    }
}
