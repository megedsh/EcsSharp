using System;
using System.Reflection;

namespace EcsSharp.Helpers.Synchronizer
{
    public interface IInvocationContext
    {
        void Invoke();
        object InvocationTarget { get; }
        MethodInfo InvocationMethod { get; }
        DateTime InvocationEnqueueTime { get; }
        bool HasException { get;}
        Exception InvocationException { get; }
        InvocationOptions InvocationOptions { get; }
        void Clear();
        Type ReturnType { get; }
    }
}