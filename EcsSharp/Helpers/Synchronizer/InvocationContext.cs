using System;
using System.Reflection;
using System.Threading.Tasks;

namespace EcsSharp.Helpers.Synchronizer
{
    public class InvocationContext<T> : IInvocationContext
    {
        private static readonly ICommonLog s_log = CommonLogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        public TaskCompletionSource<T> TaskCompletionSource { get; set; }
        public Func<T> InvocationTypedDelegate { get; set; }
        public DateTime InvocationEnqueueTime { get; set; }
        public bool HasException => TaskCompletionSource?.Task.Exception != null;
        public Exception InvocationException => TaskCompletionSource?.Task.Exception;
        public object InvocationTarget { get; set; }
        public MethodInfo InvocationMethod { get; set; }
        public InvocationOptions InvocationOptions { get; set; }

        public InvocationContext()
        {
        }

        public InvocationContext(TaskCompletionSource<T> taskCompletionSource,
                                 Func<T>                 invocationDelegate,
                                 DateTime                invocationEnqueueTime,
                                 object                  invocationTarget,
                                 MethodInfo              invocationMethod,
                                 InvocationOptions       invocationOptions)
        {
            TaskCompletionSource = taskCompletionSource;
            InvocationTarget = invocationTarget;
            InvocationMethod = invocationMethod;
            InvocationOptions = invocationOptions;
            InvocationTypedDelegate = invocationDelegate;
            InvocationEnqueueTime = invocationEnqueueTime;
        }

        public void Clear()
        {
            TaskCompletionSource = null;
            InvocationTypedDelegate = null;
            InvocationEnqueueTime = DateTime.MinValue;
            InvocationTarget = null;
            InvocationMethod = null;
            InvocationOptions = null;
        }

        public Type ReturnType => typeof(T);

        public void Invoke()
        {
            try
            {
                T result = InvocationTypedDelegate();
                TaskCompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                s_log.Error("Error invoking synchronized delegate ", ex);
                TaskCompletionSource.SetException(ex);
            }
        }

        public override string ToString()
        {
            if (InvocationTarget == null)
            {
                return $"Static - {InvocationMethod?.Name ?? "Unknown"}";
            }

            Type targetType = InvocationTarget.GetType();
            Type targetDeclaringType = targetType.DeclaringType;
            string typeName = targetDeclaringType?.FullName ?? targetType.FullName;
            string methodName = InvocationMethod?.Name ?? "Unknown";

            return $"{typeName} - {methodName}";
        }
    }
}