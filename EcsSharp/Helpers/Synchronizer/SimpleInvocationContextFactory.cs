using System;
using System.Reflection;
using System.Threading.Tasks;

namespace EcsSharp.Helpers.Synchronizer
{
    public class SimpleInvocationContextFactory : IInvocationContextFactory
    {
        InvocationContext<T> IInvocationContextFactory.GetObject<T>(TaskCompletionSource<T> taskCompletionSource,
                                                                    Func<T>                 invocationDelegate,
                                                                    DateTime                invocationEnqueueTime,
                                                                    object                  invocationTarget,
                                                                    MethodInfo              invocationMethod,
                                                                    InvocationOptions       invocationOptions) =>
            new(taskCompletionSource,
                invocationDelegate,
                invocationEnqueueTime,
                invocationTarget,
                invocationMethod,
                invocationOptions);

        private void updateContext<T>(InvocationContext<T>    invocationContext,
                                      TaskCompletionSource<T> taskCompletionSource,
                                      Func<T>                 invocationDelegate,
                                      DateTime                invocationEnqueueTime,
                                      object                  invocationTarget,
                                      MethodInfo              invocationMethod,
                                      InvocationOptions       invocationOptions)
        {
            invocationContext.TaskCompletionSource = taskCompletionSource;
            invocationContext.InvocationTarget = invocationTarget;
            invocationContext.InvocationMethod = invocationMethod;
            invocationContext.InvocationOptions = invocationOptions;
            invocationContext.InvocationTypedDelegate = invocationDelegate;
            invocationContext.InvocationEnqueueTime = invocationEnqueueTime;
        }

        public void Release(IInvocationContext obj)
        {
            // Do nothing
        }
    }
}