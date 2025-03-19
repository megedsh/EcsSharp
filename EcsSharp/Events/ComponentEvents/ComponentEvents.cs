#define CODE_ANALYSIS

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using EcsSharp.Helpers;

namespace EcsSharp.Events.ComponentEvents
{
    [SuppressMessage("csharpsquid", "S3237:\"value\" parameters should be used", Justification = "")]
    public class ComponentEvents<T>
    {
        private readonly ReaderWriterLockSlim                 m_lock             = new ReaderWriterLockSlim();
        private readonly Dictionary<Type, DelegateWrapper<T>> m_delegatesByTypes = new Dictionary<Type, DelegateWrapper<T>>();

        internal IEnumerable<DelegateWrapper<T>> GetDelegatesForType(IEnumerable<Type> types)
        {
            m_lock.EnterReadLock();

            List<DelegateWrapper<T>> res = types
                                           .Select(t => m_delegatesByTypes.GetValueOrDefault(t))
                                           .Where(d => d != null)
                                           .ToList();
            m_lock.ExitReadLock();
            return res;
        }

        public DelegateWrapper<T> this[Type type]
        {
            get
            {
                m_lock.EnterWriteLock();
                if (!m_delegatesByTypes.TryGetValue(type, out DelegateWrapper<T> wrapper))
                {
                    wrapper = new DelegateWrapper<T>();
                    m_delegatesByTypes.Add(type, wrapper);
                }

                m_lock.ExitWriteLock();
                return wrapper;
            }
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }
    }
}