using System;
using System.Threading;

namespace EcsSharp.Helpers;

public interface ICreateOrUpdateBuilder
{
    CreateOrUpdateBuilder Having<T>(string[]               tags                                                            = null);
    CreateOrUpdateBuilder Having<T>(Predicate<T>           componentPredicate, string[]                    tags            = null);
    CreateOrUpdateBuilder Having<T>(Func<T, IEntity, bool> entityPredicate,    string[]                    tags            = null);
    CreateOrUpdateBuilder Having(Type                      componentType,      Func<object, IEntity, bool> entityPredicate = null, string[] tags = null);
    CreateOrUpdateBuilder Having(Type[]                    componentTypes,     Predicate<IEntity>          entityPredicate = null, string[] tags = null);
    CreateOrUpdateBuilder Having(string                    id,                 string[]                    tags            = null);
    CreateOrUpdateBuilder Having(Func<IEcsRepo, IEntity>   queryFunction,      string[]                    tags            = null);
    CreateOrUpdateBuilder WhenCreated(Action<IEntity>      onCreatedAction);
    CreateOrUpdateBuilder WhenExists(Action<IEntity>       onExistsAction);
    CreateOrUpdateBuilder WhenEither(Action<IEntity>       onEitherAction);
    CreateOrUpdateBuilder CreationComponents(params object[] components);
    IEntity               Run();
    IEntity               Run(out bool created);
}

public class CreateOrUpdateBuilder : ICreateOrUpdateBuilder
{
    private readonly IEcsRepo        m_ecsRepo;
    private readonly EcsObjectPool      m_ecsObjectPool;
    private readonly object          m_sync = new();
    private          string          m_entityIdToCreate;
    private          Action<IEntity> m_onCreatedAction;
    private          Action<IEntity> m_onEitherAction;
    private          Action<IEntity> m_onExistsAction;

    private Func<IEcsRepo, IEntity> m_queryFunc;
    private string[]                m_tags;
    private object[]                m_creationComponents;

    public CreateOrUpdateBuilder(IEcsRepo ecsRepo, EcsObjectPool ecsObjectPool)
    {
        m_ecsRepo = ecsRepo;
        m_ecsObjectPool = ecsObjectPool;
    }

    public CreateOrUpdateBuilder Having<T>(string[] tags = null)
    {
        getLock();
        m_queryFunc = r => r.QuerySingle<T>(tags);
        m_tags = tags;
        return this;
    }

    public CreateOrUpdateBuilder Having<T>(Predicate<T> componentPredicate, string[] tags = null)
    {
        getLock();
        m_queryFunc = r => r.QuerySingle(componentPredicate, tags);
        m_tags = tags;
        return this;
    }

    public CreateOrUpdateBuilder Having<T>(Func<T, IEntity, bool> entityPredicate, string[] tags = null)
    {
        getLock();
        m_queryFunc = r => r.QuerySingle(entityPredicate, tags);
        m_tags = tags;
        return this;
    }

    public CreateOrUpdateBuilder Having(Type componentType, Func<object, IEntity, bool> entityPredicate = null, string[] tags = null)
    {
        getLock();
        m_queryFunc = r => r.QuerySingle(componentType, entityPredicate, tags);
        m_tags = tags;
        return this;
    }

    public CreateOrUpdateBuilder Having(Type[] componentTypes, Predicate<IEntity> entityPredicate = null, string[] tags = null)
    {
        getLock();
        m_queryFunc = r => r.QuerySingle(componentTypes, entityPredicate, tags);
        m_tags = tags;
        return this;
    }

    public CreateOrUpdateBuilder Having(string id, string[] tags = null)
    {
        getLock();
        m_queryFunc = r => r.QuerySingle(id);
        m_entityIdToCreate = id;
        m_tags = tags;
        return this;
    }

    public CreateOrUpdateBuilder Having(Func<IEcsRepo, IEntity> queryFunction, string[] tags = null)
    {
        getLock();
        m_queryFunc = queryFunction;
        m_tags = tags;
        return this;
    }

    public CreateOrUpdateBuilder CreationComponents(params object[] components)
    {
        getLock();
        m_creationComponents = components;
        return this;
    }

    public CreateOrUpdateBuilder WhenCreated(Action<IEntity> onCreatedAction)
    {
        getLock();
        m_onCreatedAction = onCreatedAction;
        return this;
    }

    public CreateOrUpdateBuilder WhenExists(Action<IEntity> onExistsAction)
    {
        getLock();
        m_onExistsAction = onExistsAction;
        return this;
    }

    public CreateOrUpdateBuilder WhenEither(Action<IEntity> onEitherAction)
    {
        getLock();
        m_onEitherAction = onEitherAction;
        return this;
    }

    public IEntity Run() => run(out bool _);

    public IEntity Run(out bool created) => run(out created);

    private IEntity run(out bool created)
    {
        getLock();
        IEntity result = null;
        bool tmpCreated = false;
        m_ecsRepo.BatchUpdate(repo => { result = invoke(repo, out tmpCreated); });
        created = tmpCreated;
        reset();
        releaseLock();
        m_ecsObjectPool.Release(this);
        return result;
    }

    private void reset()
    {
        m_onCreatedAction = null;
        m_onEitherAction = null;
        m_onExistsAction = null;
        m_entityIdToCreate = null;
        m_queryFunc = null;
        m_creationComponents = null;
    }

    private void releaseLock()
    {
        if (Monitor.IsEntered(m_sync))
        {
            Monitor.Exit(m_sync);
        }
    }

    private void getLock()
    {
        if (!Monitor.IsEntered(m_sync))
        {
            Monitor.Enter(m_sync);
        }
    }

    private IEntity invoke(IEcsRepo repo, out bool created)
    {
        if (m_queryFunc == null)
        {
            throw new EcsException("Query not set for Create or Update builder");
        }

        IEntity entity = m_queryFunc.Invoke(repo);
        created = false;
        if (entity == null)
        {
            if (m_onEitherAction != null || m_onCreatedAction != null || m_creationComponents != null)
            {
                entity = createEntity(repo);
            }

            entity = doOnNotExist(entity);
            created = true;
        }
        else
        {
            doOnExists(entity);
        }

        doOnEither(entity);
        return entity;
    }

    private IEntity createEntity(IEcsRepo repo) => repo.Create(m_creationComponents ?? Array.Empty<object>(), m_tags ?? Array.Empty<string>(), m_entityIdToCreate);

    private void doOnEither(IEntity entity)
    {
        if (m_onEitherAction != null && entity != null)
        {
            m_onEitherAction.Invoke(entity);
        }
    }

    private void doOnExists(IEntity entity)
    {
        if (m_onExistsAction != null)
        {
            m_onExistsAction.Invoke(entity);
        }
    }

    private IEntity doOnNotExist(IEntity entity)
    {
        if (m_onCreatedAction == null)
        {
            return entity;
        }

        m_onCreatedAction.Invoke(entity);
        return entity;
    }
}