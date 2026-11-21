using System;

namespace EcsSharp.Helpers.Synchronizer
{
    internal static class Consts
    {
        internal static TimeSpan DefaultTaskTimeout              = TimeSpan.FromSeconds(2);
        internal static TimeSpan DefaultInvocationWarningTimeout = TimeSpan.FromSeconds(1);
        internal static TimeSpan DefaultExecutionWarningTimeout  = TimeSpan.FromSeconds(1);
        internal static string   DefaultThreadName               = "Synchronizer";
        internal static TimeSpan JoinTimeout = TimeSpan.FromSeconds(5);
    }
}