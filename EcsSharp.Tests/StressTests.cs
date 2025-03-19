using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using EcsSharp.Helpers;
using EcsSharp.Logging;
using EcsSharp.Logging.BuiltIn;
using EcsSharp.StructComponents;
using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class StressTests
    {
        private IEcsRepo m_ecsRepo;

        private static readonly ICommonLog s_log = CommonLogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        public StressTests()
        {
            CommonLogManager.InitLogProvider(new ConsoleCommonLogsAdapter { RootLoggerLevel = CommonLogLevel.Info });
        }

        private ConcurrentStack<double> m_sedanSystemAvg        = new ConcurrentStack<double>();
        private ConcurrentStack<double> m_sedanSystemWithBucketAvg = new ConcurrentStack<double>();

        [Test]
        [Explicit]
        public void MultiThread_update_Queries_deletes()
        {
            m_ecsRepo = new DefaultEcsRepoFactory().Create();

            int deleteCounter = 0;
            int createdCounter = 0;
            int updatedCounter = 0;
            m_ecsRepo.Events.GlobalDeleted += a => Interlocked.Add(ref deleteCounter,  1);
            m_ecsRepo.Events.GlobalCreated += a => Interlocked.Add(ref createdCounter, 1);
            m_ecsRepo.Events.GlobalUpdated += a => Interlocked.Add(ref updatedCounter, 1);

            CancellationTokenSource cts = new CancellationTokenSource();
            

            Thread[] threads =
            {
                new Thread(() => Writer(cts.Token, 0)),
                //new Thread(() => sedanSystemWithBucket(cts.Token, 100)),
                //new Thread(() => sedanSystemWithBucket(cts.Token, 200)),
                
            };

            for (int i = 0; i < threads.Length; i++)
            {
                Thread thread = threads[i];
                thread.Name = $"UpdaterThread_{i}";
                thread.Start();
            }

            Thread.Sleep(10000);
            cts.Cancel();
            foreach (Thread thread in threads)
            {
                bool join = thread.Join(30000);
                Assert.IsTrue(join);
            }

            s_log.Info($"Runtime : 10000 seconds");
            s_log.Info($"Total Updates : {updatedCounter}");
            s_log.Info($"Total update average with lookup table - with : {string.Join("\r\n", m_sedanSystemWithBucketAvg)}");
            
        }

        private void Writer(CancellationToken ctsToken, int i)
        {
            List<IEntity> l = new List<IEntity>();
            for (int j = 0; j < m_entities; j++)
            {
                IEntity? e = m_ecsRepo.Create([new Sedan(j.ToString())], ["foobar"]);
                l.Add(e);
            }

            List<TimeSpan> times = new List<TimeSpan>();
            while (!ctsToken.IsCancellationRequested)
            {
                foreach (IEntity e in l)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    ModifiedTime mt = DateTime.UtcNow;
                    e.SetComponents(new Location(0, 0, 0), mt);
                    times.Add(sw.Elapsed);
                }
            }

            s_log.InfoFormat("Thread work complete");
            double average = times.Select(t => t.TotalMilliseconds).Average();
            m_sedanSystemAvg.Push(average);
        }

        void sedanSystem(CancellationToken cancellationToken ,int startRange)
        {
            List<TimeSpan> times = new List<TimeSpan>();
            while (!cancellationToken.IsCancellationRequested)
            {
                Stopwatch sw = Stopwatch.StartNew();
                int randomCar = r.Next(startRange, startRange + m_entities);
                IEntity e = m_ecsRepo.QuerySingle<Sedan>(s => s.Id == randomCar.ToString());
                if (e == null)
                {
                    e = m_ecsRepo.Create([new Sedan(randomCar.ToString())], ["foobar"]);
                }

                
                ModifiedTime mt = DateTime.UtcNow;
                e.SetComponents(new Location(0, 0, 0), mt);
                times.Add(sw.Elapsed);
            }

            s_log.InfoFormat("Thread work complete");
            double average = times.Select(t => t.TotalMilliseconds).Average();
            m_sedanSystemAvg.Push(average);
        }

        private int m_entities = 20;
        void sedanSystemWithBucket(CancellationToken cancellationToken ,int startRange)
        {
            List<TimeSpan> times = new List<TimeSpan>();
            IEntityLookupBucket<string> entityLookupBucket = m_ecsRepo.CreateLookupBucket(e => e.HasComponent<Sedan>(), e => e.GetComponent<Sedan>().Id);
            while (!cancellationToken.IsCancellationRequested)
            {
                Stopwatch sw = Stopwatch.StartNew();
                
                int randomCar = r.Next(startRange, startRange + m_entities);
                IEntity e = entityLookupBucket.GetOrCreate(randomCar.ToString(),
                                                                  (r) => r.Create([new Sedan(randomCar.ToString())], ["foobar"]));
                
                ModifiedTime mt = DateTime.UtcNow;
                e.SetComponents(new Location(0, 0, 0), mt);
                times.Add(sw.Elapsed);
            }

            s_log.InfoFormat("Thread work complete");
            double average = times.Select(t => t.TotalMilliseconds).Average();
            m_sedanSystemWithBucketAvg.Push(average);
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
    }
}