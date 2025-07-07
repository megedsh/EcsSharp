using NUnit.Framework;
using System.Linq;

namespace EcsSharp.Tests
{
    public class CloneTests
    {
        [Test]
        public void CloneEntity()
        {
            IEcsRepo repo = new DefaultEcsRepoFactory().Create();
            IEntity entity = repo.EntityBuilder()
                                 .WithComponents(new Suv("A"))
                                 .WithTags("test")
                                 .Build();

            IEntity clone = entity.Clone();

            Suv suv = new Suv("B");
            entity.SetComponent(suv);
            Assert.AreEqual( suv, entity.GetComponent<Suv>());
            Assert.AreNotEqual(suv, clone.GetComponent<Suv>());
            Assert.AreEqual(suv, clone.RefreshComponent<Suv>());
        }

        [Test]
        public void CloneEntityCollection()
        {
            IEcsRepo repo = new DefaultEcsRepoFactory().Create();
            IEntity e1 = repo.EntityBuilder()
                                 .WithComponents(new Suv("A"))
                                 .WithTags("test")
                                 .Build();
            IEntity e2 = repo.EntityBuilder()
                             .WithComponents(new Suv("B"))
                             .WithTags("test")
                             .Build();
            IEntity e3 = repo.EntityBuilder()
                             .WithComponents(new Suv("C"))
                             .WithTags("test")
                             .Build();
            IEntity e4 = repo.EntityBuilder()
                             .WithComponents(new Suv("D"))
                             .WithTags("test")
                             .Build();

            IEntityCollection collection = repo.QueryByTags("test");

            Assert.AreEqual(4, collection.Count);

            Assert.IsTrue(collection.Any(e => e.Equals(e1)));
            Assert.IsTrue(collection.Any(e => e.Equals(e2)));
            Assert.IsTrue(collection.Any(e => e.Equals(e3)));
            Assert.IsTrue(collection.Any(e => e.Equals(e4)));

            var clonedCollection = collection.Clone();

            Assert.IsTrue(clonedCollection.Any(e => e.Equals(e1)));
            Assert.IsTrue(clonedCollection.Any(e => e.Equals(e2)));
            Assert.IsTrue(clonedCollection.Any(e => e.Equals(e3)));
            Assert.IsTrue(clonedCollection.Any(e => e.Equals(e4)));

            Suv suv = new Suv("E");

            IEntity entity = collection.First(e => e.Equals(e1));
            entity.SetComponent(suv);
            Assert.IsTrue(collection.Any(e => e.Equals(e1)));
            IEntity clonedEntity = clonedCollection.First(e => e.Id.Equals(e1.Id));
            Assert.AreEqual(suv, entity.GetComponent<Suv>());
            Assert.AreNotEqual(suv, clonedEntity.GetComponent<Suv>());
            Assert.AreEqual(suv, clonedEntity.RefreshComponent<Suv>());
        }
    }
}