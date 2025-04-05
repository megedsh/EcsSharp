using System;
using System.Collections.Generic;
using EcsSharp.Distribute;
using EcsSharp.Events;
using EcsSharp.Helpers;

namespace EcsSharp
{
    public interface IEcsRepo
    {
        IEcsEventService Events { get; }
        IEntity                   Create();
        IEntity                   CreateWithComponents(params object[]      components);
        IEntity                   Create(object[]                           components, IEnumerable<string> tags, string id = null);
        IEntity                   CreateOrGetWithId(string                  id,         string[]            tags               = null);
        IEntity                   CreateOrGetByComponent<T>(T               component,  Predicate<T>        componentPredicate = null, string[] tags = null);
        void                      Delete(string                             id);
        void                      Delete(IEntity                            entity);
        void                      DeleteEntitiesByComponent<T>(Predicate<T> componentPredicate = null, string[] tags = null);
        void                      DeleteEntitiesWithTag(params string[]     tags);
        void                      MergePackage(EcsPackage                   ecsPackage);
        void                      BatchUpdate(Action<IEcsRepo>              batch);
        IEntity                   QuerySingle<T>(string[]                   tags                                                            = null);
        IEntity                   QuerySingle<T>(Predicate<T>               componentPredicate, string[]                    tags            = null);
        IEntity                   QuerySingle<T>(Func<T, IEntity, bool>     entityPredicate,    string[]                    tags            = null);
        IEntity                   QuerySingle(Type                          componentType,      Func<object, IEntity, bool> entityPredicate = null, string[] tags = null);
        IEntity                   QuerySingle(Type[]                        componentTypes,     Predicate<IEntity>          entityPredicate = null, string[] tags = null);
        IEntity                   QuerySingle(string                        id);
        IEntityCollection         Query<T>(string[]                         tags                                                            = null);
        IEntityCollection         Query<T>(Predicate<T>                     componentPredicate, string[]                    tags            = null);
        IEntityCollection         Query<T>(Func<T, IEntity, bool>           entityPredicate,    string[]                    tags            = null);
        IEntityCollection         Query(Type                                componentType,      Func<object, IEntity, bool> entityPredicate = null, string[] tags = null);
        IEntityCollection         Query(Type[]                              componentTypes,     Predicate<IEntity>          entityPredicate = null, string[] tags = null);
        IEntityCollection         Query(params string[]                     ids);
        IEntityCollection         Query(params IEntity[]                    entities);
        IEntityCollection         QueryAll();
        T                         BatchQuery<T>(Func<IEcsRepo,T> queryFunc);
        IEntityCollection         QueryByTags(params string[]   tags);
        ICreateOrUpdateBuilder    CreateOrUpdate();
        EntityBuilder             EntityBuilder();
        IEntityLookupBucket<TKey> CreateLookupBucket<TKey>(Predicate<IEntity> addPredicate, Func<IEntity, TKey> keyFactory);
        IEntity                   QuerySingleByTags(params string[]           tags);
    }
}