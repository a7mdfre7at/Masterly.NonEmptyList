namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Guards in internal "trusted" helpers and "unreachable" branches that are in fact reachable through the public API.
/// </summary>
public class TrustedPathReachabilityTests
{
    #region Null elements reaching the trusted factories

    [Fact]
    public void Map_WithSelectorReturningNull_ShouldRejectTheNullElement()
    {
        NonEmptyList<int> list = NonEmptyList.Of(1, 2);

        ArgumentNullException mapped = Assert.Throws<ArgumentNullException>(() => list.Map<string>(x => x == 2 ? null! : "a"));
        Assert.StartsWith("NonEmptyList cannot contain null values", mapped.Message);

        ArgumentNullException converted = Assert.Throws<ArgumentNullException>(() => list.ConvertAll<string>(_ => null!));
        Assert.StartsWith("NonEmptyList cannot contain null values", converted.Message);

        Assert.Equal(new[] { 1, 2 }, list);
    }

    [Fact]
    public void NonEmptyEnumerable_ToNonEmptyList_WithNullInTheRest_ShouldRejectIt()
    {
        NonEmptyEnumerable<string> lazy = new("a", new string[] { null! });

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => lazy.ToNonEmptyList());
        Assert.StartsWith("NonEmptyList cannot contain null values", ex.Message);
    }

    [Fact]
    public void ImmutableCreate_FromSpanContainingNull_ShouldRejectIt()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => ImmutableNonEmptyList.Create<string>(new string[] { null! }));
        Assert.StartsWith("ImmutableNonEmptyList cannot contain null values", ex.Message);
    }

    #endregion

    #region Emptying a non-empty set or dictionary

    [Fact]
    public void NonEmptySet_RemoveWhere_WithPredicateThatChangesItsAnswer_ShouldCallItOncePerElement()
    {
        NonEmptySet<int> set = new(42);
        int calls = 0;

        // Answers "no" on the first call and "yes" afterwards, so a check-then-remove that calls it twice is fooled.
        int removed = set.RemoveWhere(_ => calls++ >= 1);

        Assert.Equal(0, removed);
        Assert.Equal(1, calls);
        Assert.Equal(42, set.Any());
        Assert.Equal(new[] { 42 }, set.ToNonEmptyList());
        Assert.Equal(new[] { 42 }, set.ToSortedNonEmptyList());
    }

    [Fact]
    public void NonEmptySet_RemoveWhere_WithCountingPredicate_ShouldRemoveExactlyWhatItSelected()
    {
        NonEmptySet<int> set = new(1, 2, 3);
        int taken = 0;

        int removed = set.RemoveWhere(_ => taken++ < 2);

        Assert.Equal(2, removed);
        Assert.Single(set);
        Assert.Equal(3, taken);
    }

    private sealed class ReentrantComparer : IEqualityComparer<string>
    {
        private int _seen;

        public Action? Hook { get; set; }

        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        public int GetHashCode(string s)
        {
            // The first hash of "a" after the hook is armed is NonEmptyDictionary.Remove's ContainsKey check;
            // the second is inside the inner Dictionary.Remove, after the "would empty" check has passed.
            if (s == "a" && Hook is not null && _seen++ == 1)
            {
                Action hook = Hook;
                Hook = null;
                hook();
            }

            return StringComparer.Ordinal.GetHashCode(s);
        }
    }

    [Fact]
    public void NonEmptyDictionary_Remove_WithReentrantComparer_ShouldRefuseAndRestoreTheEntry()
    {
        ReentrantComparer comparer = new();
        NonEmptyDictionary<string, int> dictionary = NonEmptyDictionary<string, int>.From(
            new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2) }, comparer);
        comparer.Hook = () => dictionary.Remove("b");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => dictionary.Remove("a"));

        Assert.Equal("Cannot remove the last entry of a NonEmptyDictionary", ex.Message);
        Assert.Equal(new[] { new KeyValuePair<string, int>("a", 1) }, dictionary);
        Assert.Equal("a", dictionary.Any().Key);
        Assert.Equal(new[] { "a" }, dictionary.KeyList());
    }

    [Fact]
    public void NonEmptyDictionary_RemoveWithValue_WithReentrantComparer_ShouldRefuseAndRestoreTheEntry()
    {
        ReentrantComparer comparer = new();
        NonEmptyDictionary<string, int> dictionary = NonEmptyDictionary<string, int>.From(
            new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2) }, comparer);
        comparer.Hook = () => dictionary.Remove("b");

        Assert.Throws<InvalidOperationException>(() => dictionary.Remove("a", out _));

        Assert.Equal(new[] { new KeyValuePair<string, int>("a", 1) }, dictionary);
    }

    #endregion
}
