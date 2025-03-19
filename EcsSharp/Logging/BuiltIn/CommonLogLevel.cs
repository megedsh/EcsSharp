namespace EcsSharp.Logging.BuiltIn
{
    public enum CommonLogLevel
    {
        Trace = 100,
        Debug = 200,
        Info  = 300,
        Warn  = 400,
        Error = 500,
        Fatal = 600,
        None  = int.MaxValue,
    }
}