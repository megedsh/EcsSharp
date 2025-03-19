using System;

namespace EcsSharp.Logging
{
    public interface ICommonLogProvider
    {
        ICommonLog GetLogger(Type type);
        ICommonLog GetLogger(string name);
    }
}