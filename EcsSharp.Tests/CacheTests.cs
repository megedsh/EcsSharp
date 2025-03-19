using System;
using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class ConditionalSetTests
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        public ConditionalSetTests()
        {
            CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter(CommonLogLevel.Debug));
        }

        [Test]
        public void ComponentNotExists_enableOnNotExists()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            IEntity entity1 = repo.Create();
            bool res = entity1.ConditionalSet(sedan1, (o, n) => string.Compare(o.Id, n.Id, StringComparison.Ordinal) < 0);
            Assert.AreEqual(sedan1, entity1.GetComponent<Sedan>());
            Assert.IsTrue(res);
        }

        [Test]
        public void ComponentNotExists_disableOnNotExists()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan1 = new Sedan("A");
            IEntity entity1 = repo.Create();
            bool res = entity1.ConditionalSet(sedan1,
                                                             (o, n) => string.Compare(o.Id, n.Id, StringComparison.Ordinal) < 0,
                                                             false);
            Assert.IsNull(entity1.GetComponent<Sedan>());
            Assert.IsFalse(res);
        }

        [Test]
        public void ComponentNotExists_ConditionTrue()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.CreateWithComponents(sedan1);
            bool res = entity1.ConditionalSet(sedan2,
                                                             (o, n) => string.Compare(o.Id, n.Id, StringComparison.Ordinal) < 0,
                                                             false);
            Assert.AreEqual(sedan2, entity1.GetComponent<Sedan>());
            Assert.IsTrue(res);
        }

        [Test]
        public void ComponentNotExists_ConditionFalse()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan1 = new Sedan("A");
            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.CreateWithComponents(sedan2);
            bool res = entity1.ConditionalSet(sedan1,
                                                             (o, n) => string.Compare(o.Id, n.Id, StringComparison.Ordinal) < 0,
                                                             false);
            Assert.AreEqual(sedan2, entity1.GetComponent<Sedan>());
            Assert.IsFalse(res);
        }

        [Test]
        public void ConditionWithFactory_PredicateTrue()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.CreateWithComponents(sedan2);
            bool res = entity1.ConditionalSet<Sedan>(current => current != null,
                                                                    current => new Sedan(((char)(current.Id[0] + 1)).ToString()), false);
            IEntity q = repo.QuerySingle<Sedan>();
            Assert.AreEqual("C", q.GetComponent<Sedan>().Id);
            Assert.IsTrue(res);
        }

        [Test]
        public void ConditionWithFactory_PredicateFalse()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.CreateWithComponents(sedan2);
            bool res = entity1.ConditionalSet<Sedan>(current => current.Id == "AAAA",
                                                                    current => new Sedan(((char)(current.Id[0] + 1)).ToString()), false);
            IEntity q = repo.QuerySingle<Sedan>();
            Assert.AreEqual("B", q.GetComponent<Sedan>().Id);
            Assert.IsFalse(res);
        }

        [Test]
        public void UpdateWithFactory_ComponentExists()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.CreateWithComponents(sedan2);
            entity1.UpdateComponent<Sedan>(current => new Sedan(((char)(current.Id[0] + 1)).ToString()), false);
            IEntity q = repo.QuerySingle<Sedan>();
            Assert.AreEqual("C", q.GetComponent<Sedan>().Id);

        }

        [Test]
        public void UpdateWithFactory_ComponentNotExists_DoNotSet()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.Create();
            entity1.UpdateComponent<Sedan>(current => new Sedan(((char)(current.Id[0] + 1)).ToString()), false);
            IEntity q = repo.QuerySingle<Sedan>();
            Assert.IsNull(q);
        }

        [Test]
        public void UpdateWithFactory_ComponentNotExists_DoSet()
        {
            IEcsRepo repo = getNewRepo();

            Sedan sedan2 = new Sedan("B");

            IEntity entity1 = repo.Create();
            entity1.UpdateComponent<Sedan>(current =>
            {
                if (current == null)
                {
                    return new Sedan("A");
                }

                return new Sedan(((char)(current.Id[0] + 1)).ToString());
            });
            IEntity q = repo.QuerySingle<Sedan>();
            Assert.AreEqual("A", q.GetComponent<Sedan>().Id);
        }

        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}