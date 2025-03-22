using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EcsSharp.Storage;

namespace EcsSharp;

public class Entity : IEntity, IEntityInternal
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ConcurrentDictionary<Type, Component> m_cachedComponents = new();
    private readonly IEcsStorage                           m_ecsStorage;
    private readonly object                                m_sync = new();
    private readonly HashSet<string>                       m_tags;
    
    private HashSet<string> m_readOnlyTags;

    internal Entity(IEcsStorage ecsStorage, string entityId, IEnumerable<string> tags = null)
    {
        m_ecsStorage = ecsStorage;
        Id = entityId ?? Guid.NewGuid().ToString();
        m_tags = new HashSet<string>(tags ?? Array.Empty<string>());
    }

    public string Id { get; }

    public string[] Tags => getTagArray();

    public Component GetComponent(Type componentType) => getAndCacheComponent(componentType);

    public T GetComponent<T>()
    {
        Component component = getAndCacheComponent(typeof(T));

        if (component.Data != null)
        {
            return (T)component.Data;
        }

        return default;
    }

    public T[] GetComponents<T>()
    {
        return getAndCacheComponents(typeof(T))
               .Select(c => c.Data)
               .Cast<T>().ToArray();
    }

    public Component[] GetComponents(Type componentType)    => getAndCacheComponents(componentType);
    public Component[] GetAllComponents()                   => m_ecsStorage.GetAllComponents(this);
    public Component   RefreshComponent(Type componentType) => getAndRefreshCache(componentType);

    public T RefreshComponent<T>()
    {
        Component component = getAndRefreshCache(typeof(T));

        if (component.Data != null)
        {
            return (T)component.Data;
        }

        return default;
    }

    public T[]         RefreshComponents<T>()                => GetComponents<T>();
    public Component[] RefreshComponents(Type componentType) => GetComponents(componentType);
    public Component[] RefreshAllComponents()                => GetAllComponents();

    public IEntity SetComponent<T>(T component)
    {
        setComponent(component);
        return this;
    }

    public IEntity SetComponents(params object[] components)
    {
        checkComponentsVersionConflict(components);
        m_ecsStorage.SetComponents(this, components);
        return this;
    }

    public IEntity SetWithVersion<T>(T component, ulong version)
    {
        m_ecsStorage.SetWithVersion(this, component, version);
        m_cachedComponents.TryRemove(component.GetType(), out Component _);
        return this;
    }

    public bool    ConditionalSet<T>(T            component, ComponentSetCondition<T> condition,        bool setWhenNotExist = true) => m_ecsStorage.ConditionalSet(this, component, condition,        setWhenNotExist);
    public bool    ConditionalSet<T>(Predicate<T> condition, Func<T, T>               componentFactory, bool setWhenNotExist = true) => m_ecsStorage.ConditionalSet(this, condition, componentFactory, setWhenNotExist);
    public bool    SetWhenNotEqual<T>(T           component)                                     => m_ecsStorage.SetWhenNotEqual(this, component);
    public bool    SetWhenNotEqual<T>(Func<T, T>  componentFactory, bool setWhenNotExist = true) => m_ecsStorage.SetWhenNotEqual(this, componentFactory, setWhenNotExist);
    public IEntity UpdateComponent<T>(Func<T, T>  componentFactory, bool setWhenNotExist = true) => m_ecsStorage.UpdateComponent(this, componentFactory, setWhenNotExist);
    public bool    HasComponent<T>()       => HasComponent(typeof(T));
    public bool    HasComponent(Type type) => GetComponent(type).Data != null;

    public IEntity AddTag(params string[] tag)
    {
        m_ecsStorage.AddTag(this, tag);
        lock(m_sync)
        {
            m_readOnlyTags = null;
            foreach (string s in tag)
            {
                m_tags.Add(s);
            }
        }

        return this;
    }

    public bool HasTag(params string[] tag)
    {
        bool res = false;
        HashSet<string> readOnlyTags = getReadOnlyTags();
        foreach (string s in tag)
        {
            res = readOnlyTags.Contains(s);
            if (!res)
            {
                break;
            }
        }

        return res;
    }
    
    public Component[] CachedComponents => m_cachedComponents.Values.ToArray();
    public bool Exists()
    {
        return m_ecsStorage.QuerySingle(Id) !=null;
    }

    private HashSet<string> getReadOnlyTags()
    {
        HashSet<string> tmp = m_readOnlyTags;
        if (tmp == null)
        {
            lock(m_sync)
            {
                tmp = m_readOnlyTags ??= new HashSet<string>(m_tags);
            }
        }

        return tmp;
    }

    private string[] getTagArray() => getReadOnlyTags().ToArray();

    public override string ToString() => $"{nameof(Id)}: {Id}";

    public void SetComponents(params Component[] components)
    {
        foreach (Component component in components)
        {
            m_cachedComponents[component.Data.GetType()] = component;
        }
    }

    private Component getAndCacheComponent(Type componentType)
    {
        if (!m_cachedComponents.TryGetValue(componentType, out Component component))
        {
            component = m_ecsStorage.GetComponent(this, componentType);
            if (component.Data != null)
            {
                m_cachedComponents[componentType] = component;
            }
        }

        return component;
    }

    private Component getAndRefreshCache(Type componentType)
    {
        Component component = m_ecsStorage.GetComponent(this, componentType);
        if (component.Data != null)
        {
            m_cachedComponents[componentType] = component;
        }

        return component;
    }

    private Component[] getAndCacheComponents(Type componentType)
    {
        Component[] components = m_ecsStorage.GetComponents(this, componentType);

        for (int i = 0; i < components.Length; i++)
        {
            m_cachedComponents[components[i].GetType()] = components[i];
        }

        return components;
    }

    private void setComponent<T>(T component)
    {
        checkComponentVersionConflict(component.GetType());
        m_ecsStorage.SetComponent(this, component);
    }

    //Review : Move to storage ?
    private void checkComponentVersionConflict(Type componentType)
    {
        if (m_cachedComponents.TryGetValue(componentType, out Component component))
        {
            ulong storedComponentVersion = m_ecsStorage.GetComponentVersion(this, componentType);
            if (storedComponentVersion > component.Version)
            {
                //TODO: Log component conflict issue
            }
        }
    }

    private void checkComponentsVersionConflict(object[] components)
    {
        List<Type> typesToCheck = new();
        List<Component> cachedComponents = new();
        foreach (object updatedComp in components)
        {
            if (m_cachedComponents.TryGetValue(updatedComp.GetType(), out Component cachedComponent))
            {
                typesToCheck.Add(updatedComp.GetType());
                cachedComponents.Add(cachedComponent);
            }
        }

        if (typesToCheck.Count > 0)
        {
            Dictionary<Type, ulong> dictionary = m_ecsStorage.GetComponentsVersion(this, typesToCheck)
                                                             .ToDictionary(p => p.Type, p => p.Version);

            foreach (Component cachedComponent in cachedComponents)
            {
                Type type = cachedComponent.GetType();
                if (dictionary.TryGetValue(type, out ulong currentVersion))
                {
                    if (currentVersion > cachedComponent.Version)
                    {
                        //TODO: Log component conflict issue
                    }
                }
            }
        }
    }

    protected bool Equals(IEntity other) => Id == other.Id;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Entity)obj);
    }

    public override int GetHashCode() => Id != null ? Id.GetHashCode() : 0;
}