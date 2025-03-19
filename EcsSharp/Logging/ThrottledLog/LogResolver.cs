using System;
using System.Threading;
using EcsSharp.Logging.BuiltIn;

namespace EcsSharp.Logging.ThrottledLog
{
    internal class LogResolver
    {
        private readonly ICommonLog       m_logger;
        private          DateTime         m_timeVisited;
        private volatile int              m_counter;
        private readonly Func<ICommonLog> m_func;
        private volatile int              m_timeDelayMs;
        private volatile uint             m_counterFactor;

        public LogResolver(ThrottledLogConfig config, ICommonLog logger)
        {
            m_logger = logger;
            if (config.Span != TimeSpan.MinValue)
            {
                m_timeDelayMs = (int)config.Span.TotalMilliseconds;
                m_func = byTime;
            }
            else if (config.Count > 0)
            {
                m_counterFactor = config.Count;
                m_func = byCounter;
            }
        }

        private ICommonLog byCounter()
        {
            if (m_counter % m_counterFactor == 0)
            {
                Interlocked.Exchange(ref m_counter, 1);
                return m_logger;
            }

            Interlocked.Increment(ref m_counter);

            return s_dummyLogger;
        }

        private ICommonLog byTime()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - m_timeVisited).TotalMilliseconds > m_timeDelayMs)
            {
                m_timeVisited = now;
                return m_logger;
            }

            return s_dummyLogger;
        }

        public ICommonLog GetLogger() => m_func();

        private static readonly ICommonLog s_dummyLogger = new EmptyLogger("null");
    }
}