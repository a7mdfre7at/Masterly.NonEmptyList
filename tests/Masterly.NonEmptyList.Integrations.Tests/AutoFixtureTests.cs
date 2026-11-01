using AutoFixture;
using AutoFixture.Kernel;
using Masterly.NonEmptyList.Testing;

namespace Masterly.NonEmptyList.Integrations.Tests;

public class AutoFixtureTests
{
    private sealed record Order(Guid Id, NonEmptyList<string> Lines, NonEmptyString Customer, NonEmptySet<int> Codes, NonEmptyDictionary<string, decimal> Prices, ConstrainedList<int, Distinct> Ids, ImmutableNonEmptyList<int> Snapshot, NonEmptyList<int>? Optional);

    [Fact]
    public void Fixture_ShouldCreateEveryNonEmptyTypeAndNestedDtos()
    {
        IFixture fixture = new Fixture().Customize(new NonEmptyCustomization());

        NonEmptyList<int> list = fixture.Create<NonEmptyList<int>>();
        NonEmptyString text = fixture.Create<NonEmptyString>();
        Order order = fixture.Create<Order>();
        Scores scores = fixture.Create<Scores>();

        Assert.Equal(3, list.Count);
        Assert.True(text.HasValue);
        Assert.Equal(3, order.Lines.Count);
        Assert.True(order.Customer.HasValue);
        Assert.InRange(order.Codes.Count, 1, 3);
        Assert.InRange(order.Prices.Count, 1, 3);
        Assert.InRange(order.Ids.Count, 1, 3);
        Assert.Equal(3, order.Snapshot.Count);
        Assert.NotNull(order.Optional);
        Assert.InRange(scores.Count, 1, 3);
    }

    [Fact]
    public void Count_ShouldOverrideElementCount()
    {
        IFixture fixture = new Fixture().Customize(new NonEmptyCustomization { Count = 1 });

        Assert.Single(fixture.Create<NonEmptyList<string>>());
        Assert.Single(fixture.Create<NonEmptySet<string>>());
        Assert.Single(fixture.Create<NonEmptyDictionary<string, int>>());
    }

    [Fact]
    public void Builder_ShouldReturnNoSpecimenForOtherRequests()
    {
        NonEmptySpecimenBuilder builder = new();
        ISpecimenContext context = new SpecimenContext(new Fixture());

        Assert.IsType<NoSpecimen>(builder.Create("not a type", context));
        Assert.IsType<NoSpecimen>(builder.Create(typeof(List<int>), context));
        Assert.IsType<NoSpecimen>(builder.Create(typeof(string), context));
        Assert.IsType<NonEmptyList<int>>(builder.Create(typeof(NonEmptyList<int>), context));
        Assert.IsType<NonEmptyString>(builder.Create(typeof(NonEmptyString?), context));
        Assert.Throws<ArgumentNullException>(() => builder.Create(typeof(int), null!));
        Assert.Throws<ArgumentNullException>(() => new NonEmptyCustomization().Customize(null!));
    }

    [Fact]
    public void ConstrainedTypes_ShouldShrinkUntilTheConstraintHolds()
    {
        IFixture fixture = new Fixture().Customize(new NonEmptyCustomization { Count = 5 });

        ConstrainedList<int, AtMost<Two>> bounded = fixture.Create<ConstrainedList<int, AtMost<Two>>>();

        Assert.InRange(bounded.Count, 1, 2);
    }

    [Fact]
    public void ConstrainedTypes_ShouldShrinkAllTheWayDown_EvenFromALargeCount()
    {
        IFixture fixture = new Fixture().Customize(new NonEmptyCustomization { Count = 40 });

        ConstrainedList<int, AtMost<Two>> bounded = fixture.Create<ConstrainedList<int, AtMost<Two>>>();
        ConstrainedList<int, Exactly<One>> single = fixture.Create<ConstrainedList<int, Exactly<One>>>();

        Assert.Equal(2, bounded.Count);
        Assert.Single(single);
    }

    [Fact]
    public void CountBelowTheConstraintMinimum_ShouldSurfaceTheConstraintError()
    {
        NonEmptySpecimenBuilder builder = new(1);

        Assert.ThrowsAny<ArgumentException>(() => builder.Create(typeof(ConstrainedList<int, AtLeast<Two>>), new SpecimenContext(new Fixture())));
    }

    private sealed class BlankStrings : ISpecimenContext
    {
        public object Resolve(object request) => (Type)request == typeof(string) ? " " : 7;
    }

    [Fact]
    public void NonEmptyString_ShouldNeverBeBlank_EvenWhenTheFixtureProducesBlankText()
    {
        NonEmptyString text = (NonEmptyString)new NonEmptySpecimenBuilder().Create(typeof(NonEmptyString), new BlankStrings());

        Assert.False(string.IsNullOrWhiteSpace(text.Value));
        Assert.Equal(32, text.Value.Length);
    }

    [Fact]
    public void DerivedCollectionWithoutFactory_ShouldExplainWhatIsMissing()
    {
        NonEmptySpecimenBuilder builder = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => builder.Create(typeof(NoFactoryList), new SpecimenContext(new Fixture())));

        Assert.Contains("does not expose a static From", ex.Message);
    }

    [Fact]
    public void Count_ShouldDefaultToTheFixtureRepeatCount()
    {
        IFixture fixture = new Fixture { RepeatCount = 5 }.Customize(new NonEmptyCustomization());

        NonEmptyList<int> list = fixture.Create<NonEmptyList<int>>();

        Assert.Equal(5, list.Count);
    }
}
