using System;
using System.Collections.Generic;
using System.Threading;
using EcsSharp.Storage;

namespace EcsSharp.Helpers
{
    internal interface IEntityLookupBucketInternal : IEntityLookupBucket
    {
        void Add(IEntity entity);
        void Remove(IEntity entity);
    }

    public interface IEntityLookupBucket
    {
        int Count { get; }
    }

    public interface IEntityLookupBucket<TKey> : IEntityLookupBucket
    {
        bool TryGetEntity(TKey key, out IEntity entity);
        bool Contains(TKey key);
        IEntity GetOrCreate(TKey key, Func<IEcsRepo,IEntity> creationFunc);
    }

    internal class EntityLookupBucket<TKey> : IEntityLookupBucket<TKey>, IEntityLookupBucketInternal
    {
        private readonly Dictionary<TKey, string> m_bucket = new Dictionary<TKey, string>();
        private readonly IEcsRepo m_ecsRepo;
        private readonly IEcsStorage              m_storage;
        private readonly ReaderWriterLockSlim     m_lockObject;
        private readonly Predicate<IEntity>       m_addPredicate;
        private readonly Func<IEntity, TKey>      m_keyFactory;

        internal EntityLookupBucket(IEcsRepo             ecsRepo,
                                    IEcsStorage storage,
                                    ReaderWriterLockSlim lockObject,
                                    Predicate<IEntity>   addPredicate,
                                    Func<IEntity, TKey>  keyFactory)
        {
            m_ecsRepo = ecsRepo;
            m_storage = storage;
            m_lockObject = lockObject;
            m_addPredicate = addPredicate;
            m_keyFactory = keyFactory;
        }

        public void Add(IEntity entity)
        {
            if (m_addPredicate(entity))
            {
                TKey key = m_keyFactory(entity);
                m_bucket[key] = entity.Id;
            }
        }

        public void Remove(IEntity entity)
        {
            if (m_addPredicate(entity))
            {
                TKey key = m_keyFactory(entity);
                m_bucket.Remove(key);
            }
        }

        public bool Contains(TKey key)
        {
            enterReadLock();
            bool containsKey = m_bucket.ContainsKey(key);
            exitReadLock();
            return containsKey;
        }

        public IEntity GetOrCreate(TKey key, Func<IEcsRepo,IEntity> creationFunc)
        {
            IEntity entity = null;
            m_storage.BatchUpdate( null, (r) =>
            {
                if (m_bucket.TryGetValue(key, out string id))
                {
                    entity = m_storage.QuerySingle(id);
                }
                else
                {
                    entity = creationFunc.Invoke(m_ecsRepo);
                }
            });
            return entity;
        }

        public bool TryGetEntity(TKey key, out IEntity entity)
        {
            entity = null;
            enterReadLock();
            if (m_bucket.TryGetValue(key, out string id))
            {
                entity = m_storage.QuerySingle(id);
            }

            exitReadLock();
            return entity != null;
        }

        private void enterReadLock()
        {
            m_lockObject.EnterReadLock();
        }

        private void exitReadLock()
        {
            m_lockObject.ExitReadLock();
        }

        public int Count {
            get
            {
                enterReadLock();
                int count = m_bucket.Count;
                exitReadLock();
                return count;
            }
        }
    }
}