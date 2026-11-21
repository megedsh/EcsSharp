using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace EcsSharp.Helpers.Synchronizer
{
    public interface IInvocationContextFactory
    {
        InvocationContext<T> GetObject<T>(TaskCompletionSource<T> taskCompletionSource,
                                          Func<T>                 invocationDelegate,
                                          DateTime                invocationEnqueueTime,
                                          object                  invocationTarget,
                                          MethodInfo              invocationMethod,
                                          InvocationOptions       invocationOptions);

        void Release(IInvocationContext obj);
    }

    public class PooledInvocationContextFactory : IInvocationContextFactory
    {
        private const int MaxObjectsPerType = 1000;

        private readonly ConcurrentDictionary<Type, uint>                  m_objectCounters = new();
        private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> m_objectPool     = new();

        public InvocationContext<T> GetObject<T>(TaskCompletionSource<T> taskCompletionSource,
                                                                 Func<T>                 invocationDelegate,
                                                                 DateTime                invocationEnqueueTime,
                                                                 object                  invocationTarget,
                                                                 MethodInfo              invocationMethod,
                                                                 InvocationOptions       invocationOptions)
        {
            Type type = typeof(T);
            ConcurrentBag<object> bag = m_objectPool.GetOrAdd(type, _ => new ConcurrentBag<object>());
            InvocationContext<T> invocationContext;
            if (bag.TryTake(out object result))
            {
                invocationContext = (InvocationContext<T>)result;
            }
            else
            {
                updateCounter(type, val => val + 1);
                invocationContext = new InvocationContext<T>();
            }

            updateContext(invocationContext,
                          taskCompletionSource,
                          invocationDelegate,
                          invocationEnqueueTime,
                          invocationTarget,
                          invocationMethod,
                          invocationOptions);

            return invocationContext;
        }

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

        private void updateCounter(Type type, Func<uint, uint> f)
        {
            uint val = m_objectCounters.GetOrAdd(type, 0);
            m_objectCounters[type] = f.Invoke(val);
        }

        public void Release(IInvocationContext obj)
        {
            obj.Clear();
            ConcurrentBag<object> bag = m_objectPool.GetOrAdd(obj.ReturnType, _ => new ConcurrentBag<object>());
            if (bag.Count < MaxObjectsPerType)
            {
                bag.Add(obj);
                updateCounter(obj.ReturnType, val => val - 1);
            }
        }
    }
}