using System;
using System.Collections.Generic;
using System.Threading;
using EcsSharp.Logging;

namespace EcsSharp.Events
{
    public class DelegateWrapper
    {
        private static readonly ICommonLog s_log = CommonLogManager.GetLogger(typeof(DelegateWrapper));

        private readonly ReaderWriterLockSlim m_lock      = new ReaderWriterLockSlim();
        private readonly HashSet<Delegate>    m_delegates = new HashSet<Delegate>();

        internal void Invoke(object args)
        {
            foreach (Delegate callback in m_delegates)
            {
                try
                {
                    callback.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    s_log.Error($"Error invoking event delegate with args :{args}: {callback.Method.Name}", ex);
                }
            }
        }

        internal bool HasDelegates => m_delegates.Count > 0;

        protected void BaseAddCallback<T>(Action<T> callback)
        {
            m_lock.EnterWriteLock();
            m_delegates.Add(callback);
            m_lock.ExitWriteLock();
        }

        protected void BaseRemoveCallback<T>(Action<T> callback)
        {
            m_lock.EnterWriteLock();
            m_delegates.Remove(callback);
            m_lock.ExitWriteLock();
        }
    }

    public class DelegateWrapper<T> : DelegateWrapper
    {
        public static DelegateWrapper<T> operator +(DelegateWrapper<T> d, Action<T> callback) => d.AddCallback(callback);

        public static DelegateWrapper<T> operator -(DelegateWrapper<T> d, Action<T> callback) => d.RemoveCallback(callback);

        internal DelegateWrapper<T> AddCallback(Action<T> callback)
        {
            BaseAddCallback(callback);
            return this;
        }

        internal DelegateWrapper<T> RemoveCallback(Action<T> callback)
        {
            BaseRemoveCallback(callback);
            return this;
        }

    }
}