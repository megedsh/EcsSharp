using System.Collections.Generic;
using System.Linq;

using EcsSharp.Collectors;
using EcsSharp.Events;
using EcsSharp.Events.EventArgs;
using EcsSharp.Logging.BuiltIn;

using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class RemoveComponentsTest
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        public RemoveComponentsTest()
        {
            CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter());
        }

        [Test]
        public void Sanity()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new("A");
            Sedan sedan2 = new("B");
            Suv suv1 = new("C");

            // Create entities with components
            IEntity entity1 = repo.CreateWithComponents(sedan1, suv1);
            IEntity entity2 = repo.CreateWithComponents(sedan2);

            // Verify components exist
            Assert.IsTrue(entity1.HasComponent<Sedan>());
            Assert.IsTrue(entity1.HasComponent<Suv>());
            Assert.IsTrue(entity2.HasComponent<Sedan>());

            // Remove component using generic method
            bool removed1 = entity1.RemoveComponent<Sedan>();
            Assert.IsTrue(removed1);
            Assert.IsFalse(entity1.HasComponent<Sedan>());
            Assert.IsTrue(entity1.HasComponent<Suv>());
            Assert.IsTrue(entity1.HasComponent<ICar>());

            // Remove component using type parameter
            bool removed2 = entity1.RemoveComponent(typeof(Suv));
            Assert.IsTrue(removed2);
            Assert.IsFalse(entity1.HasComponent<Suv>());
            Assert.IsFalse(entity1.HasComponent<ICar>());

            // Verify entity2 still has its component
            Assert.IsTrue(entity2.HasComponent<Sedan>());

            // Try to remove non-existent component
            bool removed3 = entity2.RemoveComponent<Suv>();
            Assert.IsFalse(removed3);

            // Verify entity still exists after component removal
            Assert.IsTrue(entity1.Exists());
            Assert.IsTrue(entity2.Exists());
        }

        [Test]
        public void RemoveComponent_RaisesDeletedEvent()
        {
            IEcsRepo repo = getNewRepo();

            List<ComponentDeletedEventArgs> sedanDeleted = new();
            List<ComponentDeletedEventArgs> carDeleted = new();
            List<ComponentDeletedEventArgs> suvDeleted = new();
            List<ComponentsRemovedFromEntitiesEventArgs> globalDeleted = new();

            // not supposed to happen on RemoveComponent, only on DeleteEntity
            repo.Events.ComponentDeletedWithEntity[typeof(Sedan)] += args => sedanDeleted.Add(args);
            repo.Events.ComponentDeletedWithEntity[typeof(ICar)] += args => carDeleted.Add(args);
            repo.Events.ComponentDeletedWithEntity[typeof(Suv)] += args => suvDeleted.Add(args);

            
            repo.Events.ComponentRemovedFromEntity[typeof(Sedan)] += args => sedanDeleted.Add(args);
            repo.Events.ComponentRemovedFromEntity[typeof(ICar)] += args => carDeleted.Add(args);
            repo.Events.ComponentRemovedFromEntity[typeof(Suv)] += args => suvDeleted.Add(args);

            repo.Events.GlobalComponentsRemovedFromEntity += args => globalDeleted.Add(args);

            Sedan sedan1 = new("A");
            Suv suv1 = new("C");

            // Create entity with components
            IEntity entity1 = repo.CreateWithComponents(sedan1, suv1);

            // Remove Sedan component
            bool removed1 = entity1.RemoveComponent<Sedan>();
            Assert.IsTrue(removed1);

            // Verify ComponentDeleted event was raised for Sedan and ICar
            Assert.That(() => sedanDeleted.Count, Is.EqualTo(1).After(500, 100));
            Assert.That(() => carDeleted.Count,   Is.EqualTo(1).After(500, 100));
            Assert.That(() => suvDeleted.Count,   Is.EqualTo(0).After(500, 100));
            Assert.That(() => globalDeleted.Count, Is.EqualTo(1).After(500, 100));

            ComponentDeletedEventArgs sedanDeletedArgs = sedanDeleted[0];
            Assert.AreEqual(sedan1,        sedanDeletedArgs.Component.Data);
            Assert.AreEqual(entity1.Id,    sedanDeletedArgs.Entity.Id);
            Assert.AreEqual(typeof(Sedan), sedanDeletedArgs.ComponentType);

            // Remove Suv component using Type
            bool removed2 = entity1.RemoveComponent(typeof(Suv));
            Assert.IsTrue(removed2);

            // Verify ComponentDeleted event was raised for Suv and ICar (second time)
            Assert.That(() => suvDeleted.Count,    Is.EqualTo(1).After(500, 100));
            Assert.That(() => carDeleted.Count,    Is.EqualTo(2).After(500, 100));
            Assert.That(() => globalDeleted.Count, Is.EqualTo(2).After(500, 100));
            ComponentDeletedEventArgs suvDeletedArgs = suvDeleted[0];
            Assert.AreEqual(suv1,        suvDeletedArgs.Component.Data);
            Assert.AreEqual(entity1.Id,  suvDeletedArgs.Entity.Id);
            Assert.AreEqual(typeof(Suv), suvDeletedArgs.ComponentType);

            // Try to remove non-existent component - should not raise event
            bool removed3 = entity1.RemoveComponent<Sedan>();
            Assert.IsFalse(removed3);
            Assert.That(() => sedanDeleted.Count,  Is.EqualTo(1).After(500, 100));
            Assert.That(() => globalDeleted.Count, Is.EqualTo(2).After(500, 100));
        }

                [Test]
        public void RemoveComponent_PartialDeletedEvent_WithBatch()
        {
            IEcsRepo repo = getNewRepo();

            List<ComponentsRemovedFromEntitiesEventArgs> partialDeleted = new();
            repo.Events.GlobalComponentsRemovedFromEntity += args => partialDeleted.Add(args);

            Sedan sedan1 = new("A");
            Suv suv1 = new("B");
            Location location1 = new(1, 1, 1);

            IEntity entity1 = repo.CreateWithComponents(sedan1, suv1, location1);
            IEntity entity2 = repo.CreateWithComponents(new Sedan("C"), new Suv("D"));

            // Remove multiple components in a batch
            repo.BatchUpdate(r =>
            {
                entity1.RemoveComponent<Sedan>();
                entity1.RemoveComponent<Location>();
                entity2.RemoveComponent<Suv>();
            });

            // Verify EntitiesPartialDeleted event was raised once for the batch
            Assert.That(() => partialDeleted.Count, Is.EqualTo(1).After(500, 100));

            ComponentsRemovedFromEntitiesEventArgs batchDeletedArgs = partialDeleted[0];
            Assert.AreEqual(2, batchDeletedArgs.Count);

            // Verify entity1 partial deletion
            Assert.IsTrue(batchDeletedArgs.TryGetEventForEntity(entity1, out ComponentRemovedFromEntityEventArgs entity1Args));
            Assert.AreEqual(2, entity1Args.DeletedComponents.Length);
            Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == sedan1));
            Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == location1));

            // Verify entity2 partial deletion
            Assert.IsTrue(batchDeletedArgs.TryGetEventForEntity(entity2, out ComponentRemovedFromEntityEventArgs entity2Args));
            Assert.AreEqual(1, entity2Args.DeletedComponents.Length);
            Assert.IsTrue(entity2Args.DeletedComponents.Any(c => c.ComponentType == typeof(Suv)));

            // Verify entities still exist
            Assert.IsTrue(entity1.Exists());
            Assert.IsTrue(entity2.Exists());

            // Verify remaining components
            Assert.IsFalse(entity1.HasComponent<Sedan>());
            Assert.IsTrue(entity1.HasComponent<Suv>());
            Assert.IsFalse(entity1.HasComponent<Location>());
        }

        [Test]
        public void RemoveComponent_PartialDeletedCollector()
        {
            IEcsRepo repo = getNewRepo();

            ComponentChangesCollector collector = new(repo, new[] { typeof(Sedan), typeof(Suv) });

            Sedan sedan1 = new("A");
            Suv suv1 = new("B");
            Location location1 = new(1, 1, 1);

            // Create entities with components
            IEntity entity1 = repo.CreateWithComponents(sedan1,         suv1);
            IEntity entity2 = repo.CreateWithComponents(new Sedan("C"), location1);

            // Pop initial creation updates
            Assert.That(() => collector.HasUpdates, Is.True.After(500, 100));
            CollectorReport initialReport = collector.Pop();
            Assert.AreEqual(2, initialReport.Updated.Length);
            Assert.IsFalse(collector.HasUpdates);

            // Remove components from entities
            entity1.RemoveComponent<Sedan>();
            entity2.RemoveComponent<Sedan>();

            // Verify partial deletion is NOT tracked by collector (entities still exist)
            Assert.That(() => collector.HasUpdates, Is.True.After(500));
            CollectorReport afterRemoveReport = collector.Pop();
            Assert.AreEqual(0, afterRemoveReport.Updated.Length);
            Assert.AreEqual(0, afterRemoveReport.Deleted.Length);
            Assert.AreEqual(2, afterRemoveReport.PartialDeleted.Length);

            // Verify entities still exist
            Assert.IsTrue(entity1.Exists());
            Assert.IsTrue(entity2.Exists());

            // Now delete an entire entity - this should be tracked
            repo.Delete(entity1);

            Assert.That(() => collector.HasUpdates, Is.True.After(500, 100));
            CollectorReport deleteReport = collector.Pop();
            Assert.AreEqual(1,          deleteReport.Deleted.Length);
            Assert.AreEqual(entity1.Id, deleteReport.Deleted[0].Entity.Id);
            Assert.AreEqual(0, deleteReport.PartialDeleted.Length);
            

            collector.Dispose();
        }
        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}