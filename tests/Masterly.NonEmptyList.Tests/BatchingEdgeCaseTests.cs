using System.Threading.Channels;

namespace Masterly.NonEmptyList.Tests;

/// <summary>
/// Early exit and expired-window paths of the async batching operators.
/// </summary>
public class BatchingEdgeCaseTests
{
    private sealed class Box
    {
        public int Pulled;
        public bool Disposed;
    }

    /// <summary>Records how far the source was pulled and whether its enumerator was disposed.</summary>
    private static async IAsyncEnumerable<int> Tracked(Box state)
    {
        try
        {
            for (int i = 1; i <= 5; i++)
            {
                await Task.Yield();
                state.Pulled = i;
                yield return i;
            }
        }
        finally
        {
            state.Disposed = true;
        }
    }

    [Fact]
    public async Task TimedBatch_WhenWindowExpiresWhileSourceBlocks_ShouldEmitThePendingBatchFirst()
    {
        // The source blocks synchronously for longer than the window between items, so when the operator
        // checks the clock the window has already closed and the first batch must be flushed on its own.
        static async IAsyncEnumerable<int> Source()
        {
            yield return 1;
            Thread.Sleep(100);
            yield return 2;
            await Task.CompletedTask;
        }

        List<int[]> batches = new();
        await foreach (NonEmptyList<int> batch in Source().BatchNonEmpty(10, TimeSpan.FromMilliseconds(10)))
            batches.Add(batch.ToArray());

        Assert.Equal(new[] { new[] { 1 }, new[] { 2 } }, batches);
    }

    [Fact]
    public async Task AsyncBatchOperators_WhenConsumerStopsEarly_ShouldYieldOnlyWhatWasRead()
    {
        Box sizedState = new();
        NonEmptyList<int>? sized = null;
        await foreach (NonEmptyList<int> batch in Tracked(sizedState).BatchNonEmpty(2))
        {
            sized = batch;
            break;
        }

        Box timedState = new();
        NonEmptyList<int>? timed = null;
        await foreach (NonEmptyList<int> batch in Tracked(timedState).BatchNonEmpty(2, TimeSpan.FromSeconds(10)))
        {
            timed = batch;
            break;
        }

        Assert.Equal(new[] { 1, 2 }, sized);
        Assert.Equal(new[] { 1, 2 }, timed);

        // No read-ahead past the yielded batch, and the source enumerator is disposed on early exit.
        Assert.Equal(2, sizedState.Pulled);
        Assert.True(sizedState.Disposed);
        Assert.Equal(2, timedState.Pulled);
        Assert.True(timedState.Disposed);
    }

    [Fact]
    public async Task SizedAsyncBatch_WithAlreadyCancelledToken_ShouldThrowBeforeReadingTheSource()
    {
        bool touched = false;
        async IAsyncEnumerable<int> Source()
        {
            touched = true;
            await Task.Yield();
            yield return 1;
        }

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (NonEmptyList<int> _ in Source().BatchNonEmpty(2, cts.Token))
            {
            }
        });
        Assert.False(touched);
    }

    [Fact]
    public async Task ReadAllBatchesAsync_WhenConsumerStopsEarly_ShouldLeaveRemainingItemsInTheChannel()
    {
        Channel<int> channel = Channel.CreateUnbounded<int>();
        for (int i = 1; i <= 5; i++)
            channel.Writer.TryWrite(i);

        NonEmptyList<int>? first = null;
        await foreach (NonEmptyList<int> batch in channel.Reader.ReadAllBatchesAsync(2))
        {
            first = batch;
            break;
        }

        Assert.Equal(new[] { 1, 2 }, first);
        Assert.Equal(3, channel.Reader.Count);
    }

    [Fact]
    public async Task NonEmptyAsyncEnumerable_SelectStoppedEarly_ShouldProjectOnlyWhatWasRead()
    {
        bool disposed = false;
        async IAsyncEnumerable<int> Source()
        {
            try
            {
                for (int i = 1; i <= 4; i++)
                {
                    await Task.Yield();
                    yield return i;
                }
            }
            finally
            {
                disposed = true;
            }
        }

        NonEmptyAsyncEnumerable<int> rows = (await Source().ToNonEmptyAsync())!;
        List<int> seen = new();
        int calls = 0;

        await foreach (int item in rows.Select(x => { calls++; return x * 10; }))
        {
            seen.Add(item);
            if (seen.Count == 2)
                break;
        }

        Assert.Equal(new[] { 10, 20 }, seen);
        // One eager call for the head, one for the single rest item that was read.
        Assert.Equal(2, calls);
        Assert.True(disposed);
    }
}
