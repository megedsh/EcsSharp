using System;

namespace EcsSharp.Logging.BuiltIn
{
    public class DefaultCommonLoggerAdapter : ICommonLogProvider
    {
        public ICommonLog EmptyLogger { get; } = new EmptyLogger("EmptyLogger");
        public ICommonLog GetLogger(Type type) => EmptyLogger;
        public ICommonLog GetLogger(string name) => EmptyLogger;
    }

    public class EmptyLogger : BaseCommonLogger
    {
        public EmptyLogger(string loggerName) : base(loggerName)
        {
        }

        public override CommonLogLevel LogLevel => CommonLogLevel.None;
        public override CommonLogEventDelegate LogEventDelegate => logDelegate;

        private void logDelegate(CommonLogLevel loglevel, object message, Exception exception)
        {
        }
    }
}