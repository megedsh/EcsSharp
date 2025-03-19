using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using EcsSharp.Events.EventArgs;
using EcsSharp.Storage;

namespace EcsSharp.Distribute;

public class EcsPackage
{
    private readonly HashSet<string>                          m_deleted      = new();
    private readonly Dictionary<string, string[]>             m_entityTags   = new();
    private readonly ReaderWriterLockSlim                     m_lock         = new(LockRecursionPolicy.NoRecursion);
    private readonly HashSet<string>                          m_tagsToDelete = new();
    private readonly Dictionary<string, ComponentTypeNameMap> m_updated      = new();
    private          ReadOnlyCollection<string>               m_deletedCache;
    private          ReadOnlyDictionary<string, string[]>     m_entityTagsCache;
    private          ReadOnlyCollection<string>               m_tagsToDeleteCache;

    private ReadOnlyDictionary<string, ComponentTypeNameMap> m_updatedCache;

    public ReadOnlyDictionary<string, ComponentTypeNameMap> Updated
    {
        get
        {
            ReadOnlyDictionary<string, ComponentTypeNameMap> res = m_updatedCache;
            if (res == null)
            {
                m_lock.EnterReadLock();
                res = m_updatedCache ??= new ReadOnlyDictionary<string, ComponentTypeNameMap>(m_updated.ToDictionary(pair => pair.Key, pair => pair.Value));
                m_lock.ExitReadLock();
            }

            return res;
        }
    }

    public ReadOnlyCollection<string> Deleted
    {
        get
        {
            ReadOnlyCollection<string> res = m_deletedCache;
            if (res == null)
            {
                m_lock.EnterReadLock();
                res = m_deletedCache ??= new ReadOnlyCollection<string>(m_deleted.ToList());
                m_lock.ExitReadLock();
            }

            return res;
        }
    }

    public ReadOnlyCollection<string> DeletedTags
    {
        get
        {
            ReadOnlyCollection<string> res = m_tagsToDeleteCache;
            if (res == null)
            {
                m_lock.EnterReadLock();
                res = m_tagsToDeleteCache ??= new ReadOnlyCollection<string>(m_tagsToDelete.ToList());
                m_lock.ExitReadLock();
            }

            return res;
        }
    }

    public ReadOnlyDictionary<string, string[]> EntityTags
    {
        get
        {
            ReadOnlyDictionary<string, string[]> res = m_entityTagsCache;
            if (res == null)
            {
                m_lock.EnterReadLock();
                res = m_entityTagsCache ??= new ReadOnlyDictionary<string, string[]>(m_entityTags.ToDictionary(pair => pair.Key, pair => pair.Value));
                m_lock.ExitReadLock();
            }

            return res;
        }
    }

    public EcsPackage AddComponent(IEntity entity, params Component[] components) => addComponent(entity, components);

    public EcsPackage AddComponent<T>(IEntity entity)
    {
        Component component = entity.GetComponent(typeof(T));
        if (component.Data != null)
        {
            addComponent(entity, component);
        }

        return this;
    }

    public EcsPackage AddComponent(string entityId, string[] tags, params Component[] components)
    {
        m_lock.EnterWriteLock();
        addMultipleComponents(entityId, components, tags);
        m_lock.ExitWriteLock();
        return this;
    }

    public EcsPackage AddFromEvent(ComponentEventArgs componentEvent) => addFromEventComponent(componentEvent);

    public EcsPackage AddFromEvent<T>(EntityEventArgs<T> eventsArg)
        where T : ComponentEventArgs
    {
        foreach (T componentsEvents in eventsArg.ComponentEventArgs)
        {
            addFromEventComponent(componentsEvents);
        }

        return this;
    }

    public EcsPackage AddFromEvent<T>(IReadOnlyCollection<EntityEventArgs<T>> eventsArg)
        where T : ComponentEventArgs
    {
        foreach (EntityEventArgs<T> entityEventArgs in eventsArg)
        {
            foreach (T componentsEvents in entityEventArgs.ComponentEventArgs)
            {
                addFromEventComponent(componentsEvents);
            }
        }

        return this;
    }

    public EcsPackage AddAllComponents(params IEntity[] entities)
    {
        if (entities.Any())
        {
            m_lock.EnterWriteLock();
            foreach (IEntity entity in entities)
            {
                Component[] allComponents = entity.GetAllComponents();
                addMultipleComponents(entity.Id, allComponents, entity.Tags);
            }

            m_lock.ExitWriteLock();
        }

        return this;
    }

    public EcsPackage AddDeletedEntity(params IEntity[] entity) => addDeletedEntity(entity);

    public EcsPackage AddDeletedEntity(params string[] entityId)
    {
        if (entityId.Any())
        {
            m_lock.EnterWriteLock();
            m_deletedCache = null;

            foreach (string id in entityId)
            {
                m_deleted.Add(id);
            }

            m_lock.ExitWriteLock();
        }

        return this;
    }

    public EcsPackage AddDeleteByTag(params string[] tags)
    {
        if (tags.Any())
        {
            m_lock.EnterWriteLock();
            m_tagsToDeleteCache = null;

            foreach (string id in tags)
            {
                m_tagsToDelete.Add(id);
            }

            m_lock.ExitWriteLock();
        }

        return this;
    }

    public void Clear()
    {
        m_lock.EnterWriteLock();
        m_updated.Clear();
        m_deleted.Clear();
        m_tagsToDelete.Clear();
        m_entityTags.Clear();

        m_deletedCache = null;
        m_entityTagsCache = null;
        m_tagsToDeleteCache = null;
        m_updatedCache = null;
        m_lock.ExitWriteLock();
    }

    private void addMultipleComponents(string entityId, IEnumerable<Component> components, string[] tags)
    {
        m_updatedCache = null;

        if (!m_updated.TryGetValue(entityId, out ComponentTypeNameMap dict))
        {
            dict = new ComponentTypeNameMap();
            m_updated.Add(entityId, dict);
        }

        foreach (Component component in components)
        {
            if (component.Data is EntityTags)
            {
                continue;
            }

            if (component.Data != null)
            {
                string typeName = component.Data.GetType().FullName ?? string.Empty;
                dict[typeName] = component;
            }
        }

        if (tags != null && tags.Length > 0)
        {
            m_entityTags[entityId] = tags.ToArray();
        }
    }

    private EcsPackage addFromEventComponent(ComponentEventArgs componentEvent)
    {
        switch (componentEvent)
        {
            case ComponentCreatedEventArgs:
            case ComponentUpdatedEventArgs:
                addComponent(componentEvent.Entity, componentEvent.Component);
                return this;
            case ComponentDeletedEventArgs:
                addDeletedEntity(componentEvent.Entity);
                return this;
        }

        return this;
    }

    private EcsPackage addComponent(IEntity entity, params Component[] components)
    {
        m_lock.EnterWriteLock();
        addMultipleComponents(entity.Id, components, entity.Tags);
        m_lock.ExitWriteLock();
        return this;
    }

    private EcsPackage addDeletedEntity(params IEntity[] entity)
    {
        if (entity.Any())
        {
            m_lock.EnterWriteLock();
            m_deletedCache = null;
            foreach (IEntity e in entity)
            {
                m_deleted.Add(e.Id);
            }

            m_lock.ExitWriteLock();
        }

        return this;
    }
}