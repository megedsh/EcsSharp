using System.Collections.Generic;
using EcsSharp.Events.EventArgs;
using log4net.Config;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    [TestFixture]
    public class EcsRepoBatchTests
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        public EcsRepoBatchTests()
        {
            BasicConfigurator.Configure();
        }

        [Test]
        public void BatchUpdate_Test()
        {
            IEcsRepo repo = getNewRepo();

            IEntity forDelete = repo.CreateWithComponents(new Suv("11"));

            List<EntitiesCreatedEventArgs> createdList = new List<EntitiesCreatedEventArgs>();
            List<EntitiesUpdatedEventArgs> updatedList = new();
            List<EntitiesDeletedEventArgs> deletedList = new();
            repo.Events.GlobalCreated += args => createdList.Add(args);
            repo.Events.GlobalUpdated += args => updatedList.Add(args);
            repo.Events.GlobalDeleted += args => deletedList.Add(args);



            repo.BatchUpdate(r =>
            {
                IEntity entity = r.Create();
                entity.AddTag("foo");
                entity.SetComponent(new Sedan("aaa"));
                entity.SetComponent(new Suv("bbb"));


                IEntity entity2 = r.Create();
                entity2.AddTag("bar");
                entity2.SetComponent(new Sedan("aaa"));
                entity2.SetComponent(new Suv("bbb"));

                r.Delete(forDelete);
            });

            Assert.That(()=>createdList.Count,Is.EqualTo(1).After(500,100));
            EntitiesCreatedEventArgs createdArgs1 = createdList[0];
            Assert.AreEqual(2,createdArgs1.Count);
            

            Assert.That(()=>updatedList.Count,Is.EqualTo(1).After(500,100));
            EntitiesUpdatedEventArgs updatedArgs1 = updatedList[0];
            Assert.AreEqual(2,updatedArgs1.Count);

            Assert.That(()=>deletedList.Count,Is.EqualTo(1).After(500,100));
            EntitiesDeletedEventArgs deletedArgs1 = deletedList[0];
            Assert.AreEqual(1,deletedArgs1.Count);
        }

        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}