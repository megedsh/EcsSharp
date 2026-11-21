using System;
using System.Linq;

using EcsSharp.Collectors;
using EcsSharp.Events.EventArgs;

using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class ComponentChangesCollectorTest
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        [Test]
        public void Sanity()
        {
            ComponentChangesCollector subject = getSubject(new[] { typeof(Sedan) }, out IEcsRepo repo);
            IEntity e1 = repo.CreateWithComponents(new Sedan("a"));
            IEntity e2 = repo.CreateWithComponents(new Sedan("b"));
            IEntity e3 = repo.CreateWithComponents(new Suv("c"));
            repo.Delete(e2);
            repo.Delete(e3);

            Assert.That(() => subject.HasUpdates, Is.True.After(1000, 250));
            CollectorReport collectorReport = subject.Pop();
            EntityUpdatedEventArgs[] updated = collectorReport.Updated;
            EntityDeletedEventArgs[] deleted = collectorReport.Deleted;

            Assert.That(() => subject.HasUpdates, Is.False);
            Assert.AreEqual(2, updated.Length);
            Assert.AreEqual(1, deleted.Length);

            Assert.AreEqual(e1, updated[0].Entity);
            Assert.AreEqual(e2, updated[1].Entity);
            Assert.AreEqual(e2, deleted[0].Entity);
            Assert.AreEqual(1,updated[0].ComponentEventArgs.Length);
        }

        [Test]
        public void Pop_CoalescesUpdatesForEntityAndComponent()
        {
            ComponentChangesCollector subject = getSubject(new[] { typeof(Sedan) }, out IEcsRepo repo);
            int delivered = 0;
            repo.Events.ComponentUpdated[typeof(Sedan)] += _ => delivered++;
            IEntity entity = repo.Create();

            entity.SetComponent(new Sedan("A"));
            Sedan latest = new Sedan("B");
            entity.SetComponent(latest);

            Assert.That(() => delivered, Is.EqualTo(2).After(1000, 100));
            CollectorReport report = subject.Pop();
            Assert.AreEqual(1, report.Updated.Length);
            Assert.AreEqual(entity, report.Updated[0].Entity);
            Assert.AreSame(latest, report.Updated[0].UpdatedComponents.Single().Component.Data);
        }

        [Test]
        public void Pop_ReportsPartialComponentDeletion()
        {
            ComponentChangesCollector subject = getSubject(new[] { typeof(Sedan) }, out IEcsRepo repo);
            IEntity entity = repo.CreateWithComponents(new Sedan("A"), new Suv("B"));
            Assert.That(() => subject.HasUpdates, Is.True.After(1000, 100));
            subject.Pop();

            entity.RemoveComponent<Sedan>();

            Assert.That(() => subject.HasUpdates, Is.True.After(1000, 100));
            CollectorReport report = subject.Pop();
            Assert.AreEqual(1, report.PartialDeleted.Length);
            Assert.AreEqual(entity, report.PartialDeleted[0].Entity);
            Assert.IsEmpty(report.Deleted);
        }

        [Test]
        public void Dispose_UnsubscribesFromPartialComponentDeletions()
        {
            ComponentChangesCollector subject = getSubject(new[] { typeof(Sedan) }, out IEcsRepo repo);
            IEntity entity = repo.CreateWithComponents(new Sedan("A"));
            Assert.That(() => subject.HasUpdates, Is.True.After(1000, 100));
            subject.Dispose();
            int delivered = 0;
            repo.Events.GlobalComponentsRemovedFromEntity += _ => delivered++;

            entity.RemoveComponent<Sedan>();

            Assert.That(() => delivered, Is.EqualTo(1).After(1000, 100));
            Assert.IsEmpty(subject.Pop().PartialDeleted);
            Assert.IsFalse(subject.HasUpdates);
        }

        private ComponentChangesCollector getSubject(Type[] components, out IEcsRepo ecsRepo)
        {
            ecsRepo = getNewRepo();
            return new ComponentChangesCollector(ecsRepo, components);
        }

        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}