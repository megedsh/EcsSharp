using System;
using System.Collections.Generic;
using System.Linq;
using EcsSharp.Distribute;
using EcsSharp.Events.EventArgs;
using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using EcsSharp.Storage;
using NUnit.Framework;

namespace EcsSharp.Tests;

public class DistributionTests
{
    private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

    public DistributionTests()
    {
        CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter(CommonLogLevel.Trace));
    }

    [Test]
    public void SimpleMergeTest()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        int updatCount = 0;
        int deletedCount = 0;

        repo2.Events.ComponentUpdated[typeof(ICar)] += _ => updatCount++;
        repo2.Events.ComponentDeleted[typeof(Location)] += _ => deletedCount++;

        IEntity entity1 = repo1.CreateWithComponents(new Sedan("a"), new Suv("a"), new Location(1, 1, 1));
        entity1.AddTag("foo");
        IEntity entity2 = repo1.CreateWithComponents(new Sedan("b"), new Suv("b"), new Location(2, 2, 2));
        IEntity entity3 = repo1.CreateWithComponents(new Sedan("c"), new Suv("c"), new Location(3, 3, 3));

        EcsPackage pack1 = new EcsPackage().AddAllComponents(entity1, entity2);

        repo2.MergePackage(pack1);
        Assert.That(() => updatCount, Is.EqualTo(4).After(500, 100));
        IEntity e1 = repo2.QuerySingle(entity1.Id);
        IEntity e2 = repo2.QuerySingle(entity2.Id);
        assertEntity(entity1, e1);
        assertEntity(entity2, e2);

        EcsPackage pack2 = new EcsPackage().AddAllComponents(entity3).AddDeletedEntity(entity2);
        repo2.MergePackage(pack2);
        Assert.That(() => updatCount, Is.EqualTo(6).After(500, 100));
        Assert.AreEqual(1, deletedCount);
        Assert.Null(repo2.QuerySingle(entity2.Id));

        IEntity e3 = repo2.QuerySingle(entity3.Id);
        assertEntity(entity3, e3);
    }

    [Test]
    public void DistributeFromEventTest()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();
        EcsPackage pack1 = new();

        repo1.Events.GlobalUpdated += a => pack1.AddFromEvent(a);

        IEntity entity1 = null;
        IEntity entity2 = null;
        IEntity entity3 = null;

        repo1.BatchUpdate(r =>
        {
            entity1 = r.Create(new object[] { new Sedan("a"), new Suv("a"), new Location(1, 1, 1) }, new[] { "foo" });
            entity2 = r.Create(new object[] { new Sedan("b"), new Suv("b"), new Location(2, 2, 2) }, new[] { "foo" });
            entity3 = r.Create(new object[] { new Sedan("c"), new Suv("c"), new Location(3, 3, 3) }, new[] { "foo" });
        });

        Assert.That(() => pack1.Updated.Count, Is.EqualTo(3).After(500));
        int updatCount = 0;
        int deletedCount = 0;

        repo2.Events.ComponentUpdated[typeof(ICar)] += _ => updatCount++;
        repo2.Events.ComponentDeleted[typeof(Location)] += _ => deletedCount++;

        repo2.MergePackage(pack1);
        Assert.That(() => updatCount, Is.EqualTo(6).After(500, 100));
        IEntity e1 = repo2.QuerySingle(entity1.Id);
        IEntity e2 = repo2.QuerySingle(entity2.Id);
        IEntity e3 = repo2.QuerySingle(entity3.Id);
        assertEntity(entity1, e1);
        assertEntity(entity2, e2);
        assertEntity(entity3, e3);
    }

    [Test]
    public void DistributeSamePackageTest()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();
        EcsPackage pack1 = new();

        repo1.Events.GlobalUpdated += a => pack1.AddFromEvent(a);

        IEntity entity1 = null;
        IEntity entity2 = null;
        IEntity entity3 = null;

        repo1.BatchUpdate(r =>
        {
            entity1 = r.Create(new object[] { new Sedan("a"), new Suv("a"), new Location(1, 1, 1) }, new[] { "foo" });
            entity2 = r.Create(new object[] { new Sedan("b"), new Suv("b"), new Location(2, 2, 2) }, new[] { "foo" });
            entity3 = r.Create(new object[] { new Sedan("c"), new Suv("c"), new Location(3, 3, 3) }, new[] { "foo" });
        });

        Assert.That(() => pack1.Updated.Count, Is.EqualTo(3).After(500));
        int createdCount = 0;
        int updatCount = 0;
        int entityCreated = 0;

        repo2.Events.ComponentCreated[typeof(ICar)] += _ => createdCount++;
        repo2.Events.ComponentUpdated[typeof(ICar)] += _ => updatCount++;
        repo2.Events.GlobalCreated += a => entityCreated += a.Count;

        repo2.MergePackage(pack1);
        repo2.MergePackage(pack1);

        Assert.That(() => updatCount, Is.EqualTo(12).After(500, 100));
        Assert.That(() => createdCount, Is.EqualTo(6).After(500, 100));
        Assert.That(() => entityCreated, Is.EqualTo(3).After(500, 100));
    }

    [Test]
    public void TagsDeleteMergeTest()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        int updatCount = 0;
        int deletedCount = 0;

        repo2.Events.ComponentUpdated[typeof(ICar)] += _ => updatCount++;
        repo2.Events.GlobalDeleted += args => deletedCount += args.Count;

        IEntity entity1 = repo1.CreateWithComponents(new Sedan("a")).AddTag("tag1");
        IEntity entity2 = repo1.CreateWithComponents(new Sedan("a")).AddTag("tag1");
        IEntity entity3 = repo1.CreateWithComponents(new Sedan("a")).AddTag("tag1");
        IEntity entity4 = repo1.CreateWithComponents(new Sedan("a")).AddTag("tag1");

        IEntity entity20 = repo1.CreateWithComponents(new Sedan("b")).AddTag("tag2");
        IEntity entity30 = repo1.CreateWithComponents(new Sedan("c")).AddTag("tag3");

        EcsPackage pack1 = new EcsPackage().AddAllComponents(entity1, entity2, entity3, entity4, entity20, entity30);

        repo2.MergePackage(pack1);
        Assert.That(() => updatCount, Is.EqualTo(6).After(500, 100));

        EcsPackage pack2 = new EcsPackage().AddDeleteByTag("tag1", "tag3");
        repo2.MergePackage(pack2);
        Assert.That(() => deletedCount, Is.EqualTo(5).After(500, 100));
        Assert.NotNull(repo2.QuerySingle(entity20.Id));
        Assert.Null(repo2.QuerySingle(entity1.Id));
        Assert.Null(repo2.QuerySingle(entity2.Id));
        Assert.Null(repo2.QuerySingle(entity3.Id));
        Assert.Null(repo2.QuerySingle(entity4.Id));
        Assert.Null(repo2.QuerySingle(entity30.Id));
    }

    [Test]
    public void VersionTest()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        int updateCount = 0;

        repo2.Events.ComponentUpdated[typeof(ICar)] += _ => updateCount++;

        IEntity entity1 = repo1.Create();

        Sedan sedan1 = new("a");
        EcsPackage pack1 = new EcsPackage().AddComponent(entity1, new Component(0, sedan1), new Component(0, new Suv("a")), new Component(0, new Location(1, 1, 1)));

        repo2.MergePackage(pack1);
        repo2.MergePackage(pack1);
        repo2.MergePackage(pack1);
        repo2.MergePackage(pack1);

        Assert.That(() => updateCount, Is.EqualTo(8).After(500, 100));
        IEntity e1 = repo2.QuerySingle(entity1.Id);
        Component sedan2 = e1.GetAllComponents().FirstOrDefault(c => c.Data.GetType() == typeof(Sedan));
        Assert.AreEqual(sedan1.Id, ((Sedan)sedan2.Data).Id);

        Assert.AreEqual(4, sedan2.Version);
    }

    [Test]
    public void VersionTest_keepVersion()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        IEntity entity1 = repo1.Create();
        Sedan sedan1 = new("a");
        EcsPackage pack1 = new EcsPackage().AddComponent(entity1, new Component(100, sedan1));
        repo2.MergePackage(pack1);
        IEntity e1 = repo2.QuerySingle(entity1.Id);
        Component sedan2 = e1.GetAllComponents().FirstOrDefault(c => c.Data.GetType() == typeof(Sedan));
        Assert.AreEqual(sedan1.Id, ((Sedan)sedan2.Data).Id);

        Assert.AreEqual(100, sedan2.Version);
    }

    [Test]
    public void VersionTest_newVersionExists()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        int updateCount = 0;

        repo2.Events.ComponentUpdated[typeof(ICar)] += _ => updateCount++;

        IEntity entity1 = repo1.Create();

        Sedan sedan1 = new("a");
        EcsPackage pack1 = new EcsPackage().AddComponent(entity1, new Component(100, sedan1));
        repo2.MergePackage(pack1);
        Assert.That(() => updateCount, Is.EqualTo(1).After(500, 100));
        IEntity e1 = repo2.QuerySingle(entity1.Id);
        Component sedan2 = e1.GetAllComponents().FirstOrDefault(c => c.Data.GetType() == typeof(Sedan));
        Assert.AreEqual(sedan1.Id, ((Sedan)sedan2.Data).Id);

        EcsPackage pack2 = new EcsPackage().AddComponent(entity1, new Component(99, sedan1));
        repo2.MergePackage(pack2);
        // when sent version is older, do not update the component
        Assert.That(() => updateCount, Is.EqualTo(1).After(500));
        IEntity e2 = repo2.QuerySingle(entity1.Id);
        Component sedan3 = e2.GetAllComponents().FirstOrDefault(c => c.Data.GetType() == typeof(Sedan));
        Assert.AreEqual(100, sedan3.Version);
    }

    [Test]
    public void VersionTest_maxVersion()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        IEntity entity1 = repo1.Create();

        Sedan sedan1 = new("a");

        EcsPackage pack1 = new EcsPackage().AddComponent(entity1, new Component(ulong.MaxValue, sedan1));
        repo2.MergePackage(pack1);
        IEntity e2 = repo2.QuerySingle(entity1.Id);
        Assert.IsTrue(e2.GetAllComponents().Any(c => c.Version.Equals(ulong.MaxValue)));

        e2.SetComponent(sedan1);
        Assert.IsTrue(e2.GetAllComponents().Any(c => c.Version.Equals(1)));
    }

    [Test]
    public void Merge_entityTags_component_test()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();
        List<EntitiesUpdatedEventArgs> repo1_eventList = new();
        List<EntitiesUpdatedEventArgs> repo2_eventList = new();
        repo1.Events.TaggedEntitiesUpdated["t1"] += a => { repo1_eventList.Add(a); };
        repo2.Events.TaggedEntitiesUpdated["t1"] += a => { repo2_eventList.Add(a); };

        IEntity entity1 = repo1.Create(new[] { new Sedan("a") }, new[] { "t1" });
        Assert.That(() => repo1_eventList.Count, Is.EqualTo(1).After(1000, 100));
        EcsPackage pack1 = new EcsPackage().AddFromEvent(repo1_eventList[0]);
        repo2.MergePackage(pack1);
        Assert.That(() => repo2_eventList.Count, Is.EqualTo(1).After(1000, 100));

        //Expected entityTags and sedan updated
        assertUpdatedEvent(repo2_eventList[0], entity1, 2);

        entity1.SetComponent(new Location(1, 1, 1));
        Assert.That(() => repo1_eventList.Count, Is.EqualTo(2).After(1000, 100));
        EcsPackage pack2 = new EcsPackage().AddFromEvent(repo1_eventList[1]);
        repo2.MergePackage(pack2);
        Assert.That(() => repo2_eventList.Count, Is.EqualTo(2).After(1000, 100));
        //Expected only yhe location to be updated
        assertUpdatedEvent(repo2_eventList[1], entity1, 1);
    }

    [Test]
    public void Merge_entityTags_component_test3()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();
        List<EntitiesUpdatedEventArgs> repo1_eventList = new();
        List<EntitiesUpdatedEventArgs> repo2_eventList = new();
        repo1.Events.TaggedEntitiesUpdated["t1"] += a => { repo1_eventList.Add(a); };
        repo2.Events.TaggedEntitiesUpdated["t1"] += a => { repo2_eventList.Add(a); };

        IEntity entity1 = repo1.Create(new[] { new Sedan("a") }, new[] { "t1" });
        Assert.That(() => repo1_eventList.Count, Is.EqualTo(1).After(1000, 100));
        EcsPackage pack1 = new EcsPackage().AddFromEvent(repo1_eventList[0]);
        repo2.MergePackage(pack1);
        Assert.That(() => repo2_eventList.Count, Is.EqualTo(1).After(1000, 100));

        //Expected entityTags and sedan updated
        assertUpdatedEvent(repo2_eventList[0], entity1, 2);

        entity1.SetComponent(new Location(1, 1, 1));
        Assert.That(() => repo1_eventList.Count, Is.EqualTo(2).After(1000, 100));
        EcsPackage pack2 = new EcsPackage().AddFromEvent(repo1_eventList[1]);
        repo2.MergePackage(pack2);
        Assert.That(() => repo2_eventList.Count, Is.EqualTo(2).After(1000, 100));
        //Expected only yhe location to be updated
        assertUpdatedEvent(repo2_eventList[1], entity1, 1);
    }

    [Test]
    public void TaggedEntitiesCreated()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesCreatedEventArgs> l1 = new();
        repo.Events.TaggedEntitiesCreated["foo"] += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");
        Sedan car2 = new("B");

        IEntity entity1 = repo.Create(new object[] { car1, suv1 }, new[] { "foo" });

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntitiesCreatedEventArgs args1 = l1[0];

        Assert.IsTrue(args1.TryGetEventForEntity(entity1, out EntityCreatedEventArgs entity1Args));
        Assert.AreEqual(entity1.Id, entity1Args.Entity.Id);
        Assert.AreEqual(3, entity1Args.CreatedComponents.Length);
        Assert.IsTrue(entity1Args.CreatedComponents.Any(c => c.Component.Data == car1));
    }

    [Test]
    public void MergeGlobalEventsTest()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        List<EntitiesCreatedEventArgs> entitiesCreatedEvents = new();
        List<EntitiesUpdatedEventArgs> entitiesUpdatedEvents = new();
        List<EntitiesDeletedEventArgs> entitiesDeletedEvents = new();

        repo2.Events.GlobalCreated += args => entitiesCreatedEvents.Add(args);
        repo2.Events.GlobalUpdated += args => entitiesUpdatedEvents.Add(args);
        repo2.Events.GlobalDeleted += args => entitiesDeletedEvents.Add(args);

        IEntity entity1 = repo1.CreateWithComponents(new Sedan("a"), new Suv("a"), new Location(1, 1, 1));
        IEntity entity2 = repo1.CreateWithComponents(new Sedan("b"), new Suv("b"), new Location(2, 2, 2));
        IEntity entity3 = repo1.CreateWithComponents(new Sedan("c"), new Suv("c"), new Location(3, 3, 3));

        //Created
        EcsPackage pack1 = new EcsPackage().AddAllComponents(entity1, entity2);

        repo2.MergePackage(pack1);
        Assert.That(() => entitiesCreatedEvents.Count, Is.EqualTo(1).After(500, 100));
        EntitiesCreatedEventArgs created1 = entitiesCreatedEvents[0];
        Assert.AreEqual(2, created1.Count);
        assertCreatedEvent(created1, entity1, 3);
        assertCreatedEvent(created1, entity2, 3);

        Assert.That(() => entitiesUpdatedEvents.Count, Is.EqualTo(1).After(500, 100));
        EntitiesUpdatedEventArgs updated1 = entitiesUpdatedEvents[0];
        Assert.AreEqual(2, updated1.Count);
        assertUpdatedEvent(updated1, entity1, 3);
        assertUpdatedEvent(updated1, entity2, 3);

        //Created and updated
        entitiesCreatedEvents.Clear();
        entitiesUpdatedEvents.Clear();
        EcsPackage pack2 = new EcsPackage().AddAllComponents(entity1, entity3);
        repo2.MergePackage(pack2);

        Assert.That(() => entitiesCreatedEvents.Count, Is.EqualTo(1).After(500, 100));
        EntitiesCreatedEventArgs created2 = entitiesCreatedEvents[0];
        Assert.AreEqual(1, created2.Count);
        assertCreatedEvent(created2, entity3, 3);

        Assert.That(() => entitiesUpdatedEvents.Count, Is.EqualTo(1).After(500, 100));
        EntitiesUpdatedEventArgs updated2 = entitiesUpdatedEvents[0];
        Assert.AreEqual(2, updated2.Count);
        assertUpdatedEvent(updated2, entity1, 3);
        assertUpdatedEvent(updated2, entity3, 3);

        //Deleted
        EcsPackage pack3 = new EcsPackage().AddDeletedEntity(entity1, entity2);
        repo2.MergePackage(pack3);
        Assert.That(() => entitiesDeletedEvents.Count, Is.EqualTo(1).After(500, 100));
        EntitiesDeletedEventArgs deleted1 = entitiesDeletedEvents[0];
        Assert.AreEqual(2, deleted1.Count);
        assertDeletedEvent(deleted1, entity1, 3);
        assertDeletedEvent(deleted1, entity2, 3);
    }

    [Test]
    public void AddFromEntityEvent()
    {
        IEcsRepo repo1 = getNewRepo();
        IEcsRepo repo2 = getNewRepo();

        EcsPackage ecsPackage = new();

        void onEventsGlobalCreated(EntitiesCreatedEventArgs args) => ecsPackage.AddFromEvent(args);
        void onEventsGlobalUpdated(EntitiesUpdatedEventArgs args) => ecsPackage.AddFromEvent(args);
        void onEventsGlobalDeleted(EntitiesDeletedEventArgs args) => ecsPackage.AddFromEvent(args);
        repo1.Events.GlobalCreated += onEventsGlobalCreated;
        repo1.Events.GlobalUpdated += onEventsGlobalUpdated;
        repo1.Events.GlobalDeleted += onEventsGlobalDeleted;

        Sedan sedan = new("a");
        Sedan sedanB = new("b");
        Suv suv = new("a");

        //create
        IEntity entity1 = repo1.Create(new object[] { sedan, suv }, new[] { "foo" });
        Assert.That(() => ecsPackage.Updated.Count, Is.AtLeast(1).After(1000, 100));
        repo2.MergePackage(ecsPackage);
        IEntity e1 = repo2.QuerySingle(entity1.Id);
        assertEntity(entity1, e1);

        //update
        ecsPackage = new EcsPackage();
        IEntity entity2 = repo1.CreateOrUpdate().Having(entity1.Id)
                               .WhenExists(e => e.SetComponent(sedanB))
                               .Run();

        Assert.That(() => ecsPackage.Updated.Count, Is.AtLeast(1).After(1000, 100));

        repo2.MergePackage(ecsPackage);

        IEntity e2 = repo2.QuerySingle(entity2.Id);
        assertEntity(entity2, e2);

        //delete
        ecsPackage = new EcsPackage();
        repo1.Delete(entity1);
        Assert.That(() => ecsPackage.Deleted.Count, Is.AtLeast(1).After(1000, 100));
        repo2.MergePackage(ecsPackage);
        Assert.Null(repo2.QuerySingle(entity1.Id));
    }

    private void assertEntity(IEntity first, IEntity second)
    {
        Dictionary<Type, Component> components = first.GetAllComponents().ToDictionary(c => c.Data.GetType(), c => c);

        Assert.IsNotNull(components);
        foreach (Component c in second.GetAllComponents())
        {
            Component component = components[c.Data.GetType()];
            if (c.Data is ICar car)
            {
                Assert.AreEqual(car.Id, ((ICar)component.Data).Id);
            }

            if (c.Data is Location location)
            {
                Assert.AreEqual(location, (Location)component.Data);
            }

            if (c.Data is EntityTags tags)
            {
                Assert.AreEqual(first.Tags, tags);
            }
        }
    }

    private void assertCreatedEvent(EntitiesCreatedEventArgs entitiesCreatedEvent, IEntity expectedEntity, int expectedComponentCount)
    {
        Assert.IsTrue(entitiesCreatedEvent.TryGetEventForEntity(expectedEntity.Id, out EntityCreatedEventArgs args));
        Assert.AreEqual(expectedComponentCount, args.CreatedComponents.Length);
    }

    private void assertUpdatedEvent(EntitiesUpdatedEventArgs eventArgs, IEntity expectedEntity, int expectedComponentCount)
    {
        Assert.IsTrue(eventArgs.TryGetEventForEntity(expectedEntity.Id, out EntityUpdatedEventArgs args));
        Assert.AreEqual(expectedComponentCount, args.UpdatedComponents.Length);
    }

    private void assertDeletedEvent(EntitiesDeletedEventArgs eventArgs, IEntity expectedEntity, int expectedComponentCount)
    {
        Assert.IsTrue(eventArgs.TryGetEventForEntity(expectedEntity.Id, out EntityDeletedEventArgs args));
        Assert.AreEqual(expectedComponentCount, args.DeletedComponents.Length);
    }

    private IEcsRepo getNewRepo()
    {
        IEcsRepo ecsRepo = m_factory.Create();
        return ecsRepo;
    }
}