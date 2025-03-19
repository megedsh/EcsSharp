using System.Linq;
using System.Threading;
using EcsSharp.Distribute;
using EcsSharp.Helpers;
using log4net.Config;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class LookupBucketTests
    {
        public LookupBucketTests()
        {
            BasicConfigurator.Configure();
        }

        [Test]
        public void Bucket_Sanity()
        {
            IEcsRepo repo = getNewRepo();

            IEntityLookupBucket<string> bucket = repo.CreateLookupBucket(e => e.HasComponent<ICar>(), e => e.GetComponent<ICar>().Id);

            IEntity entity1 = repo.CreateWithComponents(new Sedan("A"));
            IEntity entity2 = repo.CreateWithComponents(new Sedan("B"));

            Assert.IsTrue(bucket.Contains("A"));
            Assert.IsTrue(bucket.TryGetEntity("B", out IEntity entity));
            Assert.AreEqual(entity2, entity);

            repo.Delete(entity1);
            Assert.IsFalse(bucket.Contains("A"));
        }

        [Test]
        public void Multiple_Buckets()
        {
            IEcsRepo repo = getNewRepo();

            IEntityLookupBucket<string> bucket1 = repo.CreateLookupBucket(e => e.HasComponent<Sedan>(), e => e.GetComponent<Sedan>().Id);
            IEntityLookupBucket<string> bucket2 = repo.CreateLookupBucket(e => e.HasComponent<Suv>(), e => e.GetComponent<Suv>().Id);
            
            IEntity entity1 = repo.CreateWithComponents(new Sedan("A"));
            IEntity entity2 = repo.CreateWithComponents(new Sedan("B"));

            IEntity entity3 = repo.CreateWithComponents(new Suv("C"));
            IEntity entity4 = repo.CreateWithComponents(new Suv("D"));

            Assert.IsTrue(bucket1.Contains("A"));
            Assert.IsTrue(bucket1.TryGetEntity("B", out IEntity entity));
            Assert.AreEqual(entity2, entity);

            Assert.IsTrue(bucket2.Contains("C"));
            Assert.IsTrue(bucket2.TryGetEntity("D", out IEntity entityD));
            Assert.AreEqual(entity4, entityD);

            repo.Delete(entity1);
            Assert.IsFalse(bucket1.Contains("A"));
            repo.Delete(entity3);
            Assert.IsFalse(bucket2.Contains("C"));
        }


        [Test]
        public void Bucket_InBatch()
        {
            IEcsRepo repo = getNewRepo();

            IEntityLookupBucket<string> bucket = repo.CreateLookupBucket(e => e.HasComponent<ICar>(), e => e.GetComponent<ICar>().Id);

            repo.CreateWithComponents(new Sedan("A"));
            IEntity _ = repo.CreateWithComponents(new Sedan("B"));

            repo.BatchUpdate(_ =>
            {
                if (bucket.TryGetEntity("B", out IEntity entity))
                {
                    entity.AddTag("foo");
                }

                if (bucket.TryGetEntity("A", out IEntity entityA))
                {
                    entityA.AddTag("foo");
                }
            });
        }

        [Test]
        public void MultiThread_AddAndRemoveFromBucket()
        {
            IEcsRepo repo = getNewRepo();
            IEntityLookupBucket<string> bucket = repo.CreateLookupBucket(e => e.HasComponent<ICar>(), e => e.GetComponent<ICar>().Id);

            Thread[] threads =
            {
                new Thread(() => update(repo, 0, 1000)){Name = "UpdaterThread_1"},
                new Thread(() => update(repo, 1000, 1000)){Name = "UpdaterThread_2"},
                new Thread(() => update(repo, 2000, 1000)){Name = "UpdaterThread_3"},
                new Thread(() => update(repo, 3000, 1000)){Name = "UpdaterThread_4"},
            };

            threads.AsParallel().ForAll(t=>t.Start());
            

            foreach (Thread thread in threads)
            {
                bool join = thread.Join(10000);
                Assert.IsTrue(join);
            }

            Assert.AreEqual(4000, bucket.Count);
            for (int i = 0; i < bucket.Count; i++)
            {
                bucket.Contains(i.ToString());
            }

            Thread[] removeThreads =
            {
                new Thread(() => remove(repo, bucket, 0, 1000)),
                new Thread(() => remove(repo, bucket, 1000, 1000)),
                new Thread(() => remove(repo, bucket, 2000, 1000)),
                new Thread(() => remove(repo, bucket, 3000, 1000)),
            };
            foreach (Thread thread in removeThreads)
            {
                thread.Name = "RemoveThread";
                thread.Start();
            }

            foreach (Thread thread in removeThreads)
            {
                bool join = thread.Join(10000);
                Assert.IsTrue(join);
            }

            Assert.AreEqual(0, bucket.Count);
        }

        [Test]
        public void RepoWithBucket_MergeTest()
        {
            IEcsRepo repo1 = getNewRepo();
            IEcsRepo repo2 = getNewRepo();

            IEntityLookupBucket<string> bucket = repo2.CreateLookupBucket(e => e.HasComponent<ICar>(), e => e.GetComponent<ICar>().Id);

            IEntity entity1 = repo1.CreateWithComponents(new Sedan("a"),  new Location(1, 1, 1));
            IEntity entity2 = repo1.CreateWithComponents(new Sedan("b"),  new Location(2, 2, 2));
            IEntity entity3 = repo1.CreateWithComponents(new Sedan("c"),  new Location(3, 3, 3));
            EcsPackage pack1 = new EcsPackage().AddAllComponents(entity1, entity2, entity3);

            repo2.MergePackage(pack1);
            Assert.AreEqual(3,bucket.Count);
            Assert.IsTrue(bucket.TryGetEntity("a", out IEntity _));
            Assert.IsTrue(bucket.TryGetEntity("b", out IEntity _));
            Assert.IsTrue(bucket.TryGetEntity("c", out IEntity _));
            EcsPackage deleted = new EcsPackage().AddDeletedEntity(entity1, entity2);
            repo2.MergePackage(deleted);

            Assert.AreEqual(1,bucket.Count);
            Assert.IsFalse(bucket.TryGetEntity("a", out IEntity _));
            Assert.IsFalse(bucket.TryGetEntity("b", out IEntity _));
            Assert.IsTrue(bucket.TryGetEntity("c", out IEntity _));
        }

        [Test]
        public void Bucket_CreatedAfterEntitiesAdded()
        {
            IEcsRepo repo = getNewRepo();

            IEntity entity1 = repo.CreateWithComponents(new Sedan("A"));

            IEntityLookupBucket<string> bucket = repo.CreateLookupBucket(e => e.HasComponent<ICar>(), e => e.GetComponent<ICar>().Id);

            IEntity entity2 = repo.CreateWithComponents(new Sedan("B"));

            Assert.IsTrue(bucket.Contains("A"));
            Assert.IsTrue(bucket.TryGetEntity("B", out IEntity entity));
            Assert.AreEqual(entity2, entity);

            repo.Delete(entity1);
            Assert.IsFalse(bucket.Contains("A"));
        }


        
        [Test]
        public void Bucket_GetOrCreate()
        {
            IEcsRepo repo = getNewRepo();
            Sedan sedan = new Sedan("A");
            Suv suv = new Suv("B");
            IEntityLookupBucket<string> bucket = repo.CreateLookupBucket(e => e.HasComponent<ICar>(), e => e.GetComponent<ICar>().Id);

            IEntity e1  = bucket.GetOrCreate("A", (r) => r.CreateWithComponents(sedan));
            IEntity e2  = bucket.GetOrCreate("B", (r) => r.CreateWithComponents(suv));

            Assert.IsTrue(bucket.Contains("A"));
            Assert.IsTrue(bucket.Contains("B"));

            bucket.TryGetEntity("A", out IEntity e3);
            Assert.AreEqual(e3,e1);
            Assert.AreEqual(e1.GetComponent<ICar>(),e3.GetComponent<ICar>());

            bucket.TryGetEntity("B", out IEntity e4);
            Assert.AreEqual(e4,                      e2);
            Assert.AreEqual(e2.GetComponent<ICar>(), e4.GetComponent<ICar>());

            repo.Delete(e1);
            repo.Delete(e2);
            Assert.IsFalse(bucket.Contains("A"));
            Assert.IsFalse(bucket.Contains("B"));
        }


        private void remove(IEcsRepo repo, IEntityLookupBucket<string> bucket, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (bucket.TryGetEntity((i + start).ToString(), out IEntity e))
                {
                    repo.Delete(e);
                }
            }
        }

        private void update(IEcsRepo repo, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                repo.CreateWithComponents(new Sedan((i + start).ToString()));
            }
        }

        private IEcsRepo getNewRepo()
        {
            IEcsRepo ecsRepo = new DefaultEcsRepoFactory().Create();
            return ecsRepo;
        }
    }
}