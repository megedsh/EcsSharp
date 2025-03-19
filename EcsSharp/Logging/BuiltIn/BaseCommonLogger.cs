using System;
using System.Globalization;

namespace EcsSharp.Logging.BuiltIn
{
    public delegate void CommonLogEventDelegate(CommonLogLevel logLevel, object message, Exception exception);

    public abstract class BaseCommonLogger : ICommonLog
    {
        protected readonly string m_loggerName;

        public BaseCommonLogger(string loggerName) => m_loggerName = loggerName;

        public void Trace(object message)
        {
            doInvoke(CommonLogLevel.Trace, message);
        }

        public void Trace(object message, Exception exception)
        {
            doInvoke(CommonLogLevel.Trace, message, exception);
        }

        public void TraceFormat(string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Trace, new SafeStringFormat(CultureInfo.InvariantCulture, format, args));
        }

        public void TraceFormat(IFormatProvider provider, string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Trace, new SafeStringFormat(provider, format, args));
        }

        public void Debug(object message)
        {
            doInvoke(CommonLogLevel.Debug, message);
        }

        public void Debug(object message, Exception exception)
        {
            doInvoke(CommonLogLevel.Debug, message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Debug, new SafeStringFormat(CultureInfo.InvariantCulture, format, args));
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Debug, new SafeStringFormat(provider, format, args));
        }

        public void Info(object message)
        {
            doInvoke(CommonLogLevel.Info, message);
        }

        public void Info(object message, Exception exception)
        {
            doInvoke(CommonLogLevel.Info, message, exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Info, new SafeStringFormat(CultureInfo.InvariantCulture, format, args));
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Info, new SafeStringFormat(provider, format, args));
        }

        public void Warn(object message)
        {
            doInvoke(CommonLogLevel.Warn, message);
        }

        public void Warn(object message, Exception exception)
        {
            doInvoke(CommonLogLevel.Warn, message, exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Warn, new SafeStringFormat(CultureInfo.InvariantCulture, format, args));
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Warn, new SafeStringFormat(provider, format, args));
        }

        public void Error(object message)
        {
            doInvoke(CommonLogLevel.Error, message);
        }

        public void Error(object message, Exception exception)
        {
            doInvoke(CommonLogLevel.Error, message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Error, new SafeStringFormat(CultureInfo.InvariantCulture, format, args));
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Error, new SafeStringFormat(provider, format, args));
        }

        public void Fatal(object message)
        {
            doInvoke(CommonLogLevel.Fatal, message);
        }

        public void Fatal(object message, Exception exception)
        {
            doInvoke(CommonLogLevel.Fatal, message, exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Fatal, new SafeStringFormat(CultureInfo.InvariantCulture, format, args));
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            doInvoke(CommonLogLevel.Fatal, new SafeStringFormat(provider, format, args));
        }

        public virtual bool IsTraceEnabled => LogLevel >= CommonLogLevel.Trace;
        public virtual bool IsDebugEnabled => LogLevel >= CommonLogLevel.Debug;
        public virtual bool IsInfoEnabled => LogLevel >= CommonLogLevel.Info;
        public virtual bool IsWarnEnabled => LogLevel >= CommonLogLevel.Warn;
        public virtual bool IsErrorEnabled => LogLevel >= CommonLogLevel.Error;
        public virtual bool IsFatalEnabled => LogLevel >= CommonLogLevel.Fatal;
        public void Log(CommonLogLevel level, object message)
        {
            doInvoke(level,message);
        }

        public void Log(CommonLogLevel level, object message, Exception exception)
        {
            doInvoke(level,message, exception);
        }

        public void LogFormat(CommonLogLevel level, string format, params object[] args)
        {
            if (IsEnabledForLevel(level))
            {
                doInvoke(level, new SafeStringFormat(CultureInfo.InvariantCulture, format, args));
            }
        }

        public void LogFormat(CommonLogLevel level, IFormatProvider provider, string format, params object[] args)
        {
            if (IsEnabledForLevel(level))
            {
                doInvoke(level, new SafeStringFormat(provider, format, args));
            }
        }


        public bool IsEnabledForLevel(CommonLogLevel level)
        {
            switch (level)
            {
                case CommonLogLevel.Trace:
                    return IsTraceEnabled;
                case CommonLogLevel.Debug:
                    return IsDebugEnabled;
                case CommonLogLevel.Info:
                    return IsInfoEnabled;
                case CommonLogLevel.Warn:
                    return IsWarnEnabled;
                case CommonLogLevel.Error:
                    return IsErrorEnabled;
                case CommonLogLevel.Fatal:
                    return IsFatalEnabled;
                case CommonLogLevel.None:
                    return LogLevel == CommonLogLevel.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        public abstract CommonLogLevel LogLevel { get; }
        public abstract CommonLogEventDelegate LogEventDelegate { get; }

        private void doInvoke(CommonLogLevel logLevel, object message, Exception exception = null)
        {
            if (logLevel >= LogLevel)
            {
                LogEventDelegate.Invoke(logLevel, message, exception);
            }
        }
    }
}