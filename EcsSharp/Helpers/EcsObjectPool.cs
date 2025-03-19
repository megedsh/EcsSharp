using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using EcsSharp.Logging;

namespace EcsSharp.Helpers
{
    
        public interface IEcsObjectPool
        {
            void Register<T>(Func<EcsObjectPool, T> factory, string releaseMethodName);
            T    GetObject<T>();
            void Release<T>(T obj);
        }

        public class EcsObjectPool : IEcsObjectPool
        {
            private static readonly ICommonLog s_log = CommonLogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

            private readonly Dictionary<Type, Func<object>>          m_factoryMap        = new();
            private readonly Dictionary<Type, uint>                  m_objectCounters    = new();
            private readonly Dictionary<Type, ConcurrentBag<object>> m_objectPool        = new();
            private readonly Dictionary<Type, Action<object>>        m_releaseMethodsMap = new();
            private readonly object                                  m_sync              = new();


            internal void Register<T>(Func<EcsObjectPool, T> factory)
            {
                Register<T>(factory, null);
            }

            public void Register<T>(Func<EcsObjectPool, T> factory, string releaseMethodName)
            {
                Type type = typeof(T);
                if (!string.IsNullOrEmpty(releaseMethodName))
                {
                    MethodInfo methodInfo = type.GetMethod(releaseMethodName);
                    Action<object> releaseMethod = o => methodInfo.Invoke(o, Array.Empty<object>());
                    m_releaseMethodsMap[type] = releaseMethod;
                }

                m_factoryMap[type] = () => factory.Invoke(this);
                m_objectPool[type] = new ConcurrentBag<object>();
                m_objectCounters[type] = 0;
            }

            public T GetObject<T>()
            {
                Type type = typeof(T);
                if (m_objectPool.TryGetValue(type, out ConcurrentBag<object> bag))
                {
                    if (bag.TryTake(out object result))
                    {
                        return (T)result;
                    }

                    lock(m_sync)
                    {
                        m_objectCounters[type]++;
                        if (m_factoryMap.TryGetValue(type, out Func<object> factory))
                        {
                            s_log.InfoFormat("Total objects for type {0} : {1}", type, m_objectCounters[type]);
                            return (T)factory.Invoke();
                        }
                    }
                }

                throw new Exception("Could not retrieve object from pool");
            }

            public void Release<T>(T obj)
            {
                Type type = obj.GetType();
                if (m_releaseMethodsMap.TryGetValue(type, out Action<object> releaseAction))
                {
                    releaseAction.Invoke(obj);
                }

                if (m_objectPool.TryGetValue(type, out ConcurrentBag<object> bag))
                {
                    bag.Add(obj);
                }

                m_objectCounters[type]--;
            }
        }
 
}