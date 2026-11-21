#if NETSTANDARD2_0

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EcsSharp.Helpers.Synchronizer
{
    public class TplSynchronizer : SynchronizerBase,  IDisposable
    {
        private static readonly ICommonLog s_log = CommonLogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        private volatile int                         m_activeAndQueued;
        private readonly Channel<IInvocationContext> m_channel;
        private readonly CancellationTokenSource     m_cts;
        
        private          int           m_disposed;
        private readonly Task          m_readerTask;
        private readonly TaskScheduler m_scheduler;

        public override int TasksCount => m_activeAndQueued;

        public TplSynchronizer(SynchronizerConfig config,
                               TaskScheduler scheduler = null) : base(config)
        {
            m_scheduler = scheduler ?? TaskScheduler.Default;

            m_cts = new CancellationTokenSource();
            UnboundedChannelOptions unboundedChannelOptions = new UnboundedChannelOptions
            {
                SingleReader = true
            };
            m_channel = Channel.CreateUnbounded<IInvocationContext>(unboundedChannelOptions);

            // ... existing code
            m_readerTask = Task.Run(startReader)
                               .ContinueWith(onComplete, TaskContinuationOptions.NotOnRanToCompletion);
        }

        private void onComplete(Task task)
        {
            if (task.IsCanceled)
            {
                s_log.WarnFormat("[TplSynchronizer-{0}] - reader task canceled", Id);
            }
            else if (task.IsFaulted)
            {
                s_log.Error($"[TplSynchronizer-{Id}] - reader task faulted", task.Exception);
            }
        }

        protected override Task<T> doEnqueue<T>(InvocationContext<T> invocationContext)
        {
            if (m_disposed == 1)
            {
                throw new ObjectDisposedException($"{nameof(TplSynchronizer)}-{Id}");
            }
            Interlocked.Increment(ref m_activeAndQueued);
            Task<T> task = invocationContext.TaskCompletionSource.Task;
            m_channel.Writer.TryWrite(invocationContext);
            return task;
        }

        private async Task startReader()
        {
            ChannelReader<IInvocationContext> channelReader = m_channel.Reader;
            CancellationToken cancellationToken = m_cts.Token;
            try
            {
                while (await channelReader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    SetInvocationThread(Thread.CurrentThread);
                    while (channelReader.TryRead(out IInvocationContext invocationContext))
                    {
                        if (invocationContext == null)
                        {
                            continue;
                        }

                        try
                        {
                            IInvocationContext item = invocationContext;
                            await Task.Factory.StartNew(() => executeInvocation(item),
                                                        cancellationToken,
                                                        TaskCreationOptions.DenyChildAttach,
                                                        m_scheduler)
                                      .ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            s_log.Error($"[TplSynchronizer-{Id}] - Error in executing thread", e);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref m_activeAndQueued);
                        }
                    }
                    SetInvocationThread(null);
                }
            }
            catch (OperationCanceledException)
            {
                s_log.InfoFormat("[TplSynchronizer-{0}] - reader task cancelled gracefully", Id);
            }
            finally
            {
                SetInvocationThread(null);
            }
        }

        private void executeInvocation(IInvocationContext invocationContext)
        {
            SetInvocationThread(Thread.CurrentThread);
            try
            {
                DateTime invocationStartTime = DateTime.UtcNow;
                logWaitTime(invocationContext, invocationContext.InvocationEnqueueTime, invocationStartTime, Config.InvocationWarningTimeout, Messages.InvocationWaitedForTooLong);

                invocationContext.Invoke();

                DateTime invocationCompleteTime = DateTime.UtcNow;
                logWaitTime(invocationContext, invocationStartTime, invocationCompleteTime, Config.ExecutionWarningTimeout, Messages.InvocationAbnormalTimeToExecute);

                if (invocationContext.InvocationOptions != null)
                {
                    onComplete(invocationContext);
                }

                invocationContextFactory.Release(invocationContext);
            }
            finally
            {
                SetInvocationThread(null);
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref m_disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    m_channel.Writer.Complete();
                    m_cts.Cancel();
                    try
                    {
                        m_readerTask?.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
                    {
                        // Expected - task was canceled
                    }
                    catch (AggregateException ex)
                    {
                        s_log.Warn($"[TplSynchronizer-{Id}] - Exception during disposal wait", ex);
                    }
                    m_cts.Dispose();
                }
            }
        }
    }
}
#endif