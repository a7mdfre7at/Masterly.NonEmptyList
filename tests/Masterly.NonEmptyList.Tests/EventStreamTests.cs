namespace Masterly.NonEmptyList.Tests;

public class EventStreamTests
{
    private abstract record OrderEvent;
    private sealed record OrderCreated(Guid Id, string Customer) : OrderEvent;
    private sealed record LineAdded(string Sku, int Quantity) : OrderEvent;
    private sealed record OrderPaid(decimal Amount) : OrderEvent;

    private sealed record Order(Guid Id, string Customer, int Lines, bool Paid)
    {
        public static Order Create(OrderEvent origin) => origin is OrderCreated c ? new Order(c.Id, c.Customer, 0, false) : throw new InvalidOperationException();

        public Order Apply(OrderEvent e) => e switch
        {
            LineAdded => this with { Lines = Lines + 1 },
            OrderPaid => this with { Paid = true },
            _ => this,
        };
    }

    [Fact]
    public void Start_ShouldHaveOriginAsLatestAndVersionOne()
    {
        var created = new OrderCreated(Guid.NewGuid(), "ada");

        EventStream<OrderEvent> stream = EventStream.Start<OrderEvent>(created);

        Assert.Same(created, stream.Origin);
        Assert.Same(created, stream.Latest);
        Assert.Equal(1, stream.Version);
        Assert.Single(stream);
        Assert.Equal(created, stream[0]);
        Assert.Equal(created, stream.OriginAs<OrderCreated>());
        Assert.Throws<ArgumentNullException>(() => EventStream.Start<OrderEvent>(null!));
    }

    [Fact]
    public void Append_ShouldNotMutateAndReplayShouldFold()
    {
        EventStream<OrderEvent> stream = EventStream.Start<OrderEvent>(new OrderCreated(Guid.NewGuid(), "ada"));

        EventStream<OrderEvent> longer = stream.Append(new LineAdded("A", 2)).Append(new OrderPaid(10m));

        Assert.Equal(1, stream.Version);
        Assert.Equal(3, longer.Version);
        Order order = longer.Replay(Order.Create, (state, e) => state.Apply(e));
        Assert.Equal("ada", order.Customer);
        Assert.Equal(1, order.Lines);
        Assert.True(order.Paid);
        Assert.IsType<OrderPaid>(longer.Latest);
    }

    [Fact]
    public void ReplayWithSeed_AndHistory_ShouldExposeIntermediateStates()
    {
        EventStream<int> stream = EventStream.Start(1).AppendRange(new[] { 2, 3 });

        int sum = stream.Replay(0, (acc, e) => acc + e);
        NonEmptyList<int> history = stream.ReplayHistory(e => e, (acc, e) => acc + e);

        Assert.Equal(6, sum);
        Assert.Equal(new[] { 1, 3, 6 }, history.ToArray());
        Assert.Same(stream, stream.AppendRange(Array.Empty<int>()));
    }

    [Fact]
    public void Since_UpTo_OfType_ShouldSliceTheStream()
    {
        EventStream<OrderEvent> stream = EventStream.Start<OrderEvent>(new OrderCreated(Guid.NewGuid(), "ada"))
            .Append(new LineAdded("A", 1))
            .Append(new LineAdded("B", 1))
            .Append(new OrderPaid(5m));

        Assert.Equal(2, stream.Since(2).Count);
        Assert.Empty(stream.Since(4));
        Assert.Equal(4, stream.Since(0).Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Since(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Since(-1));

        EventStream<OrderEvent> truncated = stream.UpTo(2);
        Assert.Equal(2, truncated.Version);
        Assert.IsType<LineAdded>(truncated.Latest);
        Assert.Same(stream, stream.UpTo(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.UpTo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.UpTo(5));

        Assert.Equal(2, stream.OfType<LineAdded>().Count());
        Assert.Throws<InvalidCastException>(() => stream.OriginAs<OrderPaid>());
    }

    [Fact]
    public void From_ShouldRebuildFromStoredEventsAndRejectEmpty()
    {
        List<int> stored = new() { 1, 2 };

        EventStream<int> fromEnumerable = EventStream.From(stored);
        EventStream<int> fromList = NonEmptyList.Of(1, 2).ToEventStream();

        Assert.Equal(fromEnumerable, fromList);
        Assert.Equal(fromEnumerable.GetHashCode(), fromList.GetHashCode());
        Assert.Equal(2, fromEnumerable.Events.Count);
        Assert.Throws<ArgumentException>(() => EventStream.From(Array.Empty<int>()));
        Assert.Throws<ArgumentNullException>(() => EventStream<int>.From((NonEmptyList<int>)null!));
    }

    [Fact]
    public void Equality_AndToString_ShouldWork()
    {
        EventStream<int> a = EventStream.Start(1).Append(2);
        EventStream<int> b = EventStream.Start(1).Append(2);

        Assert.Equal(a, b);
        Assert.NotEqual(a, a.Append(3));
        Assert.True(a.Equals((object)b));
        Assert.False(a.Equals(null));
        Assert.Equal("EventStream: v2, [1, 2]", a.ToString());
        Assert.Equal(new[] { 1, 2 }, a.ToArray());
        Assert.Equal(2, ((System.Collections.IEnumerable)a).Cast<int>().Count());
    }
}
