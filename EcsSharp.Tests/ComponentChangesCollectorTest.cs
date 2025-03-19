using System;
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