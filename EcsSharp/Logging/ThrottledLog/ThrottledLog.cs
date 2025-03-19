using System.Collections.Generic;

namespace EcsSharp.Logging.ThrottledLog
{
    public class ThrottledLog : ICommonThrottledLog
    {
        private readonly ICommonLog                      m_commonLog;
        private readonly ThrottledLogConfig              m_config;
        private readonly object                          m_sync = new object();
        private readonly Dictionary<string, LogResolver> m_map  = new Dictionary<string, LogResolver>();

        public ThrottledLog(ICommonLog commonLog, ThrottledLogConfig config)
        {
            m_commonLog = commonLog;
            m_config = config;
        }

        public ICommonLog ForKey(string key)
        {
            LogResolver resolver;
            lock(m_sync)
            {
                if (!m_map.TryGetValue(key, out resolver))
                {
                    resolver = new LogResolver(m_config, m_commonLog);
                    m_map[key] = resolver;
                }
            }

            return resolver.GetLogger();
        }

        public ICommonLog InternalLogger => m_commonLog;
        public bool IsTraceEnabled => m_commonLog.IsTraceEnabled;
        public bool IsDebugEnabled => m_commonLog.IsDebugEnabled;
        public bool IsInfoEnabled => m_commonLog.IsInfoEnabled;
        public bool IsWarnEnabled => m_commonLog.IsWarnEnabled;
        public bool IsErrorEnabled => m_commonLog.IsErrorEnabled;
        public bool IsFatalEnabled => m_commonLog.IsFatalEnabled;
    }
}