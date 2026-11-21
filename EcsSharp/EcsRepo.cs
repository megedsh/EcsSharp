using System;
using System.Collections.Generic;

using EcsSharp.Collectors;
using EcsSharp.Distribute;
using EcsSharp.Events;
using EcsSharp.Events.EventArgs;
using EcsSharp.Helpers;
using EcsSharp.Storage;

namespace EcsSharp
{
    public class EcsRepo : IEcsRepo
    {
        private static readonly ICommonLog              s_log = CommonLogManager.GetLogger(typeof(EcsRepo));
        private readonly        IEcsStorage             m_ecsStorage;
        private readonly        EcsObjectPool           m_ecsObjectPool;
        private readonly        IEcsChangeRecorder      m_changeRecorder;
        

        public string Name { get; }
        public IEcsStorage Storage => m_ecsStorage;
        public IEcsEventService Events { get; }
        public IEcsChangeTracker Changes { get; }
        public IEcsObjectPool ObjectPool =>m_ecsObjectPool;

        public EcsRepo(string name, 
                       IEcsStorage      ecsStorage,
                       IEcsEventService ecsEventService)
        {
            AssemblyLoadLogger.Init();
            ecsStorage.SetRepo(this);
            m_ecsStorage = ecsStorage;
            Name = name;
            Events = ecsEventService;
            EcsChangeTracker changeTracker = new EcsChangeTracker();
            Changes = changeTracker;
            m_changeRecorder = changeTracker;
            m_ecsStorage.OnEntitiesCreated += onEntitiesCreated;
            m_ecsStorage.OnComponentCreated += onComponentCreated;
            m_ecsStorage.OnComponentUpdated += onComponentUpdated;
            m_ecsStorage.OnComponentDeleted += onComponentDeleted;
            //m_ecsStorage.OnPartialComponentDeleted += onPartialComponentDeleted;
            m_ecsObjectPool = new EcsObjectPool();
            m_ecsObjectPool.Register(_ => new CreateOrUpdateBuilder(this, m_ecsObjectPool));
            m_ecsObjectPool.Register(_ => new EntityBuilder(m_ecsObjectPool, this));
            m_ecsObjectPool.Register(_ => new EntityBatchUpdateBuilder(m_ecsObjectPool), builder => builder.Clear());
            
            
            //SynchronizerConfig synchronizerConfig = new SynchronizerConfigBuilder()
            //                                        .WithId($"EcsWriteThread_{Name}")
            //                                        .WithThreadName($"EcsWriteThread_{Name}")
            //                                        .Build();
            //m_invocationSynchronizer = new DedicatedThreadSynchronizerFactory().Create(synchronizerConfig);
            s_log.Info("Ecs Repository Created");
        }
        public IEntity       Create()      => m_ecsStorage.Create();
        public IEntity CreateWithComponents(params object[] components) => m_ecsStorage.CreateWithComponents(components);

        public IEntity Create(object[] components, IEnumerable<string> tags, string id = null) => m_ecsStorage.Create(components, tags, id);

        public IEntity CreateOrGetWithId(string id, string[] tags = null) => m_ecsStorage.CreateOrGetWithId(id, tags);

        public void Delete(string id)
        {
            m_ecsStorage.DeleteEntity(id);
        }

        public void Delete(IEntity entity)
        {
            m_ecsStorage.DeleteEntity(entity);
        }

        public void DeleteEntitiesByComponent<T>(Predicate<T> componentPredicate = null, string[] tags = null)
        {
            m_ecsStorage.DeleteEntitiesByComponent(componentPredicate, tags);
        }

        public void DeleteEntitiesWithTag(params string[] tags)
        {
            m_ecsStorage.DeleteEntitiesWithTag(tags);
        }

        public void MergePackage(EcsPackage ecsPackage)
        {
            m_ecsStorage.MergePackage(ecsPackage);
        }

        public void BatchUpdate(Action<IEcsRepo> batch)
        {
            m_ecsStorage.BatchUpdate(this, batch);
        }

        
        public IEntity CreateOrGetByComponent<T>(T component, Predicate<T> componentPredicate = null, string[] tags = null) => m_ecsStorage.CreateOrGetByComponent(component, componentPredicate, tags);

        public IEntityCollection Query<T>(string[] tags = null) => m_ecsStorage.Query<T>(tags);

        public IEntityCollection Query<T>(Predicate<T> componentPredicate, string[] tags = null) => m_ecsStorage.Query(componentPredicate, tags);

        public IEntityCollection Query<T>(Func<T, IEntity, bool> entityPredicate, string[] tags = null) => m_ecsStorage.Query(entityPredicate, tags);

        public IEntityCollection Query(Type componentType, Func<object, IEntity, bool> entityPredicate = null, string[] tags = null) => m_ecsStorage.Query(componentType, entityPredicate, tags);

        public IEntityCollection Query(Type[] componentTypes, Predicate<IEntity> entityPredicate = null, string[] tags = null) => m_ecsStorage.Query(componentTypes, entityPredicate, tags);

        public IEntityCollection Query(params string[] ids) => m_ecsStorage.Query(ids);

        public IEntityCollection      Query(params IEntity[] entities)  => m_ecsStorage.Query(entities);
        public IEntityCollection      QueryAll()                        => m_ecsStorage.QueryAll();
        public IEntityCollection      QueryByTags(params string[] tags) => m_ecsStorage.QueryByTags(tags);
        public ICreateOrUpdateBuilder CreateOrUpdate()                  => m_ecsObjectPool.GetObject<CreateOrUpdateBuilder>();

        public EntityBuilder EntityBuilder() => m_ecsObjectPool.GetObject<EntityBuilder>();

        public IEntity QuerySingle<T>(string[] tags = null) => m_ecsStorage.QuerySingle<T>(tags);

        public IEntity QuerySingle<T>(Predicate<T> componentPredicate, string[] tags = null) => m_ecsStorage.QuerySingle(componentPredicate, tags);

        public IEntity QuerySingle<T>(Func<T, IEntity, bool> entityPredicate, string[] tags = null) => m_ecsStorage.QuerySingle(entityPredicate, tags);

        public IEntity QuerySingle(Type componentType, Func<object, IEntity, bool> entityPredicate = null, string[] tags = null) => m_ecsStorage.QuerySingle(componentType, entityPredicate, tags);

        public IEntity QuerySingle(Type[] componentTypes, Predicate<IEntity> entityPredicate = null, string[] tags = null) => m_ecsStorage.QuerySingle(componentTypes, entityPredicate, tags);

        public IEntity QuerySingle(string id) => m_ecsStorage.QuerySingle(id);

        public IEntityLookupBucket<TKey> CreateLookupBucket<TKey>(Predicate<IEntity> addPredicate,
                                                                  Func<IEntity, TKey> keyFactory) => m_ecsStorage.CreateLookupBucket(addPredicate, keyFactory, this);

        public IEntity QuerySingleByTags(params string[] tags) => m_ecsStorage.QuerySingleByTags(tags);
        public T              BatchQuery<T>(Func<IEcsRepo, T>   batch)
        {
            return m_ecsStorage.BatchQuery(this, batch);
        }

        private void onEntitiesCreated(EntityCreatedEventArgs[] args)
        {
            Events.InvokeEntityCreatedDelegates(args);
        }

        private void onComponentDeleted(ComponentDeletedEventArgs[] args)
        {
            m_changeRecorder.RecordDeleted(args);
            Events.InvokeComponentDeletedDelegates(args);
        }

        //private void onPartialComponentDeleted(ComponentDeletedEventArgs[] args)
        //{
        //    Events.InvokeComponentDeletedDelegates(args, true);
        //}

        private void onComponentUpdated(ComponentUpdatedEventArgs[] args)
        {
            m_changeRecorder.RecordUpdated(args);
            Events.InvokeComponentUpdatedDelegates(args);
        }

        private void onComponentCreated(ComponentCreatedEventArgs[] args)
        {
            Events.InvokeComponentCreatedDelegates(args);
        }
    }
}