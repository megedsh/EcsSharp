using System;
using System.Linq;
using System.Threading;
using EcsSharp.Helpers;
using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class MultiThreadTests
    {
        private IEcsRepo m_ecsRepo;

        private static readonly ICommonLog s_log = CommonLogManager.GetLogger(typeof(MultiThreadTests));

        public MultiThreadTests()
        {
            CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter { RootLoggerLevel = CommonLogLevel.Info });
        }

        [Test]
        public void MultiThread_update_Queries_deletes()
        {
            m_ecsRepo = new DefaultEcsRepoFactory().Create();

            int deleteCounter = 0;
            int createdCounter = 0;
            int updatedCounter = 0;
            m_ecsRepo.Events.GlobalDeleted += a => Interlocked.Add(ref deleteCounter,  1);
            m_ecsRepo.Events.GlobalCreated += a => Interlocked.Add(ref createdCounter, 1);

            m_ecsRepo.Events.ComponentUpdated[typeof(Location)] += a => Interlocked.Add(ref updatedCounter, 1);
            Sedan c1 = new Sedan("A");
            Sedan c2 = new Sedan("B");
            Sedan c3 = new Sedan("C");
            Sedan c4 = new Sedan("D");
            Sedan c5 = new Sedan("E");

            Sedan[] sedans = { c1, c2, c3, c4, c5 };

            Thread[] threads =
            {
                new Thread(() => update(sedans)),
                new Thread(() => update(sedans)),
                new Thread(() => update(sedans)),
                new Thread(() => update(sedans)),
                new Thread(() => update(sedans)),
                new Thread(() => delete(sedans)),
                new Thread(() => delete(sedans))
            };

            for (int i = 0; i < threads.Length; i++)
            {
                Thread thread = threads[i];
                thread.Name = $"UpdaterThread_{i}";
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                bool join = thread.Join(30000);
                Assert.IsTrue(join);
            }

            Assert.That(() => updatedCounter, Is.EqualTo(50000).After(1000, 500));

            int totalCounter = createdCounter - deleteCounter;

            IEntityCollection entityCollection = m_ecsRepo.QueryAll();
            ICar[] array = entityCollection.Select(e => e.GetComponent<ICar>()).ToArray();

            Assert.AreEqual(totalCounter, array.Length);
        }

        private void delete(Sedan[] sedans)
        {
            for (int i = 0; i < 10000; i++)
            {
                int randomCar = r.Next(0, 5);
                Sedan sedan = sedans[randomCar];
                m_ecsRepo.DeleteEntitiesByComponent<Sedan>(s => s.Id.Equals(sedan.Id));
            }
        }

        private readonly Random r = new Random();

        private void update(Sedan[] sedans)
        {
            for (int i = 0; i < 10000; i++)
            {
                int randomCar = r.Next(0, 5);
                Sedan sedan = sedans[randomCar];
                m_ecsRepo.CreateOrUpdate().Having<Sedan>(s => s.Id.Equals(sedan.Id))
                         .WhenCreated(e =>
                         {
                             e.AddTag("foobar");
                             e.SetComponent(sedan);
                         })
                         .WhenEither(e => e.SetComponent(new Location(0, 0, 0)))
                         .Run();
            }

            s_log.InfoFormat("Thread work complete");
        }

        [Test]
        public void MultiThread_CreateOrUpdate()
        {
            m_ecsRepo = new DefaultEcsRepoFactory().Create();

            ICreateOrUpdateBuilder createOrUpdateBuilder = m_ecsRepo.CreateOrUpdate();

            Sedan c1 = new Sedan("A");
            Sedan c2 = new Sedan("B");
            Sedan c3 = new Sedan("C");
            Sedan c4 = new Sedan("D");
            Sedan c5 = new Sedan("E");

            Sedan[] sedans = { c1, c2, c3, c4, c5 };

            Thread[] threads =
            {
                new Thread(() => createOurUpdate(sedans, createOrUpdateBuilder)){ Name = "UpdaterThread_1" },
                new Thread(() => createOurUpdate(sedans, createOrUpdateBuilder)) { Name = "UpdaterThread_2" },
                new Thread(() => createOurUpdate(sedans, createOrUpdateBuilder)) { Name = "UpdaterThread_3" },
                new Thread(() => createOurUpdate(sedans, createOrUpdateBuilder)) { Name = "UpdaterThread_4" },
                new Thread(() => createOurUpdate(sedans, createOrUpdateBuilder)) { Name = "UpdaterThread_5" },
            };

            threads.AsParallel()
                   .ForAll(t => t.Start());

            foreach (Thread thread in threads)
            {
                bool join = thread.Join(30000);
                Assert.IsTrue(join);
            }

            IEntityCollection entityCollection = m_ecsRepo.Query<Sedan>();
            Assert.AreEqual(5, entityCollection.Count);
            foreach (IEntity entity in entityCollection)
            {
                int counter = entity.GetComponent<int>();
                Assert.AreEqual(5,counter);
            }
        }

        private void createOurUpdate(Sedan[] sedans, ICreateOrUpdateBuilder createOrUpdateBuilder)
        {
            foreach (Sedan sedan in sedans)
            {
                createOrUpdateBuilder.Having<Sedan>(s => s.Id == sedan.Id)
                                     .WhenCreated(e => e.SetComponent(sedan))
                                     .WhenEither(updateCounterComponent)
                                     .Run();
            }
        }

        private void updateCounterComponent(IEntity e)
        {
            e.UpdateComponent<int>(c =>
            {
                Thread.Sleep(100);
                int res = c+1;
                return res;
            });
        }
    }
}