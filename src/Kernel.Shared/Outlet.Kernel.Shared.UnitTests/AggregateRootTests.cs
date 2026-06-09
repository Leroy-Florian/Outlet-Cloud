namespace Outlet.Kernel.Shared.UnitTests;

public sealed class AggregateRootTests
{
    private sealed record TestEvent(string Name) : IDomainEvent
    {
        public Guid EventId => Guid.Empty;
        public DateTime OccurredOn => DateTime.MinValue;
    }

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Raise(string name) => RaiseDomainEvent(new TestEvent(name));
    }

    [Fact]
    public void RaiseDomainEvent_Should_AddEventToCollection()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.Raise("first");
        aggregate.Raise("second");

        aggregate.DomainEvents.Should().HaveCount(2);
        aggregate.DomainEvents.OfType<TestEvent>().Select(e => e.Name)
            .Should().BeEquivalentTo("first", "second");
    }

    [Fact]
    public void ClearDomainEvents_Should_RemoveAllEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.Raise("first");
        aggregate.Raise("second");

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_Should_BeEmpty_Initially()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DomainEvents.Should().BeEmpty();
    }
}
