using System.Linq;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    [TestFixture]
    public class QueryTests
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

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
            IEntity entity2 = repo.CreateWithComponents(car2, location2);
            IEntity entity3 = repo.CreateWithComponents(car3);

            IEntityCollection entityCollection = repo.Query<ICar>((c, e) => e.GetComponent<Location>() != null && c is Sedan);
            Assert.AreEqual(1,          entityCollection.Count);
            Assert.AreEqual(entity1.Id, entityCollection.First().Id);
        }

        [Test]
        public void Query_multiple_ByComponentData()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.CreateWithComponents(sedan1);
            IEntity entity2 = repo.CreateWithComponents(sedan2);

            IEntityCollection? results = repo.Query<Sedan>();
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(c => c.Id == entity1.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity2.Id));
        }

        [Test]
        public void Query_multiple_ByComponentDataWithPredicate()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.CreateWithComponents(sedan1);
            IEntity entity2 = repo.CreateWithComponents(sedan2);
            IEntity entity3 = repo.CreateWithComponents(sedan1);

            IEntityCollection? results = repo.Query<Sedan>(c => c.Id == "A");
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(c => c.Id == entity1.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity3.Id));
        }

        [Test]
        public void Query_multipleTypes()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            Location l1 = new Location(1, 1, 1);
            Location l2 = new Location(2, 2, 2);
            Location l3 = new Location(3, 3, 3);

            IEntity entity1 = repo.CreateWithComponents(sedan1, l1);
            IEntity entity2 = repo.CreateWithComponents(sedan1, l2);
            IEntity entity3 = repo.CreateWithComponents(l3);
            IEntity entity4 = repo.CreateWithComponents(sedan2);

            IEntityCollection? results = repo.Query(new[]
            {
                typeof(ICar),
                typeof(Location)
            });
            Assert.AreEqual(4, results.Count);
            Assert.IsTrue(results.Any(c => c.Id == entity1.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity2.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity3.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity4.Id));
        }

        [Test]
        public void Query_multipleTypesWithPredicate()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            Location l1 = new Location(1, 1, 1);
            Location l2 = new Location(2, 2, 2);
            Location l3 = new Location(3, 3, 3);

            IEntity entity1 = repo.CreateWithComponents(sedan1, l1);
            IEntity entity2 = repo.CreateWithComponents(sedan2, l2);
            IEntity entity3 = repo.CreateWithComponents(l3);
            IEntity entity4 = repo.CreateWithComponents(sedan2);

            IEntityCollection? results = repo.Query(new[]
            {
                typeof(ICar),
                typeof(Location)
            }, e => e.HasComponent<Sedan>() && e.GetComponent<Sedan>().Id == "B");
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(c => c.Id == entity2.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity4.Id));
        }

        [Test]
        public void Query_Ids()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity1 = repo.Create();
            IEntity entity2 = repo.Create();
            IEntity entity3 = repo.Create();
            IEntity entity4 = repo.Create();

            string?[] strings = new[]
                                {
                                    entity1,
                                    entity2,
                                    entity3,
                                    entity4
                                }
                                .Select(e => e.Id)
                                .Concat(new[]
                                {
                                    "foo-bar",
                                    null
                                })
                                .ToArray();

            IEntityCollection? results = repo.Query(strings);

            Assert.AreEqual(4, results.Count);
            Assert.IsTrue(results.Any(c => c.Id == entity1.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity2.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity3.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity4.Id));
        }

        [Test]
        public void Query_Entities()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity1 = repo.Create();
            IEntity entity2 = repo.Create();
            IEntity entity3 = repo.Create();
            IEntity entity4 = repo.Create();

            IEntityCollection? results = repo.Query(entity1, entity2, entity3);

            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Any(c => c.Id == entity1.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity2.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity3.Id));
        }

        [Test]
        public void Query_singleWithEntityPredicate()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            Location l1 = new Location(1, 1, 1);
            Location l2 = new Location(2, 2, 2);
            Location l3 = new Location(3, 3, 3);

            IEntity entity1 = repo.CreateWithComponents(sedan1, l1);
            IEntity entity2 = repo.CreateWithComponents(sedan2, l2);
            IEntity entity3 = repo.CreateWithComponents(l3);
            IEntity entity4 = repo.CreateWithComponents(sedan2);

            IEntity? results = repo.QuerySingle<ICar>((c, e) => e.HasComponent<Location>() && c.Id == "B");
            Assert.AreEqual(entity2.Id, results.Id);
        }

        [Test]
        public void Query_singleWithMultipleTypes()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");
            Suv suv1 = new Suv("B");

            Location l1 = new Location(1, 1, 1);
            Location l2 = new Location(2, 2, 2);
            Location l3 = new Location(3, 3, 3);

            IEntity entity1 = repo.CreateWithComponents(sedan1, l1);
            IEntity entity2 = repo.CreateWithComponents(sedan2, l2);
            IEntity entity3 = repo.CreateWithComponents(l3);
            IEntity entity4 = repo.CreateWithComponents(sedan2);
            IEntity entity5 = repo.CreateWithComponents(suv1);

            Assert.Throws(typeof(EcsException), () => repo.QuerySingle(new[] { typeof(Sedan) }, e => e.HasComponent<Location>()));

            IEntity? results = repo.QuerySingle(new[] { typeof(Sedan) },
                                                e => e.HasComponent<Location>() && e.GetComponent<Sedan>().Id == "B");
            Assert.AreEqual(entity2.Id, results.Id);

            IEntity res2 = repo.QuerySingle(new[] { typeof(Suv) });
            Assert.AreEqual(entity5.Id, res2.Id);
        }

        //removed cache of component

        //[Test]
        //public void Query_Conflict()
        //{
        //    IEcsRepo repo = getNewRepo();

        //    Sedan sedan1 = new Sedan("A");
        //    Sedan sedan2 = new Sedan("B");
        //    Sedan sedan3 = new Sedan("C");

        //    IEntity entity1 = repo.CreateWithComponents(sedan1);

        //    IEntity entityWithCache = repo.QuerySingle<Sedan>(c => c.Id == "A");

        //    Sedan e1 = entityWithCache.GetComponent<Sedan>();

        //    entity1.SetComponent(sedan2);

        //    Sedan e2 = entityWithCache.GetComponent<Sedan>();

        //    Assert.AreEqual(sedan1, e1);
        //    Assert.AreEqual(e1, e2);

        //    entityWithCache.SetComponent(sedan3);
        //}

        [Test]
        public void QueryAll()
        {
            IEcsRepo repo = getNewRepo();

            Sedan car1 = new Sedan("A");
            Suv car2 = new Suv("B");
            Sedan car3 = new Sedan("C");

            IEntity entity1 = repo.CreateWithComponents(car1);
            IEntity entity2 = repo.CreateWithComponents(car2);
            IEntity entity3 = repo.CreateWithComponents(car3);

            IEntityCollection results = repo.QueryAll();
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results.Any(c => c.Id == entity1.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity2.Id));
            Assert.IsTrue(results.Any(c => c.Id == entity3.Id));
        }

        [Test]
        public void QueryByTags()
        {
            IEcsRepo repo = getNewRepo();
            IEntity e1 = repo.EntityBuilder().WithTags("foo", "bar").Build();
            IEntity e2 = repo.EntityBuilder().WithTags("foo").Build();

            IEntityCollection q1 = repo.QueryByTags("foo");
            Assert.AreEqual(2,  q1.Count);
            Assert.AreEqual(e1, q1[0]);
            Assert.AreEqual(e2, q1[1]);

            IEntityCollection q2 = repo.QueryByTags("foo", "bar");
            Assert.AreEqual(2,  q1.Count);
            Assert.AreEqual(e1, q1[0]);
            Assert.AreEqual(e2, q1[1]);

            IEntityCollection q3 = repo.QueryByTags("bar");
            Assert.AreEqual(1,  q3.Count);
            Assert.AreEqual(e1, q3[0]);
        }

        [Test]
        public void QuerySingleByTags()
        {
            IEcsRepo repo = getNewRepo();
            IEntity e1 = repo.EntityBuilder().WithTags("foo", "bar").Build();
            IEntity e2 = repo.EntityBuilder().WithTags("foo").Build();

            IEntity q1 = repo.QuerySingleByTags("foo");
            Assert.NotNull(q1);
            IEntity q2 = repo.QuerySingleByTags("foo", "bar");
            Assert.NotNull(q2);
            IEntity q3 = repo.QuerySingleByTags("bar");
            Assert.NotNull(q3);

            IEntity q4 = repo.QuerySingleByTags("foobar");
            Assert.Null(q4);
        }

        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}