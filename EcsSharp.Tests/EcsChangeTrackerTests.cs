using System.Collections.Generic;
using System.Linq;

using EcsSharp.Collectors;
using EcsSharp.Events.EventArgs;

using NUnit.Framework;

namespace EcsSharp.Tests;

[TestFixture]
public class EcsChangeTrackerTests
{
    private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

    [Test]
    public void Subscriptions_DrainIndependentlyAndFilterByType()
    {
        IEcsRepo repo = m_factory.Create();
        using IEcsChangeSubscription first = repo.Changes.Subscribe(new[] { typeof(Sedan) });
        using IEcsChangeSubscription second = repo.Changes.Subscribe(new[] { typeof(Sedan) });
        using IEcsChangeSubscription unrelated = repo.Changes.Subscribe(new[] { typeof(Suv) });

        IEntity entity = repo.CreateWithComponents(new Sedan("A"));

        CollectorReport firstReport = first.Pop();
        CollectorReport secondReport = second.Pop();
        CollectorReport unrelatedReport = unrelated.Pop();

        Assert.AreEqual(1, firstReport.Updated.Length);
        Assert.AreEqual(entity, firstReport.Updated[0].Entity);
        Assert.AreEqual(1, secondReport.Updated.Length);
        Assert.AreEqual(entity, secondReport.Updated[0].Entity);
        Assert.IsEmpty(unrelatedReport.Updated);
        Assert.IsEmpty(first.Pop().Updated);
    }

    [Test]
    public void Subscription_ReportsEntityAndComponentDeletions()
    {
        IEcsRepo repo = m_factory.Create();
        using IEcsChangeSubscription subscription = repo.Changes.Subscribe(new[] { typeof(Sedan) });
        IEntity partiallyChanged = repo.CreateWithComponents(new Sedan("A"), new Suv("B"));
        IEntity deleted = repo.CreateWithComponents(new Sedan("C"));
        subscription.Pop();

        partiallyChanged.RemoveComponent<Sedan>();
        repo.Delete(deleted);

        CollectorReport report = subscription.Pop();

        Assert.AreEqual(1, report.PartialDeleted.Length);
        Assert.AreEqual(partiallyChanged, report.PartialDeleted[0].Entity);
        Assert.AreEqual(1, report.Deleted.Length);
        Assert.AreEqual(deleted, report.Deleted[0].Entity);
    }

    [Test]
    public void Tracking_DoesNotReplaceExistingEventApi()
    {
        IEcsRepo repo = m_factory.Create();
        using IEcsChangeSubscription subscription = repo.Changes.Subscribe(new[] { typeof(Sedan) });
        List<EntitiesUpdatedEventArgs> events = new();
        repo.Events.GlobalUpdated += events.Add;

        IEntity entity = repo.CreateWithComponents(new Sedan("A"));
        CollectorReport report = subscription.Pop();

        Assert.AreEqual(1, report.Updated.Length);
        Assert.AreEqual(entity, report.Updated[0].Entity);
        Assert.That(() => events.Count, Is.EqualTo(1).After(500, 100));
        Assert.IsTrue(events[0].TryGetEventForEntity(entity, out _));
    }

    [Test]
    public void Subscription_CoalescesUpdatesToLatestComponent()
    {
        IEcsRepo repo = m_factory.Create();
        using IEcsChangeSubscription subscription = repo.Changes.Subscribe(new[] { typeof(Sedan) });
        IEntity entity = repo.CreateWithComponents(new Sedan("A"));
        Sedan latest = new("B");

        entity.SetComponent(latest);
        CollectorReport report = subscription.Pop();

        Assert.AreEqual(1, report.Updated.Length);
        Assert.AreSame(latest, report.Updated[0].UpdatedComponents.Single().Component.Data);
    }

    [Test]
    public void Subscription_AccumulatesPartialDeletionsForSameEntity()
    {
        IEcsRepo repo = m_factory.Create();
        using IEcsChangeSubscription subscription = repo.Changes.Subscribe(new[] { typeof(Sedan), typeof(Suv) });
        IEntity entity = repo.CreateWithComponents(new Sedan("A"), new Suv("B"));
        subscription.Pop();

        entity.RemoveComponent<Sedan>();
        entity.RemoveComponent<Suv>();
        CollectorReport report = subscription.Pop();

        Assert.AreEqual(1, report.PartialDeleted.Length);
        Assert.AreEqual(2, report.PartialDeleted[0].DeletedComponents.Length);
        Assert.IsTrue(report.PartialDeleted[0].DeletedComponents.Any(c => c.ComponentType == typeof(Sedan)));
        Assert.IsTrue(report.PartialDeleted[0].DeletedComponents.Any(c => c.ComponentType == typeof(Suv)));
    }

    [Test]
    public void Subscription_EntityDeletionContainsAllComponents()
    {
        IEcsRepo repo = m_factory.Create();
        using IEcsChangeSubscription subscription = repo.Changes.Subscribe(new[] { typeof(Sedan) });
        IEntity entity = repo.CreateWithComponents(new Sedan("A"), new Suv("B"));
        subscription.Pop();

        repo.Delete(entity);
        CollectorReport report = subscription.Pop();

        Assert.AreEqual(1, report.Deleted.Length);
        Assert.AreEqual(2, report.Deleted[0].DeletedComponents.Length);
    }

    [Test]
    public void DisposedSubscription_DoesNotRecordFurtherChanges()
    {
        IEcsRepo repo = m_factory.Create();
        IEcsChangeSubscription subscription = repo.Changes.Subscribe(new[] { typeof(Sedan) });
        subscription.Dispose();

        repo.CreateWithComponents(new Sedan("A"));
        CollectorReport report = subscription.Pop();

        Assert.IsEmpty(report.Updated);
        Assert.IsEmpty(report.Deleted);
        Assert.IsEmpty(report.PartialDeleted);
    }
}
