namespace Masterly.NonEmptyList.Tests;

public partial class NonEmptyListTests
{
    #region MapAsync Tests

    [Fact]
    public async Task MapAsync_ShouldTransformAllElements()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        NonEmptyList<int> result = await list.MapAsync(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 2, 4, 6 }, result.ToArray());
    }

    [Fact]
    public async Task MapWithIndexAsync_ShouldIncludeIndex()
    {
        NonEmptyList<string> list = new("a", "b", "c");
        NonEmptyList<string> result = await list.MapWithIndexAsync(async (item, index) =>
        {
            await Task.Delay(1);
            return $"{index}:{item}";
        });

        Assert.Equal("0:a", result[0]);
        Assert.Equal("1:b", result[1]);
        Assert.Equal("2:c", result[2]);
    }

    #endregion

    #region MapParallelAsync Tests

    [Fact]
    public async Task MapParallelAsync_ShouldTransformInParallel()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        NonEmptyList<int> result = await list.MapParallelAsync(async x =>
        {
            await Task.Delay(10);
            return x * 10;
        });

        Assert.Equal(5, result.Count);
        Assert.Equal(new[] { 10, 20, 30, 40, 50 }, result.ToArray());
    }

    [Fact]
    public async Task MapParallelAsync_WithDegreeOfParallelism_ShouldLimit()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        int concurrent = 0;
        int maxConcurrent = 0;
        object lockObj = new();

        NonEmptyList<int> result = await list.MapParallelAsync(async x =>
        {
            lock (lockObj)
            {
                concurrent++;
                if (concurrent > maxConcurrent)
                    maxConcurrent = concurrent;
            }

            await Task.Delay(50);

            lock (lockObj) { concurrent--; }

            return x * 10;
        }, maxDegreeOfParallelism: 2);

        Assert.Equal(5, result.Count);
        Assert.True(maxConcurrent <= 2, $"Max concurrent was {maxConcurrent}, expected <= 2");
    }

    #endregion

    #region ForEachAsync Tests

    [Fact]
    public async Task ForEachAsync_ShouldExecuteForAllElements()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        List<int> results = new();

        await list.ForEachAsync(async x =>
        {
            await Task.Delay(1);
            lock (results) { results.Add(x); }
        });

        Assert.Equal(3, results.Count);
        Assert.Contains(1, results);
        Assert.Contains(2, results);
        Assert.Contains(3, results);
    }

    [Fact]
    public async Task ForEachWithIndexAsync_ShouldProvideIndices()
    {
        NonEmptyList<string> list = new("a", "b", "c");
        List<(string, int)> results = new();

        await list.ForEachWithIndexAsync(async (item, index) =>
        {
            await Task.Delay(1);
            lock (results) { results.Add((item, index)); }
        });

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ForEachParallelAsync_ShouldExecuteInParallel()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5);
        List<int> results = new();

        await list.ForEachParallelAsync(async x =>
        {
            await Task.Delay(10);
            lock (results) { results.Add(x); }
        });

        Assert.Equal(5, results.Count);
    }

    #endregion

    #region FilterAsync Tests

    [Fact]
    public async Task FilterAsync_ShouldReturnMatchingElements()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4, 5, 6);
        NonEmptyList<int>? result = await list.FilterAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0;
        });

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal(new[] { 2, 4, 6 }, result.ToArray());
    }

    [Fact]
    public async Task FilterAsync_WithNoMatches_ShouldReturnNull()
    {
        NonEmptyList<int> list = new(1, 3, 5);
        NonEmptyList<int>? result = await list.FilterAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0;
        });

        Assert.Null(result);
    }

    #endregion

    #region FoldAsync Tests

    [Fact]
    public async Task FoldAsync_ShouldAccumulateWithSeed()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        int result = await list.FoldAsync(10, async (acc, x) =>
        {
            await Task.Delay(1);
            return acc + x;
        });

        Assert.Equal(16, result);
    }

    #endregion

    #region AllAsync/AnyAsync Tests

    [Fact]
    public async Task AllAsync_WhenAllMatch_ShouldReturnTrue()
    {
        NonEmptyList<int> list = new(2, 4, 6);
        bool result = await list.AllAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0;
        });

        Assert.True(result);
    }

    [Fact]
    public async Task AllAsync_WhenNotAllMatch_ShouldReturnFalse()
    {
        NonEmptyList<int> list = new(2, 3, 6);
        bool result = await list.AllAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0;
        });

        Assert.False(result);
    }

    [Fact]
    public async Task AnyAsync_WhenAnyMatch_ShouldReturnTrue()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        bool result = await list.AnyAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0;
        });

        Assert.True(result);
    }

    [Fact]
    public async Task AnyAsync_WhenNoneMatch_ShouldReturnFalse()
    {
        NonEmptyList<int> list = new(1, 3, 5);
        bool result = await list.AnyAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0;
        });

        Assert.False(result);
    }

    #endregion

    #region FirstOrDefaultAsync Tests

    [Fact]
    public async Task FirstOrDefaultAsync_WithMatch_ShouldReturnElement()
    {
        NonEmptyList<int> list = new(1, 2, 3, 4);
        int result = await list.FirstOrDefaultAsync(async x =>
        {
            await Task.Delay(1);
            return x > 2;
        });

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithNoMatch_ShouldReturnDefault()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        int result = await list.FirstOrDefaultAsync(async x =>
        {
            await Task.Delay(1);
            return x > 10;
        });

        Assert.Equal(0, result);
    }

    #endregion

    #region ToAsyncEnumerable Tests

    [Fact]
    public async Task ToAsyncEnumerable_ShouldYieldAllElements()
    {
        NonEmptyList<int> list = new(1, 2, 3);
        List<int> results = new();

        await foreach (int item in list.ToAsyncEnumerable())
        {
            results.Add(item);
        }

        Assert.Equal(new[] { 1, 2, 3 }, results);
    }

    #endregion
}
