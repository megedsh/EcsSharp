using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using EcsSharp.Distribute;
using EcsSharp.Extensions.Json;
using EcsSharp.Storage;
using EcsSharp.StructComponents;

using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class SystemTextConverterTests
    {

        private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

        private readonly EcsPackageConverter m_subject = new EcsPackageConverter();
        private readonly StructComponentJsonConverter structComponentJsonConverter = new StructComponentJsonConverter();

        [Test]
        public void DeserializationTest()
        {
            JsonSerializerOptions options = new ();
            options.Converters.Add(m_subject);
            IEcsRepo repo = getNewRepo();
            IEntity entity = repo.CreateWithComponents(new Sedan("aa"), new Location(1, 2, 3), new Suv("cc"));
            entity.AddTag("foo", "bar");
            IEntity entity1 = repo.CreateWithComponents(new Sedan("bb"), new Location(4, 5, 6), new Suv("dd"));
            IEntity entity2 = repo.Create();

            EcsPackage p = new EcsPackage();
            p.AddAllComponents(entity);
            p.AddAllComponents(entity1);
            p.AddDeletedEntity(entity2);
            p.AddDeleteByTag("foo1", "bar1");

            string json = JsonSerializer.Serialize(p, options);
            EcsPackage? deserializeObject = JsonSerializer.Deserialize<EcsPackage>(json, options);

            Assert.IsNotNull(deserializeObject);

            assertEntityUpdated(entity, deserializeObject);
            assertEntityUpdated(entity1, deserializeObject);
            assertEntityDeleted(entity2, deserializeObject);
        }

        [Test]
        public void DeserializationBigVersionTest()
        {
            JsonSerializerOptions options = new ();
            options.Converters.Add(m_subject);
            IEcsRepo repo = getNewRepo();
            IEntity entity = repo.Create();

            EcsPackage p = new EcsPackage();
            p.AddComponent(entity, new Component(ulong.MaxValue, new Location(1, 1, 1)));


            string json = JsonSerializer.Serialize(p, options);
            EcsPackage? deserializeObject = JsonSerializer.Deserialize<EcsPackage>(json, options);

            Assert.IsNotNull(deserializeObject);
            Assert.IsTrue( deserializeObject.Updated.Values.SelectMany(p => p.Values).Any(c => c.Version.Equals(ulong.MaxValue)));
        }

        [Test]
        public void StructComponentTest()
        {
            JsonSerializerOptions options = new ();
            options.Converters.Add(m_subject);
            options.Converters.Add(structComponentJsonConverter);

            IEcsRepo repo = getNewRepo();
            CreateTime createTime = new CreateTime(){Data = DateTime.UtcNow};
            ModifiedTime modifiedTime = new ModifiedTime() { Data = DateTime.Now };
            NameComponent name = "foo";


            IEntity entity = repo.EntityBuilder().WithComponents(name, modifiedTime, createTime).Build();
            
            EcsPackage p = new EcsPackage();
            p.AddAllComponents(entity);

            string json = JsonSerializer.Serialize(p, options);
            EcsPackage? deserializeObject = JsonSerializer.Deserialize<EcsPackage>(json, options);

            Assert.IsNotNull(deserializeObject);
            assertEntityUpdated(entity, deserializeObject);
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

                if(c.Data is IStructComponent structComponent)
                {
                    Assert.AreEqual(structComponent.UntypedData, ((IStructComponent)component.Data).UntypedData);
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

    

    public class NameComponent : StringComponent<NameComponent>
    {
        public static implicit operator NameComponent(string d) =>
            new NameComponent
            {
                Data = d
            };
    }
}