using System;
using System.Collections.Generic;
using System.Linq;
using EcsSharp.Events.EventArgs;
using EcsSharp.Helpers;
using NUnit.Framework;

namespace EcsSharp.Tests;

[TestFixture]
public class EventsTests
{
    private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

    [Test]
    public void CreatedEventsTest()
    {
        IEcsRepo repo = getNewRepo();

        List<ComponentCreatedEventArgs> l1 = new();
        List<ComponentCreatedEventArgs> l2 = new();
        List<ComponentUpdatedEventArgs> l3 = new();

        void handler1(ComponentCreatedEventArgs args) => l1.Add(args);
        void handler2(ComponentCreatedEventArgs args) => l2.Add(args);
        void handler3(ComponentUpdatedEventArgs args) => l3.Add(args);

        repo.Events.ComponentCreated[typeof(ICar)] += handler1;
        repo.Events.ComponentCreated[typeof(Sedan)] += handler2;
        repo.Events.ComponentUpdated[typeof(ICar)] += handler3;
        Sedan car1 = new("A");

        IEntity entity1 = repo.CreateWithComponents(car1);
        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));
        Assert.That(() => l2.Count, Is.EqualTo(1).After(500, 100));
        Assert.That(() => l3.Count, Is.EqualTo(1).After(500, 100));

        ComponentCreatedEventArgs createdArgs1 = l1[0];
        ComponentCreatedEventArgs createdArgs2 = l2[0];
        Assert.AreEqual(car1, createdArgs1.Component.Data);
        Assert.AreEqual(car1, createdArgs2.Component.Data);
        Assert.AreEqual(entity1.Id, createdArgs1.Entity.Id);
        Assert.AreEqual(entity1.Id, createdArgs2.Entity.Id);

        repo.Events.ComponentCreated[typeof(ICar)] -= handler1;
        repo.Events.ComponentCreated[typeof(Sedan)] -= handler2;
        repo.Events.ComponentUpdated[typeof(ICar)] -= handler3;

        repo.CreateWithComponents(car1);

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500));
        Assert.That(() => l2.Count, Is.EqualTo(1).After(500));
        Assert.That(() => l3.Count, Is.EqualTo(1).After(500));
    }

    [Test]
    public void UpdatedEventsTest()
    {
        IEcsRepo repo = getNewRepo();

        List<ComponentUpdatedEventArgs> l1 = new();
        List<ComponentUpdatedEventArgs> l2 = new();

        repo.Events.ComponentUpdated[typeof(ICar)] += args => l1.Add(args);
        repo.Events.ComponentUpdated[typeof(Sedan)] += args => l2.Add(args);

        Sedan car1 = new("A");
        Sedan car2 = new("B");

        IEntity entity1 = repo.CreateWithComponents(car1);
        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));
        Assert.That(() => l2.Count, Is.EqualTo(1).After(500, 100));

        ComponentUpdatedEventArgs createdArgs1 = l1[0];
        ComponentUpdatedEventArgs createdArgs2 = l2[0];
        Assert.AreEqual(car1, createdArgs1.Component.Data);
        Assert.AreEqual(car1, createdArgs2.Component.Data);

        entity1.SetComponent(car2);

        Assert.That(() => l1.Count, Is.EqualTo(2).After(500, 100));
        Assert.That(() => l2.Count, Is.EqualTo(2).After(500, 100));

        ComponentUpdatedEventArgs createdArgs3 = l1[1];
        ComponentUpdatedEventArgs createdArgs4 = l2[1];
        Assert.AreEqual(car2, createdArgs3.Component.Data);
        Assert.AreEqual(car2, createdArgs4.Component.Data);

        Assert.AreEqual(car1, createdArgs4.OldComponent.Data);
    }

    [Test]
    public void DeletedEventsTest()
    {
        IEcsRepo repo = getNewRepo();

        List<ComponentDeletedEventArgs> l1 = new();
        List<ComponentDeletedEventArgs> l2 = new();

        repo.Events.ComponentDeleted[typeof(ICar)] += args => l1.Add(args);
        repo.Events.ComponentDeleted[typeof(Sedan)] += args => l2.Add(args);

        Sedan car1 = new("A");

        IEntity entity1 = repo.CreateWithComponents(car1);

        repo.Delete(entity1);

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));
        Assert.That(() => l2.Count, Is.EqualTo(1).After(500, 100));

        ComponentDeletedEventArgs createdArgs1 = l1[0];
        ComponentDeletedEventArgs createdArgs2 = l2[0];
        Assert.AreEqual(car1, createdArgs1.Component.Data);
        Assert.AreEqual(car1, createdArgs2.Component.Data);
    }

    [Test]
    public void GlobalEntitiesUpdated()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesUpdatedEventArgs> l1 = new();
        repo.Events.GlobalUpdated += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");
        Sedan car2 = new("B");

        IEntity entity1 = repo.CreateWithComponents(car1, suv1);
        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntitiesUpdatedEventArgs args1 = l1[0];

        Assert.IsTrue(args1.TryGetEventForEntity(entity1, out EntityUpdatedEventArgs entity1Args));
        Assert.AreEqual(entity1.Id, entity1Args.Entity.Id);
        Assert.AreEqual(2, entity1Args.UpdatedComponents.Length);
        Assert.IsTrue(entity1Args.UpdatedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(entity1Args.UpdatedComponents.Any(c => c.Component.Data == suv1));

        entity1.SetComponent(car2);

        Assert.That(() => l1.Count, Is.EqualTo(2).After(500, 100));

        EntitiesUpdatedEventArgs args2 = l1[1];

        Assert.IsTrue(args2.TryGetEventForEntity(entity1, out EntityUpdatedEventArgs entity1Args2));
        Assert.AreEqual(entity1.Id, entity1Args2.Entity.Id);
        Assert.AreEqual(1, entity1Args2.UpdatedComponents.Length);
        Assert.IsTrue(entity1Args2.UpdatedComponents.Any(c => c.Component.Data == car2));
    }

    [Test]
    public void GlobalEntitiesDeleted()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesDeletedEventArgs> l1 = new();

        repo.Events.GlobalDeleted += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");

        IEntity entity1 = repo.CreateWithComponents(car1, suv1);

        repo.Delete(entity1);
        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntitiesDeletedEventArgs args1 = l1[0];

        Assert.IsTrue(args1.TryGetEventForEntity(entity1, out EntityDeletedEventArgs entity1Args));
        Assert.AreEqual(entity1.Id, entity1Args.Entity.Id);
        Assert.AreEqual(2, entity1Args.DeletedComponents.Length);
        Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == suv1));
    }

    [Test]
    public void GlobalEntitiesCreatedInBatch()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesCreatedEventArgs> l1 = new();
        repo.Events.GlobalCreated += args => l1.Add(args);

        repo.BatchUpdate(r =>
        {
            IEntity entity = r.Create();
            entity.SetComponents(new Sedan(), new Suv());
        });

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));
        EntitiesCreatedEventArgs args1 = l1[0];
        EntityCreatedEventArgs entityCreatedEventArgs = args1.FirstOrDefault();
        Assert.NotNull(entityCreatedEventArgs);
        Assert.AreEqual(2, entityCreatedEventArgs.CreatedComponents.Length);
    }

    [Test]
    public void GlobalEntitiesCreated()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesCreatedEventArgs> l1 = new();
        repo.Events.GlobalCreated += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");

        //Create With Components
        IEntity entity1 = repo.CreateWithComponents(car1, suv1);
        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));
        EntitiesCreatedEventArgs args1 = l1[0];
        Assert.IsTrue(args1.TryGetEventForEntity(entity1, out EntityCreatedEventArgs entity1Args));
        Assert.AreEqual(entity1.Id, entity1Args.Entity.Id);
        Assert.AreEqual(2, entity1Args.CreatedComponents.Length);
        Assert.IsTrue(entity1Args.CreatedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(entity1Args.CreatedComponents.Any(c => c.Component.Data == suv1));

        //Create
        IEntity entity2 = repo.Create();
        Assert.That(() => l1.Count, Is.EqualTo(2).After(500, 100));
        EntitiesCreatedEventArgs args2 = l1[1];
        Assert.IsTrue(args2.TryGetEventForEntity(entity2, out EntityCreatedEventArgs entity2Args));
        Assert.AreEqual(entity2.Id, entity2Args.Entity.Id);
        Assert.AreEqual(0, entity2Args.CreatedComponents.Length);

        ICreateOrUpdateBuilder builder = repo.CreateOrUpdate();
        //Create or update with Id
        IEntity entity3 = builder.Having("foo-bar").WhenCreated(e => { }).Run();
        Assert.That(() => l1.Count, Is.EqualTo(3).After(500, 100));
        EntitiesCreatedEventArgs args3 = l1[2];
        Assert.IsTrue(args3.TryGetEventForEntity(entity3, out EntityCreatedEventArgs entity3Args));
        Assert.AreEqual(entity3.Id, entity3Args.Entity.Id);
        Assert.AreEqual(0, entity3Args.CreatedComponents.Length);

        builder = repo.CreateOrUpdate();
        //Create Or update with Id and components
        IEntity entity4 = builder.Having("foo-bar-1").WhenEither(e => e.SetComponents(car1, suv1)).Run();
        Assert.That(() => l1.Count, Is.EqualTo(4).After(500, 100));
        EntitiesCreatedEventArgs args4 = l1[3];
        Assert.IsTrue(args4.TryGetEventForEntity(entity4, out EntityCreatedEventArgs entity4Args));
        Assert.AreEqual(entity4.Id, entity4Args.Entity.Id);
        Assert.AreEqual(2, entity4Args.CreatedComponents.Length);
        Assert.IsTrue(entity4Args.CreatedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(entity4Args.CreatedComponents.Any(c => c.Component.Data == suv1));

        //Create or update with Id (already exists - no event)
        builder = repo.CreateOrUpdate();
        builder.Having("foo-bar").Run();
        Assert.That(() => l1.Count, Is.EqualTo(4).After(500, 100));

        //Create or get 
        IEntity entity5 = repo.CreateOrGetWithId("foo-bar-2");
        Assert.That(() => l1.Count, Is.EqualTo(5).After(500, 100));
        EntitiesCreatedEventArgs args5 = l1[4];
        Assert.IsTrue(args5.TryGetEventForEntity(entity5, out EntityCreatedEventArgs entity5Args));
        Assert.AreEqual(entity5.Id, entity5Args.Entity.Id);
        Assert.AreEqual(0, entity5Args.CreatedComponents.Length);

        //Create or get (already exists - no event)
        repo.CreateOrGetWithId("foo-bar-2");
        Assert.That(() => l1.Count, Is.EqualTo(5).After(500, 100));

        //CreateOrGetByComponent
        Suv suvAds = new("ADS");
        IEntity entity6 = repo.CreateOrGetByComponent(suvAds, s => s.Id == "ADS");
        Assert.That(() => l1.Count, Is.EqualTo(6).After(500, 100));
        EntitiesCreatedEventArgs args6 = l1[5];
        Assert.IsTrue(args6.TryGetEventForEntity(entity6, out EntityCreatedEventArgs entity6Args));
        Assert.AreEqual(entity6.Id, entity6Args.Entity.Id);
        Assert.AreEqual(1, entity6Args.CreatedComponents.Length);
        Assert.IsTrue(entity6Args.CreatedComponents.Any(c => c.Component.Data == suvAds));

        //CreateOrGetByComponent entity exists, no event
        repo.CreateOrGetByComponent(suvAds, s => s.Id == "ADS");
        Assert.That(() => l1.Count, Is.EqualTo(6).After(500, 100));
    }

    [Test]
    public void TaggedEntitiesUpdated()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesUpdatedEventArgs> l1 = new();
        repo.Events.TaggedEntitiesUpdated["foo"] += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");
        Sedan car2 = new("B");
        IEntity entity1 = null;
        repo.BatchUpdate(r =>
        {
            entity1 = r.Create(Array.Empty<object>(), new[] { "foo" });
            entity1.SetComponents(car1);
        });

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntitiesUpdatedEventArgs args1 = l1[0];

        Assert.IsTrue(args1.TryGetEventForEntity(entity1, out EntityUpdatedEventArgs entity1Args));
        Assert.AreEqual(entity1.Id, entity1Args.Entity.Id);
        Assert.AreEqual(2, entity1Args.UpdatedComponents.Length);
        Assert.IsTrue(entity1Args.UpdatedComponents.Any(c => c.Component.Data == car1));

        entity1.SetComponent(car2);

        Assert.That(() => l1.Count, Is.EqualTo(2).After(500, 100));

        EntitiesUpdatedEventArgs args2 = l1[1];

        Assert.IsTrue(args2.TryGetEventForEntity(entity1, out EntityUpdatedEventArgs entity1Args2));
        Assert.AreEqual(entity1.Id, entity1Args2.Entity.Id);
        Assert.AreEqual(1, entity1Args2.UpdatedComponents.Length);
        Assert.IsTrue(entity1Args2.UpdatedComponents.Any(c => c.Component.Data == car2));
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
    public void TaggedEntitiesDeleted()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesDeletedEventArgs> l1 = new();
        repo.Events.TaggedEntitiesDeleted["foo"] += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");
        IEntity entity1 = repo.Create(new object[] { car1, suv1 }, new[] { "foo" });
        IEntity entity2 = repo.Create();

        repo.Delete(entity2);
        repo.Delete(entity1);

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntitiesDeletedEventArgs args1 = l1[0];

        Assert.IsTrue(args1.TryGetEventForEntity(entity1, out EntityDeletedEventArgs entity1Args));
        Assert.AreEqual(entity1.Id, entity1Args.Entity.Id);
        Assert.AreEqual(2, entity1Args.DeletedComponents.Length);
        Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == suv1));
    }

    [Test]
    public void TaggedEntitiesDeletedByTag()
    {
        IEcsRepo repo = getNewRepo();

        List<EntitiesDeletedEventArgs> l1 = new();
        repo.Events.TaggedEntitiesDeleted["foo"] += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");
        IEntity entity1 = repo.Create(new object[] { car1, suv1 }, new[] { "foo" });
        IEntity entity2 = repo.Create();

        repo.DeleteEntitiesWithTag("foo");

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntitiesDeletedEventArgs args1 = l1[0];

        Assert.IsTrue(args1.TryGetEventForEntity(entity1, out EntityDeletedEventArgs entity1Args));
        Assert.AreEqual(entity1.Id, entity1Args.Entity.Id);
        Assert.AreEqual(2, entity1Args.DeletedComponents.Length);
        Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(entity1Args.DeletedComponents.Any(c => c.Component.Data == suv1));
    }


    [Test]
    public void SpecificEntitiesCreated()
    {
        IEcsRepo repo = getNewRepo();

        List<EntityCreatedEventArgs> l1 = new();
        repo.Events.SpecificCreated["foo"] += args => l1.Add(args);

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");

        IEntity entity1 = repo.Create(new object[] { car1, suv1 }, Array.Empty<string>(),"foo");

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntityCreatedEventArgs args1 = l1[0];
        
        Assert.AreEqual(entity1.Id, args1.Entity.Id);
        Assert.AreEqual(2, args1.CreatedComponents.Length);
        Assert.IsTrue(args1.CreatedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(args1.CreatedComponents.Any(c => c.Component.Data == suv1));

        // updating should not fire the event
        entity1.SetComponent(car1);
        Assert.That(() => l1.Count, Is.EqualTo(1).After(500));
    }


    [Test]
    public void SpecificEntitiesUpdated()
    {
        IEcsRepo repo = getNewRepo();

        List<EntityUpdatedEventArgs> l1 = new();

        void onAction(EntityUpdatedEventArgs args) => l1.Add(args);

        repo.Events.SpecificUpdated["foo"] += onAction;

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");
        Location loc = new Location(1, 1, 1);

        IEntity entity1 = repo.Create(new object[] { car1, suv1 }, Array.Empty<string>(),"foo");

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        EntityUpdatedEventArgs args1 = l1[0];
        
        Assert.AreEqual(entity1.Id, args1.Entity.Id);
        Assert.AreEqual(2, args1.UpdatedComponents.Length);
        Assert.IsTrue(args1.UpdatedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(args1.UpdatedComponents.Any(c => c.Component.Data == suv1));

        
        entity1.SetComponent(loc);
        Assert.That(() => l1.Count, Is.EqualTo(2).After(500,100));

        EntityUpdatedEventArgs args2 = l1[1];
        
        Assert.AreEqual(entity1.Id, args2.Entity.Id);
        Assert.AreEqual(1, args2.UpdatedComponents.Length);
        Assert.IsTrue(args2.UpdatedComponents.Any(c => Equals(c.Component.Data, loc)));

        repo.Events.SpecificUpdated["foo"] -= onAction;

        
        entity1.SetComponent(loc);
        Assert.That(() => l1.Count, Is.EqualTo(2).After(500));
    }


    [Test]
    public void SpecificEntitiesDeleted()
    {
        IEcsRepo repo = getNewRepo();

        List<EntityDeletedEventArgs> l1 = new();

        void onAction(EntityDeletedEventArgs args) => l1.Add(args);

        repo.Events.SpecificDeleted["foo"] += onAction;

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");

        IEntity entity1 = repo.Create(new object[] { car1, suv1 }, Array.Empty<string>(),"foo");
        IEntity entity2 = repo.Create(new object[] { car1, suv1 }, Array.Empty<string>(),"bar");

        repo.Delete(entity1);
        repo.Delete(entity2);

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500));

        EntityDeletedEventArgs args1 = l1[0];
        
        Assert.AreEqual(entity1.Id, args1.Entity.Id);
        Assert.AreEqual(2, args1.DeletedComponents.Length);
        Assert.IsTrue(args1.DeletedComponents.Any(c => c.Component.Data == car1));
        Assert.IsTrue(args1.DeletedComponents.Any(c => c.Component.Data == suv1));
    }


    [Test]
    public void SpecificEntitiesUpdated_multi()
    {
        IEcsRepo repo = getNewRepo();

        List<EntityUpdatedEventArgs> l1 = new();
        List<EntityUpdatedEventArgs> l2 = new();

        void onAction(EntityUpdatedEventArgs args) => l1.Add(args);
        void onAction2(EntityUpdatedEventArgs args) => l2.Add(args);

        repo.Events.SpecificUpdated["foo"] += onAction;
        repo.Events.SpecificUpdated["foo"] += onAction2;

        Sedan car1 = new("A");
        Suv suv1 = new("SuvA");
        Location loc = new Location(1, 1, 1);

        IEntity entity1 = repo.Create(new object[] { car1, suv1 }, Array.Empty<string>(),"foo");

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));
        Assert.That(() => l2.Count, Is.EqualTo(1).After(500, 100));
        
        entity1.SetComponent(loc);
        Assert.That(() => l1.Count, Is.EqualTo(2).After(500,100));
        Assert.That(() => l2.Count, Is.EqualTo(2).After(500,100));

        
        repo.Events.SpecificUpdated["foo"] -= onAction;
        entity1.SetComponent(loc);
        Assert.That(() => l1.Count, Is.EqualTo(2).After(500));
        Assert.That(() => l2.Count, Is.EqualTo(3).After(500,100));

        repo.Events.SpecificUpdated["foo"] -= onAction2;
        entity1.SetComponent(loc);
        Assert.That(() => l2.Count, Is.EqualTo(3).After(500));

    }

    private IEcsRepo getNewRepo()
    {
        IEcsRepo ecsRepo = m_factory.Create();
        return ecsRepo;
    }
}