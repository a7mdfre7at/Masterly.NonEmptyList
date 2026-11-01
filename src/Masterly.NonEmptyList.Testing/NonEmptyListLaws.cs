namespace Masterly.NonEmptyList.Testing;

/// <summary>
/// The laws every <see cref="NonEmptyList{T}"/> must obey. Each method returns <see langword="true"/> when the law holds
/// for the given inputs, so it can be plugged into CsCheck, FsCheck or a plain loop. <see cref="CheckAll{T}"/> runs every
/// law that needs no extra functions and returns the names of the ones that failed.
/// </summary>
public static class NonEmptyListLaws
{
    /// <summary>Functor identity: mapping the identity function changes nothing.</summary>
    public static bool MapIdentity<T>(NonEmptyList<T> xs) => xs.Map(static x => x).Equals(xs);

    /// <summary>Functor composition: mapping f then g equals mapping their composition.</summary>
    public static bool MapComposition<T, TU, TV>(NonEmptyList<T> xs, Func<T, TU> f, Func<TU, TV> g)
        => xs.Map(x => g(f(x))).Equals(xs.Map(f).Map(g));

    /// <summary>Monad left identity: a singleton flat-mapped with f equals f applied to the element.</summary>
    public static bool FlatMapLeftIdentity<T, TU>(T x, Func<T, NonEmptyList<TU>> f)
        => NonEmptyList<T>.Singleton(x).FlatMap(f).Equals(f(x));

    /// <summary>Monad right identity: flat-mapping with the singleton constructor changes nothing.</summary>
    public static bool FlatMapRightIdentity<T>(NonEmptyList<T> xs) => xs.FlatMap(NonEmptyList<T>.Singleton).Equals(xs);

    /// <summary>Monad associativity.</summary>
    public static bool FlatMapAssociativity<T, TU, TV>(NonEmptyList<T> xs, Func<T, NonEmptyList<TU>> f, Func<TU, NonEmptyList<TV>> g)
        => xs.FlatMap(f).FlatMap(g).Equals(xs.FlatMap(x => f(x).FlatMap(g)));

    /// <summary>Select agrees with Map.</summary>
    public static bool SelectMatchesMap<T, TU>(NonEmptyList<T> xs, Func<T, TU> f) => xs.Select(f).Equals(xs.Map(f));

    /// <summary>Reduce agrees with LINQ Aggregate.</summary>
    public static bool ReduceMatchesAggregate<T>(NonEmptyList<T> xs, Func<T, T, T> op)
        => EqualityComparer<T>.Default.Equals(xs.Reduce(op), xs.AsEnumerable().Aggregate(op));

    /// <summary>Fold agrees with seeded LINQ Aggregate.</summary>
    public static bool FoldMatchesAggregate<T, TAcc>(NonEmptyList<T> xs, TAcc seed, Func<TAcc, T, TAcc> folder)
        => EqualityComparer<TAcc>.Default.Equals(xs.Fold(seed, folder), xs.AsEnumerable().Aggregate(seed, folder));

    /// <summary>The last element of a seeded Scan equals the Fold with the same seed.</summary>
    public static bool ScanLastEqualsFold<T, TAcc>(NonEmptyList<T> xs, TAcc seed, Func<TAcc, T, TAcc> folder)
        => EqualityComparer<TAcc>.Default.Equals(xs.Scan(seed, folder).Last, xs.Fold(seed, folder));

    /// <summary>Head followed by Tail rebuilds the list.</summary>
    public static bool HeadTailRoundTrip<T>(NonEmptyList<T> xs)
        => xs.Tail is null ? xs.Count == 1 : xs.Tail.Prepend(xs.Head).Equals(xs);

    /// <summary>Init followed by Last rebuilds the list.</summary>
    public static bool InitLastRoundTrip<T>(NonEmptyList<T> xs)
        => xs.Init is null ? xs.Count == 1 : xs.Init.Append(xs.Last).Equals(xs);

    /// <summary>Reversing twice is the identity.</summary>
    public static bool ReverseTwiceIsIdentity<T>(NonEmptyList<T> xs) => xs.ReverseList().ReverseList().Equals(xs);

    /// <summary>Concat preserves the counts and the order.</summary>
    public static bool ConcatPreservesElements<T>(NonEmptyList<T> xs, NonEmptyList<T> ys)
    {
        NonEmptyList<T> zs = xs.Concat(ys);
        return zs.Count == xs.Count + ys.Count && zs.Take(xs.Count).SequenceEqual(xs) && zs.Skip(xs.Count).SequenceEqual(ys);
    }

    /// <summary>Zip then Unzip rebuilds both lists, truncated to the shorter length.</summary>
    public static bool ZipUnzipRoundTrip<T, TU>(NonEmptyList<T> xs, NonEmptyList<TU> ys)
    {
        (NonEmptyList<T> left, NonEmptyList<TU> right) = xs.Zip(ys).Unzip();
        int n = Math.Min(xs.Count, ys.Count);
        return left.SequenceEqual(xs.Take(n)) && right.SequenceEqual(ys.Take(n));
    }

    /// <summary>There are exactly Count suffixes, the first being the list itself and the last the singleton of Last.</summary>
    public static bool TailsShape<T>(NonEmptyList<T> xs)
    {
        NonEmptyList<NonEmptyList<T>> tails = xs.Tails();
        return tails.Count == xs.Count && tails.Head.Equals(xs) && tails.Last.Count == 1 && EqualityComparer<T>.Default.Equals(tails.Last.Head, xs.Last);
    }

    /// <summary>There are exactly Count prefixes, the first being the singleton of Head and the last the list itself.</summary>
    public static bool InitsShape<T>(NonEmptyList<T> xs)
    {
        NonEmptyList<NonEmptyList<T>> inits = xs.Inits();
        return inits.Count == xs.Count && inits.Last.Equals(xs) && inits.Head.Count == 1 && EqualityComparer<T>.Default.Equals(inits.Head.Head, xs.Head);
    }

    /// <summary>Sorting is stable with respect to the default comparer and keeps every element.</summary>
    public static bool OrderByIsSortedPermutation<T>(NonEmptyList<T> xs)
    {
        NonEmptyList<T> sorted = xs.Order();
        return sorted.Count == xs.Count
            && ConstrainedList<T, Sorted>.Satisfies(sorted)
            && sorted.OrderBy(static x => x).SequenceEqual(xs.AsEnumerable().OrderBy(static x => x));
    }

    /// <summary>Distinct keeps first occurrences, in order.</summary>
    public static bool DistinctKeepsFirstOccurrences<T>(NonEmptyList<T> xs)
        => xs.Distinct().SequenceEqual(xs.AsEnumerable().Distinct());

    /// <summary>Equality is structural and consistent with GetHashCode.</summary>
    public static bool EqualityIsStructural<T>(NonEmptyList<T> xs)
    {
        NonEmptyList<T> copy = NonEmptyList<T>.From(xs.ToArray());
        return xs.Equals(copy) && copy.Equals(xs) && xs.GetHashCode() == copy.GetHashCode();
    }

    /// <summary>Serializing to System.Text.Json and back yields an equal list.</summary>
    public static bool JsonRoundTrip<T>(NonEmptyList<T> xs)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(xs);
        NonEmptyList<T>? back = System.Text.Json.JsonSerializer.Deserialize<NonEmptyList<T>>(json);
        return back is not null && back.Equals(xs);
    }

    /// <summary>Runs every parameterless law and returns the names of those that fail. An empty result means all laws hold.</summary>
    public static IReadOnlyList<string> CheckAll<T>(NonEmptyList<T> xs)
    {
        ArgumentNullException.ThrowIfNull(xs);
        List<string> failures = new();

        void Check(string name, Func<bool> law)
        {
            if (!law())
                failures.Add(name);
        }

        Check(nameof(MapIdentity), () => MapIdentity(xs));
        Check(nameof(FlatMapRightIdentity), () => FlatMapRightIdentity(xs));
        Check(nameof(HeadTailRoundTrip), () => HeadTailRoundTrip(xs));
        Check(nameof(InitLastRoundTrip), () => InitLastRoundTrip(xs));
        Check(nameof(ReverseTwiceIsIdentity), () => ReverseTwiceIsIdentity(xs));
        Check(nameof(TailsShape), () => TailsShape(xs));
        Check(nameof(InitsShape), () => InitsShape(xs));
        Check(nameof(DistinctKeepsFirstOccurrences), () => DistinctKeepsFirstOccurrences(xs));
        Check(nameof(EqualityIsStructural), () => EqualityIsStructural(xs));
        Check(nameof(ConcatPreservesElements), () => ConcatPreservesElements(xs, xs));
        Check(nameof(ZipUnzipRoundTrip), () => ZipUnzipRoundTrip(xs, xs));
        Check(nameof(SelectMatchesMap), () => SelectMatchesMap(xs, static x => x));
        Check(nameof(ReduceMatchesAggregate), () => ReduceMatchesAggregate(xs, static (a, _) => a));
        Check(nameof(FoldMatchesAggregate), () => FoldMatchesAggregate(xs, 0, static (acc, _) => acc + 1));
        Check(nameof(ScanLastEqualsFold), () => ScanLastEqualsFold(xs, 0, static (acc, _) => acc + 1));
        Check(nameof(JsonRoundTrip), () => JsonRoundTrip(xs));

        return failures;
    }
}
