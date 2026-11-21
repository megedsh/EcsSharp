using System;
using System.Collections.Generic;

using EcsSharp.Storage;

namespace EcsSharp.Helpers;

public interface IEntityBatchUpdateBuilder
{
    IEntityBatchUpdateBuilder WithSetComponent<T>(T              component);
    IEntityBatchUpdateBuilder WithSetComponents(params object[]  components);
    IEntityBatchUpdateBuilder WithSetWithVersion<T>(T            component, ulong                    version);
    IEntityBatchUpdateBuilder WithConditionalSet<T>(T            component, ComponentSetCondition<T> condition,        bool setWhenNotExist = true);
    IEntityBatchUpdateBuilder WithConditionalSet<T>(Predicate<T> condition, Func<T, T>               componentFactory, bool setWhenNotExist = true);
    IEntityBatchUpdateBuilder WithSetWhenNotEqual<T>(T           component);
    IEntityBatchUpdateBuilder WithSetWhenNotEqual<T>(Func<T, T>  componentFactory, bool setWhenNotExist = true);
    IEntityBatchUpdateBuilder WithUpdateComponent<T>(Func<T, T>  componentFactory, bool setWhenNotExist = true);
    IEntity                   Run();
}
public class EntityBatchUpdateBuilder : IEntityBatchUpdateBuilder
{
    private readonly EcsObjectPool m_ecsObjectPool;

    private readonly List<Action<IEcsStorage>> m_updates = new();
    private          Entity                    m_entity;

    internal EntityBatchUpdateBuilder(EcsObjectPool ecsObjectPool)
    {
        m_ecsObjectPool = ecsObjectPool;
    }

    internal EntityBatchUpdateBuilder ForEntity(Entity entity)
    {
        m_entity = entity;
        return this;
    }
    
    public IEntityBatchUpdateBuilder WithSetComponent<T>(T component)
    {
        m_updates.Add(storage => storage.SetComponent(m_entity, component));
        return this;
    }

    public IEntityBatchUpdateBuilder WithSetComponents(params object[] components)
    {
        m_updates.Add(storage => storage.SetComponents(m_entity, components));
        return this;
    }

    public IEntityBatchUpdateBuilder WithSetWithVersion<T>(T component, ulong version)
    {
        m_updates.Add(storage => storage.SetWithVersion(m_entity, component, version));
        return this;
    }

    public IEntityBatchUpdateBuilder WithConditionalSet<T>(T component, ComponentSetCondition<T> condition, bool setWhenNotExist = true)
    {
        m_updates.Add(storage => storage.ConditionalSet(m_entity, component, condition, setWhenNotExist));
        return this;
    }

    public IEntityBatchUpdateBuilder WithConditionalSet<T>(Predicate<T> condition, Func<T, T> componentFactory, bool setWhenNotExist = true)
    {
        m_updates.Add(storage => storage.ConditionalSet(m_entity, condition, componentFactory, setWhenNotExist));
        return this;
    }

    public IEntityBatchUpdateBuilder WithSetWhenNotEqual<T>(T component)
    {
        m_updates.Add(storage => storage.SetWhenNotEqual(m_entity, component));
        return this;
    }

    public IEntityBatchUpdateBuilder WithSetWhenNotEqual<T>(Func<T, T> componentFactory, bool setWhenNotExist = true)
    {
        m_updates.Add(storage => storage.SetWhenNotEqual(m_entity, componentFactory, setWhenNotExist));
        return this;
    }

    public IEntityBatchUpdateBuilder WithUpdateComponent<T>(Func<T, T> componentFactory, bool setWhenNotExist = true)
    {
        m_updates.Add(storage => storage.UpdateComponent(m_entity, componentFactory, setWhenNotExist));
        return this;
    }

    public IEntity Run()
    {
        m_entity.EcsStorage.EntityBatchUpdate(m_entity, m_updates);
        m_updates.Clear();
        Entity e = m_entity;
        m_ecsObjectPool.Release(this);
        return e;
    }

    public void Clear()
    {
        m_updates.Clear();
        m_entity = null;
    }
}