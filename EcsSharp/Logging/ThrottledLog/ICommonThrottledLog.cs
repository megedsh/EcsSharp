namespace EcsSharp.Logging.ThrottledLog
{
    public interface ICommonThrottledLog
    {
        ICommonLog ForKey(string key);
        ICommonLog InternalLogger { get; }
        bool IsTraceEnabled { get; }
        bool IsDebugEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
    }
}