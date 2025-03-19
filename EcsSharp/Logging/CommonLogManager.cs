using System;
using EcsSharp.Logging.BuiltIn;
using EcsSharp.Logging.ThrottledLog;

namespace EcsSharp.Logging
{
    public class CommonLogManager
    {
        private static ICommonLogProvider     s_logProvider;
        private static object s_sync = new object();

        public static void InitLogProvider(ICommonLogProvider provider)
        {
            s_logProvider = provider;
        }

        public static ICommonLog GetLogger(Type type)
        {
            ensureInitilized();
            return s_logProvider.GetLogger(type);
        }

        public static ICommonLog GetLogger(string name)
        {
            ensureInitilized();
            return s_logProvider.GetLogger(name);
        }

        public static ICommonThrottledLog GetThrottledLogger(string name, ThrottledLogConfig throttleConfig)
        {
            ensureInitilized();
            ICommonLog commonLog = s_logProvider.GetLogger(name);
            return new ThrottledLog.ThrottledLog(commonLog,throttleConfig);
        }

        public static ICommonThrottledLog GetThrottledLogger(Type type, ThrottledLogConfig throttleConfig)
        {
            ensureInitilized();
            ICommonLog commonLog = s_logProvider.GetLogger(type);
            return new ThrottledLog.ThrottledLog(commonLog, throttleConfig);
        }

        private static void ensureInitilized()
        {
            if (s_logProvider == null)
            {
                lock(s_sync)
                {
                    s_logProvider = new DefaultCommonLoggerAdapter();
                }
            }
        }
    }
}