using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace EcsSharp.Tests;

[TestFixture]
public class PerformanceLoadTests
{
    private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

    [Test]
    [Explicit("Performance/load test. Run manually to estimate capacity on target hardware.")]
    [Category("Performance")]
    public void SingleThread_Baseline_ReadWriteDelete_Throughput()
    {
        IEcsRepo repo = m_factory.Create();

        const int entitiesCount = 50_000;
        const int writeOps = 100_000;

        Stopwatch createWatch = Stopwatch.StartNew();
        for (int i = 0; i < entitiesCount; i++)
        {
            repo. Create([new Sedan(i.ToString()), new Location(i, i, i)],[$"t{i}"]);
        }

        createWatch.Stop();

        IEntity[] entities = repo.QueryAll().ToArray();
        Assert.That(entities.Length, Is.EqualTo(entitiesCount));

        Stopwatch writeWatch = Stopwatch.StartNew();
        for (int i = 0; i < writeOps; i++)
        {
            IEntity entity = entities[i % entities.Length];
            entity.SetComponent(new Location(i, i + 1, i + 2));
        }

        writeWatch.Stop();

        Stopwatch queryWatch = Stopwatch.StartNew();
        for (int i = 0; i < entitiesCount; i++)
        {
            IEntityCollection? entity = repo.QueryByTags($"t{i}");
            Assert.That(entity, Is.Not.Null);
        }

        queryWatch.Stop();

        Stopwatch deleteWatch = Stopwatch.StartNew();
        for (int i = 0; i < entitiesCount; i++)
        {
          repo.DeleteEntitiesWithTag($"t{i}");
        }
        deleteWatch.Stop();

        int remaining = repo.QueryAll().Count;

        TestContext.Progress.WriteLine($"[SingleThread] create={entitiesCount} in {createWatch.ElapsedMilliseconds} ms ({entitiesCount / createWatch.Elapsed.TotalSeconds:N0} ops/s)");
        TestContext.Progress.WriteLine($"[SingleThread] write={writeOps} in {writeWatch.ElapsedMilliseconds} ms ({writeOps / writeWatch.Elapsed.TotalSeconds:N0} ops/s)");
        TestContext.Progress.WriteLine($"[SingleThread] query={entitiesCount} in {queryWatch.ElapsedMilliseconds} ms ({entitiesCount / queryWatch.Elapsed.TotalSeconds:N0} ops/s)");
        TestContext.Progress.WriteLine($"[SingleThread] delete-even in {deleteWatch.ElapsedMilliseconds} ms, remaining={remaining}");

        Assert.That(remaining, Is.EqualTo(0));
    }

    [TestCase(2, 2, 8)]
    [TestCase(4, 4, 10)]
    [TestCase(8, 8, 10)]
    [Explicit("Performance/load test. Run manually to estimate contention and tail latency.")]
    [Category("Performance")]
    public async Task Concurrent_ReadWrite_LoadProfile(int writers, int readers, int durationSeconds)
    {
        IEcsRepo repo = m_factory.Create();

        const int seededEntities = 20_000;
        for (int i = 0; i < seededEntities; i++)
        {
            repo.CreateWithComponents(new Sedan(i.ToString()), new Location(i, i, i));
        }

        IEntity[] entities = repo.QueryAll().ToArray();
        string[] indexedEntityIds = entities.Select(e => e.Id).ToArray();

        long writeOps = 0;
        long readOps = 0;
        ConcurrentQueue<long> sampledWriteLatenciesTicks = new();
        ConcurrentQueue<long> sampledReadLatenciesTicks = new();
        List<Exception> exceptions = new();

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(durationSeconds));

        Task[] writerTasks = Enumerable.Range(0, writers)
                                       .Select(worker => Task.Run(() =>
                                       {
                                           int localCounter = 0;
                                           Random random = new(worker + 17);
                                           while (!cts.IsCancellationRequested)
                                           {
                                               try
                                               {
                                                   IEntity entity = entities[random.Next(entities.Length)];
                                                   Stopwatch sw = Stopwatch.StartNew();
                                                   entity.SetComponent(new Location(localCounter, localCounter + 1, localCounter + 2));
                                                   sw.Stop();

                                                   if ((localCounter & 0x3F) == 0)
                                                   {
                                                       sampledWriteLatenciesTicks.Enqueue(sw.ElapsedTicks);
                                                   }

                                                   localCounter++;
                                                   Interlocked.Increment(ref writeOps);
                                               }
                                               catch (Exception e)
                                               {
                                                   lock (exceptions)
                                                   {
                                                       exceptions.Add(e);
                                                   }
                                                   break;
                                               }
                                           }
                                       }, cts.Token))
                                       .ToArray();

        Task[] readerTasks = Enumerable.Range(0, readers)
                                       .Select(worker => Task.Run(() =>
                                       {
                                           int localCounter = 0;
                                           Random random = new(worker + 5000);
                                           while (!cts.IsCancellationRequested)
                                           {
                                               try
                                               {
                                                    string id = indexedEntityIds[random.Next(indexedEntityIds.Length)];
                                                   Stopwatch sw = Stopwatch.StartNew();
                                                    IEntity entity = repo.QuerySingle(id);
                                                   sw.Stop();

                                                   if (entity == null)
                                                   {
                                                       continue;
                                                   }

                                                   if ((localCounter & 0x3F) == 0)
                                                   {
                                                       sampledReadLatenciesTicks.Enqueue(sw.ElapsedTicks);
                                                   }

                                                   localCounter++;
                                                   Interlocked.Increment(ref readOps);
                                               }
                                               catch (Exception e)
                                               {
                                                   lock (exceptions)
                                                   {
                                                       exceptions.Add(e);
                                                   }
                                                   break;
                                               }
                                           }
                                       }, cts.Token))
                                       .ToArray();

        await Task.WhenAll(writerTasks.Concat(readerTasks));

        Assert.That(exceptions, Is.Empty, "No exceptions are expected under load");
        Assert.That(writeOps, Is.GreaterThan(0));
        Assert.That(readOps, Is.GreaterThan(0));

        TimeSpan tickDuration = TimeSpan.FromSeconds(1.0 / Stopwatch.Frequency);
        (double writeP50, double writeP95, double writeP99) = calculatePercentilesMs(sampledWriteLatenciesTicks, tickDuration);
        (double readP50, double readP95, double readP99) = calculatePercentilesMs(sampledReadLatenciesTicks, tickDuration);

        double writesPerSecond = writeOps / (double)durationSeconds;
        double readsPerSecond = readOps / (double)durationSeconds;

        TestContext.Progress.WriteLine($"[Concurrent] writers={writers}, readers={readers}, duration={durationSeconds}s");
        TestContext.Progress.WriteLine($"[Concurrent] writes={writeOps:N0} ({writesPerSecond:N0} ops/s), p50={writeP50:F3}ms p95={writeP95:F3}ms p99={writeP99:F3}ms");
        TestContext.Progress.WriteLine($"[Concurrent] reads={readOps:N0} ({readsPerSecond:N0} ops/s), p50={readP50:F3}ms p95={readP95:F3}ms p99={readP99:F3}ms");
    }

    [Test]
    [Explicit("Performance/load test. Run manually to see scalability curve by writer count.")]
    [Category("Performance")]
    public async Task WriteScalabilityCurve_ByWorkerCount()
    {
        int[] workerCounts = { 1, 2, 4, 8, 16 };

        foreach (int workers in workerCounts)
        {
            IEcsRepo repo = m_factory.Create();
            const int entitiesCount = 20_000;
            for (int i = 0; i < entitiesCount; i++)
            {
                repo.CreateWithComponents(new Sedan(i.ToString()), new Location(i, i, i));
            }

            IEntity[] entities = repo.QueryAll().ToArray();
            long totalOps = 0;
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(6));

            Task[] tasks = Enumerable.Range(0, workers)
                                     .Select(worker => Task.Run(() =>
                                     {
                                         Random random = new(worker + 93);
                                         while (!cts.IsCancellationRequested)
                                         {
                                             IEntity entity = entities[random.Next(entities.Length)];
                                             entity.SetComponent(new Location(worker, worker + 1, worker + 2));
                                             Interlocked.Increment(ref totalOps);
                                         }
                                     }, cts.Token))
                                     .ToArray();

            await Task.WhenAll(tasks);
            double opsPerSecond = totalOps / 6d;
            TestContext.Progress.WriteLine($"[Scale] workers={workers}, writes={totalOps:N0}, throughput={opsPerSecond:N0} ops/s");

            Assert.That(totalOps, Is.GreaterThan(0));
        }
    }

    private static (double p50Ms, double p95Ms, double p99Ms) calculatePercentilesMs(IEnumerable<long> ticksSamples, TimeSpan tickDuration)
    {
        long[] sorted = ticksSamples.OrderBy(t => t).ToArray();
        if (sorted.Length == 0)
        {
            return (0, 0, 0);
        }

        double p50 = toMs(selectPercentile(sorted, 0.50), tickDuration);
        double p95 = toMs(selectPercentile(sorted, 0.95), tickDuration);
        double p99 = toMs(selectPercentile(sorted, 0.99), tickDuration);
        return (p50, p95, p99);

        static long selectPercentile(IReadOnlyList<long> ordered, double percentile)
        {
            int index = (int)Math.Ceiling(percentile * ordered.Count) - 1;
            if (index < 0)
            {
                index = 0;
            }

            if (index >= ordered.Count)
            {
                index = ordered.Count - 1;
            }

            return ordered[index];
        }

        static double toMs(long ticks, TimeSpan tickDuration) => ticks * tickDuration.TotalMilliseconds;
    }
}
