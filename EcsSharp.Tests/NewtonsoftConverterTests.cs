using System;
using System.Collections.Generic;
using System.Linq;
using EcsSharp.Distribute;
using EcsSharp.Extensions.Newtonsoft;
using EcsSharp.Storage;
using Newtonsoft.Json;

using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class NewtonsoftConverterTests
    {
        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        private readonly EcsPackageConverter m_subject = new EcsPackageConverter();

        [Test]
        public void DeserializationTest()
        {
            IEcsRepo repo = getNewRepo();
            IEntity entity = repo.CreateWithComponents(new Sedan("aa"), new Location(1, 2, 3), new Suv("cc"));
            entity.AddTag("foo","bar");
            IEntity entity1 = repo.CreateWithComponents(new Sedan("bb"), new Location(4, 5, 6), new Suv("dd"));
            IEntity entity2 = repo.Create();

            EcsPackage p = new EcsPackage();
            p.AddAllComponents(entity);
            p.AddAllComponents(entity1);
            p.AddDeletedEntity(entity2);

            string json = JsonConvert.SerializeObject(p, m_subject);
            EcsPackage? deserializeObject = JsonConvert.DeserializeObject<EcsPackage>(json, m_subject);

            Assert.IsNotNull(deserializeObject);

            assertEntityUpdated(entity, deserializeObject);
            assertEntityUpdated(entity1, deserializeObject);

            assertEntityDeleted(entity2, deserializeObject);
        }

        private void assertEntityDeleted(IEntity entity, EcsPackage deserializeObject)
        {
            Assert.IsTrue(deserializeObject.Deleted.Contains(entity.Id));
        }

        private void assertEntityUpdated(IEntity entity, EcsPackage deserializeObject)
        {
            Dictionary<string, Component>? components = deserializeObject.Updated.Where(p => p.Key.Equals(entity.Id))
                                                                         .Select(p => p.Value)
                                                                         .FirstOrDefault();
            Assert.IsNotNull(components);
            foreach (Component c in entity.GetAllComponents().Where(c=>c.Data is not EntityTags))
            {
                Component component = components[c.Data.GetType().FullName];
                if (c.Data is ICar car)
                {
                    Assert.AreEqual(car.Id, ((ICar)component.Data).Id);
                }

                if (c.Data is Location location)
                {
                    Assert.AreEqual(location, (Location)component.Data);
                }
            }

            string[] actualTags = deserializeObject.EntityTags.GetValueOrDefault(entity.Id)??Array.Empty<string>();
            CollectionAssert.AreEquivalent(entity.Tags,actualTags);
        }

        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = m_factory.Create();
            return ecsRepo;
        }
    }
}