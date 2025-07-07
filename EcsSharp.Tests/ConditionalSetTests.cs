using System;

using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using NUnit.Framework;
using System.Linq;

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
            assertComponentExistsInCache(sedan1, entity1.CachedComponents);
        }

        [Test]
        public void MultiCachedComponentOnCreate()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            Suv suv1 = new Suv("A");
            IEntity entity1 = repo.EntityBuilder().WithComponents(sedan1, suv1).Build();
            Assert.AreEqual(2,      entity1.CachedComponents.Length);
            assertComponentExistsInCache(sedan1, entity1.CachedComponents);            
            assertComponentExistsInCache(suv1,   entity1.CachedComponents);
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

            assertComponentExistsInCache(sedan1, entity1.CachedComponents);
            Sedan sedan2 = new Sedan("B");
            entity1.SetComponent(sedan2);

            assertComponentExistsInCache(sedan2, entity1.CachedComponents);
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
            assertComponentExistsInCache(suv1, entity2.CachedComponents);
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
            assertComponentExistsInCache(suv1, entity2.CachedComponents);
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
            assertComponentExistsInCache(sedan1, entity2.CachedComponents);

            IEntity entity3 = repo.QuerySingle([typeof(Sedan), typeof(Suv)]);
            Assert.AreEqual(2,      entity3.CachedComponents.Length);
            assertComponentExistsInCache(sedan1, entity3.CachedComponents);
            assertComponentExistsInCache(suv1, entity3.CachedComponents);
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
                assertComponentExistsInCache(suv1, entityColl[i].CachedComponents);
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
                assertComponentExistsInCache(sedan1, entityColl[i].CachedComponents);
                assertComponentExistsInCache(suv1, entityColl[i].CachedComponents);
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
                assertComponentExistsInCache(sedan1, entityColl[i].CachedComponents);
                assertComponentExistsInCache(suv1, entityColl[i].CachedComponents);
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

            assertValueInCache<Sedan>((c) => c.Id == "A", e1.CachedComponents);            
            assertValueInCache<Sedan>((c) => c.Id == "B", e2.CachedComponents);

            Sedan refreshComponent = e1.RefreshComponent<Sedan>();
            Assert.AreEqual("B", refreshComponent.Id);
            assertValueInCache<Sedan>((c) => c.Id == "B", e1.CachedComponents);

            e2.SetComponent(new Sedan("C"));
            Component component = e1.RefreshComponent(typeof(Sedan));
            Assert.AreEqual("C", ((Sedan)component.Data).Id);
            
            assertValueInCache<Sedan>((c) => c.Id == "C", e1.CachedComponents);
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
            assertValueInCache<Sedan>((c)=>c.Id=="B",     e1.CachedComponents);
            assertValueInCache<Suv>((c) => c.Id == "B", e1.CachedComponents);
            
        }

        private void assertValueInCache<T>(Func<T, bool> assertationFunc, Component[] cache)
        {
            T? firstOrDefault = cache.Where(e => e.Data is T).Select(comp=>(T)comp.Data).FirstOrDefault();
            Assert.IsNotNull(firstOrDefault);
            Assert.IsTrue(assertationFunc(firstOrDefault!), $"Component {firstOrDefault} does not match the assertion");
        }

        private void assertComponentExistsInCache<T>(T comp, Component[] cache)
        {
            bool any = cache.Any(c =>
            {
                if (c.Data is T t)
                {
                    return t.Equals(comp);
                }
                return false;
            });

            Assert.IsTrue(any, $"Component {comp} not found in cache");
        }


        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}