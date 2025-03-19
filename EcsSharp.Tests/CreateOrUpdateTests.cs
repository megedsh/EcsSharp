using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EcsSharp.Events.EventArgs;
using EcsSharp.Helpers;
using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using EcsSharp.Storage;
using NUnit.Framework;

namespace EcsSharp.Tests;

[TestFixture]
public class CreateOrUpdateTests
{
    private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

    public CreateOrUpdateTests()
    {
        CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter());
    }

    [Test]
    public void CreatedOnly()
    {
        IEcsRepo repo = getNewRepo();
        
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");

        IEntity entity1 = repo.CreateOrUpdate().Having<Sedan>()
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .Run();

        IEntity querySingle = repo.QuerySingle<Sedan>(c => c.Id == sedan1.Id);
        Assert.AreEqual(entity1.Id, querySingle.Id);

        IEntity entity2 = repo.CreateOrUpdate().Having<Sedan>()
                              .WhenCreated(e => e.SetComponent(sedan2))
                              .Run();

        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan1, entity2.GetComponent<Sedan>());
    }

    [Test]
    public void UpdateOnly()
    {
        IEcsRepo repo = getNewRepo();

        

        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");

        IEntity entity1 = repo.CreateOrUpdate().Having<Sedan>()
                              .WhenExists(e => e.SetComponent(sedan2))
                              .Run();

        IEntity querySingle = repo.QuerySingle<Sedan>(c => c.Id == sedan1.Id);
        Assert.Null(entity1);
        Assert.Null(querySingle);

        repo.CreateWithComponents(sedan1);

        IEntity entity2 = repo.CreateOrUpdate().Having<Sedan>()
                              .WhenExists(e => e.SetComponent(sedan2))
                              .Run();

        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
    }

    [Test]
    public void CreatedOrUpdate()
    {
        IEcsRepo repo = getNewRepo();

        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");

        IEntity entity1 = repo.CreateOrUpdate().Having<Sedan>()
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .Run();

        IEntity querySingle = repo.QuerySingle<Sedan>(c => c.Id == sedan1.Id);
        Assert.AreEqual(entity1.Id, querySingle.Id);
        Assert.AreEqual(sedan1, entity1.GetComponent<Sedan>());

        IEntity entity2 = repo.CreateOrUpdate().Having<Sedan>()
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .Run();

        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
    }

    [Test]
    public void Either()
    {
        IEcsRepo repo = getNewRepo();
        
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");

        string suvId = "A";

        IEntity entity1 = repo.CreateOrUpdate()
                              .Having<Sedan>()
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponent(new Suv(suvId)))
                              .Run(out bool created);
        Assert.IsTrue(created);

        IEntity querySingle = repo.QuerySingle<Sedan>(c => c.Id == sedan1.Id);
        Assert.AreEqual(entity1.Id, querySingle.Id);
        Assert.AreEqual(sedan1, entity1.GetComponent<Sedan>());
        Assert.AreEqual(suvId, entity1.GetComponent<Suv>().Id);

        suvId = "B";
        IEntity entity2 = repo.CreateOrUpdate()
                              .Having<Sedan>()
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponent(new Suv(suvId)))
                              .Run(out bool created2);
        Assert.IsFalse(created2);

        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
        Assert.AreEqual(suvId, entity2.GetComponent<Suv>().Id);
    }

    [Test]
    public void Having_ByComponentPredicate()
    {
        IEcsRepo repo = getNewRepo();
        
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");

        string suvId = "A";

        IEntity entity1 = repo.CreateOrUpdate().Having<Sedan>(s => s.Id == "A")
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponent(new Suv(suvId)))
                              .Run();

        IEntity entity2 = repo.CreateOrUpdate().Having<Sedan>(s => s.Id == "A")
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponent(new Suv(suvId)))
                              .Run();

        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
        Assert.AreEqual(suvId, entity2.GetComponent<Suv>().Id);
    }

    [Test]
    public void Having_ByAndEntityAndData()
    {
        IEcsRepo repo = getNewRepo();
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");
        Location location1 = new(1, 1, 1);

        string suvId = "A";

        IEntity entity1 = repo.CreateOrUpdate().Having<Sedan>((c, e) => e.GetComponent<Location>() != null && c.Id == "A")
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        IEntity entity2 = repo.CreateOrUpdate().Having<Sedan>((c, e) => e.GetComponent<Location>() != null && c.Id == "A")
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
        Assert.AreEqual(suvId, entity2.GetComponent<Suv>().Id);
    }

    [Test]
    public void Having_ByTags()
    {
        IEcsRepo repo = getNewRepo();
        
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");
        Location location1 = new(1, 1, 1);

        string suvId = "A";
        string tag = "fooBar";

        List<EntitiesCreatedEventArgs> l1 = new();
        repo.Events.TaggedEntitiesCreated[tag] += args => l1.Add(args);

        IEntity entity1 = repo.CreateOrUpdate().Having<Sedan>(new[] { tag })
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        IEntity entity2 = repo.CreateOrUpdate().Having<Sedan>(new[] { tag })
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
        Assert.AreEqual(suvId, entity2.GetComponent<Suv>().Id);
        Assert.IsTrue(entity1.HasTag(tag));
        Assert.IsTrue(entity2.HasTag(tag));

        Assert.That(() => l1.Count, Is.EqualTo(1).After(500, 100));

        Assert.IsTrue(l1[0].TryGetEventForEntity(entity1, out EntityCreatedEventArgs createdEventArgs));
        Assert.IsTrue(createdEventArgs.CreatedComponents.Length == 4);
        Assert.IsTrue(createdEventArgs.CreatedComponents.Any(c => c.Component.Data is EntityTags tags && tags.Contains(tag)));
    }

    [Test]
    public void Having_ByMultipleTypes()
    {
        IEcsRepo repo = getNewRepo();
        
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");
        Location location1 = new(1, 1, 1);

        string suvId = "A";

        IEntity entity1 = repo.CreateOrUpdate().Having(new[] { typeof(Sedan) }, e => e.HasComponent<Location>())
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        IEntity entity2 = repo.CreateOrUpdate().Having(new[] { typeof(Sedan) }, e => e.HasComponent<Location>())
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
        Assert.AreEqual(suvId, entity2.GetComponent<Suv>().Id);
    }

    [Test]
    public void Having_BySingleType()
    {
        IEcsRepo repo = getNewRepo();
        
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");
        Location location1 = new(1, 1, 1);

        string suvId = "A";

        IEntity entity1 = repo.CreateOrUpdate().Having(typeof(Sedan))
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        IEntity entity2 = repo.CreateOrUpdate().Having(typeof(Sedan))
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();
        Assert.AreEqual(entity1.Id, entity2.Id);
        Assert.AreEqual(sedan2, entity2.GetComponent<Sedan>());
        Assert.AreEqual(suvId, entity2.GetComponent<Suv>().Id);
    }

    [Test]
    public void On()
    {
        IEcsRepo repo = getNewRepo();
        IEntityLookupBucket<string> bucket = repo.CreateLookupBucket(e => e.HasComponent<Sedan>(), e => e.GetComponent<Sedan>().Id);

        Sedan sedan1 = new("A");
        Location location1 = new(1, 1, 1);

        string suvId = "A";

        IEntity entity1 = repo.CreateOrUpdate().Having(typeof(Sedan))
                              .CreationComponents(sedan1)
                              .WhenEither(e => e.SetComponents(new Suv(suvId), location1))
                              .Run();

        Assert.IsTrue(bucket.Contains(sedan1.Id));
        Assert.AreEqual(sedan1.Id, entity1.GetComponent<Sedan>().Id);
    }

    [Test]
    public void Query_moreThanOneThrows()
    {
        IEcsRepo repo = getNewRepo();
        
        Sedan sedan1 = new("A");
        Sedan sedan2 = new("B");

        string suvId = "A";

        IEntity _ = repo.CreateOrUpdate().Having<ICar>()
                              .WhenCreated(e => e.SetComponent(sedan1))
                              .WhenExists(e => e.SetComponent(sedan2))
                              .WhenEither(e => e.SetComponent(new Suv(suvId)))
                              .Run();

        // create another entity with ICar component
        repo.CreateWithComponents(sedan2);

        suvId = "B";
        Assert.Throws(typeof(EcsException), () => repo.CreateOrUpdate().Having<ICar>()
                                                      .WhenCreated(e => e.SetComponent(sedan1))
                                                      .WhenExists(e => e.SetComponent(sedan2))
                                                      .WhenEither(e => e.SetComponent(new Suv(suvId)))
                                                      .Run());
    }

    [Test]
    public void MultiThreadTests()
    {
        IEcsRepo repo = getNewRepo();
        int updatedA = 0;
        int updatedB = 0;
        int updatedCounter = 0;

        Sedan c1 = new("A");
        Sedan c2 = new("B");

        repo.Events.ComponentUpdated[typeof(Suv)] += a =>
        {
            Interlocked.Add(ref updatedCounter, 1);
            string id = ((Suv)a.Component.Data).Id;
            if (id == "A")
            {
                Interlocked.Add(ref updatedA, 1);
            }
            else
            {
                Interlocked.Add(ref updatedB, 1);
            }
        };

        
        Thread[] threads =
        {
            new(() => update(c1, repo)),
            new(() => update(c2, repo))
        };

        foreach (Thread thread in threads)
        {
            thread.Name = "UpdaterThread";
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            bool join = thread.Join(10000);
            Assert.IsTrue(join);
        }

        Assert.That(() => updatedCounter, Is.EqualTo(20000).After(1000, 500));
        Assert.AreEqual(updatedA, updatedB);
        Assert.AreEqual(updatedCounter, updatedA + updatedB);
    }

    private void update(Sedan sedan, IEcsRepo repo)
    {
        Suv suv = new(sedan.Id);
        for (int i = 0; i < 10000; i++)
        {
            repo.CreateOrUpdate().Having<Sedan>(s => s.Id.Equals(sedan.Id))
             .WhenCreated(e =>
             {
                 e.AddTag("foobar");
                 e.SetComponent(sedan);
             })
             .WhenEither(e => e.SetComponent(suv))
             .Run();
        }
    }

    private IEcsRepo getNewRepo()
    {
        IEcsRepo ecsRepo = m_factory.Create();
        return ecsRepo;
    }
}