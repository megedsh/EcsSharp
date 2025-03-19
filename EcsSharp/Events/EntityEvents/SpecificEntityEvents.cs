#define CODE_ANALYSIS
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace EcsSharp.Events.EntityEvents
{
    [SuppressMessage("csharpsquid", "S3237:\"value\" parameters should be used", Justification = "")]
    public class SpecificEntityEvents<T>
    {
        private readonly ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();

        private readonly Dictionary<string, DelegateWrapper<T>> m_delegatesByIds = new Dictionary<string, DelegateWrapper<T>>();

        public DelegateWrapper<T> this[string id]
        {
            get
            {
                m_lock.EnterWriteLock();
                if (!m_delegatesByIds.TryGetValue(id, out DelegateWrapper<T> wrapper))
                {
                    wrapper = new DelegateWrapper<T>();
                    m_delegatesByIds.Add(id, wrapper);
                }

                m_lock.ExitWriteLock();

                return wrapper;
            }
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        public bool HasDelegates => m_delegatesByIds.Count > 0;

        internal DelegateWrapper<T> GetDelegateForId(string id)
        {
            m_lock.EnterReadLock();
            m_delegatesByIds.TryGetValue(id, out DelegateWrapper<T> dlgt);
            m_lock.ExitReadLock();
            return dlgt;
        }
    }
}