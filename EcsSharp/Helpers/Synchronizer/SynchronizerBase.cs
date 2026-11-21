using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EcsSharp.Helpers.Synchronizer
{
    public abstract class SynchronizerBase
    {
        private static readonly ICommonLog s_log = CommonLogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        public string Id { get; }
        public abstract int TasksCount { get; }

        protected readonly SynchronizerConfig Config;

        private readonly   object             m_instancesSync = new object();

        private readonly Dictionary<MethodInfo, int> m_instanceCount = new Dictionary<MethodInfo, int>();
        protected IInvocationContextFactory                  invocationContextFactory { get; }

        public Thread InvocationThread { get; private set; }
        public bool IsInInvocationContext => Thread.CurrentThread == InvocationThread;
        protected SynchronizerBase(SynchronizerConfig config)
        {
            config.VerifyConfig();
            Config = config;
            Id = string.IsNullOrEmpty(config.Id)?Guid.NewGuid().ToString():config.Id;
            invocationContextFactory = config.UseObjectPool ? new PooledInvocationContextFactory() : new SimpleInvocationContextFactory();            
        }

        public Task<T> QueueInvocation<T>(Func<T> invocation, InvocationOptions options = null)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            T invocationWrapper()
            {
                T result = invocation();
                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                }

                return result;
            }

            bool shouldInvoke = checkShouldInvoke(invocation, options);

            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();

            if (shouldInvoke)
            {
                InvocationContext<T> invocationContext = invocationContextFactory.GetObject(taskCompletionSource,
                                                                                            invocationWrapper,
                                                                                            DateTime.UtcNow,
                                                                                            invocation.Target,
                                                                                            invocation.Method,
                                                                                            options);
                return doEnqueue(invocationContext);
            }

            taskCompletionSource.SetCanceled();
            return taskCompletionSource.Task;
        }

        public Task QueueInvocation(Action invocation, InvocationOptions options = null)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            bool shouldInvoke = checkShouldInvoke(invocation, options);
            
            TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();

            if (shouldInvoke)
            {
                object voidWrapper()
                {
                    invocation();
                    return null;
                }

                InvocationContext<object> invocationContext = invocationContextFactory.GetObject(taskCompletionSource,
                                                                                              voidWrapper,
                                                                                              DateTime.UtcNow,
                                                                                              invocation.Target,
                                                                                              invocation.Method,
                                                                                              options);
                return doEnqueue(invocationContext);
            }

            taskCompletionSource.SetCanceled();
            return taskCompletionSource.Task;
        }

        public Task<T> QueueInvocation<T>(Func<Task<T>> invocation, InvocationOptions options = null)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            bool shouldInvoke = checkShouldInvoke(invocation, options);

            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();

            if (shouldInvoke)
            {
                T invocationWrapper()
                {
                    Task<T> task = invocation();
                    if (task == null)
                    {
                        throw new InvalidOperationException("Invocation returned null task.");
                    }

                    return task.GetAwaiter().GetResult();
                }

                InvocationContext<T> invocationContext = invocationContextFactory.GetObject(taskCompletionSource,
                                                                                            invocationWrapper,
                                                                                            DateTime.UtcNow,
                                                                                            invocation.Target,
                                                                                            invocation.Method,
                                                                                            options);
                return doEnqueue(invocationContext);
            }

            taskCompletionSource.SetCanceled();
            return taskCompletionSource.Task;
        }

                public bool TryQueueInvocation<T>(Func<T> invocation, InvocationOptions options = null)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            T invocationWrapper()
            {
                T result = invocation();
                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                }

                return result;
            }

            return tryQueueInvocation(invocationWrapper, invocation, options);
        }

        public bool TryQueueInvocation(Action invocation, InvocationOptions options = null)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            object invocationWrapper()
            {
                invocation();
                return null;
            }

            return tryQueueInvocation(invocationWrapper, invocation, options);
        }

        public bool TryQueueInvocation<T>(Func<Task<T>> invocation, InvocationOptions options = null)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            T invocationWrapper()
            {
                Task<T> task = invocation();
                if (task == null)
                {
                    throw new InvalidOperationException("Invocation returned null task.");
                }

                return task.GetAwaiter().GetResult();
            }

            return tryQueueInvocation(invocationWrapper, invocation, options);
        }

        private bool tryQueueInvocation<T>(Func<T> invocationWrapper,
                                           Delegate invocation,
                                           InvocationOptions options)
        {
            if (!checkShouldInvoke(invocation, options))
            {
                return false;
            }

            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();
            InvocationContext<T> invocationContext = invocationContextFactory.GetObject(taskCompletionSource,
                                                                                         invocationWrapper,
                                                                                         DateTime.UtcNow,
                                                                                         invocation.Target,
                                                                                         invocation.Method,
                                                                                         options);
            try
            {
                if (doTryEnqueue(invocationContext))
                {
                    return true;
                }
            }
            catch
            {
                onComplete(invocationContext);
                invocationContextFactory.Release(invocationContext);
                throw;
            }

            onComplete(invocationContext);
            invocationContextFactory.Release(invocationContext);
            return false;
        }


        private bool checkShouldInvoke(Delegate invocation, InvocationOptions options)
        {
            bool shouldInvoke = true;

            if (options?.ConcurrentInvocationInstances == null)
            {
                return true;
            }

            lock(m_instancesSync)
            {
                if (m_instanceCount.TryGetValue(invocation.Method, out int count))
                {
                    if (count >= options.ConcurrentInvocationInstances)
                    {
                        shouldInvoke = false;
                    }
                }

                if (shouldInvoke)
                {
                    count++;
                    m_instanceCount[invocation.Method] = count;
                }
            }

            return shouldInvoke;
        }

        public int WaitForAllToComplete(TimeSpan timeout)
        {
            int sleepMs = (int)timeout.TotalMilliseconds;
            int count = TasksCount;
            if (count > 0)
            {
                return waitForCompletion(sleepMs);
            }

            return 0;
        }

        public Task WaitForAllToComplete(TimeSpan? timeout = null)
        {
            if(timeout.HasValue)
            {
                return Task.Run(() => WaitForAllToComplete(timeout.Value));
            }
            else
            {
                return Task.Run(() => WaitForAllToComplete(TimeSpan.FromMinutes(10)));
            }
        }

        private int waitForCompletion(int sleepMs)
        {
            while (sleepMs > 0 && TasksCount > 0)
            {
                int toSleep = Math.Min(sleepMs, 500);
                Thread.Sleep(toSleep);
                sleepMs -= toSleep;
            }

            return TasksCount;
        }

        protected abstract Task<T> doEnqueue<T>(InvocationContext<T> invocationContext);

        protected virtual bool doTryEnqueue<T>(InvocationContext<T> invocationContext)
        {
            doEnqueue(invocationContext);
            return true;
        }

        protected void onComplete(IInvocationContext invocationContext)
        {
            InvocationOptions options = invocationContext.InvocationOptions;
            updateInstances(invocationContext, options);
        }

        private void updateInstances(IInvocationContext invocationContext, InvocationOptions options)
        {
            MethodInfo method = invocationContext.InvocationMethod;
            if (options?.ConcurrentInvocationInstances == null)
            {
                return;
            }

            lock(m_instancesSync)
            {
                if (m_instanceCount.TryGetValue(method, out int count))
                {
                    count--;
                    if (count <= 0)
                    {
                        m_instanceCount.Remove(method); // Clean up
                    }
                    else
                    {
                        m_instanceCount[method] = count;
                    }
                }
            }
        }

        protected void logWaitTime(IInvocationContext invocationContext, DateTime timeStart, DateTime timeEnd, TimeSpan timeOut, string msg)
        {
            TimeSpan span = timeEnd - timeStart;
            if (span >= timeOut)
            {
                string message = $"{msg} : {span} : {invocationContext}";
                s_log.Warn(message);
            }
        }

        protected void SetInvocationThread(Thread thread)
        {
            InvocationThread = thread;
        }
    }
}