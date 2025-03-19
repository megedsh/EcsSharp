#define CODE_ANALYSIS
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace EcsSharp.Events.TagEvents
{
    [SuppressMessage("csharpsquid", "S3237:\"value\" parameters should be used", Justification = "")]
    public class TagEvents<T>
    {
        private readonly ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();

        private readonly Dictionary<string, DelegateWrapper<T>> m_delegatesByTags = new Dictionary<string, DelegateWrapper<T>>();

        public DelegateWrapper<T> this[string tag]
        {
            get
            {
                m_lock.EnterWriteLock();
                if (!m_delegatesByTags.TryGetValue(tag, out DelegateWrapper<T> wrapper))
                {
                    wrapper = new DelegateWrapper<T>();
                    m_delegatesByTags.Add(tag, wrapper);
                }

                m_lock.ExitWriteLock();

                return wrapper;
            }
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        public bool HasDelegates => m_delegatesByTags.Count > 0;

        internal DelegateWrapper<T> GetDelegateForTag(string tag)
        {
            m_lock.EnterReadLock();
            m_delegatesByTags.TryGetValue(tag, out DelegateWrapper<T> dlgt);
            m_lock.ExitReadLock();
            return dlgt;
        }
    }
}