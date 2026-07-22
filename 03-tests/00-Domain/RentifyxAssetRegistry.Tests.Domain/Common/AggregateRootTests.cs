using FluentAssertions;
using RentifyxAssetRegistry.Domain.Common;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.Common;

public class AggregateRootTests
{
    private sealed record TestEvent(DateTime OccurredAt) : IDomainEvent;

    private sealed class TestAggregate : AggregateRoot
    {
        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    [Fact]
    public void RaiseDomainEvent_MultipleEvents_ShouldAccumulateInDomainEvents()
    {
        TestAggregate aggregate = new();

        aggregate.Raise(new TestEvent(DateTime.UtcNow));
        aggregate.Raise(new TestEvent(DateTime.UtcNow));

        aggregate.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void ClearDomainEvents_AfterRaisingEvents_ShouldEmptyCollection()
    {
        TestAggregate aggregate = new();
        aggregate.Raise(new TestEvent(DateTime.UtcNow));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }
}
