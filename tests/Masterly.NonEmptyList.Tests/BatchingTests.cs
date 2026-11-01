#pragma warning disable xUnit1051 // the streams under test own their cancellation; TestContext tokens would change what is being tested
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Masterly.NonEmptyList.Tests;

public class BatchingTests
{
    private static async IAsyncEnumerable<int> Stream(int count, int delayMs = 0, [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 1; i <= count; i++)
        {
            if (delayMs > 0)
                await Task.Delay(delayMs, ct);
            yield return i;
        }
    }

    [Fact]
    public void BatchNonEmpty_Sync_ShouldNeverYieldEmptyBatches()
    {
        List<NonEmptyList<int>> batches = Enumerable.Range(1, 7).BatchNonEmpty(3).ToList();

        Assert.Equal(3, batches.Count);
        Assert.Equal(new[] { 1, 2, 3 }, batches[0].ToArray());
        Assert.Equal(new[] { 7 }, batches[2].ToArray());
        Assert.Empty(Enumerable.Empty<int>().BatchNonEmpty(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1, 3).BatchNonEmpty(0));
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<int>)null!).BatchNonEmpty(1));
    }

    [Fact]
    public async Task BatchNonEmpty_Async_ShouldBatchBySize()
    {
        List<NonEmptyList<int>> batches = new();
        await foreach (NonEmptyList<int> batch in Stream(5).BatchNonEmpty(2))
            batches.Add(batch);

        Assert.Equal(3, batches.Count);
        Assert.Equal(new[] { 5 }, batches[2].ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => Stream(1).BatchNonEmpty(0));
        Assert.Throws<ArgumentNullException>(() => ((IAsyncEnumerable<int>)null!).BatchNonEmpty(1));

        List<NonEmptyList<int>> none = new();
        await foreach (NonEmptyList<int> batch in Stream(0).BatchNonEmpty(2))
            none.Add(batch);
        Assert.Empty(none);
    }

    [Fact]
    public async Task BatchNonEmpty_WithMaxWait_ShouldFlushOnTimeAndOnSize()
    {
        // Items 1..3 arrive immediately, then a long gap, then 4..5.
        async IAsyncEnumerable<int> Source()
        {
            yield return 1;
            yield return 2;
            yield return 3;
            await Task.Delay(400);
            yield return 4;
            yield return 5;
        }

        List<NonEmptyList<int>> batches = new();
        await foreach (NonEmptyList<int> batch in Source().BatchNonEmpty(10, TimeSpan.FromMilliseconds(100)))
            batches.Add(batch);

        Assert.Equal(2, batches.Count);
        Assert.Equal(new[] { 1, 2, 3 }, batches[0].ToArray());
        Assert.Equal(new[] { 4, 5 }, batches[1].ToArray());
        Assert.All(batches, b => Assert.NotEmpty(b));
    }

    [Fact]
    public async Task BatchNonEmpty_WithMaxWait_ShouldStillHonourSize()
    {
        List<NonEmptyList<int>> batches = new();
        await foreach (NonEmptyList<int> batch in Stream(5).BatchNonEmpty(2, TimeSpan.FromSeconds(10)))
            batches.Add(batch);

        Assert.Equal(3, batches.Count);
        Assert.Equal(new[] { 1, 2 }, batches[0].ToArray());
        Assert.Equal(new[] { 5 }, batches[2].ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => Stream(1).BatchNonEmpty(0, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Stream(1).BatchNonEmpty(1, TimeSpan.Zero));
    }

    [Fact]
    public async Task BatchNonEmpty_WithMaxWait_ShouldRespectCancellation()
    {
        using CancellationTokenSource cts = new();
        async IAsyncEnumerable<int> Slow([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return 1;
            await Task.Delay(Timeout.Infinite, ct);
            yield return 2;
        }

        cts.CancelAfter(200);
        List<NonEmptyList<int>> batches = new();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NonEmptyList<int> batch in Slow().BatchNonEmpty(10, TimeSpan.FromMilliseconds(50), cts.Token))
                batches.Add(batch);
        });

        Assert.Single(batches);
        Assert.Equal(1, batches[0].Head);
    }

    [Fact]
    public async Task ReadAllBatchesAsync_ShouldDrainChannelInNonEmptyBatches()
    {
        Channel<int> channel = Channel.CreateUnbounded<int>();
        for (int i = 1; i <= 5; i++)
            channel.Writer.TryWrite(i);
        channel.Writer.Complete();

        List<NonEmptyList<int>> batches = new();
        await foreach (NonEmptyList<int> batch in channel.Reader.ReadAllBatchesAsync(maxSize: 2))
            batches.Add(batch);

        Assert.Equal(3, batches.Count);
        Assert.Equal(new[] { 1, 2 }, batches[0].ToArray());
        Assert.Equal(new[] { 5 }, batches[2].ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => channel.Reader.ReadAllBatchesAsync(0));
        Assert.Throws<ArgumentNullException>(() => ((ChannelReader<int>)null!).ReadAllBatchesAsync());

        Channel<int> all = Channel.CreateUnbounded<int>();
        all.Writer.TryWrite(1);
        all.Writer.TryWrite(2);
        all.Writer.Complete();
        List<NonEmptyList<int>> single = new();
        await foreach (NonEmptyList<int> batch in all.Reader.ReadAllBatchesAsync())
            single.Add(batch);
        Assert.Single(single);
        Assert.Equal(2, single[0].Count);
    }

    [Fact]
    public async Task ToNonEmptyAsync_ShouldReturnNullForEmptyAndStreamRest()
    {
        NonEmptyAsyncEnumerable<int>? empty = await Stream(0).ToNonEmptyAsync();
        NonEmptyAsyncEnumerable<int>? rows = await Stream(3).ToNonEmptyAsync();

        Assert.Null(empty);
        Assert.NotNull(rows);
        Assert.Equal(1, rows!.Head);

        List<int> rest = new();
        await foreach (int item in rows.Rest)
            rest.Add(item);
        Assert.Equal(new[] { 2, 3 }, rest);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (int _ in rows.Rest)
            {
            }
        });
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_ShouldEnumerateHeadThenRestAndBuffer()
    {
        NonEmptyAsyncEnumerable<int> rows = (await Stream(3).ToNonEmptyAsync())!;
        List<int> all = new();
        await foreach (int item in rows)
            all.Add(item);
        Assert.Equal(new[] { 1, 2, 3 }, all);

        NonEmptyAsyncEnumerable<int> again = (await Stream(3).ToNonEmptyAsync())!;
        NonEmptyList<int> buffered = await again.ToNonEmptyListAsync();
        Assert.Equal(new[] { 1, 2, 3 }, buffered.ToArray());

        NonEmptyAsyncEnumerable<string> mapped = (await Stream(2).ToNonEmptyAsync())!.Select(x => $"#{x}");
        Assert.Equal("#1", mapped.Head);
        Assert.Equal(new[] { "#1", "#2" }, (await mapped.ToNonEmptyListAsync()).ToArray());

        NonEmptyAsyncEnumerable<int> batched = (await Stream(5).ToNonEmptyAsync())!;
        List<NonEmptyList<int>> restBatches = new();
        await foreach (NonEmptyList<int> batch in batched.BatchRest(3))
            restBatches.Add(batch);
        Assert.Equal(2, restBatches.Count);
        Assert.Equal(new[] { 2, 3, 4 }, restBatches[0].ToArray());
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_PublicCtorAndDispose_ShouldWork()
    {
        NonEmptyAsyncEnumerable<int> seq = new(0, Stream(2));
        Assert.Equal(new[] { 0, 1, 2 }, (await seq.ToNonEmptyListAsync()).ToArray());
        await seq.DisposeAsync();
        await seq.DisposeAsync();

        NonEmptyAsyncEnumerable<int> disposedEarly = (await Stream(3).ToNonEmptyAsync())!;
        await disposedEarly.DisposeAsync();
        Assert.Equal(new[] { 1 }, (await disposedEarly.ToNonEmptyListAsync()).ToArray());

        Assert.Throws<ArgumentNullException>(() => new NonEmptyAsyncEnumerable<string>(null!, EmptyAsync.Empty<string>()));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyAsyncEnumerable<int>(1, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ((IAsyncEnumerable<int>)null!).ToNonEmptyAsync());
    }

    [Fact]
    public async Task ToNonEmptyAsync_ShouldRejectNullHeadAndPropagateErrors()
    {
        static async IAsyncEnumerable<string?> NullFirst()
        {
            await Task.Yield();
            yield return null;
        }

        static async IAsyncEnumerable<int> Failing()
        {
            await Task.Yield();
            throw new InvalidDataException("boom");
#pragma warning disable CS0162
            yield return 1;
#pragma warning restore CS0162
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await NullFirst().ToNonEmptyAsync());
        await Assert.ThrowsAsync<InvalidDataException>(async () => await Failing().ToNonEmptyAsync());
    }

    // A source that never looks at its cancellation token, so only the operator's own checks can stop it.
    private static async IAsyncEnumerable<int> IgnoresToken(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    [Fact]
    public async Task BatchNonEmpty_SizeOnly_ShouldStop_WhenTheSourceIgnoresTheToken()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NonEmptyList<int> _ in IgnoresToken(10).BatchNonEmpty(2, cts.Token))
            {
            }
        });
    }

    [Fact]
    public async Task BatchNonEmpty_SizeOnly_ShouldStopBetweenElements_WhenCancelledMidStream()
    {
        using CancellationTokenSource cts = new();
        List<NonEmptyList<int>> batches = [];

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NonEmptyList<int> batch in IgnoresToken(10).BatchNonEmpty(2, cts.Token))
            {
                batches.Add(batch);
                cts.Cancel();
            }
        });

        Assert.Single(batches);
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_ToNonEmptyListAsync_ShouldHonourItsToken()
    {
        NonEmptyAsyncEnumerable<int> stream = (await IgnoresToken(5).ToNonEmptyAsync())!;
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await stream.ToNonEmptyListAsync(cts.Token));
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_BatchRest_ShouldHonourItsToken()
    {
        NonEmptyAsyncEnumerable<int> stream = (await IgnoresToken(5).ToNonEmptyAsync())!;
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NonEmptyList<int> _ in stream.BatchRest(2, cts.Token))
            {
            }
        });
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_RestWithCancellation_ShouldStopBetweenElements()
    {
        NonEmptyAsyncEnumerable<int> stream = (await IgnoresToken(5).ToNonEmptyAsync())!;
        using CancellationTokenSource cts = new();
        List<int> seen = [];

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (int item in stream.Rest.WithCancellation(cts.Token))
            {
                seen.Add(item);
                cts.Cancel();
            }
        });

        Assert.Equal([2], seen);
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_PublicConstructor_ShouldPassTheTokenToTheRest()
    {
        CancellationToken received = default;
        async IAsyncEnumerable<int> Rest([EnumeratorCancellation] CancellationToken ct = default)
        {
            received = ct;
            await Task.Yield();
            yield return 2;
        }

        using CancellationTokenSource cts = new();
        NonEmptyAsyncEnumerable<int> stream = new(1, Rest());

        NonEmptyList<int> all = await stream.ToNonEmptyListAsync(cts.Token);

        Assert.Equal([1, 2], all);
        Assert.Equal(cts.Token, received);
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_Select_ShouldPassTheTokenThrough()
    {
        NonEmptyAsyncEnumerable<int> stream = (await IgnoresToken(5).ToNonEmptyAsync())!;
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await stream.Select(x => x * 10).ToNonEmptyListAsync(cts.Token));
    }
}

file static class EmptyAsync
{
    public static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}
