using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace EcsSharp.Logging.BuiltIn;

public class ConsoleCommonLogsAdapter : ICommonLogProvider
{
    public ConsoleCommonLogsAdapter(CommonLogLevel rootLoggerLevel = CommonLogLevel.Debug) => RootLoggerLevel = rootLoggerLevel;

    private readonly Dictionary<string, ICommonLog> m_activeLoggers = new();

    public CommonLogLevel RootLoggerLevel { get; set; }

    public ICommonLog GetLogger(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        ICommonLog logger = null;
        if (!m_activeLoggers.TryGetValue(type.FullName, out logger))
        {
            logger = new ConsoleCommonLogger(type.FullName, RootLoggerLevel);
            m_activeLoggers[type.FullName] = logger;
        }

        return logger;
    }

    public ICommonLog GetLogger(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        ICommonLog logger = null;
        if (!m_activeLoggers.TryGetValue(name, out logger))
        {
            logger = new ConsoleCommonLogger(name, RootLoggerLevel);
            m_activeLoggers[name] = logger;
        }

        return logger;
    }
}
public class ConsoleCommonLogger : BaseCommonLogger
{
    private readonly object m_sync = new();

    public ConsoleCommonLogger(string loggerName, CommonLogLevel logLevel) : base(loggerName) => LogLevel = logLevel;

    public override CommonLogLevel LogLevel { get; }
    public override CommonLogEventDelegate LogEventDelegate => logEvent;

    private void logEvent(CommonLogLevel logLevel, object message, Exception exception)
    {
        StringBuilder sb = new();

        sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append(' ')
          .Append($"[{logLevel}]").Append(' ')
          .Append($"[{getThreadName()}]").Append('\t')
          .Append($"{m_loggerName}").Append(" - ")
          .Append(buildMessage(message, exception));
        lock(m_sync)
        {
            Console.ForegroundColor = getColor(logLevel);
            Console.WriteLine(sb.ToString());
        }
    }

    private string getThreadName()
    {
        string threadName = Thread.CurrentThread.Name;
        // '.NET ThreadPool Worker' appears as a default thread name in the .NET 6-7 thread pool.
        // '.NET TP Worker' is the default thread name in the .NET 8+ thread pool.
        // '.NET Long Running Task' is used for long running tasks
        // Prefer the numeric thread ID instead.
        if (threadName is string { Length: > 0 } name
            && !name.StartsWith(".NET ", StringComparison.Ordinal))
        {
            return name;
        }

        return Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
    }

    private ConsoleColor getColor(CommonLogLevel loglevle)
    {
        switch (loglevle)
        {
            case CommonLogLevel.None:
            case CommonLogLevel.Trace:
                return ConsoleColor.DarkGray;
            case CommonLogLevel.Debug:
                return ConsoleColor.DarkGray;
            case CommonLogLevel.Info:
                return ConsoleColor.White;
            case CommonLogLevel.Warn:
                return ConsoleColor.Yellow;
            case CommonLogLevel.Error:
                return ConsoleColor.Red;
            case CommonLogLevel.Fatal:
                return ConsoleColor.Cyan;
            default:
                throw new ArgumentOutOfRangeException(nameof(loglevle), loglevle, null);
        }
    }

    private string buildMessage(object message, Exception exception)
    {
        string exceptionStackTrace = null;
        string exceptionType = null;
        string exceptionMessage = null;
        if (exception != null)
        {
            exceptionType = exception.GetType().Name;
            exceptionMessage = exception.Message;
            exceptionStackTrace = exception.StackTrace;
        }

        string result = message.ToString();

        if (!string.IsNullOrEmpty(exceptionStackTrace))
        {
            result = result + $" - {exceptionType} : {exceptionMessage} - {exceptionStackTrace}";
        }

        return result;
    }
}