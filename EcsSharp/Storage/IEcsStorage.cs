using System;
using System.Collections.Generic;
using EcsSharp.Distribute;
using EcsSharp.Events.EventArgs;
using EcsSharp.Helpers;

namespace EcsSharp.Storage
{
    public interface IEcsStorage
    {
        event Action<EntityCreatedEventArgs[]> OnEntitiesCreated;
        event Action<ComponentUpdatedEventArgs[]> OnComponentUpdated;
        event Action<ComponentCreatedEventArgs[]> OnComponentCreated;
        event Action<ComponentDeletedEventArgs[]> OnComponentDeleted;
        IEntity           Create();
        IEntity Create(object[] components, IEnumerable<string> tags, string id = null);
        IEntity CreateWithComponents(params object[] components);
        IEntity CreateOrGetWithId(string id, string[] tags = null);
        void              SetComponent<T>(IEntity entity, T component);
        void              SetComponents(IEntity entity, params object[] components);
        bool ConditionalSet<T>(Entity entity, T component, ComponentSetCondition<T> condition, bool setWhenNotExist = true);
        bool ConditionalSet<T>(Entity entity, Predicate<T> condition, Func<T,T> componentFactory, bool setWhenNotExist = true);
        bool SetWhenNotEqual<T>(Entity entity, T component, bool setWhenNotExist = true);
        bool SetWhenNotEqual<T>(Entity entity, Func<T, T> componentFactory, bool setWhenNotExist = true);

        void SetWithVersion<T>(Entity entity, T component, ulong version);

        IEntity UpdateComponent<T>(Entity entity, Func<T, T> componentFactory, bool setWhenNotExist = true);
        IEntity CreateOrGetByComponent<T>(T component,Predicate<T> componentPredicate = null, string[] tags = null);
        Component         GetComponent(IEntity entity, Type componentType);
        Component[]       GetComponents(IEntity entity, Type componentType);

        Component[]       GetAllComponents(IEntity              entity);
        ulong             GetComponentVersion(Entity            entity, Type              componentType);
        TypeVersionPair[] GetComponentsVersion(IEntity          entity, IEnumerable<Type> types);
        Type[]            GetComponentTypes(IEntity             entity);
        void              DeleteEntity(IEntity                  entity);
        void              DeleteEntity(string                   id);
        void              DeleteEntitiesWithTag(params string[] tags);
        void              MergePackage(EcsPackage               ecsPackage);
        void              BatchUpdate(IEcsRepo                  repo, Action<IEcsRepo>  batch);
        T                 BatchQuery<T>(EcsRepo                 repo, Func<IEcsRepo, T> queryFunc);
        IEntityCollection Query(params string[]                 ids);
        IEntityCollection Query<T>(string[]                     tags                                                            = null);
        IEntityCollection Query<T>(Predicate<T>                 componentPredicate, string[]                    tags            = null);
        IEntityCollection Query<T>(Func<T, IEntity, bool>       entityPredicate,    string[]                    tags            = null);
        IEntityCollection Query(Type                            componentType,      Func<object, IEntity, bool> entityPredicate = null, string[] tags = null);
        IEntityCollection Query(Type[]                          componentTypes,     Predicate<IEntity>          entityPredicate = null, string[] tags = null);
        IEntityCollection Query(params IEntity[]                entities);
        IEntity           QuerySingle<T>(string[]               tags                             = null);
        IEntity           QuerySingle<T>(Predicate<T>           componentPredicate,string[] tags = null);
        IEntity           QuerySingle(string                    id);
        IEntity           QuerySingle<T>(Func<T, IEntity, bool> entityPredicate, string[]                    tags            = null);
        IEntity           QuerySingle(Type                      componentType,   Func<object, IEntity, bool> entityPredicate = null, string[] tags = null);
        IEntity           QuerySingle(Type[]                    componentTypes,  Predicate<IEntity>          entityPredicate = null, string[] tags = null);
        IEntityCollection QueryAll();

        IEntityCollection QueryByTags(string[] tags);

        void AddTag(Entity entity, params string[] tag);
        bool HasTag(Entity entity, params string[] tag);
        void DeleteEntitiesByComponent<T>(Predicate<T> componentPredicate = null,string[] tags = null);

        IEntityLookupBucket<TKey> CreateLookupBucket<TKey>(Predicate<IEntity> addPredicate,
                                                           Func<IEntity, TKey> keyFactory,
                                                           IEcsRepo ecsRepo);

        IEntity QuerySingleByTags(params string[] tags);        
    }
}