using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class CacheTests
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        public CacheTests()
        {
            CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter());
        }

        [Test]
        public void SingleCachedComponentOnCreate()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            IEntity entity1 = repo.EntityBuilder().WithComponents(sedan1).Build();
            Assert.AreEqual(sedan1, entity1.CachedComponents[0].Data);
        }

        [Test]
        public void MultiCachedComponentOnCreate()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity entity1 = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();
            Assert.AreEqual(2,      entity1.CachedComponents.Length);
            Assert.AreEqual(sedan1, entity1.CachedComponents[0].Data);
            Assert.AreEqual(suv1,   entity1.CachedComponents[1].Data);
        }

        [Test]
        public void SingleCachedComponentOnSet()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity entity1 = repo.EntityBuilder().WithComponents(sedan1).Build();
            Assert.AreEqual(1, entity1.CachedComponents.Length);
            IEntity e = entity1.SetComponent(suv1);
            Assert.AreEqual(2, e.CachedComponents.Length);
            Assert.AreEqual(2, entity1.CachedComponents.Length);

            Assert.AreEqual(sedan1, entity1.CachedComponents[0].Data);
            Sedan sedan2 = new Sedan("B");
            entity1.SetComponent(sedan2);

            Assert.AreEqual(sedan2, entity1.CachedComponents[0].Data);
        }

        [Test]
        public void QueryById()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity entity1 = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();

            IEntity entity2 = repo.QuerySingle(entity1.Id);

            // should also cache the component
            Assert.IsTrue(entity2.HasComponent<Suv>());
            Assert.AreEqual(1,    entity2.CachedComponents.Length);
            Assert.AreEqual(suv1, entity2.CachedComponents[0].Data);
        }

        [Test]
        public void QuerySingleByComponent()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity _ = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();
            IEntity entity2 = repo.QuerySingle<Suv>();
            Assert.AreEqual(1,    entity2.CachedComponents.Length);
            Assert.AreEqual(suv1, entity2.CachedComponents[0].Data);
        }

        [Test]
        public void QuerySingleByComponents()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity _ = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();
            IEntity entity2 = repo.QuerySingle([typeof(Sedan)]);
            Assert.AreEqual(2,    entity2.CachedComponents.Length);
            Assert.AreEqual(sedan1, entity2.CachedComponents[0].Data);

            IEntity entity3 = repo.QuerySingle([typeof(Sedan), typeof(Suv)]);
            Assert.AreEqual(2,      entity3.CachedComponents.Length);
            Assert.AreEqual(sedan1, entity3.CachedComponents[0].Data);
            Assert.AreEqual(suv1, entity3.CachedComponents[1].Data);
        }

        [Test]
        public void QueryMultipleByComponent()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity _ = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();
            IEntity __ = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();

            var entityColl = repo.Query<Suv>();
            Assert.AreEqual(2,entityColl.Count);
            for (var i = 0; i < entityColl.Count; i++)
            {
                Assert.AreEqual(1,    entityColl[i].CachedComponents.Length);
                Assert.AreEqual(suv1, entityColl[i].CachedComponents[0].Data);
            }
        }

        [Test]
        public void QueryMultipleByMultipleComponents()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity _ = repo.EntityBuilder().WithComponents(sedan1,  suv1).Build();
            IEntity __ = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();

            var entityColl = repo.Query([typeof(Sedan), typeof(Suv)]);
            Assert.AreEqual(2,entityColl.Count);
            for (var i = 0; i < entityColl.Count; i++)
            {
                Assert.AreEqual(2,    entityColl[i].CachedComponents.Length);
                Assert.AreEqual(sedan1, entityColl[i].CachedComponents[0].Data);
                Assert.AreEqual(suv1, entityColl[i].CachedComponents[1].Data);
            }
        }

        [Test]
        public void QueryMultipleByInterface()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity _ = repo.EntityBuilder().WithComponents(sedan1,  suv1).Build();
            IEntity __ = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();

            var entityColl = repo.Query<ICar>();
            Assert.AreEqual(2,entityColl.Count);
            for (var i = 0; i < entityColl.Count; i++)
            {
                Assert.AreEqual(2,      entityColl[i].CachedComponents.Length);
                Assert.AreEqual(sedan1, entityColl[i].CachedComponents[0].Data);
                Assert.AreEqual(suv1,   entityColl[i].CachedComponents[1].Data);
            }
        }



        [Test]
        public void RefreshComponent()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity e1 = repo.EntityBuilder().WithComponents(sedan1).Build();
            IEntity e2 = repo.QuerySingle(e1.Id);

            Assert.AreEqual(e1,e2);

            e2.SetComponent(new Sedan("B"));

            Assert.AreEqual("A", ((Sedan)e1.CachedComponents[0].Data).Id);
            Assert.AreEqual("B", ((Sedan)e2.CachedComponents[0].Data).Id);

            Sedan refreshComponent = e1.RefreshComponent<Sedan>();
            Assert.AreEqual("B", refreshComponent.Id);
            Assert.AreEqual("B", ((Sedan)e1.CachedComponents[0].Data).Id);

            e2.SetComponent(new Sedan("C"));
            Component component = e1.RefreshComponent(typeof(Sedan));
            Assert.AreEqual("C", ((Sedan)component.Data).Id);
            Assert.AreEqual("C", ((Sedan)e1.CachedComponents[0].Data).Id);
        }

        [Test]
        public void RefreshMultipleComponent()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity e1 = repo.EntityBuilder().WithComponents(sedan1,suv1).Build();
            IEntity e2 = repo.QuerySingle(e1.Id);

            Assert.AreEqual(e1,e2);

            e2.SetComponent(new Sedan("B"));
            e2.SetComponent(new Suv("B"));

            ICar[] refreshComponents = e1.RefreshComponents<ICar>();

            Assert.AreEqual("B", ((Sedan)e1.CachedComponents[0].Data).Id);
            Assert.AreEqual("B", ((Suv)e1.CachedComponents[1].Data).Id);
        }


        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}