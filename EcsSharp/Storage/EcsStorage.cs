using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using EcsSharp.Distribute;
using EcsSharp.Events.EventArgs;
using EcsSharp.Helpers;
using EcsSharp.Logging;

namespace EcsSharp.Storage;

public class EcsStorage : IEcsStorage
{
    private const           string                              MoreThenOneElementExceptionLabel = "Trying to fetch a single entity, but multiple entities found for your criteria";
    private static readonly ICommonLog                          s_log                            = CommonLogManager.GetLogger(typeof(EcsStorage));
    private readonly        Dictionary<string, HashSet<string>> m_entitiesByTag                  = new();

    private readonly Dictionary<Type, HashSet<string>>               m_entitiesByType   = new();
    private readonly Dictionary<string, ParentTypeMap>               m_entityComponents = new();
    private readonly ReaderWriterLockSlim                            m_lock             = new(LockRecursionPolicy.SupportsRecursion);
    private readonly Dictionary<string, HashSet<string>>             m_tagsForEntity    = new();
    private readonly Dictionary<string, IEntityLookupBucketInternal> m_lookupBuckets    = new();

    private BatchEventsCollector m_batchEventsCollector;

    public event Action<ComponentUpdatedEventArgs[]> OnComponentUpdated;
    public event Action<ComponentCreatedEventArgs[]> OnComponentCreated;
    public event Action<ComponentDeletedEventArgs[]> OnComponentDeleted;
    public event Action<EntityCreatedEventArgs[]> OnEntitiesCreated;

    public ITypeFamilyProvider TypeFamilyProvider { get; init; } = new AllInterfacesFamilyProvider();

    #region Create

    public IEntity Create() => Create(Array.Empty<object>(), Array.Empty<string>());

    public IEntity CreateWithComponents(params object[] components) => Create(components, Array.Empty<string>());

    public IEntity Create(object[]            components,
                          IEnumerable<string> tags,
                          string              id = null)
    {
        enterWriteLock();
        BatchEventsCollector ec = m_batchEventsCollector;
        if (id != null)
        {
            if (m_entityComponents.ContainsKey(id))
            {
                exitWriteLock();
                throw new EcsException($"Id already exists : {id}");
            }
        }

        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();

        IEntity entity = createAndSaveEntity(updatedList,
                                             createdList,
                                             id, tags.ToArray());

        setComponents(entity, components, updatedList, createdList);
        addToBuckets(entity);
        exitWriteLock();
        invokeIfNeeded(new[] { new EntityCreatedEventArgs(entity, createdList.ToArray()) }, ec);
        invokeIfNeeded(createdList.ToArray(),                                               updatedList.ToArray(), ec);
        return entity;
    }

    public IEntity CreateOrGetWithId(string id, string[] tags = null)
    {
        enterWriteLock();
        List<ComponentUpdatedEventArgs> updatedList = new(1);
        List<ComponentCreatedEventArgs> createdList = new(1);
        BatchEventsCollector ec = m_batchEventsCollector;
        IEntity entity = createOrGetWithId(id,
                                           tags ?? Array.Empty<string>(),
                                           out bool created,
                                           updatedList,
                                           createdList);

        if (created)
        {
            addToBuckets(entity);
        }

        exitWriteLock();
        if (created)
        {
            invokeIfNeeded(new[] { new EntityCreatedEventArgs(entity, Array.Empty<ComponentCreatedEventArgs>()) }, ec);
        }

        return entity;
    }

    #endregion

    #region Delete

    public void DeleteEntity(string id)
    {
        enterWriteLock();
        BatchEventsCollector ec = m_batchEventsCollector;
        deleteEntity(id, out ComponentDeletedEventArgs[] deletedArgs);
        exitWriteLock();
        invokeIfNeeded(deletedArgs, ec);
    }

    public void DeleteEntitiesWithTag(params string[] tags)
    {
        if (tags != null && tags.Length > 0)
        {
            enterWriteLock();
            BatchEventsCollector ec = m_batchEventsCollector;
            deleteEntitiesForTags(tags, out ComponentDeletedEventArgs[] deletedArgs);
            exitWriteLock();
            invokeIfNeeded(deletedArgs, ec);
        }
    }

    public void DeleteEntity(IEntity entity)
    {
        enterWriteLock();
        BatchEventsCollector ec = m_batchEventsCollector;
        deleteEntity(entity.Id, out ComponentDeletedEventArgs[] deletedArgs);
        exitWriteLock();
        invokeIfNeeded(deletedArgs, ec);
    }

    public void DeleteEntitiesByComponent<T>(Predicate<T> componentPredicate = null, string[] tags = null)
    {
        try
        {
            List<ComponentDeletedEventArgs> deletedEventArgsList = new();
            enterWriteLock();
            BatchEventsCollector ec = m_batchEventsCollector;
            ICollection<IEntity> entities = query(componentPredicate, tags);

            foreach (IEntity entity in entities)
            {
                deleteEntity(entity.Id, out ComponentDeletedEventArgs[] args);
                if (args != null && args.Length > 0)
                {
                    deletedEventArgsList.AddRange(args);
                }
            }

            exitWriteLock();
            invokeIfNeeded(deletedEventArgsList.ToArray(), ec);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntityLookupBucket<TKey> CreateLookupBucket<TKey>(Predicate<IEntity>  addPredicate,
                                                              Func<IEntity, TKey> keyFactory,
                                                              IEcsRepo            ecsRepo)
    {
        try
        {
            enterWriteLock();
            EntityLookupBucket<TKey> bucket = new(ecsRepo, this, m_lock, addPredicate, keyFactory);
            IEntityCollection allCurrentEntities = new EntityCollection(m_entityComponents.Keys.Select(createEntity));
            foreach (IEntity entity in allCurrentEntities)
            {
                bucket.Add(entity);
            }

            string bucketId = Guid.NewGuid().ToString();
            m_lookupBuckets[bucketId] = bucket;
            exitWriteLock();
            return bucket;
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    #endregion

    #region Get

    public Type[] GetComponentTypes(IEntity entity)
    {
        enterReadLock();
        Type[] componentTypes = getComponentTypes(entity.Id);
        exitReadLock();
        return componentTypes;
    }

    public Component[] GetComponents(IEntity entity, Type componentType)
    {
        enterReadLock();
        Component[] components = getComponents(entity, componentType);
        exitReadLock();
        return components;
    }

    public Component[] GetAllComponents(IEntity entity)
    {
        enterReadLock();
        Component[] components = getAllComponents(entity.Id);
        exitReadLock();
        return components;
    }

    public Component GetComponent(IEntity entity, Type componentType)
    {
        enterReadLock();
        Component[] components = getComponents(entity, componentType);
        try
        {
            exitReadLock();
            return firstOrThrow(components);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public ulong GetComponentVersion(Entity entity, Type componentType)
    {
        enterReadLock();
        Component[] components = getComponents(entity, componentType);
        exitReadLock();
        Component component = firstOrThrow(components);
        return component.Version;
    }

    public TypeVersionPair[] GetComponentsVersion(IEntity entity, IEnumerable<Type> types)
    {
        enterReadLock();

        TypeVersionPair[] typeVersionPairs = types.SelectMany(t => getComponents(entity, t))
                                                  .Select(c => new TypeVersionPair(c.Data.GetType(), c.Version))
                                                  .ToArray();
        exitReadLock();
        return typeVersionPairs;
    }

    #endregion

    #region Set

    public void SetComponent<T>(IEntity entity, T component)
    {
        enterWriteLock();
        if (!exists(entity.Id))
        {
            exitWriteLock();
            return;
        }

        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();

        BatchEventsCollector ec = m_batchEventsCollector;
        addOrUpdateComponent(entity, new Component(0, component), false,
                             updatedList,
                             createdList);
        exitWriteLock();

        invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
    }

    public void SetComponents(IEntity entity, params object[] components)
    {
        enterWriteLock();
        if (!exists(entity.Id))
        {
            exitWriteLock();
            return;
        }

        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();
        BatchEventsCollector ec = m_batchEventsCollector;
        setComponents(entity, components, updatedList, createdList);
        exitWriteLock();
        invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
    }

    #endregion

    #region Tags

    public void AddTag(Entity entity, params string[] tag)
    {
        enterWriteLock();
        if (!exists(entity.Id))
        {
            exitWriteLock();
            return;
        }

        List<ComponentUpdatedEventArgs> updatedList = new(1);
        List<ComponentCreatedEventArgs> createdList = new(1);

        BatchEventsCollector ec = m_batchEventsCollector;
        addTags(entity, tag, updatedList, createdList);
        exitWriteLock();

        invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
    }

    public bool HasTag(Entity entity, params string[] tag)
    {
        enterReadLock();
        bool res = false;
        if (m_tagsForEntity.TryGetValue(entity.Id, out HashSet<string> tags))
        {
            foreach (string s in tag)
            {
                res = tags.Contains(s);
                if (!res)
                {
                    break;
                }
            }
        }

        exitReadLock();
        return res;
    }

    #endregion

    #region Atomic sequances

    public bool ConditionalSet<T>(Entity entity, T component, ComponentSetCondition<T> condition, bool setWhenNotExist = true)
    {
        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();
        bool result = false;
        try
        {
            enterWriteLock();
            if (!exists(entity.Id))
            {
                exitWriteLock();
                return false;
            }

            BatchEventsCollector ec = m_batchEventsCollector;
            Component[] components = getComponents(entity, component.GetType());
            Component comp = firstOrThrow(components);
            bool shouldSet = setWhenNotExist;
            if (comp.Data != null)
            {
                shouldSet = condition.Invoke((T)comp.Data, component);
            }

            if (shouldSet)
            {
                addOrUpdateComponent(entity, new Component(0, component), false,
                                     updatedList,
                                     createdList);
                result = true;
            }

            exitWriteLock();
            invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }

        return result;
    }

    public bool ConditionalSet<T>(Entity entity, Predicate<T> condition, Func<T, T> componentFactory, bool setWhenNotExist = true)
    {
        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();
        bool result = false;
        try
        {
            enterWriteLock();
            if (!exists(entity.Id))
            {
                exitWriteLock();
                return false;
            }

            BatchEventsCollector ec = m_batchEventsCollector;
            Component[] components = getComponents(entity, typeof(T));
            Component comp = firstOrThrow(components);
            bool shouldSet = setWhenNotExist;
            object currentComponent = comp.Data;
            if (currentComponent != null)
            {
                shouldSet = condition.Invoke((T)currentComponent);
            }

            if (shouldSet)
            {
                addOrUpdateComponent(entity, new Component(0, componentFactory((T)currentComponent)), false,
                                     updatedList,
                                     createdList);
                result = true;
            }

            exitWriteLock();
            invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }

        return result;
    }

    public void SetWithVersion<T>(Entity entity, T component, ulong version)
    {
        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();
        enterWriteLock();
        if (!exists(entity.Id))
        {
            exitWriteLock();
            return;
        }

        BatchEventsCollector ec = m_batchEventsCollector;
        addOrUpdateComponent(entity,
                             new Component(version, component),
                             true,
                             updatedList,
                             createdList);
        exitWriteLock();

        invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
    }

    public IEntity UpdateComponent<T>(Entity entity, Func<T, T> componentFactory, bool setWhenNotExist = true)
    {
        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();

        try
        {
            enterWriteLock();
            if (!exists(entity.Id))
            {
                exitWriteLock();
                return entity;
            }

            BatchEventsCollector ec = m_batchEventsCollector;
            Component[] components = getComponents(entity, typeof(T));
            Component comp = firstOrThrow(components);
            object currentComponent = comp.Data;

            if (setWhenNotExist || currentComponent != null)
            {
                T built = componentFactory(currentComponent == null ? default : (T)currentComponent);
                addOrUpdateComponent(entity,
                                     new Component(0, built), false,
                                     updatedList,
                                     createdList);
            }

            exitWriteLock();
            invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }

        return entity;
    }

    public bool SetWhenNotEqual<T>(Entity entity, T component, bool setWhenNotExist = true)
    {
        return SetWhenNotEqual<T>(entity, _ => component, setWhenNotExist);
    }

    public bool SetWhenNotEqual<T>(Entity entity, Func<T, T> componentFactory, bool setWhenNotExist = true)
    {
        List<ComponentUpdatedEventArgs> updatedList = new();
        List<ComponentCreatedEventArgs> createdList = new();
        bool result = false;
        try
        {
            enterWriteLock();
            if (!exists(entity.Id))
            {
                exitWriteLock();
                return false;
            }

            BatchEventsCollector ec = m_batchEventsCollector;
            Component[] components = getComponents(entity, typeof(T));
            Component comp = firstOrThrow(components);
            object currentComponent = comp.Data;

            if (setWhenNotExist || currentComponent != null)
            {
                T updatedComponent = componentFactory((T)currentComponent);
                if (updatedComponent != null && !updatedComponent.Equals(currentComponent))
                {
                    addOrUpdateComponent(entity, new Component(0, updatedComponent), false,
                                         updatedList,
                                         createdList);
                    result = true;
                }
            }

            exitWriteLock();
            invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }

        return result;
    }

    public IEntity CreateOrGetByComponent<T>(T component, Predicate<T> componentPredicate = null, string[] tags = null)
    {
        List<ComponentUpdatedEventArgs> updatedList = new(1);
        List<ComponentCreatedEventArgs> createdList = new(1);
        try
        {
            bool entityCreated = false;
            enterWriteLock();
            BatchEventsCollector ec = m_batchEventsCollector;
            IEntity entity;
            ICollection<IEntity> entityCollection = query(componentPredicate, tags);
            if (entityCollection.Count > 1)
            {
                exitWriteLock();
                throw new EcsException(MoreThenOneElementExceptionLabel);
            }

            if (entityCollection.Count == 1)
            {
                entity = entityCollection.First();
            }
            else
            {
                entity = createAndSaveEntity(updatedList,
                                             createdList,
                                             null, tags);

                if (tags != null && tags.Length > 0)
                {
                    addEntityToEntitiesByTag(entity.Id, tags);
                    addTagsToEntityMap(entity.Id, tags);
                }

                entityCreated = true;
            }

            addOrUpdateComponent(entity, new Component(0, component), false,
                                 updatedList,
                                 createdList);

            if (entityCreated)
            {
                addToBuckets(entity);
            }

            exitWriteLock();
            if (entityCreated)
            {
                s_log.InfoFormat("Entity created : {0} Tags = [{1}]", entity.Id, string.Join(",", tags ?? Array.Empty<string>()));
                invokeIfNeeded(new[] { new EntityCreatedEventArgs(entity, createdList.ToArray()) }, ec);
            }

            invokeIfNeeded(createdList.ToArray(), updatedList.ToArray(), ec);
            return entity;
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public void MergePackage(EcsPackage ecsPackage)
    {
        try
        {
            enterWriteLock();
            s_log.DebugFormat("merging package - Updated:{0}, Deleted:{1}, DeleteTags:{2}",
                              ecsPackage.Updated.Count,
                              ecsPackage.Deleted.Count,
                              ecsPackage.DeletedTags.Count);
            BatchEventsCollector ec = m_batchEventsCollector;
            List<EntityCreatedEventArgs> createdEntitiesList = new();
            List<ComponentUpdatedEventArgs> updatedArgsList = new();
            List<ComponentCreatedEventArgs> createdArgsList = new();
            List<ComponentDeletedEventArgs> deletedArgsList = new();

            handleUpdated(ecsPackage, updatedArgsList, createdArgsList, createdEntitiesList);
            handleDeletedTags(ecsPackage, deletedArgsList);
            handleDeletedEntities(ecsPackage, deletedArgsList);

            exitWriteLock();
            invokeIfNeeded(createdEntitiesList.ToArray(), ec);
            invokeIfNeeded(createdArgsList.ToArray(),     updatedArgsList.ToArray(), ec);
            invokeIfNeeded(deletedArgsList.ToArray(),     ec);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    private void handleDeletedEntities(EcsPackage ecsPackage, List<ComponentDeletedEventArgs> deletedArgsList)
    {
        foreach (string id in ecsPackage.Deleted)
        {
            deleteEntity(id, out ComponentDeletedEventArgs[] deletedArgs);
            if (deletedArgs.Length > 0)
            {
                deletedArgsList.AddRange(deletedArgs);
            }
        }
    }

    private void handleUpdated(EcsPackage                      ecsPackage,
                               List<ComponentUpdatedEventArgs> updatedList,
                               List<ComponentCreatedEventArgs> createdList,
                               List<EntityCreatedEventArgs>    createdEntitiesList)
    {
        foreach (KeyValuePair<string, ComponentTypeNameMap> pair in ecsPackage.Updated)
        {
            List<ComponentCreatedEventArgs> createdForEntity = new();
            string id = pair.Key;
            ecsPackage.EntityTags.TryGetValue(id, out string[] entityTags);
            IEntity entity = createOrGetWithId(id,
                                               entityTags,
                                               out bool entityCreated,
                                               updatedList,
                                               createdForEntity);

            IEnumerable<Component> components = pair.Value?.Values;
            setComponentsWithVersion(entity, components, updatedList, createdForEntity);

            if (entityCreated)
            {
                addToBuckets(entity);
                createdEntitiesList.Add(new EntityCreatedEventArgs(entity, createdForEntity.ToArray()));
            }

            createdList.AddRange(createdForEntity);
        }
    }

    private void handleDeletedTags(EcsPackage ecsPackage, List<ComponentDeletedEventArgs> deletedArgsList)
    {
        if (ecsPackage.DeletedTags is { Count: > 0 })
        {
            deleteEntitiesForTags(ecsPackage.DeletedTags.ToArray(), out ComponentDeletedEventArgs[] deletedArgs);

            if (deletedArgs is { Length: > 0 })
            {
                deletedArgsList.AddRange(deletedArgs);
            }
        }
    }

    public void BatchUpdate(IEcsRepo repo, Action<IEcsRepo> batch)
    {
        try
        {
            enterWriteLock();
            m_batchEventsCollector = new BatchEventsCollector();
            batch.Invoke(repo);
            EntityCreatedEventArgs[] createdArgs = m_batchEventsCollector.EntityCreatedEventsArgs;
            ComponentCreatedEventArgs[] componentCreatedEventArgs = m_batchEventsCollector.ComponentCreatedEventArgs;
            ComponentUpdatedEventArgs[] componentUpdatedEventArgs = m_batchEventsCollector.ComponentUpdatedEventArgs;
            ComponentDeletedEventArgs[] componentDeletedEventArgs = m_batchEventsCollector.ComponentDeletedEventArgs;
            m_batchEventsCollector = null;
            releaseAllLocks();
            if (createdArgs.Length > 0)
            {
                EntityCreatedEventArgs[] mergedCreatedArgs = mergeIfNeeded(createdArgs, componentCreatedEventArgs);
                invokeIfNeeded(mergedCreatedArgs, null);
            }
            else
            {
                invokeIfNeeded(createdArgs, null);
            }

            invokeIfNeeded(componentCreatedEventArgs, componentUpdatedEventArgs, null);
            invokeIfNeeded(componentDeletedEventArgs, null);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public T BatchQuery<T>(EcsRepo repo, Func<IEcsRepo, T> queryFunc)
    {
        enterReadLock();
        try
        {
            T res = queryFunc(repo);
            releaseAllLocks();
            return res;
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            return default;
        }        
    }

    private EntityCreatedEventArgs[] mergeIfNeeded(EntityCreatedEventArgs[]    createdArgs,
                                                   ComponentCreatedEventArgs[] componentCreatedEventArgs)
    {
        Dictionary<string, List<ComponentCreatedEventArgs>> map = new();
        List<EntityCreatedEventArgs> result = new();

        foreach (ComponentCreatedEventArgs ca in componentCreatedEventArgs)
        {
            string entityId = ca.Entity.Id;
            if (!map.TryGetValue(entityId, out List<ComponentCreatedEventArgs> args))
            {
                map[entityId] = args = new List<ComponentCreatedEventArgs>();
            }

            args.Add(ca);
        }

        foreach (EntityCreatedEventArgs ea in createdArgs)
        {
            if (map.TryGetValue(ea.Entity.Id, out List<ComponentCreatedEventArgs> componentCreated))
            {
                EntityCreatedEventArgs updated = new(ea.Entity, componentCreated.ToArray());
                result.Add(updated);
            }
            else
            {
                result.Add(ea);
            }
        }

        return result.ToArray();
    }

    #endregion

    #region Queries

    public IEntityCollection Query(Type[] componentTypes, Predicate<IEntity> entityPredicate = null, string[] tags = null)
    {
        enterReadLock();
        try
        {
            IEnumerable<IEntity> result = query(componentTypes, entityPredicate, tags);
            EntityCollection res = new(result);
            exitReadLock();
            return res;
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntityCollection QueryAll()
    {
        enterReadLock();
        IEntityCollection entityCollection = new EntityCollection(m_entityComponents.Keys.Select(createEntity));
        exitReadLock();
        return entityCollection;
    }

    public IEntity QuerySingle(Type[] componentTypes, Predicate<IEntity> entityPredicate = null, string[] tags = null)
    {
        enterReadLock();
        try
        {
            IEntity[] res = query(componentTypes, entityPredicate, tags).ToArray();
            exitReadLock();
            return firstOrThrow(res);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntity QuerySingle(Type componentType, Func<object, IEntity, bool> entityPredicate = null, string[] tags = null)
    {
        try
        {
            enterReadLock();
            ICollection<IEntity> result = query(componentType, entityPredicate, tags);
            exitReadLock();
            return firstOrThrow(result);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntity QuerySingle<T>(Func<T, IEntity, bool> entityPredicate, string[] tags = null)
    {
        enterReadLock();
        try
        {
            ICollection<IEntity> result = query(entityPredicate, tags);
            exitReadLock();
            return firstOrThrow(result);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntity QuerySingle(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new EcsException("id Cannot be null or empty");
        }

        IEntity res = null;
        enterReadLock();
        if (exists(id))
        {
            res = createEntity(id);
        }

        exitReadLock();

        return res;
    }

    private bool exists(string id) => m_entityComponents.ContainsKey(id);

    public IEntity QuerySingle<T>(string[] tags = null)
    {
        enterReadLock();
        try
        {
            Type type = typeof(T);
            if (m_entitiesByType.TryGetValue(type, out HashSet<string> entityIds))
            {
                HashSet<string> entitiesForTags = getEntitiesForTags(tags);
                if (entitiesForTags != null)
                {
                    string[] filtered = entityIds.Where(id => entitiesForTags.Contains(id)).ToArray();
                    return firstOrThrow(filtered, type);
                }

                return firstOrThrow(entityIds, type);
            }
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
        finally
        {
            if (m_lock.IsReadLockHeld)
            {
                exitReadLock();
            }
        }

        return null;
    }

    public IEntity QuerySingle<T>(Predicate<T> componentPredicate, string[] tags = null)
    {
        enterReadLock();
        try
        {
            ICollection<IEntity> result = query(componentPredicate, tags);
            exitReadLock();
            return firstOrThrow(result);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntityCollection Query(Type componentType, Func<object, IEntity, bool> entityPredicate = null, string[] tags = null)
    {
        enterReadLock();
        try
        {
            ICollection<IEntity> result = query(componentType, entityPredicate, tags);
            exitReadLock();
            return new EntityCollection(result);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntityCollection Query<T>(Predicate<T> componentPredicate, string[] tags = null)
    {
        enterReadLock();
        try
        {
            ICollection<IEntity> result = query(componentPredicate, tags);
            exitReadLock();
            return new EntityCollection(result);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntityCollection Query(params IEntity[] entities)
    {
        enterReadLock();
        IList<IEntity> result = new List<IEntity>();
        foreach (IEntity entity in entities)
        {
            if (m_entityComponents.ContainsKey(entity.Id))
            {
                result.Add(createEntity(entity.Id));
            }
        }

        exitReadLock();
        return new EntityCollection(result);
    }

    public IEntityCollection Query<T>(string[] tags = null)
    {
        IEntityCollection res = EntityCollection.Empty;

        enterReadLock();
        Type componentType = typeof(T);
        if (m_entitiesByType.TryGetValue(componentType, out HashSet<string> entityIds))
        {
            HashSet<string> entitiesForTags = getEntitiesForTags(tags);
            IEnumerable<string> e = entityIds;
            if (entitiesForTags != null)
            {
                e = e.Where(id => entitiesForTags.Contains(id));
            }

            IEnumerable<IEntity> enumerable = e.Select(id => createEntityAndAddType(id, componentType));
            res = new EntityCollection(enumerable);
            exitReadLock();
        }
        else
        {
            exitReadLock();
        }

        return res;
    }

    private IEntity createEntityAndAddType(string entityId, Type componentType)
    {
        IEntity entity = createEntity(entityId);
        getComponents(entity, componentType);
        return entity;
    }

    public IEntityCollection Query(params string[] ids)
    {
        enterReadLock();
        IList<IEntity> entities = new List<IEntity>();
        foreach (string id in ids)
        {
            if (id == null)
            {
                continue;
            }

            if (m_entityComponents.ContainsKey(id))
            {
                entities.Add(createEntity(id));
            }
        }

        exitReadLock();
        return new EntityCollection(entities);
    }

    public IEntityCollection Query<T>(Func<T, IEntity, bool> entityPredicate, string[] tags = null)
    {
        enterReadLock();
        try
        {
            ICollection<IEntity> result = query(entityPredicate, tags);
            exitReadLock();
            return new EntityCollection(result);
        }
        catch (Exception e)
        {
            logAndReleaseLock(e);
            throw;
        }
    }

    public IEntityCollection QueryByTags(string[] tags)
    {
        HashSet<string> result = new();
        enterReadLock();
        foreach (string tag in tags)
        {
            if (m_entitiesByTag.TryGetValue(tag, out HashSet<string> entities))
            {
                foreach (string e in entities)
                {
                    result.Add(e);
                }
            }
        }

        IEntityCollection entityCollection = new EntityCollection(result.Select(createEntityWithAllComponents));
        exitReadLock();
        return entityCollection;
    }

    public IEntity QuerySingleByTags(params string[] tags)
    {
        IEntity result = null;
        enterReadLock();
        foreach (string tag in tags)
        {
            if (m_entitiesByTag.TryGetValue(tag, out HashSet<string> entities))
            {
                if (entities.Count > 0)
                {
                    result = createEntity(entities.First());
                    break;
                }
            }
        }

        exitReadLock();
        return result;
    }

    #endregion

    #region private functions

    private void enterWriteLock()
    {
        m_lock.EnterWriteLock();
    }

    private void exitWriteLock()
    {
        m_lock.ExitWriteLock();
    }

    private void enterReadLock()
    {
        m_lock.EnterReadLock();
    }

    private void exitReadLock()
    {
        m_lock.ExitReadLock();
    }

    private void logAndReleaseLock(Exception e)
    {
        releaseAllLocks();
        s_log.Error("Error in ECS", e);
    }

    private void releaseAllLocks()
    {
        while (m_lock.IsReadLockHeld)
        {
            m_lock.ExitReadLock();
        }

        while (m_lock.IsWriteLockHeld)
        {
            m_lock.ExitWriteLock();
        }
    }

    private HashSet<string> getEntitiesForTags(string[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return null;
        }

        HashSet<string> result = new();
        foreach (string tag in tags)
        {
            if (m_entitiesByTag.TryGetValue(tag, out HashSet<string> entities))
            {
                foreach (string entity in entities)
                {
                    result.Add(entity);
                }
            }
        }

        return result;
    }

    private IEntity createEntity(string entityId)
    {
        m_tagsForEntity.TryGetValue(entityId, out HashSet<string> tags);
        return new Entity(this, entityId, tags);
    }

    private IEntity createEntityWithAllComponents(string entityId)
    {
        m_tagsForEntity.TryGetValue(entityId, out HashSet<string> tags);
        IEntity entity = new Entity(this, entityId, tags);
        entity = setComponentCache(entity);
        return entity;
    }

    private IEntity setComponentCache(IEntity entity)
    {
        Component[] allComponents = getAllComponents(entity.Id);
        ((IEntityInternal) entity).SetComponentsInEntityCache(allComponents);
        return entity; 
    }

    private IEnumerable<IEntity> query(Type[] componentType, Predicate<IEntity> entityPredicate = null, string[] tags = null)
    {
        entityPredicate ??= _ => true;

        HashSet<string> entitiesForTags = getEntitiesForTags(tags);

        IEnumerable<Type> stream = componentType;

        IEnumerable<string> idsStream = stream.Select(m_entitiesByType.GetValueOrDefault)
                                              .Where(set => set != null)
                                              .SelectMany(set => set);

        if (entitiesForTags != null)
        {
            idsStream = idsStream.Where(id => entitiesForTags.Contains(id));
        }

        return idsStream
               .Distinct()
               .Select(id => getByEntityPredicate(id, entityPredicate))
               .Where(e => e != null)
               .Select(setComponentCache);
        
    }

    private IEntity addComponentsToEntity(IEntity entity, Type[] componentTypes)
    {
        if (componentTypes != null)
        {
            foreach (Type componentType in componentTypes)
            {
                getComponents(entity, componentType);
            }
        }

        return entity;
    }

    private ICollection<IEntity> query(Type componentType, Func<object, IEntity, bool> entityPredicate = null, string[] tags = null)
    {
        Type type = componentType;

        if (!m_entitiesByType.TryGetValue(type, out HashSet<string> entityIds))
        {
            return Array.Empty<IEntity>();
        }

        entityPredicate ??= (_, _) => true;
        HashSet<string> entitiesForTags = getEntitiesForTags(tags);
        IEnumerable<string> stream = entityIds;

        if (entitiesForTags != null)
        {
            stream = stream.Where(id => entitiesForTags.Contains(id));
        }

        return stream
               .Select(id => getByComponentAndEntityPredicate(id, type, entityPredicate))
               .Where(e => e != null)
               .Select(setComponentCache)
                           .ToArray();
    }

    private ICollection<IEntity> query<T>(Func<T, IEntity, bool> entityPredicate, string[] tags = null)
    {
        Type type = typeof(T);
        entityPredicate ??= (_, _) => true;
        if (!m_entitiesByType.TryGetValue(type, out HashSet<string> entityIds))
        {
            return Array.Empty<IEntity>();
        }

        HashSet<string> entitiesForTags = getEntitiesForTags(tags);

        IEnumerable<string> stream = entityIds;

        if (entitiesForTags != null)
        {
            stream = stream.Where(id => entitiesForTags.Contains(id));
        }

        return stream
               .Select(id => getByComponentAndEntityPredicate(id, entityPredicate))
               .Where(e => e != null)
               .Select(setComponentCache)
               .ToArray();
    }

    private ICollection<IEntity> query<T>(Predicate<T> componentPredicate = null, string[] tags = null)
    {
        Type type = typeof(T);
        if (!m_entitiesByType.TryGetValue(type, out HashSet<string> entityIds))
        {
            return Array.Empty<IEntity>();
        }

        componentPredicate ??= _ => true;

        HashSet<string> entitiesForTags = getEntitiesForTags(tags);

        IEnumerable<string> stream = entityIds;

        if (entitiesForTags != null)
        {
            stream = stream.Where(id => entitiesForTags.Contains(id));
        }

        return stream
               .Select(id => getByComponentPredicate(id, type, componentPredicate))
               .Where(e => e != null)
               .Select(setComponentCache)
               .ToArray();
    }

    private IEntity getByComponentPredicate<T>(string id, Type type, Predicate<T> componentPredicate)
    {
        Component[] components = getComponentsById(id, type);

        foreach (Component component in components)
        {
            if (component.Data == null || !componentPredicate((T)component.Data))
            {
                continue;
            }

            IEntity entity = createEntity(id);
            ((IEntityInternal)entity).SetComponentsInEntityCache(components);
            return entity;
        }

        return null;
    }

    private IEntity getByEntityPredicate(string id, Predicate<IEntity> entityPredicate)
    {
        IEntity entity = createEntity(id);
        if (entityPredicate(entity))
        {
            return entity;
        }

        return null;
    }

    private IEntity getByComponentAndEntityPredicate<T>(string id, Func<T, IEntity, bool> entityPredicate)
    {
        Type componentType = typeof(T);
        Component[] components = getComponentsById(id, componentType);

        foreach (Component component in components)
        {
            if (component.Data == null)
            {
                continue;
            }

            IEntity entity = createEntity(id);
            ((IEntityInternal)entity).SetComponentsInEntityCache(components);
            if (entityPredicate((T)component.Data, entity))
            {
                return entity;
            }
        }

        return null;
    }

    private IEntity getByComponentAndEntityPredicate(string id, Type componentType, Func<object, IEntity, bool> entityPredicate)
    {
        Component[] components = getComponentsById(id, componentType);

        foreach (Component component in components)
        {
            if (component.Data == null)
            {
                continue;
            }

            IEntity entity = createEntity(id);
            ((IEntityInternal)entity).SetComponentsInEntityCache(components);
            if (entityPredicate(component.Data, entity))
            {
                return entity;
            }
        }

        return null;
    }

    private Component[] getComponents(IEntity entity, Type componentType)
    {
        Component[] components = getComponentsById(entity.Id, componentType);
        if (components.Length > 0)
        {
            ((IEntityInternal)entity).SetComponentsInEntityCache(components);
        }

        return components;
    }

    private Component[] getComponentsById(string entityId, Type componentType)
    {
        if (m_entityComponents.TryGetValue(entityId, out ParentTypeMap parentTypeMap))
        {
            if (parentTypeMap.TryGetValue(componentType, out TypeMap typeMap))
            {
                Component[] components = typeMap.Values.ToArray();
                return components;
            }
        }

        return Array.Empty<Component>();
    }

    private Component[] getAllComponents(string entityId)
    {
        List<Component> result = new();
        if (m_entityComponents.TryGetValue(entityId, out ParentTypeMap parentTypeMap))
        {
            foreach (KeyValuePair<Type, TypeMap> pair in parentTypeMap)
            {
                Type parentType = pair.Key;
                if (pair.Value.Count == 1)
                {
                    if (pair.Value.ContainsKey(parentType)) // the actual component, not the interfaces type.
                    {
                        result.Add(pair.Value[pair.Key]);
                    }
                }
            }
        }

        return result.ToArray();
    }

    private IEntity createAndSaveEntity(List<ComponentUpdatedEventArgs> updatedList,
                                        List<ComponentCreatedEventArgs> createdList,
                                        string                          id = null, string[] tags = null)
    {
        IEntity entity = new Entity(this, id, tags);
        m_entityComponents[entity.Id] = new ParentTypeMap();
        if (tags != null)
        {
            addTags(entity, tags, updatedList, createdList);
        }

        s_log.InfoFormat("Entity created : {0} Tags = [{1}]", entity.Id, string.Join(",", tags ?? Array.Empty<string>()));
        return entity;
    }

    private void addToBuckets(IEntity entity)
    {
        if (m_lookupBuckets.Count <= 0)
        {
            return;
        }

        foreach (IEntityLookupBucketInternal bucket in m_lookupBuckets.Values)
        {
            bucket.Add(entity);
        }
    }

    private IEntity createOrGetWithId(string                          id,
                                      string[]                        tags,
                                      out bool                        entityCreated,
                                      List<ComponentUpdatedEventArgs> updatedList,
                                      List<ComponentCreatedEventArgs> createdList)
    {
        entityCreated = false;
        IEntity entity;
        if (m_entityComponents.ContainsKey(id))
        {
            if (!m_tagsForEntity.TryGetValue(id, out HashSet<string> existingTags))
            {
                existingTags = new HashSet<string>();
            }

            entity = new Entity(this, id, existingTags);

            if (tags != null && existingTags.Count != tags.Length)
            {
                addTags(entity, tags, updatedList, createdList);
            }
        }
        else
        {
            entity = createAndSaveEntity(updatedList, createdList, id, tags);
            entityCreated = true;
        }

        return entity;
    }

    private void deleteEntity(string id, out ComponentDeletedEventArgs[] deletedArgs)
    {
        if (m_lookupBuckets.Count > 0)
        {
            if (m_entityComponents.ContainsKey(id))
            {
                IEntity entity = createEntity(id);
                foreach (IEntityLookupBucketInternal bucket in m_lookupBuckets.Values.AsParallel())
                {
                    bucket.Remove(entity);
                }
            }
        }

        removeTags(id, out HashSet<string> tags);
        string[] tagsArray = tags?.ToArray() ?? Array.Empty<string>();
        deleteEntityInTypeEntitiesDictionary(id, tagsArray, out deletedArgs);
        m_entityComponents.Remove(id);
        s_log.InfoFormat("Entity deleted : {0} Tags = [{1}]", id, string.Join(",", tagsArray));
    }

    private void removeTags(string id, out HashSet<string> tags)
    {
        if (m_tagsForEntity.TryGetValue(id, out tags))
        {
            foreach (string tag in tags)
            {
                if (m_entitiesByTag.TryGetValue(tag, out HashSet<string> entitiesForTag))
                {
                    entitiesForTag.Remove(id);
                }
            }
        }

        m_tagsForEntity.Remove(id);
    }

    private void setComponents(IEntity                         entity, object[] components,
                               List<ComponentUpdatedEventArgs> updatedList,
                               List<ComponentCreatedEventArgs> createdList)
    {
        foreach (object component in components)
        {
            Component component1 = new(0, component);
            addOrUpdateComponent(entity, component1, false,
                                 updatedList,
                                 createdList);
        }
    }

    private void setComponentsWithVersion(IEntity                         entity,
                                          IEnumerable<Component>          components,
                                          List<ComponentUpdatedEventArgs> updatedList,
                                          List<ComponentCreatedEventArgs> createdList)
    {
        foreach (Component component in components)
        {
            addOrUpdateComponent(entity, component, false,
                                 updatedList,
                                 createdList);
        }
    }

    private void addOrUpdateComponent(IEntity                         entity,
                                      Component                       component,
                                      bool                            overrideVersion,
                                      List<ComponentUpdatedEventArgs> updatedList,
                                      List<ComponentCreatedEventArgs> createdList)
    {
        Type componentType = component.Data.GetType();

        IEnumerable<Type> componentTypes = TypeFamilyProvider.GetTypeFamily(componentType);

        ParentTypeMap parentTypeMap = m_entityComponents.ComputeIfAbsent(entity.Id, _ => new ParentTypeMap());

        addComponentTypes(entity, component, componentTypes, parentTypeMap, overrideVersion, updatedList, createdList);
    }

    private void addComponentTypes(IEntity                         entity,
                                   Component                       component,
                                   IEnumerable<Type>               componentTypes,
                                   ParentTypeMap                   parentTypeMap,
                                   bool                            overrideVersion,
                                   List<ComponentUpdatedEventArgs> updatedList,
                                   List<ComponentCreatedEventArgs> createdList)
    {
        Type componentType = component.Data.GetType();
        string entityId = entity.Id;
        bool versionUpdated = false;

        foreach (Type type in componentTypes)
        {
            bool componentExists = false;
            Component oldComponent = Component.Empty;

            TypeMap dictComponents = parentTypeMap.ComputeIfAbsent(type, _ => new TypeMap());

            if (!versionUpdated && !overrideVersion)
            {
                componentExists = dictComponents.TryGetValue(componentType, out Component currentComponent);
                if (componentExists)
                {
                    bool existingComponentIsNewer = setVersion(entityId, ref component, currentComponent, type);
                    if (existingComponentIsNewer)
                    {
                        break;
                    }

                    oldComponent = currentComponent;
                }
                else if (component.Version <= 0)
                {
                    component.Version = 1;
                }

                versionUpdated = true;
            }

            dictComponents[componentType] = component;

            if (!m_entitiesByType.TryGetValue(type, out HashSet<string> entityIds))
            {
                entityIds = new HashSet<string>();
                m_entitiesByType[type] = entityIds;
            }

            entityIds.Add(entityId);

            if (type != component.Data.GetType())
            {
                continue;
            }

            m_tagsForEntity.TryGetValue(entityId, out HashSet<string> tags);
            if (!componentExists)
            {
                createdList.Add(new ComponentCreatedEventArgs(new Entity(this, entityId, tags),
                                                              componentType, component));
            }

            updatedList.Add(new ComponentUpdatedEventArgs(new Entity(this, entityId, tags),
                                                          componentType, component, oldComponent));
        }

        ((IEntityInternal)entity).SetComponentsInEntityCache(component);
    }

    private static bool setVersion(string entityId, ref Component component, Component existingComponent, Type type)
    {
        bool newerComponentExists = false;

        if (component.Version <= 0)
        {
            if (existingComponent.Version == ulong.MaxValue)
            {
                component.Version = 1;
            }
            else
            {
                component.Version = existingComponent.Version + 1;
            }
        }
        else if (existingComponent.Version > component.Version && existingComponent.Version != ulong.MaxValue)
        {
            newerComponentExists = true;
            s_log.WarnFormat("An older version of component exist and will not be updated : entity: {0}, type:{1}, oldVersion:{2}, newVersion:{3}",
                             entityId,
                             type.FullName,
                             existingComponent.Version,
                             component.Version);
        }

        return newerComponentExists;
    }

    private Type[] getComponentTypes(string id)
    {
        if (m_entityComponents.TryGetValue(id, out ParentTypeMap parentTypeMap))
        {
            return parentTypeMap.Keys.ToArray();
        }

        return Type.EmptyTypes;
    }

    private void deleteEntityInTypeEntitiesDictionary(string id, string[] tags, out ComponentDeletedEventArgs[] deletedArgs)
    {
        List<ComponentDeletedEventArgs> argsList = new();
        if (!m_entityComponents.TryGetValue(id, out ParentTypeMap parentTypeMap))
        {
            deletedArgs = Array.Empty<ComponentDeletedEventArgs>();
            return;
        }

        foreach (KeyValuePair<Type, TypeMap> pair in parentTypeMap)
        {
            Type componentType = pair.Key;
            if (componentType == typeof(EntityTags))
            {
                continue;
            }

            Dictionary<Type, Component> finalTypes = pair.Value;
            foreach (KeyValuePair<Type, Component> finalTypesPair in finalTypes)
            {
                if (finalTypesPair.Key == componentType)
                {
                    ComponentDeletedEventArgs args = new(new Entity(this, id, tags),
                                                         componentType,
                                                         finalTypesPair.Value);
                    argsList.Add(args);
                }
            }

            if (m_entitiesByType.TryGetValue(componentType, out HashSet<string> entities))
            {
                entities.Remove(id);
            }
        }

        deletedArgs = argsList.ToArray();
    }

    private IEntity firstOrThrow(ICollection<string> ids, Type componentType = null)
    {
        if (ids.Count == 0)
        {
            return null;
        }

        if (ids.Count > 1)
        {
            throw new EcsException(MoreThenOneElementExceptionLabel + $": {ids.Count}");
        }

        IEntity entity = createEntity(ids.First());
        if (componentType != null)
        {
            getComponents(entity, componentType);
        }

        return entity;
    }

    private Component firstOrThrow(Component[] components)
    {
        if (components != null && components.Length > 0)
        {
            if (components.Length > 1)
            {
                throw new EcsException(MoreThenOneElementExceptionLabel);
            }

            return components[0];
        }

        return Component.Empty;
    }

    private IEntity firstOrThrow(ICollection<IEntity> entities)
    {
        if (entities.Count > 1)
        {
            throw new EcsException(MoreThenOneElementExceptionLabel);
        }

        return entities.FirstOrDefault();
    }

    private void invokeIfNeeded(EntityCreatedEventArgs[] entityCreatedArgs, BatchEventsCollector collector)
    {
        if (collector != null)
        {
            collector.Add(entityCreatedArgs);
            return;
        }

        if (entityCreatedArgs.Length > 0)
        {
            OnEntitiesCreated?.Invoke(entityCreatedArgs);
        }
    }

    private void invokeIfNeeded(ComponentDeletedEventArgs[] deletedArgs, BatchEventsCollector collector)
    {
        if (collector != null)
        {
            collector.Add(deletedArgs);
            return;
        }

        if (deletedArgs.Length > 0)
        {
            OnComponentDeleted?.Invoke(deletedArgs);
        }
    }

    private void invokeIfNeeded(ComponentCreatedEventArgs[] createdArgs, ComponentUpdatedEventArgs[] updatedArgs, BatchEventsCollector collector)
    {
        if (collector != null)
        {
            collector.Add(createdArgs);
            collector.Add(updatedArgs);
            return;
        }

        if (createdArgs.Length > 0)
        {
            OnComponentCreated?.Invoke(createdArgs);
        }

        if (updatedArgs.Length > 0)
        {
            OnComponentUpdated?.Invoke(updatedArgs);
        }
    }

    //private void invokeIfNeeded(ComponentUpdatedEventArgs updatedArgs, ComponentCreatedEventArgs createdArgs, BatchEventsCollector collector)
    //{
    //    ComponentUpdatedEventArgs[] updated = updatedArgs == null ? Array.Empty<ComponentUpdatedEventArgs>() : new[] { updatedArgs };
    //    ComponentCreatedEventArgs[] created = createdArgs == null ? Array.Empty<ComponentCreatedEventArgs>() : new[] { createdArgs };
    //    invokeIfNeeded(created, updated, collector);
    //}

    private void addEntityToEntitiesByTag(string entityId, IEnumerable<string> tag)
    {
        foreach (string s in tag)
        {
            if (!m_entitiesByTag.TryGetValue(s, out HashSet<string> entities))
            {
                entities = new HashSet<string>();
                m_entitiesByTag[s] = entities;
            }

            entities.Add(entityId);
        }
    }

    private EntityTags addTagsToEntityMap(string entityId, IEnumerable<string> tag)
    {
        if (!m_tagsForEntity.TryGetValue(entityId, out HashSet<string> tags))
        {
            tags = new HashSet<string>();
            m_tagsForEntity[entityId] = tags;
        }

        foreach (string s in tag)
        {
            tags.Add(s);
        }

        return new EntityTags(tags);
    }

    private void deleteEntitiesForTags(string[] toDelete, out ComponentDeletedEventArgs[] deletedArgs)
    {
        List<ComponentDeletedEventArgs> deletedEventArgsList = new();
        HashSet<string> entitiesForTags = getEntitiesForTags(toDelete);
        foreach (string id in entitiesForTags)
        {
            deleteEntity(id, out ComponentDeletedEventArgs[] args);
            if (args != null && args.Length > 0)
            {
                deletedEventArgsList.AddRange(args);
            }
        }

        deletedArgs = deletedEventArgsList.ToArray();
    }

    private void addTags(IEntity                         entity, string[] tag,
                         List<ComponentUpdatedEventArgs> updatedList,
                         List<ComponentCreatedEventArgs> createdList)
    {
        if (tag.Length > 0)
        {
            EntityTags tgsComponent = addTagsToEntityMap(entity.Id, tag);
            addEntityToEntitiesByTag(entity.Id, tag);
            addOrUpdateComponent(entity, new Component(0, tgsComponent), false,
                                 updatedList,
                                 createdList);
        }
    }

    #endregion
}