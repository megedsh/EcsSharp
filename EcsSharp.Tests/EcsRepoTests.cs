using System;
using System.Collections.Generic;
using System.Linq;
using EcsSharp.StructComponents;
using log4net.Config;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    [TestFixture]
    public class EcsRepoTests
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        public EcsRepoTests()
        {
            BasicConfigurator.Configure();
        }

        [Test]
        public void Repo_CreateEmpty_SetComponent()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity1 = repo.Create();
            IEntity entity2 = repo.Create();
            Sedan sedan = new Sedan();
            Suv suv = new Suv();
            entity1.SetComponent(sedan);
            entity2.SetComponent(suv);

            HashSet<IEntity> hashSet = repo.Query<ICar>().ToHashSet();
            Assert.AreEqual(2,          hashSet.Count);
            Assert.AreEqual(entity1.Id, repo.QuerySingle<Sedan>().Id);
            Assert.AreEqual(entity2.Id, repo.QuerySingle<Suv>().Id);

            int x = -1;
            ushort sh = (ushort)x;
        }

        [Test]
        public void Repo_SetComponents()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity = repo.Create();

            Sedan sedan = new Sedan();
            Suv suv = new Suv();

            object[] sedans =
            {
                sedan,
                suv
            };
            entity.SetComponents(sedans);

            IEntity carEntity = repo.QuerySingle<ICar>();

            Assert.AreEqual(2,         carEntity.GetComponents(typeof(ICar)).Length);
            Assert.AreEqual(2,         carEntity.GetComponents<ICar>().Length);
            Assert.AreEqual(entity.Id, repo.QuerySingle<Sedan>().Id);
            Assert.AreEqual(entity.Id, repo.QuerySingle<Suv>().Id);
        }

        [Test]
        public void Repo_CreateWithData()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan = new Sedan();
            Suv suv = new Suv();

            IEntity entity1 = repo.CreateWithComponents(sedan);
            IEntity entity2 = repo.CreateWithComponents(suv);

            HashSet<IEntity> hashSet = repo.Query(typeof(ICar)).ToHashSet();
            Assert.AreEqual(2,          hashSet.Count);
            Assert.AreEqual(entity1.Id, repo.QuerySingle(typeof(Sedan)).Id);
            Assert.AreEqual(entity2.Id, repo.QuerySingle(typeof(Suv)).Id);
        }

        [Test]
        public void Repo_Create_Update()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity1 = repo.Create();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            entity1.SetComponent(sedan1);
            entity1.SetComponent(sedan2);
            Assert.AreEqual(sedan2, repo.QuerySingle<Sedan>().GetComponent<Sedan>());
        }

        [Test]
        public void Repo_QueryByComponentData()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity1 = repo.Create();
            IEntity entity2 = repo.Create();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            entity1.SetComponent(sedan1);
            entity2.SetComponent(sedan2);

            IEntity querySingle = repo.QuerySingle<Sedan>(c => c.Id == sedan2.Id);
            Assert.AreEqual(entity2.Id, querySingle.Id);
        }

        [Test]
        public void Repo_QueryByComponentTypeAndData()
        {
            IEcsRepo repo = getNewRepo();

            Sedan car1 = new Sedan("A");
            Suv car2 = new Suv("B");
            Sedan car3 = new Sedan("C");

            Location location1 = new Location(1, 1, 1);
            Location location2 = new Location(2, 2, 2);

            IEntity entity1 = repo.CreateWithComponents(car1, location1);
            repo.CreateWithComponents(car2, location2);
            repo.CreateWithComponents(car3);

            IEntityCollection entityCollection = repo.Query<ICar>((c, e) => e.GetComponent<Location>() != null && c is Sedan);
            Assert.AreEqual(1,          entityCollection.Count);
            Assert.AreEqual(entity1.Id, entityCollection.First().Id);
        }

        [Test]
        public void Repo_DeleteEntity()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity1 = repo.Create();
            IEntity entity2 = repo.Create();
            IEntity entity3 = repo.Create();

            Sedan sedan1 = new Sedan();
            Sedan sedan2 = new Sedan();

            entity1.SetComponent(sedan1);
            entity2.SetComponent(sedan2);

            string entity1Id = entity1.Id;
            string entity2Id = entity2.Id;
            string entity3Id = entity3.Id;

            repo.Delete(entity1);   // by entity
            repo.Delete(entity3Id); // by id

            IEntity queryEntity1 = repo.QuerySingle(entity1Id);
            IEntity queryEntity2 = repo.QuerySingle(entity2Id);
            IEntity queryEntity3 = repo.QuerySingle(entity3Id);

            Assert.IsNull(queryEntity1);
            Assert.IsNotNull(queryEntity2);
            Assert.IsNull(queryEntity3);
        }

        [Test]
        public void Repo_DeleteEntiesByComponent()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");
            Sedan sedan3 = new Sedan("C");
            Suv suv1 = new Suv("C");

            IEntity entity1 = repo.CreateWithComponents(sedan1);
            IEntity entity2 = repo.CreateWithComponents(sedan2);
            IEntity entity3 = repo.CreateWithComponents(sedan3);
            entity3.AddTag("foo");
            IEntity entity4 = repo.CreateWithComponents(suv1);

            repo.DeleteEntitiesByComponent<ICar>(c => c.Id == "A");

            Assert.IsNull(repo.QuerySingle(entity1.Id));
            Assert.IsNotNull(repo.QuerySingle(entity2.Id));
            Assert.IsNotNull(repo.QuerySingle(entity3.Id));
            Assert.IsNotNull(repo.QuerySingle(entity4.Id));

            repo.DeleteEntitiesByComponent<ICar>(c => c.Id == "C", new[] { "foo" });

            Assert.IsNotNull(repo.QuerySingle(entity2.Id));
            Assert.IsNull(repo.QuerySingle(entity3.Id));
            Assert.IsNotNull(repo.QuerySingle(entity4.Id));

            repo.DeleteEntitiesByComponent<ICar>();
            Assert.AreEqual(0, repo.QueryAll().Count);
        }

        [Test]
        public void Repo_DeleteEntitiesByTag()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");
            Sedan sedan3 = new Sedan("C");
            Suv suv1 = new Suv("C");

            IEntity entity1 = repo.CreateWithComponents(sedan1);
            IEntity entity2 = repo.CreateWithComponents(sedan2);
            entity2.AddTag("foo");
            IEntity entity3 = repo.CreateWithComponents(sedan3);
            entity3.AddTag("bar");
            IEntity entity4 = repo.CreateWithComponents(suv1);
            entity4.AddTag("bar");

            repo.DeleteEntitiesWithTag("foo", "bar");

            Assert.IsNotNull(repo.QuerySingle(entity1.Id));
            Assert.IsNull(repo.QuerySingle(entity2.Id));
            Assert.IsNull(repo.QuerySingle(entity3.Id));
            Assert.IsNull(repo.QuerySingle(entity4.Id));
        }

        [Test]
        public void Repo_GetOrCreate()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");

            IEntity entity = repo.CreateOrGetByComponent(sedan1);
            Assert.AreEqual("A", entity.GetComponent<Sedan>().Id);

            IEntity entity1 = repo.CreateOrGetByComponent(sedan1);

            Assert.AreEqual(entity.Id, entity1.Id);
        }

        [Test]
        public void Repo_GetOrCreate_predicate()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            IEntity b = repo.CreateWithComponents(sedan2);
            IEntity a = repo.CreateOrGetByComponent(sedan1, s => s.Id == "A");
            Assert.AreEqual("A", a.GetComponent<Sedan>().Id);
            Assert.AreEqual("B", b.GetComponent<Sedan>().Id);
        }

        [Test]
        public void Repo_GetOrCreateMultipleThrows()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");

            repo.CreateWithComponents(sedan1);
            repo.CreateWithComponents(sedan1);

            Assert.Throws(typeof(EcsException), () => repo.CreateOrGetByComponent(sedan1));
        }

        [Test]
        public void CreateOrGetWithId()
        {
            IEcsRepo repo = getNewRepo();
            int createdCounter = 0;
            int updatedCounter = 0;
            repo.Events.ComponentCreated[typeof(Sedan)] += _ => createdCounter++;
            repo.Events.ComponentUpdated[typeof(Sedan)] += _ => updatedCounter++;

            IEntity e = repo.CreateOrGetWithId("foo-bar");
            e.SetComponent(new Sedan());
            Assert.That(() => createdCounter, Is.EqualTo(1).After(500, 100));
            Assert.That(() => updatedCounter, Is.EqualTo(1).After(500, 100));
            Assert.AreEqual("foo-bar", e.Id);

            repo.CreateOrGetWithId("foo-bar");
            e.SetComponent(new Sedan());
            Assert.That(() => updatedCounter, Is.EqualTo(2).After(500, 100));
            Assert.That(() => createdCounter, Is.EqualTo(1).After(500));
        }


        [Test]
        public void CreateWithStructComponent()
        {
            IEcsRepo repo = getNewRepo();
            int createdCounter = 0;
            int updatedCounter = 0;
            repo.Events.ComponentCreated[typeof(IDateTimeComponent)] += _ => createdCounter++;
            repo.Events.ComponentUpdated[typeof(IDateTimeComponent)] += _ => updatedCounter++;

            IEntity e = repo.CreateOrGetWithId("foo-bar");
            ModifiedTime modifiedTime = DateTime.UtcNow;
            CreateTime createTime =DateTime.UtcNow;
            e.SetComponents(modifiedTime,createTime); 
            Assert.That(() => createdCounter, Is.EqualTo(2).After(500, 100));
            Assert.That(() => updatedCounter, Is.EqualTo(2).After(500, 100));
            Assert.AreEqual("foo-bar", e.Id);
        }


        //[Test]
        //public void CreateOrUpdateWithId()
        //{
        //    IEcsRepo repo = getNewRepo();
        //    int createdCounter = 0;
        //    int updatedCounter = 0;
        //    repo.Events.ComponentCreated[typeof(ICar)] += _ => createdCounter++;
        //    repo.Events.ComponentUpdated[typeof(ICar)] += _ => updatedCounter++;

        //    IEntity e = repo.CreateOrUpdateWithId("foo-bar", new Sedan(), new Suv());
        //    Assert.That(() => createdCounter, Is.EqualTo(2).After(500, 100));
        //    Assert.That(() => updatedCounter, Is.EqualTo(2).After(500, 100));
        //    Assert.AreEqual("foo-bar", e.Id);

        //    repo.CreateOrUpdateWithId("foo-bar", new Sedan(), new Suv());
        //    Assert.That(() => updatedCounter, Is.EqualTo(4).After(500, 100));
        //    Assert.That(() => createdCounter, Is.EqualTo(2).After(500));
        //}

        [Test]
        public void SetWithVersion()
        {
            IEcsRepo repo = getNewRepo();

            IEntity e = repo.CreateOrGetWithId("foo-bar");
            e.SetComponent(new Sedan());
            e.SetComponent(new Sedan());
            e.SetComponent(new Sedan());

            ulong version = e.GetAllComponents().Where(c => c.Data.GetType().Equals(typeof(Sedan))).FirstOrDefault().Version;
            Assert.AreEqual(3, version);
            e.SetWithVersion(new Sedan(), 55);
            ulong version2 = e.GetAllComponents().Where(c => c.Data.GetType().Equals(typeof(Sedan))).FirstOrDefault().Version;
            Assert.AreEqual(55, version2);

            e.SetWithVersion(new Sedan(), 0);
            ulong version3 = e.GetAllComponents().Where(c => c.Data.GetType().Equals(typeof(Sedan))).FirstOrDefault().Version;
            Assert.AreEqual(0, version3);
        }

        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}